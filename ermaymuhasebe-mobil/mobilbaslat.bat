@echo off
title Ermay Muhasebe - Mobil Baslatici
cd /d "%~dp0"

echo ========================================================
echo         Ermay Muhasebe Mobil Uygulama Baslatici
echo ========================================================
echo.

:: Eski Node sureclerini temizle
taskkill /f /im node.exe >nul 2>&1

:: Aktif yerel Wi-Fi / LAN IPv4 adresini tespit et
set DETECTED_IP=
for /f %%i in ('node get-ip.js') do set DETECTED_IP=%%i

if "%DETECTED_IP%"=="" (
    set DETECTED_IP=192.168.3.2
)

echo Tespit Edilen Bilgisayar IP Adresi: %DETECTED_IP%
echo.
echo ========================================================
echo Metro Sunucusu Baslatiliyor (IP: %DETECTED_IP%)...
echo ========================================================
echo.
set REACT_NATIVE_PACKAGER_HOSTNAME=%DETECTED_IP%
call npx expo start -c --host lan

echo.
echo ========================================================
echo Mobil sunucu sonlandi.
echo ========================================================
pause
