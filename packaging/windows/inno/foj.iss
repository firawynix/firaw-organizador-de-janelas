#define AppVersion "0.3.2"
#define AppExe "FirawynixWindowManager.exe"

[Setup]
AppId={{625CF0E9-A58A-4952-AD93-2D5A81CFD782}
AppName=Firaw - Organizador de Janelas
AppVersion={#AppVersion}
AppVerName=Firaw - Organizador de Janelas {#AppVersion}
AppPublisher=Firawynix
AppPublisherURL=https://lab.firawynix.com.br/foj/
AppSupportURL=https://github.com/firawynix/firaw-organizador-de-janelas/issues
AppUpdatesURL=https://github.com/firawynix/firaw-organizador-de-janelas/releases
DefaultDirName={localappdata}\Programs\Firawynix\Organizador de Janelas
DefaultGroupName=Firawynix
DisableProgramGroupPage=yes
OutputDir=..\..\..\release-center
OutputBaseFilename=Firaw-Organizador-de-Janelas-Setup-x64-{#AppVersion}
SetupIconFile=..\..\..\assets\celtic-knot-transparent.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#AppVersion}.0
VersionInfoCompany=Firawynix
VersionInfoDescription=Instalador do Firaw - Organizador de Janelas
VersionInfoProductName=Firaw - Organizador de Janelas

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "..\..\..\release\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\..\release\*.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Firaw - Organizador de Janelas"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\Firaw - Organizador de Janelas"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Abrir o Firaw agora"; Flags: nowait postinstall skipifsilent
