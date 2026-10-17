; Inno Setup script for Thermo — PowerToys Command Palette Extension
#define MyAppName "Thermo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "lupixele"
#define MyAppURL "https://github.com/lupixele/Thermo"
#define MyAppCLSID "{0E8AB9C8-B702-4684-AFF0-80165DD8260A}"

[Setup]
AppId={{0E8AB9C8-B702-4684-AFF0-80165DD8260A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
OutputDir=installer
OutputBaseFilename={#MyAppName}-Setup-{#MyAppVersion}-x64
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "Thermo\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Install-ThermoHelper.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "Uninstall-ThermoHelper.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Run]
; 1. Register loose MSIX package into current user profile
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Add-AppxPackage -Register '{app}\AppxManifest.xml' -ForceApplicationShutdown"""; \
  Flags: runhidden runasoriginaluser

; 2. Register elevated background task helper for CPU MSR reading
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Install-ThermoHelper.ps1"""; \
  Flags: runhidden

[UninstallRun]
; Stop and unregister scheduled task
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\Uninstall-ThermoHelper.ps1"""; \
  RunOnceId: "RemoveThermoHelper"; Flags: runhidden

; Remove Appx package
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage -AllUsers *ThermoExtension* | Remove-AppxPackage -AllUsers"""; \
  RunOnceId: "RemoveAppxPackage"; Flags: runhidden
