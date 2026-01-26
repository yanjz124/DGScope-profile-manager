; DGScope Profile Manager Bundle Installer
; Includes DGScope, Profiles, and Profile Manager in one installer

!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

; Name and file
Name "DGScope Profile Manager Bundle v RELEASE_VERSION"
OutFile "DGScope-Profile-Manager-vRELEASE_VERSION-Setup.exe"
InstallDir "$LOCALAPPDATA\DGScope Profile Manager"
InstallDirRegKey HKCU "Software\DGScope Profile Manager" "InstallDir"

; Request user privileges (no admin needed for AppData install)
RequestExecutionLevel user

; MUI Settings
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

; Installer sections
Section "Install"
  SetOutPath "$INSTDIR"

  ; Create subdirectories
  CreateDirectory "$INSTDIR\ProfileManager"
  CreateDirectory "$INSTDIR\scope"
  CreateDirectory "$INSTDIR\profiles"
  CreateDirectory "$INSTDIR\fonts"

  ; Copy Profile Manager files
  SetOutPath "$INSTDIR\ProfileManager"
  File /r "ProfileManager\*.*"

  ; Copy DGScope files
  SetOutPath "$INSTDIR\scope"
  File /r "scope\*.*"

  ; Copy fonts
  SetOutPath "$INSTDIR\fonts"
  File /nonfatal "fonts\*.otf"
  File /nonfatal "fonts\*.ttf"

  ; Install FixedDemiBold font for the user
  ; Copy to user's local fonts folder and register
  IfFileExists "$INSTDIR\fonts\FixedDemiBold.otf" 0 SkipFontInstall
    CreateDirectory "$LOCALAPPDATA\Microsoft\Windows\Fonts"
    CopyFiles /SILENT "$INSTDIR\fonts\FixedDemiBold.otf" "$LOCALAPPDATA\Microsoft\Windows\Fonts\FixedDemiBold.otf"
    WriteRegStr HKCU "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" "FixedDemiBold (OpenType)" "$LOCALAPPDATA\Microsoft\Windows\Fonts\FixedDemiBold.otf"
  SkipFontInstall:

  ; Profiles directory will be created by the application as needed
  SetOutPath "$INSTDIR\profiles"

  ; Create Start Menu shortcuts
  CreateDirectory "$SMPROGRAMS\DGScope Profile Manager"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\Profile Manager.lnk" "$INSTDIR\ProfileManager\DGScopeProfileManager.exe"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\DGScope.lnk" "$INSTDIR\scope\scope.exe"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\Uninstall.lnk" "$INSTDIR\uninstall.exe"

  ; Create desktop shortcut
  CreateShortCut "$DESKTOP\DGScope Profile Manager.lnk" "$INSTDIR\ProfileManager\DGScopeProfileManager.exe"

  ; Store installation directory
  WriteRegStr HKCU "Software\DGScope Profile Manager" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\DGScope Profile Manager" "ScopeExePath" "$INSTDIR\scope\scope.exe"

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager" "DisplayName" "DGScope Profile Manager Bundle"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager" "UninstallString" "$INSTDIR\uninstall.exe"

  MessageBox MB_OK "DGScope Profile Manager Bundle installed successfully!$\n$\nStart Menu shortcuts have been created.$\nA desktop shortcut for Profile Manager has been added.$\n$\nThe FixedDemiBold font has been installed."
SectionEnd

Section "Uninstall"
  ; Remove shortcuts
  Delete "$SMPROGRAMS\DGScope Profile Manager\*.*"
  RMDir "$SMPROGRAMS\DGScope Profile Manager"
  Delete "$DESKTOP\DGScope Profile Manager.lnk"

  ; Remove installed font (optional - keep font if user wants it)
  Delete "$LOCALAPPDATA\Microsoft\Windows\Fonts\FixedDemiBold.otf"
  DeleteRegValue HKCU "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" "FixedDemiBold (OpenType)"

  ; Remove installation directory
  RMDir /r "$INSTDIR"

  ; Remove registry entries
  DeleteRegKey HKCU "Software\DGScope Profile Manager"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager"

  MessageBox MB_OK "DGScope Profile Manager Bundle has been uninstalled."
SectionEnd
