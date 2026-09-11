@echo off
chcp 65001 >nul
title VK Muhasebe - IPA Paketleyici
cls

echo =====================================================================
echo         VK.app -> VK.ipa OTOMATIK DONUSTURUCU
echo =====================================================================
echo.

set "SOURCE_DIR=%USERPROFILE%\Downloads"
set "OUTPUT_IPA=%USERPROFILE%\Downloads\VK.ipa"
set "PAYLOAD_DIR=%USERPROFILE%\Downloads\Payload"

echo Downloads klasorunde VK.app araniyor...

if exist "%PAYLOAD_DIR%" rd /s /q "%PAYLOAD_DIR%"
if exist "%OUTPUT_IPA%" del /f /q "%OUTPUT_IPA%"

if exist "%USERPROFILE%\Downloads\VK.app" (
    mkdir "%PAYLOAD_DIR%"
    echo VK.app kopyalaniyor...
    xcopy "%USERPROFILE%\Downloads\VK.app" "%PAYLOAD_DIR%\VK.app\" /E /H /C /I /Y >nul
) else (
    echo.
    echo [BILGI] Lutfen VK.app klasorunu Downloads (Indirilenler) klasorune koyun.
    pause
    exit /b
)

echo.
echo IPA paketi olusturuluyor (Sıkıştırılıyor)...
powershell -Command "Compress-Archive -Path '%PAYLOAD_DIR%' -DestinationPath '%USERPROFILE%\Downloads\VK.zip' -Force"
ren "%USERPROFILE%\Downloads\VK.zip" "VK.ipa"

if exist "%PAYLOAD_DIR%" rd /s /q "%PAYLOAD_DIR%"

echo.
echo =====================================================================
echo  BASARILI! 
echo  Dosyaniz hazir: Indirilenler (Downloads)\VK.ipa
echo =====================================================================
echo.
pause
