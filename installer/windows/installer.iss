#define ProductName "Pinta"
#define ProductVersion "1.8"

[Setup]
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
UninstallDisplayIcon={app}\{#ProductName}.exe
WizardSmallImageFile=installer\windows\logo.bmp
WizardStyle=modern

[Icons]
Name: "{group}\{#ProductName}"; Filename: "{app}\{#ProductName}.exe"

[Files]
Source: "bin\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
; Include the GTK distribution (which GtkSharp pulls in), but skip the zip file itself along with the unused gtkmm libraries.
Source: "{#GetEnv('LOCALAPPDATA')}\Gtk\3.24.24\*"; Excludes: "gtk.zip,*mm-*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Run]
Filename: "{app}\Pinta.exe"; Flags: nowait postinstall; Description: "{cm:LaunchProgram,{#ProductName}}"
