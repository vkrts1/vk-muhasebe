@echo off
chcp 65001 >nul
title BAWSAQ Muhasebe - IPA Paketleyici
cls

echo =====================================================================
echo         BAWSAQ.app -> BAWSAQ.ipa OTOMATIK DONUSTURUCU
echo =====================================================================
echo.

set "SOURCE_DIR=%USERPROFILE%\Downloads"
set "OUTPUT_IPA=%USERPROFILE%\Downloads\BAWSAQ.ipa"
set "PAYLOAD_DIR=%USERPROFILE%\Downloads\Payload"

echo Downloads klasorunde BAWSAQ.app araniyor...

if exist "%PAYLOAD_DIR%" rd /s /q "%PAYLOAD_DIR%"
if exist "%OUTPUT_IPA%" del /f /q "%OUTPUT_IPA%"

if exist "%USERPROFILE%\Downloads\BAWSAQ.app" (
    mkdir "%PAYLOAD_DIR%"
    echo BAWSAQ.app kopyalaniyor...
    xcopy "%USERPROFILE%\Downloads\BAWSAQ.app" "%PAYLOAD_DIR%\BAWSAQ.app\" /E /H /C /I /Y >nul
) else (
    echo.
    echo [BILGI] Lutfen BAWSAQ.app klasorunu Downloads (Indirilenler) klasorune koyun.
    pause
    exit /b
)

echo.
echo IPA paketi olusturuluyor (Sıkıştırılıyor)...
powershell -Command "Compress-Archive -Path '%PAYLOAD_DIR%' -DestinationPath '%USERPROFILE%\Downloads\BAWSAQ.zip' -Force"
ren "%USERPROFILE%\Downloads\BAWSAQ.zip" "BAWSAQ.ipa"

if exist "%PAYLOAD_DIR%" rd /s /q "%PAYLOAD_DIR%"

echo.
echo =====================================================================
echo  BASARILI! 
echo  Dosyaniz hazir: Indirilenler (Downloads)\BAWSAQ.ipa
echo =====================================================================
echo.
pause
