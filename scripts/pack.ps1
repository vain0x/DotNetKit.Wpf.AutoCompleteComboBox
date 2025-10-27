#!/bin/pwsh
# USAGE: ./scripts/pack

# Cheatsheet for deployment:
#   - bump up version number in .csproj
#   - write CHANGELOG
#   - commit
#   - create Git tag
#   - build and pack
#   - publish to NuGet
#   - write release note there (copy from CHANGELOG)

dotnet pack AutoCompleteComboBoxWpf -c Release
