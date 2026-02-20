!include "MUI2.nsh"

!ifndef APP_VERSION
  !define APP_VERSION "1.0.0"
!endif
!ifndef INPUT_DIR
  !define INPUT_DIR "dist-win"
!endif
!ifndef OUTPUT_DIR
  !define OUTPUT_DIR "release"
!endif

Name "iBaye ${APP_VERSION}"
OutFile "${OUTPUT_DIR}/iBaye-Setup-${APP_VERSION}.exe"
InstallDir "$LOCALAPPDATA\Programs\iBaye"
InstallDirRegKey HKCU "Software\iBaye" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma

!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${INPUT_DIR}/*"

  WriteRegStr HKCU "Software\iBaye" "InstallDir" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\iBaye" "DisplayName" "iBaye"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\iBaye" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\iBaye" "DisplayVersion" "${APP_VERSION}"
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\iBaye" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\iBaye" "NoRepair" 1

  CreateDirectory "$SMPROGRAMS\iBaye"
  CreateShortCut "$SMPROGRAMS\iBaye\iBaye.lnk" "$INSTDIR\baye.exe" "" "$INSTDIR\baye.exe"
  CreateShortCut "$DESKTOP\iBaye.lnk" "$INSTDIR\baye.exe" "" "$INSTDIR\baye.exe"

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\iBaye.lnk"
  Delete "$SMPROGRAMS\iBaye\iBaye.lnk"
  RMDir "$SMPROGRAMS\iBaye"

  Delete "$INSTDIR\Uninstall.exe"
  RMDir /r "$INSTDIR"

  DeleteRegKey HKCU "Software\iBaye"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\iBaye"
SectionEnd
