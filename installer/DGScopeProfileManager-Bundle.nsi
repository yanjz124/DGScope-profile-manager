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
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_LANGUAGE "English"

; Installer sections

Section "!Core Files (Required)" SecCore
  SectionIn RO ; Required section - cannot be unchecked
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

  ; Copy fonts to install directory (for optional font install section)
  SetOutPath "$INSTDIR\fonts"
  File /nonfatal "fonts\*.otf"
  File /nonfatal "fonts\*.ttf"

  ; Profiles directory will be created by the application as needed
  SetOutPath "$INSTDIR\profiles"

  ; Store installation directory
  WriteRegStr HKCU "Software\DGScope Profile Manager" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\DGScope Profile Manager" "ScopeExePath" "$INSTDIR\scope\scope.exe"

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager" "DisplayName" "DGScope Profile Manager Bundle"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager" "UninstallString" "$INSTDIR\uninstall.exe"
SectionEnd

Section "Start Menu Shortcuts" SecStartMenu
  CreateDirectory "$SMPROGRAMS\DGScope Profile Manager"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\Profile Manager.lnk" "$INSTDIR\ProfileManager\DGScopeProfileManager.exe"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\DGScope.lnk" "$INSTDIR\scope\scope.exe"
  CreateShortCut "$SMPROGRAMS\DGScope Profile Manager\Uninstall.lnk" "$INSTDIR\uninstall.exe"
SectionEnd

Section /o "Desktop Shortcut" SecDesktop
  CreateShortCut "$DESKTOP\DGScope Profile Manager.lnk" "$INSTDIR\ProfileManager\DGScopeProfileManager.exe"
SectionEnd

Section "Install FixedDemiBold Font" SecFont
  ; Install FixedDemiBold font for the user
  ; Copy to user's local fonts folder and register
  IfFileExists "$INSTDIR\fonts\FixedDemiBold.otf" 0 SkipFontInstall
    CreateDirectory "$LOCALAPPDATA\Microsoft\Windows\Fonts"
    CopyFiles /SILENT "$INSTDIR\fonts\FixedDemiBold.otf" "$LOCALAPPDATA\Microsoft\Windows\Fonts\FixedDemiBold.otf"
    WriteRegStr HKCU "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" "FixedDemiBold (OpenType)" "$LOCALAPPDATA\Microsoft\Windows\Fonts\FixedDemiBold.otf"
  SkipFontInstall:
SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} "Core application files for DGScope Profile Manager and DGScope (required)."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecStartMenu} "Create Start Menu shortcuts for Profile Manager, DGScope, and Uninstall."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} "Create a Desktop shortcut for DGScope Profile Manager."
  !insertmacro MUI_DESCRIPTION_TEXT ${SecFont} "Install the FixedDemiBold font used by DGScope for realistic radar display text."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section "Uninstall"
  ; Remove Start Menu shortcuts
  Delete "$SMPROGRAMS\DGScope Profile Manager\*.*"
  RMDir "$SMPROGRAMS\DGScope Profile Manager"

  ; Remove Desktop shortcut
  Delete "$DESKTOP\DGScope Profile Manager.lnk"

  ; Remove installed font
  Delete "$LOCALAPPDATA\Microsoft\Windows\Fonts\FixedDemiBold.otf"
  DeleteRegValue HKCU "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts" "FixedDemiBold (OpenType)"

  ; Remove installation directory
  RMDir /r "$INSTDIR"

  ; Remove registry entries
  DeleteRegKey HKCU "Software\DGScope Profile Manager"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager"

  MessageBox MB_OK "DGScope Profile Manager Bundle has been uninstalled."
SectionEnd
