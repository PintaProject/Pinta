#define ProductName "Pinta"
#define ProductVersion "3.0.1"

[Setup]
; Adds option to skip creating start menu entries
AllowNoIcons=yes
AppId=C0BCDEDA-62E7-4A43-8435-58323E096912
AppName={#ProductName}
AppPublisher=Pinta Community
AppPublisherURL=https://www.pinta-project.com/
AppVerName={#ProductName} {#ProductVersion}
AppVersion={#ProductVersion}
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
Compression=lzma2
DefaultDirName={autopf}\{#ProductName}
DefaultGroupName={#ProductName}
LicenseFile=installer\windows\license.rtf
OutputBaseFilename={#ProductName}
OutputDir=installer\windows
SetupIconFile=installer\windows\Pinta.ico
SolidCompression=yes
SourceDir=..\..\
UninstallDisplayIcon={app}\bin\{#ProductName}.exe
WizardSmallImageFile=installer\windows\logo.bmp
WizardStyle=modern

[Icons]
Name: "{group}\{#ProductName}"; Filename: "{app}\bin\{#ProductName}.exe"

[Files]
Source: "release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Run]
Filename: "{app}\bin\Pinta.exe"; Flags: nowait postinstall skipifsilent; Description: "{cm:LaunchProgram,{#ProductName}}"
