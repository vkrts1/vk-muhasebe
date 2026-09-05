@echo off
chcp 65001 >nul
title BAWSAQ Muhasebe - Mobil Baslatici (Expo Go)
cls

echo =====================================================================
echo           BAWSAQ ON MUHASEBE - iPHONE MOBIL BASLATICI
echo =====================================================================
echo.
echo 1. iPhone'unuzda App Store'dan ucretsiz "Expo Go" uygulamasini acin.
echo 2. Ekranda cikacak olan QR Kodu iPhone kameranizla okutun.
echo.
echo Uygulama telefonunuzda derlenip hemen acilacaktir!
echo =====================================================================
echo.

cd /d "%~dp0ermaymuhasebe-mobil"

npx expo start --tunnel

pause
