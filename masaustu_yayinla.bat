@echo off
chcp 65001 > nul
echo ===================================================
echo   VK ON MUHASEBE - MASAUSTU YAYIN VE SETUP
echo ===================================================
echo.

echo [1/3] Onceki yayin dosyalari temizleniyor...
if exist "Publish_Output\Desktop" rd /s /q "Publish_Output\Desktop"
mkdir "Publish_Output\Desktop"
if not exist "Publish_Output\Installer" mkdir "Publish_Output\Installer"

echo.
echo [2/3] .NET 9 Self-Contained Masaustu Surumu Derleniyor...
dotnet publish "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop\ErmayMuhasebe.Avalonia.Desktop.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false -o "Publish_Output\Desktop"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [HATA] Derleme basarisiz oldu!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Inno Setup Kurulum Paketi (Setup.exe) Olusturuluyor...
set "ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC_PATH%" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC_PATH%" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
if not exist "%ISCC_PATH%" (
    for /f "delims=" %%I in ('where iscc.exe 2^>nul') do set "ISCC_PATH=%%I"
)

if exist "%ISCC_PATH%" (
    "%ISCC_PATH%" "vk_setup.iss"
    if %ERRORLEVEL% EQU 0 (
        echo.
        echo ===================================================
        echo [TEBRIKLER] Kurulum dosyasi basariyla uretildi!
        echo Kurulum Dosyasi: Publish_Output\Installer\VK_Setup_v1.0.0.exe
        echo ===================================================
    ) else (
        echo.
        echo [UYARI] Setup olusturulamadi, lutfen vk_setup.iss dosyasini Inno Setup ile acip Compile edin.
    )
) else (
    echo.
    echo Inno Setup bulunamadi. Setup uretmek icin 'vk_setup.iss' dosyasini Inno Setup ile derleyebilirsiniz.
)

echo.
pause
