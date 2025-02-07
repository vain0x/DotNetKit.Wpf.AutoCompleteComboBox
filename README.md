# AutoCompleteComboBox for WPF

[![NuGet version](https://badge.fury.io/nu/DotNetKit.Wpf.AutoCompleteComboBox.svg)](https://badge.fury.io/nu/DotNetKit.Wpf.AutoCompleteComboBox)

Provides a lightweight combobox with filtering (auto-complete).

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

Then you can use `AutoCompleteComboBox`. It's like a normal `ComboBox` because of inheritance.

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
    - The path leads to a property whose getter produces a string value to identify items. For example, assume each item is an instance of `Person`, which has `Name` property, and the property path is "Name". If the user input "va", the combobox filters the items to remove ones (persons) whose `Name` don't contain "va".
    - No support for ``TextSeach.Text``.
- Don't use ``ComboBox.Items`` property directly. Use `ItemsSource` instead.
- Although the Demo project uses DataTemplate to display items, you can also use `DisplayMemberPath`.

### Configuration
This library works fine in the default setting, however, it also provides how to configure.

- Define a class derived from [DotNetKit.Windows.Controls.AutoCompleteComboBoxSetting](DotNetKit.Wpf.AutoCompleteComboBox/Windows/Controls/AutoCompleteComboBoxSetting.cs) to override some of properties.
- Set the instance to ``AutoCompleteComboBox.Setting`` property.

```xml
<dotNetKitControls:AutoCompleteComboBox
    Setting="..."
    ...
    />
```

- Or set to ``AutoCompleteComboBoxSetting.Default`` to apply to all comboboxes.

### Performance
Filtering allows you to add a lot of items to a combobox without loss of usability, however, that makes the performance poor. To get rid of the issue, we recommend you to use `VirtualizingStackPanel` as the panel.

Use `ItemsPanel` property:

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

## Internals
This library is basically a thin wrapper of the standard `ComboBox` with some behaviors.

### What Happens Under the Hood
- Finds the TextBox part (in the ComboBox) to listen to the TextChanged event
- Opens or close the dropdown whenever the text changed (and then the debounce timer fired)
    - TextBox selection is carefully saved and restored to not disturb the user
- Filters the ComboBox items based on the input
- Defines `ItemsSource` DependencyProperty that shadows the `ItemsControl.ItemsProperty` (see also [#26])
- Handles PreviewKeyDown events (`Ctrl+Space`) to open the dropdown

[#26]: https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/26
