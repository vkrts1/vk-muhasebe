# Ermay Muhasebe — Masaüstü (Avalonia) ↔ Mobil (React Native/Expo) Analiz ve Eksiklik Raporu

> Kapsam: Tüm repo analiz edilmiş, masaüstü versiyon (Avalonia) baz alınarak mevcut mobil versiyon (React Native/Expo)
> derinlemesine incelenmiş ve masaüstü ile arasındaki eksiklikler tespit edilmiştir.
> Tarih: 2026-08-09

---

## 1. Proje Genel Görünümü

| Katman | Proje | Teknoloji | Durum |
|---|---|---|---|
| Masaüstü | `ErmayMuhasebe.Avalonia` (+ `.Desktop/.Android/.iOS`) | Avalonia 11.3, C# .NET 9, MVVM | Tamamlanmış (~226 dosya, 60+ View) |
| Paylaşılan çekirdek | `ErmayMuhasebe.Shared` | C# — Model/Servis/VM | Tamamlanmış (146 dosya) |
| Web (Blazor) | `ErmayMuhasebe.Cloud` | Blazor WASM + MudBlazor | Kısmi |
| PDF/Backend API | `ErmayMuhasebe.Functions` | Minimal API (5244 portu) | Kısmi (20 endpoint) |
| Testler | `ErmayMuhasebe.Tests` | xUnit (70+ test dosyası) | Mevcut (sadece destek katmanı) |
| **Mobil** | `ermaymuhasebe-mobil` | **React Native + Expo SDK 54 + Firebase RTDB REST** | **Kısmi (16 ekran)** |
| Firebase cloud | `firebase_functions` | Node.js | Kısmi |

### Mimari fark (kritik)
- **Masaüstü:** Yerel `SQLCipher` şifreli SQLite (`ErmayV4_Stable.db3`) → offline-first, Firebase'e otomatik senkron (`CloudSyncService`, kuyruk mantığı, `%LocalAppData%\ermay_cloud_config*.json`).
- **Mobil:** Firebase Realtime Database'e doğrudan **REST + 2.5 sn polling** (`subscribeToPath`). Yerel veritabanı **yok**, senkron kuyruğu **yok**, offline destek **yok**.
- Her iki taraf da aynı RTDB şemasını kullanır: `companies/{tenant}/years/{yil}/{Kaynak}` (PascalCase). Mobil `mapPathToDatabase` ile camelCase↔PascalCase çevirir. **Uyumlu.**

---

## 2. Masaüstü Versiyon Özellik Durumu (Baz)

Masaüstü ana modüller: Giriş + Mali Yıl Seçimi, Dashboard (14+ KPI, LiveCharts), Cari Hesaplar, Stok Kartları, Faturalar, Finans (Nakit/Kasa/Banka/Çek-Senet/Kredi Kartı/EFT), Vade Takip, **31 Rapor**, Siparişler, Teklifler, Görev Panosu, Hesap Makinesi, Araçlar (30+ araç), Ayarlar.

### Masaüstüne özgü güçlü yönler
- **Yerel QuestPDF** ile ~20 belge türü + 31 rapor **offline PDF** üretimi (harici sunucu yok).
- **Kapalı devre cari/finans mantığı:** `FinansService.SaveTransactionAsync` — tek işlem (Tahsilat/Ödeme/Dekont) kasa,banka,KK,EFT,ciro,cari hareketi **atomik** günceller.
- **Güvenlik:** Kullanıcı/şifre + SHA256+salt, 30 dk oturum zaman aşımı (`SessionService`), Telegram 2FA, `SecuritySyncService` ile uzak oturum iptali, SQLCipher DB şifreleme.
- **Uyarı/bildirim motoru:** 9 alarm grubu (kritik stok, negatif stok, vadesi geçmiş, risk limiti, çek/senet vade, yaşlanma, günlük özet) + bildirim paneli + Windows Toast + Telegram.
- **Excel:** ClosedXML ile stilli `.xlsx` dışa aktarım; MiniExcel okuma.
- **Paylaşım:** MailKit ile e-posta + WhatsApp (klavye simülasyonu) ile PDF gönderimi.
- **Yedekleme:** Tam DB yedek / geri yükleme / otomatik yedek; **mali yıl transferi** (`YearTransferService`).
- **Canlı kur:** TCMB + dış API; özel döviz otomasyonu.
- **Kişiselleştirme:** 2 tema (ModernSaaS / IDE), Display ölçekleme, kısayol tuşları, durum çubuğu yapılandırması.

---

## 3. Mobil Versiyon Durumu (Mevcut)

### Mevcut ekranlar (16)
Giriş, Özet (4 KPI), Cariler (+hızlı tahsilat/ödeme + yaşlandırma + risk), Faturalar (+E-Arşiv bayrağı), Stoklar (+ağırlıklı ortalama maliyet), Finans (kasa/banka/çek/senet/KK/EFT + çek görseli), Siparişler (→faturaya dönüştür), Teklifler (→siparişe dönüştür), Vade Takib (+erteleme + takvim widget + çek/senet ciro), Raporlar (15 rapor), Kanban, Hesap Makinesi, Araçlar (10 sekme), Ayarlar.

### Mobilin mevcut güçlü yönleri
- Aynı RTDB şemasını kullanıyor (desktop senkronu ile birlikte çalışabilir).
- Sipariş→Fatura, Teklif→Sipariş dönüşüm zincirleri var.
- Vade erteleme + takvim widget (masaüstünde olmayan yenilik).
- PDF paylaşım (harici sunucu ile), CSV dışa aktarım.

---

## 4. Eksiklik Analizi (Masaüstü ↔ Mobil)

### 4.1 Modül / Ekran Eksikleri (masaüstünde var, mobilde yok)

| Eksik Modül | Masaüstü kaynağı | Mobil durum | Önem |
|---|---|---|---|
| Mali Yıl Seçimi ekranı | `YearSelectionView` | Sadece yıl parametresi (giriş) | Orta |
| Dashboard tam KPI seti (StokDevirHızı, DSO, Karlılık, BekleyenOdeme, BugünÖdenecek…) | `DashboardViewModel` + `DashboardStats` | Sadece 4 KPI | Yüksek |
| Belge Arşivi (tam) | `BelgeArsivView` | Araçlar'da var ama kısmi | Orta |
| Bütçe Planlama | `ButcePlanlamaView` | Yok | Orta |
| Müşteri Limit yönetimi | `MusteriLimitView` | Yok (sadece risk bayrağı) | Orta |
| Borç Hatırlatıcı | `BorcHatirlaticiView` | Yok | Orta |
| Fatura Tasarımı | `FaturaTasarimView` | Yok | Düşük |
| Barkod Tasarımı | `BarkodTasarimView` | Yok | Düşük |
| Stok Sayım (fiziksel) | `StokSayimView` | Yok | Orta |
| Veri Temizlik / Db Bakım | `VeriTemizlikView`, `DbBakimView` | Yok | Orta |
| Fiyat Listesi | `FiyatListesiView` | Yok | Orta |
| Rota Planlama | `RotaPlanlamaView` | Yok | Düşük |
| Evrak No Düzenleme | `EvrakNoDuzenleView` | Yok | Düşük |
| Kısayol Tuşları | `KisayolTusuView` | Yok (mobilde mantıksız olabilir) | Düşük |
| Döviz Otomasyon / Çevirici | `DovizOtomasyonView`, `DovizDonusturucuView` | "Canlı kur" = **simülasyon** | Yüksek |
| Çek Risk Analiz | `CekRiskAnalizView` | Yok | Orta |
| Uyarı/Bildirim ve Alarm Kontrolleri | `MainViewModel.RunAlertChecksAsync` (9 grup) | **Yok** | Yüksek |
| Windows Toast / Telegram bildirim | `NotificationItem` + Telgraf | Yok (mobilde push planlanmalı) | Yüksek |

### 4.2 Rapor Eksikleri (31 → 15)

Mobilde olan 15: Genel Özet, Cari Bakiye, Cari Hareket Dökümü, Stok Mevcudu, Stok Hareketleri, Kritik Stok, Stok Devir Hızı, Ürün Karlılık, Müşteri Karlılık, En Çok Satanlar, Nakit Akış, Kasa Hareketleri, Nakit İşlem Detay, Ölü Stok, Hareketsiz Cariler.

**Eksik 16 rapor:** Aylık Tahsilat-Ödeme Analizi, Kredi Kartı Detay, Çek Detay, Havale/EFT Detay, **Yaşlandırma**, Satış Faturası Dökümü, Gelir Tablosu, Detaylı Gelir-Maliyet, Müşteri ABC, Kar-Zarar Mukayesesi, **Vadesi Geçmiş Alacaklar** (FIFO), Müşteri Churn, Fiyat Dalgalanma, Müşteri Sadakat (LTV), Finansal Isı Haritası, Tahsilat Süresi (DSO), Bütçe/Hedef Takibi.

### 4.3 PDF Altyapı Eksikliği (KRİTİK)

- Masaüstü raporları **lokal** `PdfService` (QuestPDF) üretir → her yerde çalışır.
- Mobil, PDF için **harici HTTP sunucuya** bağımlıdır (`http://192.168.1.103:5244` sabit varsayılan; lan içi IP).
- **Uyumsuzluk:** Mobilin Raporlar ekranı `consolidated_report`, `cari_balance_report`, `stok_status_report`, `critical_stock_report`, `dead_stock_report`, `inactive_cari_report`, `cash_flow_report` gibi **endpoint isimleri** kullanıyor; ancak `ErmayMuhasebe.Functions` projesinde karşılığı olan endpointler: `fatura, teklif, siparis, generic, consolidated, budget, makbuz, eft, kk, ekstre, ekstre-detayli, fatura-batch, stok-list, stok-hareket, kasa-ekstre, cek, cek-list, kk-list, eft-list, kasa-list`. Yani **rapor endpoint'leri repo içindeki Functions projesinde mevcut değil** (muhtemelen ayrı bir "pdf-server" projesi dışarıda). Bu, rapor PDF'lerinin çalışmayacağı/çalışsa bile repo dışı sunucuya bağımlı olduğu anlamına gelir.
- E-Arşiv/fatura tasarımı gibi kişiselleştirilmiş PDF desteği mobilde yok.

### 4.4 Kimlik Doğrulama / Güvenlik Eksikleri (KRİTİK)

| Konu | Masaüstü | Mobil |
|---|---|---|
| Kullanıcı/şifre + rol | Var (salt+HASH, role) | **Yok** — sadece RTDB **Database Secret** (query-string `?auth=`) |
| Oturum zaman aşımı | 30 dk zamanlayıcı | Yok |
| Telegram 2FA / oturum iptali | Var | Yok |
| TLS zorunluluğu | — | **`usesCleartextTraffic: true`** + LAN HTTP |
| Secret güvenliği | Yerel | Secret her istekte URL'de tekrarlanır (sniff riski) |

### 4.5 Veri Sahipliği / Tutarlılık Sorunları

1. **Kritik stok alan uyumsuzluğu:** Dashboard `minSeviye` (StoklarScreen:80), Raporlar `kritikSeviye` (RaporlarScreen:159) kullanıyor → RTDB'de tutarsız alan adı (masaüstü `StokKart.MinSeviye`).
2. **Cari birleştirme eksik:** `handleCariBirlestir` (AraclarScreen:212) yalnızca `borc/alacak` toplar; `CariHareketler` kayıtlarını **taşımıyor**; masaüstünde tam taşıma yapılıyor.
3. **Sipariş→Fatura dönüşümü atomik değil:** 5 ayrı yazma; arada hata → kısmi kayıt (masaüstünde tek transaction).
4. **Kanban düğüm adı:** Görevler `Notes` altında tutuluyor; masaüstü `Notes`'u farklı amaçla (not) kullanıyor → **çakışma riski**.
5. **Portföy adlandırma:** `PortfoyKart` vs `Portfolyo` tutarsızlığı raporlanmış.
6. **Kur simülasyonu:** Mobil "Canlı Kurlar" sabit taban + random sapma; masaüstü TCMB/API gerçek verisi.
7. **Sistem Sağlığı simülasyonu:** Mobil random; masaüstü gerçek.
8. **Yedekleme kapsamı:** Mobil yedek yalnızca `FirmaProfili`+`SistemAyarlari`'nı kapsar; **iş verisi yedeklenmiyor**.
9. **Müşteri Karlılık:** Mobil sabit `ciro*0.7` maliyet varsayımı kullanıyor (masaüstünde gerçek `OrtalamaAlisFiyati` ile hesap).

### 4.6 Özellik Eksiklikleri (işlevsel)

| Özellik | Masaüstü | Mobil |
|---|---|---|
| Offline çalışma + senkron kuyruğu | SQLCipher + kuyruk | Yok (tam çevrimiçi) |
| Gerçek zamanlı | Anlık yerel + Firebase | 2.5 sn polling (akü/ağ maliyeti) |
| Excel `.xlsx` dışa aktarım | ClosedXML | Sadece CSV |
| E-posta PDF / WhatsApp | MailKit + klavye sim | Sadece `expo-sharing` |
| Mali yıl transferi | `YearTransferService` | Yok |
| Otomatik yedek | `BackupService` zamanlayıcı | Manual, kapsam dar |
| Tests | 70+ xUnit dosyası | **0 test** |

---

## 5. Önerilen Geliştirme Planı (Mobil İçin, Öncelik Sırasına Göre)

### Faz 1 — Temel Parite (yüksek etki)
1. **PDF altyapısını netleştirmek:** Rapor PDF endpoint isimlerini repo'daki `ErmayMuhasebe.Functions` (`/generate/consolidated`, `/generate/ekstre`, `/generate/stok-list`, `/generate/cek-list` …) ile birebir eşle. `pdfService.ts` içindeki `_report` endpoint'lerini düzelt. PDF sunucusu URL'sini ayarlanabilir/prod adresine taşı.
2. **Kimlik doğrulama:** `?auth=secret` yerine en azından RTDB kuralları + rol/izlemeyi destekleyen bir auth katmanı; secret'ı AsyncStorage'da tutmaya devam ama isteklerde header/kısa ömürlü token yönetimi.
3. **Kritik stok alan tutarlılığı:** `kritikSeviye` → `minSeviye` (masaüstü şemasıyla aynı).
4. **Kanban:** Görevleri `Notes` yerine `Gorevler` (veya `KanbanTask`) düğümüne taşı.
5. **Cari birleştirme:** `CariHareketler`, faturalar ve alt kayıtları da taşı.

### Faz 2 — Rapor & Analiz Tamamla
- 16 eksik raporu masaüstü `RaporListViewModel` mantığını (özellikle FIFO yaşlandırma, DSO, ABC, LTV, nakit akışı, gelir tablosu) mobil TS'ye taşı.
- Müşteri Karlılık'ta sabit %70 yerine `ortAlisFiyati * satış miktarı` hesabı.

### Faz 3 — Verimlilik & Güvenlik (offline + push)
- `@react-native-async-storage` tabanlı basit local cache + `@react-native-community/netinfo` ile çevrimdışı senkron kuyruğu (işlem atomikliğini garanti etmek için yazma sırası).
- Expo Notifications ile masaüstü alarm gruplarının (kritik stok, vadesi geçmiş cari, çek/senet vade) mobil push'a taşınması.
- `usesCleartextTraffic` devre dışı + PDF sunucusuna HTTPS.
- Polling aralığını ekran odaklanmasına göre akıllandır (foreground'da 2.5s, background'da durdur).

### Faz 4 — Tam parite (istenirse)
- Bütçe planlama, müşteri limit yönetimi, borç hatırlatıcı, stok sayım, fatura/barkod tasarımı, Excel `.xlsx` (e.g. `xlsx` npm), MailKit yerine e-posta/WhatsApp paylaşım, mali yıl transferi, mobil test kurulumu (Jest + React Native Testing Library).

---

## 6. Kritik Riskler Özeti

1. **PDF rapor endpoint uyumsuzluğu** — mobil rapor PDF'leri mevcut Functions API ile eşleşmiyor.
2. **Güvenlik zafiyetleri** — DB Secret URL'de, cleartext trafik, kullanıcı/rol yok (masaüstü seviyesinden çok geride).
3. **Veri tutarlılığı** — kritik stok alan adı, Kanban düğümü, cari birleştirme mantığı uyumsuz.
4. **Atomiklik yok** — dönüşüm/senkronda kısmi kayıt riski.
5. **Bağımlılık** — PDF için LAN içi sabit IP'li harici sunucu (prod'da kırılır).
6. **Test yok** — mobil kritik finans mantığı doğrulanmamış.

---

*Rapor, kaynak kod üzerinde otomatik ve detaylı inceleme sonucu oluşturulmuştur. Rapor istenen sıradaki geliştirme aşaması: Faz 1'in uygulanması.*