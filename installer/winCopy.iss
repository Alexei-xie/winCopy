#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{EF9D646C-FE56-46C4-9F80-3816BEA6486B}
AppName=winCopy
AppVersion={#AppVersion}
AppPublisher=Alexei-xie
AppPublisherURL=https://github.com/Alexei-xie/winCopy
AppSupportURL=https://github.com/Alexei-xie/winCopy/issues
AppUpdatesURL=https://github.com/Alexei-xie/winCopy/releases
DefaultDirName={localappdata}\Programs\winCopy
DefaultGroupName=winCopy
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
MinVersion=10.0
OutputDir=..\artifacts
OutputBaseFilename=winCopy-{#AppVersion}-setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\winCopy.ico
LicenseFile=..\LICENSE
UninstallDisplayIcon={app}\winCopy.exe
AppMutex=Local\winCopy.Desktop
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\dist\winCopy.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\winCopy"; Filename: "{app}\winCopy.exe"
Name: "{autodesktop}\winCopy"; Filename: "{app}\winCopy.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "winCopy"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\winCopy.exe"; Description: "Launch winCopy"; Flags: nowait postinstall skipifsilent
