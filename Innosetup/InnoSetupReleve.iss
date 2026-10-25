;Script pour Inno Setup Compiler 7.1.0

#define MyAppName "Relève"
#define MyAppVersion "0.4"
#define MyAppPublisher "Marskilla"
#define MyAppExeName "Releve.Game.exe"
#define DoubleAmp(Value) StringChange(Value, "&", "&&")
#define EscapeConstArgument(Value) StringChange(StringChange(StringChange(Value, "%", "%25"), ",", "%2c"), "}", "%7d")

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{59B67DE1-3F34-43DB-AB29-55193994E2E3}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={cm:NameAndVersion,{#EscapeConstArgument(MyAppName)},{#EscapeConstArgument(MyAppVersion)}}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
VersionInfoVersion={#MyAppVersion}.0.0
UninstallDisplayIcon={app}\{#MyAppExeName}
; "ArchitecturesAllowed=x64compatible" specifies that Setup cannot run on anything but x64 and Windows 11 on Arm.
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" requests that the install be done in "64-bit mode" on x64 or Windows 11 on Arm.
; This means it should use the native 64-bit Program Files directory and the 64-bit view of the registry.
ArchitecturesInstallIn64BitMode=x64compatible
; Uncomment the following line to use a 64-bit installer.
;SetupArchitecture=x64
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users).
PrivilegesRequired=lowest
OutputBaseFilename=ReleveInstall
OutputDir={#SourcePath}\publish
DiskSpanning=yes
Compression=lzma2/ultra64
SolidCompression=yes
LZMADictionarySize=131072
WizardStyle=modern dynamic polar
SetupIconFile=..\src\Releve.Game\assets\icons\releve.ico

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Exécutable principal
Source: "..\src\Releve.Game\bin\Release\net8.0\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; Tous les fichiers publiés, en excluant uniquement les fichiers de débogage et les fichiers auxiliaires inutiles
Source: "..\src\Releve.Game\bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Excludes: "*.pdb,*.gitkeep,assets\fonts\*-LICENSE.txt,assets\fonts\*-README.md"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files.

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#DoubleAmp(MyAppName)}}"; Flags: nowait postinstall skipifsilent

