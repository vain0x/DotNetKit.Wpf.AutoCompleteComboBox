.nuget\nuget.exe pack DotNetKit.Wpf.AutoCompleteComboBox\DotNetKit.Wpf.AutoCompleteComboBox.csproj.nuspec
.nuget\nuget.exe push *.nupkg -Source https://www.nuget.org/api/v2/package
del /Q *.nupkg
