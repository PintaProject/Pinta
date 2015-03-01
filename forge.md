- BuildVersion: 1.7.0.{BuildCounter}

## Configuration: Windows

### NuGet Restore

- NuGetRestore
  - Solution File: Pinta.sln

### Update AssemblyVersions

- AssemblyVersion
  - Version: {BuildVersion}
  - Files: Pinta/Properties/AssemblyInfo.cs;Pinta.Core/Properties/AssemblyInfo.cs;Pinta.Effects/Properties/AssemblyInfo.cs;Pinta.Gui.Widgets/Properties/AssemblyInfo.cs;Pinta.Resources/Properties/AssemblyInfo.cs;Pinta.Tools/Properties/AssemblyInfo.cs

### Update About Page

- RegexReplace
  - Regex: ApplicationVersion\s*=\s*".*"
  - Replacement: ApplicationVersion = "{BuildVersion}"
  - Files: Pinta.Core/PintaCore.cs

### Build Solution

- MSBuild
  - Build File: Pinta.sln
  - Configuration: Debug
  - Platform: AnyCPU

### Build Language Files

- msgfmt
  - Files: po/*.po
  - Output: bin/locale/{code}/LC_MESSAGES/pinta.mo

### Version Installer

- RegexReplace
  - Regex: ProductVersion\s*=\s*".*"
  - Replacement: ProductVersion = "{BuildVersion}"
  - Files: Wix/common.wxi

### Version Installer Text

- RegexReplace
  - Regex: ProductVersionText\s*=\s*".*"
  - Replacement: ProductVersionText = "DEV BUILD - {BuildVersion}"
  - Files: Wix/common.wxi

### Download GTK-Sharp for installer

- Download
  - Url: http://download.xamarin.com/GTKforWindows/Windows/gtk-sharp-2.12.22.msi
  - Destination: Wix/gtk-sharp-2.12.22.msi
  - CanCache: true

### Build Installer

- MSBuild
  - Build File: Wix/PintaWix.sln
  - Configuration: Debug

### Rename Installer

- Rename
  - Source: Wix/bin/Debug/pinta.exe
  - Destination: Wix/bin/Debug/pinta-dev-{BuildVersion}.exe

### Collect Artifacts

- Artifacts
  - Windows Installer: Wix/bin/Debug/pinta-dev-{BuildVersion}.exe

## Configuration: Zip

### NuGet Restore

- NuGetRestore
  - Solution File: Pinta.sln

### Update AssemblyVersions

- AssemblyVersion
  - Version: {BuildVersion}
  - Files: Pinta/Properties/AssemblyInfo.cs;Pinta.Core/Properties/AssemblyInfo.cs;Pinta.Effects/Properties/AssemblyInfo.cs;Pinta.Gui.Widgets/Properties/AssemblyInfo.cs;Pinta.Resources/Properties/AssemblyInfo.cs;Pinta.Tools/Properties/AssemblyInfo.cs

### Update About Page

- RegexReplace
  - Regex: ApplicationVersion\s*=\s*".*"
  - Replacement: ApplicationVersion = "{BuildVersion}"
  - Files: Pinta.Core/PintaCore.cs

### Build Solution

- xbuild
  - Build File: Pinta.sln
  - Configuration: Debug
  - Platform: AnyCPU

### Build Language Files

- msgfmt
  - Files: po/*.po
  - Output: bin/locale/{code}/LC_MESSAGES/pinta.mo

### Zip output directory

- Zip
  - Directory: bin
  - Output: pinta-dev-{BuildVersion}.zip

### Collect Artifacts

- Artifacts
  - Zip: pinta-dev-{BuildVersion}.zip  
