@echo off
title Ermay Muhasebe - Mobil Paket Guncelle ve Baslat
cd /d "%~dp0\ermaymuhasebe-mobil"

:: Admin kontrolü
net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -Command "Start-Process '%~0' -Verb RunAs"
    exit /b
)

echo ========================================================
echo Eski Node / Metro surecleri sonlandiriliyor...
echo ========================================================
taskkill /f /im node.exe >nul 2>&1
timeout /t 2 /nobreak >nul

echo.
echo ========================================================
echo Eksik Babel / Helper dosyalari tamamlaniyor...
echo ========================================================
powershell -NoProfile -Command "if (Test-Path 'C:\Users\mazik\AppData\Local\npm-cache\_npx\249ca9fcd30c476a\node_modules\@babel\runtime\helpers') { Copy-Item -Path 'C:\Users\mazik\AppData\Local\npm-cache\_npx\249ca9fcd30c476a\node_modules\@babel\runtime\helpers\*' -Destination 'node_modules\@babel\runtime\helpers' -Recurse -Force; Write-Host 'Babel helpers basariyla kopyalandi.' }"

:: IP tespiti
set DETECTED_IP=
for /f %%i in ('node -e "const os = require('os'); const nets = os.networkInterfaces(); let found = ''; for(const name of Object.keys(nets)){ for(const net of nets[name]){ if(net.family==='IPv4' && !net.internal && !net.address.startsWith('192.168.56.') && !net.address.startsWith('169.254.')){ found = net.address; break; } } if(found) break; } console.log(found);"') do set DETECTED_IP=%%i

if "%DETECTED_IP%"=="" set DETECTED_IP=192.168.3.2
set REACT_NATIVE_PACKAGER_HOSTNAME=%DETECTED_IP%

echo.
echo ========================================================
echo Metro Sunucusu Baslatiliyor (IP: %DETECTED_IP%)...
echo ========================================================
call npx expo start -c --host lan
pause
