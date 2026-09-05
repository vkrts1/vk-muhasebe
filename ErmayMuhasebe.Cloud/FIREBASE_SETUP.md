# Firebase Kurulum Rehberi

ErmayMuhasebe.Cloud uygulaması veritabanı olarak Google Firebase kullanmaktadır. Uygulamanın çalışması için aşağıdaki adımları takip ederek `appsettings.json` dosyasını doldurmanız gerekmektedir.

## 1. Firebase Projesi Oluşturma
1. [Firebase Console](https://console.firebase.google.com/) adresine gidin.
2. "Proje Ekle" (Add Project) butonuna tıklayın.
3. Proje adı olarak `ErmayMuhasebe` (veya istediğiniz bir isim) girin.
4. Google Analytics'i kapatabilirsiniz, şu an ihtiyacımız yok.
5. "Proje Oluştur" diyerek tamamlayın.

## 2. Realtime Database Oluşturma
1. Sol menüden **Build** sekmesini açın ve **Realtime Database**'i seçin.
2. "Veritabanı Oluştur" (Create Database) butonuna tıklayın.
3. Konum olarak size en yakın olanı (örneğin `Belgium (europe-west1)`) seçin.
4. **Güvenlik Kuralları** adımında "Test modunda başlat" (Start in test mode) seçeneğini seçin. (Daha sonra güvenliği artıracağız).
5. "Etkinleştir" (Enable) diyerek bitirin.

## 3. Bağlantı Bilgilerini Alma
Veritabanınız oluşturulduktan sonra ekranın üst kısmında `https://ermaymuhasebe-default-rtdb.europe-west1.firebasedatabase.app/` benzeri bir URL göreceksiniz.

1. Bu URL'yi kopyalayın.
2. `ErmayMuhasebe.Cloud/wwwroot/appsettings.json` dosyasını açın.
3. `BaseUrl` alanına bu adresi yapıştırın.

**Örnek appsettings.json:**
```json
{
  "Firebase": {
    "BaseUrl": "https://ermay-muhasebe-demo-default-rtdb.europe-west1.firebasedatabase.app/",
    "AuthSecret": "" 
  }
}
```

*Not: AuthSecret şimdilik boş kalabilir, Test modunda okuma/yazma açıktır.*

## 4. Masaüstü Uygulaması ile Eşitleme
Masaüstü uygulamasında "Ayarlar" menüsüne giderek "Bulut Senkronizasyon" bölümüne Firebase URL ve (varsa) Auth Secret bilgilerinizi giriniz.
"Ayarları Kaydet" dedikten sonra "Şimdi Senkronize Et" butonuna basarak tüm verilerinizi buluta gönderebilirsiniz. 
Ayrıca, masaüstü uygulamasında yaptığınız yeni eklemeler (Cari, Fatura vb.) otomatik olarak buluta gönderilecektir.
