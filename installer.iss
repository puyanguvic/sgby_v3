[Setup]
AppId={{9B8A0B9D-B9C0-44A2-9F4C-8EF0E4D58F6B}
AppName=iBaye
AppVersion=1.0.0
AppPublisher=iBaye
DefaultDirName={localappdata}\Programs\iBaye
DefaultGroupName=iBaye
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=iBaye-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\iBaye"; Filename: "{app}\baye.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\iBaye"; Filename: "{app}\baye.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\baye.exe"; Description: "启动 iBaye"; Flags: nowait postinstall skipifsilent

