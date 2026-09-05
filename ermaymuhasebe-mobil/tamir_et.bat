@echo off
title Ermay Muhasebe - Sifirdan Kurulum (SDK 54)
set EXPO_NO_TELEMETRY=1
set EXPO_NO_CACHE=1
cd /d "%~dp0"

echo ===================================================
echo  ADIM 1/3: Eski bozuk dosyalar tamamen siliniyor...
echo  (Bu islem 1-2 dakika surebilir)
echo ===================================================
:: Calisan tum node.exe sureclerini sonlandirarak kilitli dosyalari serbest birakiyoruz
taskkill /f /im node.exe >nul 2>&1
if exist node_modules rmdir /s /q node_modules
if exist package-lock.json del /f package-lock.json
echo Silme tamamlandi!
echo.

echo ===================================================
echo  ADIM 2/3: SDK 54 uyumlu paketler indiriliyor...
echo  UYARI: Ekran donmus gibi gorunebilir, KAPATMAYIN!
echo ===================================================
call npm install --no-audit --no-fund --legacy-peer-deps
echo.

echo ===================================================
echo  ADIM 3/3: Expo versiyonlarini son kez kontrol...
echo ===================================================
call npx --yes expo install --fix
echo.

echo ###################################################
echo  HERSEY TAMAMLANDI!
echo  Simdi bu pencereyi kapatin ve baslat.bat'a tiklayin
echo ###################################################
pause
