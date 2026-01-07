; DGScope Profile Manager Complete Bundle Installer
; Includes DGScope, Profiles, and Profile Manager in one installer

!include "MUI2.nsh"
!include "nsDialogs.nsh"
!include "LogicLib.nsh"

; Name and file
Name "DGScope Profile Manager Bundle"
OutFile "DGScopeProfileManager-Bundle.exe"
InstallDir "$PROGRAMFILES\DGScope Profile Manager"
InstallDirRegKey HKCU "Software\DGScope Profile Manager" "InstallDir"

; Request admin privileges
RequestExecutionLevel admin

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
  
  ; Copy Profile Manager files
  SetOutPath "$INSTDIR\ProfileManager"
  File /r "ProfileManager\*.*"
  
  ; Copy DGScope files
  SetOutPath "$INSTDIR\scope"
  File /r "scope\*.*"
  
  ; Create ARTCC profile directories
  SetOutPath "$INSTDIR\profiles"
  CreateDirectory "$INSTDIR\profiles\ZAN"
  CreateDirectory "$INSTDIR\profiles\ZAK"
  CreateDirectory "$INSTDIR\profiles\ZDV"
  CreateDirectory "$INSTDIR\profiles\ZDC"
  CreateDirectory "$INSTDIR\profiles\ZID"
  CreateDirectory "$INSTDIR\profiles\ZIN"
  CreateDirectory "$INSTDIR\profiles\ZJX"
  CreateDirectory "$INSTDIR\profiles\ZKC"
  CreateDirectory "$INSTDIR\profiles\ZLA"
  CreateDirectory "$INSTDIR\profiles\ZLC"
  CreateDirectory "$INSTDIR\profiles\ZMA"
  CreateDirectory "$INSTDIR\profiles\ZME"
  CreateDirectory "$INSTDIR\profiles\ZMP"
  CreateDirectory "$INSTDIR\profiles\ZNY"
  CreateDirectory "$INSTDIR\profiles\ZOA"
  CreateDirectory "$INSTDIR\profiles\ZOB"
  CreateDirectory "$INSTDIR\profiles\ZSE"
  CreateDirectory "$INSTDIR\profiles\ZTL"
  CreateDirectory "$INSTDIR\profiles\ZUA"
  
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
  
  MessageBox MB_OK "DGScope Profile Manager Bundle installed successfully!$\n$\nStart Menu shortcuts have been created.$\nA desktop shortcut for Profile Manager has been added."
SectionEnd

Section "Uninstall"
  ; Remove shortcuts
  Delete "$SMPROGRAMS\DGScope Profile Manager\*.*"
  RMDir "$SMPROGRAMS\DGScope Profile Manager"
  Delete "$DESKTOP\DGScope Profile Manager.lnk"
  
  ; Remove installation directory
  RMDir /r "$INSTDIR"
  
  ; Remove registry entries
  DeleteRegKey HKCU "Software\DGScope Profile Manager"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DGScope Profile Manager"
  
  MessageBox MB_OK "DGScope Profile Manager Bundle has been uninstalled."
SectionEnd
