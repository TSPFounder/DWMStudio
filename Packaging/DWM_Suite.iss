; DWM Suite -- DWM Studio and the DWM_Dev runtime in one installer.
;
; WHY BOTH IN ONE PACKAGE. The Studio's UE Simulation stage launches the game, so a
; Studio installed on its own has a button that cannot work. Shipping them together
; means the stage finds the runtime by construction rather than by the person who
; installed it happening to put the game somewhere the Studio guesses.
;
; THE LAYOUT IS LOAD-BEARING. ToolWorkspaceViewModel.PackagedBuildCandidates looks for
; ..\Game\DWM_Dev.exe relative to its own executable, so Studio\ and Game\ must stay
; siblings under one root. Flattening them, or renaming either folder, silently breaks
; the Run button -- it falls through to the developer path, which will not exist here.

#define SuiteName      "Dream World Maker"
#define SuiteVersion   "0.2"
#define Publisher      "TSP"

#define StudioDir      "C:\DreamWorldMaker\Apps\DWMStudio"
#define GameDir        "C:\DreamWorldMaker\Builds\Windows"

#define StudioExe      "DWMStudio.exe"
#define GameExe        "DWM_Dev.exe"

[Setup]
; Identifies the suite for upgrade and uninstall. Never change it -- a new AppId
; makes Windows treat the next version as a separate product installed alongside.
AppId={{3A3A88CC-5153-427E-BF4F-337DBDE24513}
AppName={#SuiteName}
AppVersion={#SuiteVersion}
AppPublisher={#Publisher}
DefaultDirName={autopf}\{#SuiteName}
DefaultGroupName={#SuiteName}
OutputDir=C:\DreamWorldMaker\Installers
OutputBaseFilename=DWM_Suite_{#SuiteVersion}
Compression=lzma2/max
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; The Studio's own icon, so the installer and the Add/Remove entry carry it too.
SetupIconFile=..\DWMStudio\Resources\DwmStudio.ico
UninstallDisplayIcon={app}\Studio\{#StudioExe}

; ~12.2 GB total, and GitHub refuses a release asset over 2 GB. Slices at 1.9 GB leave
; room under that ceiling; more, smaller slices are fine, a single file is not.
DiskSpanning=yes
DiskSliceSize=1900000000

; The runtime alone unpacks to about 12 GB, and Windows needs room to write it.
ExtraDiskSpaceRequired=0
DirExistsWarning=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut for DWM Studio"; GroupDescription: "Shortcuts:"
Name: "desktopgame"; Description: "Create a desktop shortcut for the &game"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
; Studio first: it is small, so a failure here surfaces in seconds rather than after
; twelve gigabytes have been written.
Source: "{#StudioDir}\*"; DestDir: "{app}\Studio"; Flags: ignoreversion recursesubdirs createallsubdirs

; The runtime. Saved\ is excluded for the same reason as in DWM_Dev's own installer:
; it holds crash dumps, minidumps and every debug log the build has written, none of
; which belongs in someone else's install, and the game rewrites it while the compiler
; is reading it.
Source: "{#GameDir}\*"; DestDir: "{app}\Game"; Excludes: "*\Saved\*,*\Saved"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\DWM Studio"; Filename: "{app}\Studio\{#StudioExe}"; WorkingDir: "{app}\Studio"
Name: "{group}\Dream World Maker"; Filename: "{app}\Game\{#GameExe}"; WorkingDir: "{app}\Game"
Name: "{group}\Uninstall {#SuiteName}"; Filename: "{uninstallexe}"

Name: "{autodesktop}\DWM Studio"; Filename: "{app}\Studio\{#StudioExe}"; WorkingDir: "{app}\Studio"; Tasks: desktopicon
Name: "{autodesktop}\Dream World Maker"; Filename: "{app}\Game\{#GameExe}"; WorkingDir: "{app}\Game"; Tasks: desktopgame

[Run]
Filename: "{app}\Studio\{#StudioExe}"; WorkingDir: "{app}\Studio"; Description: "Launch DWM Studio"; Flags: nowait postinstall skipifsilent
