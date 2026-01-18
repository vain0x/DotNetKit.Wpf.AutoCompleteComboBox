# CHANGELOG

(On NuGet: [DotNetKit.Wpf.AutoCompleteComboBox versions](https://www.nuget.org/packages/DotNetKit.Wpf.AutoCompleteComboBox#versions-tab).)

----

## 2.0.2 - 2026-01-19

- Fix [List does not open when IsEditable is set to False · Issue #40](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/issues/40)

## 2.0.1 - 2025-11-29

- Fix [Dropdown only opening on second click #39](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/issues/39)

## 2.0.0 - 2025-10-28

- Tracking PR: [v2 plan](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/38)
- *BREAKING CHANGE*
    - Upgrade to .NET 8 (from .NET 6)
    - [Fix filter affects bound source collection by adding a dedicated FilterCollection by Fruchtzwerg94 · Pull Request #26](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/26) is reverted
- Fix
    - [Properly synchronize the current selected item - Improve and fix #28 by kvpt · Pull Request #29 · vain0x/DotNetKit.Wpf.AutoCompleteComboBox](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/29)
- Filter changes are applied when dropdown opened
- *Internals* (backward-compatible)
    - Project/solution files are recreated (assembly name and namespaces are still unchanged)
- *Announcement*: The repository will soon be renamed to a shorter name.

## 1.6.0 - 2023-04-22

- [Try to keep SelectedItem when changing ItemsSource by tmijail · Pull Request #28](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/28)

## 1.5.0 - 2023-04-02

- Upgrade to .NET Framework 4.6.2 and .NET 6
- [Fix filter affects bound source collection by adding a dedicated FilterCollection by Fruchtzwerg94 · Pull Request #26](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/26)

## 1.4.0 - 2021-10-17

- [Don't show empty list · Issue #20](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/issues/20)

## 1.3.1 - 2020-03-18

- [Fix filter regression by kvpt · Pull Request #12](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/12)

## 1.3.0

- [Upgrade to DotNetCore 3.1 by kvpt · Pull Request #9](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/9)
- [Combine combobox filter with items/CollectionView filter by kvpt · Pull Request #10](https://github.com/vain0x/DotNetKit.Wpf.AutoCompleteComboBox/pull/10)

## 1.2.0

...
