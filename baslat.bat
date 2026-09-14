@echo off
echo ========================================
echo VK On Muhasebe (Avalonia Desktop) Baslatiliyor...
echo ========================================
echo.

if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "PATH=C:\Program Files\dotnet;%PATH%"
)

cd ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop
if %errorlevel% neq 0 (
    cd ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia
)

echo Derleniyor ve Calistiriliyor...
echo.
dotnet run

if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo HATA: Proje calisirken bir hata olustu.
    echo ========================================
    echo.
    pause
) else (
    echo.
    echo Uygulama kapatildi.
)
