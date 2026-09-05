@echo off
chcp 65001 >nul
title BAWSAQ Muhasebe - Imzasiz Fiziksel IPA Derleyici
cls

echo =====================================================================
echo      BAWSAQ ON MUHASEBE - iPHONE (ARM64) IMZASIZ IPA DERLEME
echo =====================================================================
echo.
echo Bu islem Apple hesabi sormadan bulutta Sideloadly icin 
echo tam uyumlu fiziki iPhone (ARM64) IPA dosyasini derler.
echo.
echo Soru cikarsa "Do you want to log in to your Apple account?" -> "no" (n) secin!
echo =====================================================================
echo.

cd /d "%~dp0ermaymuhasebe-mobil"

call npx eas-cli build --platform ios --profile sideload

echo.
echo =====================================================================
echo  Derleme tamamlandi! Ekranda cikan linkten .ipa dosyasini indirin.
echo =====================================================================
pause
