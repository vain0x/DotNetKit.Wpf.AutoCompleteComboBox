# AutoCompleteComboBox for WPF

*The repository name has been changed from `DotNetKit.Wpf.AutoCompleteComboBox`.*

[![NuGet version](https://badge.fury.io/nu/DotNetKit.Wpf.AutoCompleteComboBox.svg)](https://badge.fury.io/nu/DotNetKit.Wpf.AutoCompleteComboBox)

A lightweight ComboBox control that supports filtering (auto completion).

## Screenshot
![](documents/images/screenshot.gif)

## Usage
[Install via NuGet](https://www.nuget.org/packages/DotNetKit.Wpf.AutoCompleteComboBox).

Declare XML namespace.

```xml
<Window
    ...
    xmlns:dotNetKitControls="clr-namespace:DotNetKit.Windows.Controls;assembly=DotNetKit.Wpf.AutoCompleteComboBox"
    ... >
```

You can then use `AutoCompleteComboBox` just like a normal `ComboBox`, since it inherits from it.

```xml
<dotNetKitControls:AutoCompleteComboBox
    SelectedValuePath="Id"
    TextSearch.TextPath="Name"
    ItemsSource="{Binding Items}"
    SelectedItem="{Binding SelectedItem}"
    SelectedValue="{Binding SelectedValue}"
    />
```

Note that:

- Set a property path to ``TextSearch.TextPath`` property.
    - The path should point to a property that returns a string used to identify items. For example, if each item is a `Person` object with a `Name` property, and ``TextSearch.TextPath`` is set to `"Name"`, typing `"va"` will filter out all items whose `Name` doesn't contain `"va"`.
    - No support for ``TextSearch.Text``.
- Don't use ``ComboBox.Items`` property directly. Use `ItemsSource` instead.
- Although the Demo project uses DataTemplate to display items, you can also use `DisplayMemberPath`.

### Configuration
The default settings should work well for most cases, but you can customize the behavior if needed.

- Define a class derived from [DotNetKit.Windows.Controls.AutoCompleteComboBoxSetting](AutoCompleteComboBoxWpf/Windows/Controls/AutoCompleteComboBoxSetting.cs) to override some of its properties.
- Set the instance to ``AutoCompleteComboBox.Setting`` property.

```xml
<dotNetKitControls:AutoCompleteComboBox
    Setting="..."
    ...
    />
```

- Or set ``AutoCompleteComboBoxSetting.Default`` to apply to all comboboxes.

### Performance
Filtering allows you to add many items without losing usability, but it can affect performance. To improve this, we recommend using `VirtualizingStackPanel` as the items panel.

Use the `ItemsPanel` property:

```csharp
<dotNetKitControls:AutoCompleteComboBox ...>
    <dotNetKitControls:AutoCompleteComboBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </dotNetKitControls:AutoCompleteComboBox.ItemsPanel>
</dotNetKitControls:AutoCompleteComboBox>
```

or declare a style in resources as the Demo app does.

See also [WPF: Using a VirtualizingStackPanel to Improve ComboBox Performance](http://vbcity.com/blogs/xtab/archive/2009/12/15/wpf-using-a-virtualizingstackpanel-to-improve-combobox-performance.aspx) for more detailed explanation.

## Known Issues
### Shared ItemsSource
- Multiple ComboBoxes can affect each other if they share the same ItemsSource instance.
- Workaround: Use distinct ItemsSource instance for each AutoCompleteComboBox. For example, wrap it with a ReadOnlyCollection.

### Filter Conflict
- Changing `AutoCompleteComboBox.Filter` in user code conflicts with the control's internal behavior.
- Workaround: Avoid changing Filter in user code. Filter ItemsSource instead.
- There seems to be no reliable way to merge CollectionView filters. Please let me know if you have a solution.

### Background Not Applied
`ComboBox` doesn't appear to support the `Background` property. No easy fix is known.

## Internals
This library is essentially a thin wrapper around the standard `ComboBox` with additional behaviors.

### What Happens Under the Hood
- Sets `IsEditable` to true to allow text input
- Finds the TextBox part (in the ComboBox) to listen to the TextChanged event
- Opens or closes the dropdown when the text changes (after the debounce timer fires)
    - TextBox selection is carefully saved and restored to not disturb the user
- Filters the ComboBox items based on the input
- Handles PreviewKeyDown events (`Ctrl+Space`) to open the dropdown
