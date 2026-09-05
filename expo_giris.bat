@echo off
title Expo Giris Yap
echo ========================================================
echo               Expo Hesabina Giris Yap
echo ========================================================
echo.
echo Tarayici veya terminal uzerinden Expo hesabinizla giris yapabilirsiniz.
echo Hesabiniz yoksa https://expo.dev/signup adresinden ucretsiz acabilirsiniz.
echo.

cd /d "%~dp0\ermaymuhasebe-mobil"
npx expo login

echo.
pause
