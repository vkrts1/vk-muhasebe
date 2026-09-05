@echo off
echo ========================================
echo Ermay Muhasebe PDF API (QuestPDF) Baslatiliyor...
echo ========================================
echo.

cd ErmayMuhasebe.Functions
if %errorlevel% neq 0 (
    echo HATA: ErmayMuhasebe.Functions klasoru bulunamadi!
    pause
    exit /b
)

echo PDF API Derleniyor ve Calistiriliyor...
echo (Bu islem ilk kez yapilirken 30-60 saniye surebilir, lutfen bekleyiniz...)
echo.
echo ========================================
echo ERISIM BILGILERI:
echo Yerel (Bu Bilgisayar): http://localhost:5244
echo Yerel (Bu Bilgisayar): http://127.0.0.1:5244
echo.
echo MOBIL UYGULAMA ICIN AG ADRESLERI (Ayni WiFi'de):
for /f "delims=" %%a in ('ipconfig ^| findstr /c:"IPv4"') do echo   %%a
echo.
echo ONEMLI NOTLAR:
echo 1. Mobil cihazinizin ayni WiFi agina bagli oldugundan emin olun.
echo 2. Windows Guvenlik Duvari (Firewall) 5244 portunu engelliyor olabilir.
echo    Firewall'i gecici olarak kapatabilir veya asagidaki komutu yonetici
echo    olarak calistirarak portu acabilirsiniz:
echo    netsh advfirewall firewall add rule name="Ermay PDF 5244" dir=in action=allow protocol=TCP localport=5244
echo 3. Mobil uygulamanin Ayarlar sayfasinda PDF Sunucu Adresini yukaridaki
echo    IPv4 adresine gore ayarlayin (ornegin: http://192.168.1.103:5244)
echo ========================================
echo.

:: --urls parametresi ile tum ag arayuzlerinden yayin yapmasini sagliyoruz
dotnet run --urls "http://*:5244"

if %errorlevel% neq 0 (
    echo.
    echo ========================================
    echo HATA: PDF API calisirken bir hata olustu.
    echo ========================================
    echo.
    pause
)
