@echo off
echo ========================================
echo Ermay Muhasebe (Avalonia Desktop) Baslatiliyor...
echo ========================================
echo.

cd ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia
if %errorlevel% neq 0 (
    echo HATA: Klasor bulunamadi: ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia
    echo.
    pause
    exit /b
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
