@echo off
chcp 65001 >nul
title GitHub Actions ile iOS IPA Derleme Hazirligi
cls

echo =====================================================================
echo    GITHUB ACTIONS ILE OTO-DERLEME (ARM64 IPA) HAZIRLAYICI
echo =====================================================================
echo.
echo Bu sihirbaz projenizi GitHub'a yuklemeniz icin hazirlayacaktir.
echo.

cd /d "%~dp0"

if not exist ".git" (
    echo Git deposu baslatiliyor...
    git init
    git branch -M main
)

echo.
echo Dosyalar hazirlaniyor...
git add .
git commit -m "feat: iOS ARM64 IPA GitHub Actions workflow eklendi"

echo.
echo =====================================================================
echo  HAZIRLIK TAMAMLANDI!
echo =====================================================================
echo.
echo Simdi son adim:
echo 1. https://github.com/new adresinden "bawsaq-muhasebe" adinda yeni bir repo olusturun (Private / Gizli yapabilirsiniz).
echo 2. Repo sayfasindaki "git remote add origin https://github.com/..." komutunu asagiya yapistirin.
echo.
pause
