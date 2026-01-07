; DGScope Profile Manager Installer (NSIS)
; Detects and installs .NET Framework 4.7.2 if needed
; Includes Profile Manager + optional DGScope setup wizard

!include "MUI2.nsh"
!include "x64.nsh"
!include "WinVer.nsh"

!define APP_NAME "DGScope Profile Manager"
!define APP_VERSION "1.0.0"
!define APP_PUBLISHER "DGScope Profile Manager"
!define INSTALLER_NAME "DGScopeProfileManager-Setup.exe"
!define UNINSTALLER_NAME "Uninstall.exe"
!define INSTALL_DIR "$PROGRAMFILES\DGScopeProfileManager"
!define REG_PATH "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScopeProfileManager"
!define NET472_URL "https://download.microsoft.com/download/0/5/C/05C91A2B-8B22-40FF-B3A8-413ECF54DD57/NDP472-KB4054530-x86-x64-AllOS-ENU.exe"
!define NET472_FILENAME "NDP472-KB4054530-x86-x64-AllOS-ENU.exe"

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

; Installer Settings
Name "${APP_NAME} ${APP_VERSION}"
OutFile "..\${INSTALLER_NAME}"
InstallDir "${INSTALL_DIR}"
ShowInstDetails show
ShowUnInstDetails show

; Version Info
VIProductVersion "${APP_VERSION}.0"
VIAddVersionKey /LANG=1033 "ProductName" "${APP_NAME}"
VIAddVersionKey /LANG=1033 "CompanyName" "${APP_PUBLISHER}"
VIAddVersionKey /LANG=1033 "FileVersion" "${APP_VERSION}"

; Request admin rights
RequestExecutionLevel admin

; Variables for maintenance mode
Var IsUpdate
Var DGScopeInstalled
Var ProfileManagerInstalled

; Init function to check for existing installation
Function .onInit
  ; Check if already installed
  ReadRegStr $0 HKLM "${REG_PATH}" "UninstallString"
  StrCmp $0 "" NewInstall
  
  ; Existing installation found - offer to modify/repair
  MessageBox MB_YESNO|MB_ICONQUESTION \
    "DGScope Profile Manager is already installed.$\n$\nDo you want to modify or repair the installation?" \
    IDYES ModifyInstall
  Abort
  
  ModifyInstall:
    StrCpy $IsUpdate "1"
    
    ; Check which components are currently installed
    ReadRegStr $0 HKLM "${REG_PATH}" "ProfileManagerInstalled"
    StrCpy $ProfileManagerInstalled $0
    
    ReadRegStr $0 HKLM "${REG_PATH}" "DGScopeInstalled"
    StrCpy $DGScopeInstalled $0
    
    Return
  
  NewInstall:
    StrCpy $IsUpdate "0"
    StrCpy $ProfileManagerInstalled "0"
    StrCpy $DGScopeInstalled "0"
FunctionEnd

; Check prerequisites and install - CORE COMPONENT
SectionGroup /e "DGScope Profile Manager" SecGroupProfileManager

Section "!Profile Manager (Required)" SecProfileManager
  SectionIn RO  ; Required section
  SetOutPath "${INSTALL_DIR}"
  
  ; Check and install .NET Framework 4.7.2 if needed
  Call CheckAndInstallDotNET472
  
  ; Extract Profile Manager files
  ; These are relative to the Release build output
  File "..\src\DGScopeProfileManager\bin\Release\net10.0-windows\DGScopeProfileManager.exe"
  File "..\src\DGScopeProfileManager\bin\Release\net10.0-windows\*.dll"
  
  ; Create Start Menu shortcut
  CreateDirectory "$SMPROGRAMS\DGScope Profile Manager"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\DGScope Profile Manager.lnk" "${INSTALL_DIR}\DGScopeProfileManager.exe" "" "${INSTALL_DIR}\DGScopeProfileManager.exe" 0
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\Uninstall.lnk" "${INSTALL_DIR}\${UNINSTALLER_NAME}"
  
  ; Create Desktop shortcut
  CreateShortCut "$DESKTOP\DGScope Profile Manager.lnk" "${INSTALL_DIR}\DGScopeProfileManager.exe"
  
  ; Write registry for uninstall
  WriteRegStr HKLM "${REG_PATH}" "DisplayName" "${APP_NAME} ${APP_VERSION}"
  WriteRegStr HKLM "${REG_PATH}" "UninstallString" "${INSTALL_DIR}\${UNINSTALLER_NAME}"
  WriteRegStr HKLM "${REG_PATH}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKLM "${REG_PATH}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "${REG_PATH}" "ProfileManagerInstalled" "1"
  WriteRegDWord HKLM "${REG_PATH}" "NoModify" 0
  WriteRegDWord HKLM "${REG_PATH}" "NoRepair" 0
  
  ; Write uninstaller
  WriteUninstaller "${INSTALL_DIR}\${UNINSTALLER_NAME}"
  
  DetailPrint "Profile Manager installation complete!"
  StrCpy $ProfileManagerInstalled "1"
SectionEnd

SectionGroupEnd

; Optional section: Install DGScope
SectionGroup "DGScope (Optional)" SecGroupDGScope

Section "DGScope Application" SecDGScope
  DetailPrint "Setting up DGScope..."
  
  ; Directory structure: DGScope/scope, DGScope/profiles
  StrCpy $2 "$DOCUMENTS\DGScope"
  CreateDirectory "$2"
  CreateDirectory "$2\profiles"
  
  ; Try to download pre-built release first (avoids need for Visual Studio)
  DetailPrint "Downloading pre-built DGScope release from GitHub..."
  SetOutPath "$TEMP"
  
  ; Download the latest release (check k2fc/scope releases for actual URL)
  NSISdl::download "https://github.com/k2fc/scope/releases/latest/download/DGScope.exe" "$TEMP\DGScope.exe"
  Pop $0
  
  ${If} $0 == "success"
    DetailPrint "Download successful! Installing pre-built DGScope..."
    
    ; Create scope directory and copy executable
    CreateDirectory "$2\scope"
    CopyFiles "$TEMP\DGScope.exe" "$2\scope\DGScope.exe"
    Delete "$TEMP\DGScope.exe"
    
    StrCpy $4 "$2\scope\DGScope.exe"
    
    ; Create shortcuts
    CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\DGScope.lnk" "$4"
    CreateShortCut "$DESKTOP\DGScope.lnk" "$4"
    
    ; Register installation
    WriteRegStr HKLM "${REG_PATH}" "DGScopeInstalled" "1"
    WriteRegStr HKLM "${REG_PATH}" "DGScopePath" "$2\scope"
    WriteRegStr HKLM "${REG_PATH}" "DGScopeExePath" "$4"
    StrCpy $DGScopeInstalled "1"
    
    MessageBox MB_OK|MB_ICONINFORMATION "DGScope Installed Successfully!$\n$\nLocation: $4"
    Goto DGScopeComplete
  ${EndIf}
  
  ; Pre-built download failed, offer to build from source
  DetailPrint "Pre-built download failed. Offering source build option..."
  
  MessageBox MB_YESNO|MB_ICONQUESTION \
    "Could not download pre-built DGScope.$\n$\nWould you like to build from source instead?$\n$\n(Requires Visual Studio 2022 or Build Tools)" \
    IDYES BuildFromSource
  
  DetailPrint "User declined source build."
  MessageBox MB_OK|MB_ICONINFORMATION "DGScope installation skipped.$\n$\nYou can download DGScope manually from:$\nhttps://github.com/k2fc/scope/releases"
  Goto SkipDGScope
  
  BuildFromSource:
  DetailPrint "User chose to build from source..."
  
  ; Check for .NET Framework 4.7.2 Developer Pack
  DetailPrint "Checking for .NET Framework 4.7.2 Developer Pack..."
  ReadRegDWord $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  
  ${If} $0 < 461808
    MessageBox MB_YESNO|MB_ICONQUESTION \
      ".NET Framework 4.7.2 Developer Pack is required to build DGScope.$\n$\nInstall .NET Framework 4.7.2 Developer Pack now?" \
      IDYES InstallDevPack
    DetailPrint "DGScope installation cancelled - Developer Pack required."
    Goto SkipDGScope
    
    InstallDevPack:
      DetailPrint "Downloading .NET Framework 4.7.2 Developer Pack..."
      SetOutPath "$TEMP"
      NSISdl::download "https://download.microsoft.com/download/0/5/C/05C91A2B-8B22-40FF-B3A8-413ECF54DD57/NDP472-DevPack-KB4054530-ENU.exe" "$TEMP\NDP472-DevPack.exe"
      Pop $0
      ${If} $0 == "success"
        DetailPrint "Installing .NET Framework 4.7.2 Developer Pack..."
        nsExec::ExecToStack "$TEMP\NDP472-DevPack.exe /q /norestart"
        Delete "$TEMP\NDP472-DevPack.exe"
      ${Else}
        MessageBox MB_ICONEXCLAMATION "Failed to download Developer Pack. Please install manually from:$\nhttps://dotnet.microsoft.com/download/dotnet-framework/net472"
        Goto SkipDGScope
      ${EndIf}
  ${EndIf}
  
  ; Check for MSBuild (comes with Visual Studio or Build Tools)
  DetailPrint "Checking for MSBuild..."
  nsExec::ExecToStack 'where msbuild'
  Pop $0
  
  ${If} $0 != 0
    MessageBox MB_OK|MB_ICONEXCLAMATION \
      "MSBuild not found.$\n$\nDGScope requires Visual Studio 2022 or Build Tools.$\n$\nPlease install from:$\nhttps://visualstudio.microsoft.com/downloads/$\n$\nThen re-run this installer to build DGScope."
    Goto SkipDGScope
  ${EndIf}
  
  ; Check for Git
  DetailPrint "Checking for Git..."
  nsExec::ExecToStack 'git --version'
  Pop $0
  
  ${If} $0 != 0
    MessageBox MB_YESNO|MB_ICONQUESTION \
      "Git is required to download DGScope source.$\n$\nInstall Git for Windows now?" \
      IDYES InstallGit
    DetailPrint "DGScope installation cancelled - Git required."
    Goto SkipDGScope
    
    InstallGit:
      DetailPrint "Downloading Git for Windows..."
      SetOutPath "$TEMP"
      NSISdl::download "https://github.com/git-for-windows/git/releases/download/v2.42.0.windows.2/Git-2.42.0.2-64-bit.exe" "$TEMP\Git-Installer.exe"
      Pop $0
      ${If} $0 == "success"
        DetailPrint "Installing Git..."
        nsExec::ExecToStack "$TEMP\Git-Installer.exe /SILENT"
        Delete "$TEMP\Git-Installer.exe"
      ${Else}
        MessageBox MB_ICONEXCLAMATION "Failed to download Git. Please install manually."
        Goto SkipDGScope
      ${EndIf}
  ${EndIf}
  
  ; Clone and build DGScope
  DetailPrint "Setting up DGScope directory structure..."
  
  ; Directory structure: DGScope/scope, DGScope/profiles, DGScope/DGScopeProfileManager
  StrCpy $2 "$DOCUMENTS\DGScope"
  
  DetailPrint "Checking for existing DGScope installation at $2..."
  
  ; Check if already cloned
  ${If} ${FileExists} "$2\scope\.git\*.*"
    DetailPrint "DGScope repository already exists, updating..."
    SetOutPath "$2\scope"
    nsExec::ExecToLog 'cmd /c "cd /d "$2\scope" && git pull"'
    Pop $0
    ${If} $0 != 0
      DetailPrint "Git pull failed, using existing repository"
    ${EndIf}
  ${Else}
    DetailPrint "Cloning DGScope source code from k2fc/scope..."
    
    CreateDirectory "$2"
    SetOutPath "$2"
    
    ; Clone the actual DGScope application source
    nsExec::ExecToLog 'git clone https://github.com/k2fc/scope.git "$2\scope"'
    Pop $0
    ${If} $0 != 0
      DetailPrint "Git clone failed with exit code $0"
      MessageBox MB_OK|MB_ICONEXCLAMATION "Failed to clone DGScope repository.$\n$\nError code: $0$\n$\nPlease check your internet connection and ensure Git is properly installed."
      Goto SkipDGScope
    ${EndIf}
  ${EndIf}
  
  ; Create profiles directory if it doesn't exist
  CreateDirectory "$2\profiles"
  
  DetailPrint "Locating solution file..."
  
  ; Find the solution file in the k2fc/scope repo
  ${If} ${FileExists} "$2\scope\scope.sln"
    StrCpy $3 "$2\scope\scope.sln"
  ${ElseIf} ${FileExists} "$2\scope\DGScope.csproj"
    StrCpy $3 "$2\scope\DGScope.csproj"
  ${ElseIf} ${FileExists} "$2\scope\DGScope.csproj"
    StrCpy $3 "$2\scope\DGScope.csproj"
  ${Else}
    DetailPrint "Could not locate solution or project file"
    MessageBox MB_OK|MB_ICONEXCLAMATION "Failed to locate DGScope solution file.$\n$\nRepository: https://github.com/k2fc/scope$\n$\nYou can try building manually from:$\n$2\scope"
    Goto SkipDGScope
  ${EndIf}
  
  DetailPrint "Found project: $3"
  DetailPrint "Restoring NuGet packages..."
  
  ; Use nuget.exe restore instead of dotnet restore for .NET Framework projects
  nsExec::ExecToLog 'cmd /c "nuget restore "$3""'
  Pop $0
  
  ${If} $0 != 0
    DetailPrint "NuGet restore failed with exit code $0"
    DetailPrint "Trying with MSBuild restore..."
    
    ; Fallback to msbuild /t:restore
    nsExec::ExecToLog 'cmd /c "msbuild "$3" /t:restore"'
    Pop $0
    
    ${If} $0 != 0
      DetailPrint "MSBuild restore also failed with exit code $0"
      MessageBox MB_OK|MB_ICONEXCLAMATION "DGScope Installation Failed$\n$\nFailed to restore dependencies.$\n$\nError code: $0$\n$\nYou can try building manually from:$\n$2\scope$\n$\nEnsure Visual Studio 2022 and .NET Framework 4.7.2 Developer Pack are installed."
      Goto SkipDGScope
    ${EndIf}
  ${EndIf}
  
  DetailPrint "Building DGScope with MSBuild (this may take a few minutes)..."
  nsExec::ExecToLog 'cmd /c "msbuild "$3" /p:Configuration=Release /v:m"'
  Pop $0
  
  ${If} $0 == 0
    DetailPrint "DGScope built successfully!"
    
    ; Try multiple possible output locations
    ${If} ${FileExists} "$2\scope\build\Release\DGScope.exe"
      StrCpy $4 "$2\scope\build\Release\DGScope.exe"
    ${ElseIf} ${FileExists} "$2\scope\bin\Release\net472\DGScope.exe"
      StrCpy $4 "$2\scope\bin\Release\net472\DGScope.exe"
    ${ElseIf} ${FileExists} "$2\scope\bin\Release\DGScope.exe"
      StrCpy $4 "$2\scope\bin\Release\DGScope.exe"
    ${Else}
      DetailPrint "Build succeeded but executable not found at expected location."
      MessageBox MB_OK|MB_ICONEXCLAMATION "DGScope Installation Incomplete$\n$\nBuild completed but executable was not found.$\n$\nSource location: $2\scope$\n$\nYou may need to build manually."
      Goto SkipDGScope
    ${EndIf}
    
    DetailPrint "Found executable: $4"
    
    ; Create shortcuts for DGScope
    CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\DGScope.lnk" "$4"
    CreateShortCut "$DESKTOP\DGScope.lnk" "$4"
    
    ; Register DGScope installation
    WriteRegStr HKLM "${REG_PATH}" "DGScopeInstalled" "1"
    WriteRegStr HKLM "${REG_PATH}" "DGScopePath" "$2\scope"
    WriteRegStr HKLM "${REG_PATH}" "DGScopeExePath" "$4"
    StrCpy $DGScopeInstalled "1"
    
    MessageBox MB_OK|MB_ICONINFORMATION "DGScope Installed Successfully!$\n$\nLocation: $4"
  ${Else}
    DetailPrint "DGScope build failed with exit code $0"
    MessageBox MB_OK|MB_ICONEXCLAMATION "DGScope Installation Failed$\n$\nBuild failed with exit code: $0$\n$\nYou can try building manually from:$\n$2\scope$\n$\nCommand: msbuild scope.sln /p:Configuration=Release$\n$\nEnsure Visual Studio 2022 is installed."
  ${EndIf}
  
  DGScopeComplete:
  
  SkipDGScope:
SectionEnd

SectionGroupEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecGroupProfileManager} "DGScope Profile Manager - Manage and edit DGScope radar scope profiles."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecProfileManager} "Core Profile Manager application (required)."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecGroupDGScope} "DGScope - Digital radar scope application."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDGScope} "Download and install DGScope. First tries to download pre-built release, falls back to building from source if needed (requires Visual Studio 2022)."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

; Function to check and install .NET Framework 4.7.2
Function CheckAndInstallDotNET472
  DetailPrint "Checking for .NET Framework 4.7.2..."
  
  ; Check registry for .NET Framework 4.7.2
  ReadRegDWord $0 HKLM "Software\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
  
  ; Release value for .NET 4.7.2 is 461808 or higher
  ${If} $0 >= 461808
    DetailPrint ".NET Framework 4.7.2 or higher is already installed."
    Return
  ${EndIf}
  
  DetailPrint ".NET Framework 4.7.2 is not installed. Downloading..."
  
  ; Download .NET Framework 4.7.2
  SetOutPath "$TEMP"
  NSISdl::download "${NET472_URL}" "$TEMP\${NET472_FILENAME}"
  Pop $0 ; result
  
  ${If} $0 == "success"
    DetailPrint "Downloaded successfully. Installing .NET Framework 4.7.2..."
    nsExec::ExecToStack "$TEMP\${NET472_FILENAME} /q /norestart"
    Pop $0 ; exit code
    
    ${If} $0 == 0
      DetailPrint ".NET Framework 4.7.2 installed successfully."
    ${Else}
      MessageBox MB_ICONEXCLAMATION ".NET Framework 4.7.2 installation returned code $0. You may need to install it manually."
    ${EndIf}
    
    ; Clean up installer
    Delete "$TEMP\${NET472_FILENAME}"
  ${Else}
    MessageBox MB_ICONEXCLAMATION "Failed to download .NET Framework 4.7.2 (Error: $0). Please install it manually from https://dotnet.microsoft.com/download/dotnet-framework/net472"
  ${EndIf}
FunctionEnd

; Uninstall section
Section "Uninstall"
  DetailPrint "Removing DGScope Profile Manager..."
  
  ; Check which components are installed
  ReadRegStr $0 HKLM "${REG_PATH}" "ProfileManagerInstalled"
  ${If} $0 == "1"
    ; Remove Profile Manager files
    Delete "${INSTALL_DIR}\DGScopeProfileManager.exe"
    Delete "${INSTALL_DIR}\*.dll"
    Delete "${INSTALL_DIR}\${UNINSTALLER_NAME}"
    RMDir "${INSTALL_DIR}"
    
    ; Remove Start Menu shortcuts
    Delete "$SMPROGRAMS\DGScope Profile Manager\DGScope Profile Manager.lnk"
    Delete "$SMPROGRAMS\DGScope Profile Manager\Uninstall.lnk"
    
    ; Remove Desktop shortcut
    Delete "$DESKTOP\DGScope Profile Manager.lnk"
    
    DetailPrint "Profile Manager removed."
  ${EndIf}
  
  ReadRegStr $0 HKLM "${REG_PATH}" "DGScopeInstalled"
  ${If} $0 == "1"
    ; Ask if user wants to remove DGScope
    MessageBox MB_YESNO|MB_ICONQUESTION \
      "Do you also want to remove DGScope and its source files?" \
      IDYES RemoveDGScope IDNO KeepDGScope
    
    RemoveDGScope:
      DetailPrint "Removing DGScope..."
      
      ; Remove DGScope shortcuts
      Delete "$SMPROGRAMS\DGScope Profile Manager\DGScope.lnk"
      Delete "$DESKTOP\DGScope.lnk"
      
      ; Remove DGScope directory
      ReadRegStr $1 HKLM "${REG_PATH}" "DGScopePath"
      ${If} $1 != ""
        RMDir /r "$1"
        DetailPrint "DGScope removed from $1"
      ${EndIf}
    
    KeepDGScope:
  ${EndIf}
  
  ; Remove Start Menu folder if empty
  RMDir "$SMPROGRAMS\DGScope Profile Manager"
  
  ; Remove registry
  DeleteRegKey HKLM "${REG_PATH}"
  
  DetailPrint "Uninstall complete."
SectionEnd

; Language strings
LangString ^UninstallCaption ${LANG_ENGLISH} "Uninstall DGScope Profile Manager"
LangString ^UninstallSubCaption ${LANG_ENGLISH} "Uninstall DGScope Profile Manager from your computer"
