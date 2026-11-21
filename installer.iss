; Inno Setup 6 Script for Island 2
; Compiles a single-file, per-user installer without administrative privileges.

#define MyAppName "Island 2"
#define MyAppVersion "2.1.0"
#define MyAppPublisher "Island Team"
#define MyAppURL "https://github.com/island-desktop/island"
#define MyAppExeName "Island.App.exe"
#define MyAppIcon "src\Island.App\Assets\Island.ico"

[Setup]
; Unique application GUID for per-user installation
AppId={{9F82C94A-5B61-4A56-B101-72D98F397F3A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\Island
DisableProgramGroupPage=yes
LicenseFile=ThirdPartyNotices.txt
InfoAfterFile=README.md
OutputDir=artifacts\installer
OutputBaseFilename=Island2-Setup-v{#MyAppVersion}
SetupIconFile={#MyAppIcon}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
CloseApplications=force
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start Island automatically when Windows starts"; GroupDescription: "Startup Options:"; Flags: unchecked

[Files]
Source: "artifacts\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "ThirdPartyNotices.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Cleanly remove runtime cache, logs and temporary files while preserving user settings
Type: files; Name: "{app}\*.log"
Type: files; Name: "{app}\crash.log"

[Code]
// Helper function to optionally prompt user to wipe settings directory on uninstall
function InitializeUninstall(): Boolean;
begin
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{localappdata}\Island');
    if DirExists(DataDir) then
    begin
      if MsgBox('Do you want to delete all saved settings, custom themes, and clipboard history?' + #13#10 + #13#10 + '(' + DataDir + ')', mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
