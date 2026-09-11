@echo off
chcp 65001 >nul
title GitHub'a Yukle ve Derlemeyi Baslat
cls

echo =====================================================================
echo           VK ON MUHASEBE - GITHUB'A YUKLEME SIHIRBAZI
echo =====================================================================
echo.
echo Repo: https://github.com/vkrts02-cell/bawsaq-muhasebe.git
echo.

cd /d "%~dp0"

if exist ".git\index.lock" del /f /q ".git\index.lock"

echo [1/3] Dosyalar ekleniyor...
git add .

echo [2/3] Paket hazirlaniyor...
git commit -m "feat: setup VK iOS ARM64 GitHub Actions IPA build"

echo [3/3] GitHub'a yukleniyor (Push)...
git push -u origin main

echo.
echo =====================================================================
echo  YUKLEME TAMAMLANDI!
echo  Simdi https://github.com/vkrts02-cell/bawsaq-muhasebe/actions adresine
echo  giderek derlemeyi izleyebilirsiniz.
echo =====================================================================
pause
