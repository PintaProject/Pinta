#define ProductName "Pinta"
#define ProductVersion "3.2"

; The architecture can be configured on the command line to build the arm64 or x64 installer
#ifndef ProductArch
  #define ProductArch "x64os"
#endif

[Setup]
; Adds option to skip creating start menu entries
AllowNoIcons=yes
AppId=C0BCDEDA-62E7-4A43-8435-58323E096912
AppName={#ProductName}
AppPublisher=Pinta Community
AppPublisherURL=https://www.pinta-project.com/
AppVerName={#ProductName} {#ProductVersion}
AppVersion={#ProductVersion}
ArchitecturesAllowed={#ProductArch}
ArchitecturesInstallIn64BitMode={#ProductArch}
Compression=lzma2
DefaultDirName={autopf}\{#ProductName}
DefaultGroupName={#ProductName}
LicenseFile=installer\windows\license.rtf
OutputBaseFilename={#ProductName}
OutputDir=installer\windows
; Allow installing for all users or only the current user
PrivilegesRequiredOverridesAllowed=dialog
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
