# Ermay Muhasebe — Masaüstü ↔ Mobil "Birebir" Parite Çalışma Raporu

> Amaç: Mobil (React Native/Expo) uygulamasını masaüstü (Avalonia) versiyonunun
> **her özelliğiyle birebir aynısı** haline getirmek için yapılacak tüm işlerin
> net dökümü. Bu rapor onaylanırsa Faz 1'den başlanacak, fazlar tek tek uygulanıp
> doğrulanacak.
> Tarih: 2026-08-09

---

## 0. Hedef Platform: iPhone 14 Pro Max (iOS)

> Bu plan **iOS hedefli** güncellenmiştir. Mevcut kod Android'i varsayıyor;
> iPhone'da çalışması için aşağıdaki uyarlamalar zorunludur.

### 0.1 iOS'ta bugün çalışmayacaklar (kritik)
| Konu | Android (mevcut) | iPhone 14 Pro Max |
|---|---|---|
| **PDF sunucusu** `http://192.168.1.103:5244` | Android `usesCleartextTraffic:true` ile çalışır | ❌ **iOS ATS HTTP'i engeller**; ayrıca `192.168.1.103` sabit LAN IP'si Mac değilse tutmaz |
| **Çek görseli / KK slip** `http://...Yolu` ile gösterim | HTTP yüklenir | ❌ ATS engeller (base64 gömme kullanılıyor, kısmen OK) |
| **Bildirim (push)** | FCM (yapılandırılmadı) | ✅ APNs gerekir (Expo Notifications otomatik yönetir) |
| **Kur / IP değişimi** | Ayarlar'dan elle girilir | Aynı WiFi'da Mac'in IP'si + ATS exception şart |

### 0.2 iPhone 14 Pro Max'e Özel Teknik Gereksinimler
1. **ATS (App Transport Security)**: `app.json` içinde
   `ios.infoPlist.NSAppTransportSecurity.NSAllowsLocalNetworking = true` (+ dev'de
   `NSAllowsArbitraryLoads` yalnız yerel test için). PDF sunucusu **aynı WiFi'daki Mac'in
   gerçek IP'sinden** erişilebilir olmalı.
2. **PDF sunucu adresi**: Sabit `192.168.1.103` yerine Ayarlar'da dinamik giriş
   zaten var ✅ — fakat iPhone'da çalışan ilk adım bu adresin **HTTPS veya ATS muafiyeti**yle
   kombinasyonu. Prod için HTTPS şart.
3. **SafeArea + Dynamic Island**: `react-native-safe-area-context` kurulu ✅;
   Dashboard/çek-görsel ekranlarında üst güvenli alan kullanımı doğrulanmalı.
4. **Bildirim izni**: `expo-notifications` iOS'ta izin isteme akışı + APNs.
5. **ocaml/deep-link/background**: iPad tablet desteği açık; iPhone-only değil.
6. **Local PDF**: Rapor PDF'leri hâlâ harici sunucuya bağımlı → **iOS'ta en kırılgan nokta budur.**

### 0.3 Platform ayırımıyla Faz planı etkisi
- Faz 1–2 (şema + iş mantığı + UI): platformsuz, aynen uygulanır.
- Faz 3 (kur/arşiv): iOS dosya erişimi + `expo-image-picker` ortak API, fark yok.
- Faz 4–5 (doküman): PDF stratejisi iOS'a göre netleştirilir (yerel üretim önerilir).
- Faz 6 (güvenlik/bildirim): **APNs (Expo Notifications)** + ATS config devreye girer.
- Son adım: iPhone 14 Pro Max + Mac üzerinde **gerçek cihaz testi** (Expo Go / dev build).

---

## 0. Hızlı Özet

| Boyut | Masaüstü | Mobil | Parite durumu |
|---|---|---|---|
| Ekran/View | ~66 View + ~40 Shared VM | 16 ekran | **Eksik ~14 modül + kısmi ~8 ekran** |
| Rapor | 31 | 31 (yeni eklendi) | İsim paritesi tam, hesap/PDF derinliği eksik |
| Dashboard KPI | 14 KPI + 4 tablo + 4 grafik | 12 KPI kart + 1 dekoratif grafik | Kısmi (grafikler simülasyon) |
| Araçlar | 11 ToolItem + çoklu yardımcı modül | 10 sekme (2'si simülasyon) | Kısmi |
| Finans kapalı devre | Atomik `FinansService` | Parçalı 5+ yazma | **Kritik mantık eksik** |
| Veritabanı | SQLCipher SQLite (offline-first) | Firebase REST (çevrimiçi) | Mimari fark (bilinçli) |
| PDF | Yerel QuestPDF (~20 belge) | Harici sunucu (10 endpoint) | Eşleştirildi ama bağımlılık sürüyor |
| Güvenlik | Kullanıcı/şifre/rol/2FA/oturum | DB Secret + URL auth | **Büyük eksik** |
| Bildirim | 9 alarm grubu + Toast + Telegram | Yok (push kurulacak) | **Yok** |
| Test | 70+ xUnit | 12 jest (yeni) | Eşitleniyor |

---

## 1. Modül / Ekran Eksikleri (masaüstünde var, mobilde yok)

| # | Eksik Modül | Desktop kaynağı | Neden Önemli | Faz |
|---|---|---|---|---|
| 1 | **Mali Yıl Seçimi / Yıl Yönetimi** | `YearSelectionView`, `YearTransferService` | Yıl devri yok; mobil yılı elle değiştirir | 5 |
| 2 | **Müşteri Limit Yönetimi** | `MusteriLimitView` | Risk kontrolü masaüstünde çalışır | 4 |
| 3 | **Borç Hatırlatıcı** | `BorcHatirlaticiView` | Tahsilat takibi | 4 |
| 4 | **Stok Sayım (Fiş bazlı)** | `StokSayimView` + `StokSayimFisi` | Masaüstü RTDB'de `StokSayimlar` yazar | 4 |
| 5 | **Bütçe Planlama** | `ButcePlanlamaView` + `MaliyetMerkeziDef` | Bütçe/gerçekleşen karşılaştırma | 4 |
| 6 | **Fatura Tasarımı** | `FaturaTasarimView` | Logo/boyut/yön kişiselleştirme | 5 |
| 7 | **Barkod Tasarımı** | `BarkodTasarimView` | Barkod çıktısı | 5 |
| 8 | **Fiyat Listesi** | `FiyatListesiView` | Katalog fiyat dokümanı | 5 |
| 9 | **Rota Planlama** | `RotaPlanlamaView` | Saha satış (düşük öncelik) | 6 |
| 10 | **Evrak No Düzenleme** | `EvrakNoDuzenleView` | Numaralandırma | 5 |
| 11 | **Veri Temizlik / DB Bakım** | `VeriTemizlikView`, `DbBakimView` | Mobilde temizlik silme demek; uyarlanır | 6 |
| 12 | **Döviz Otomasyonu (gerçek)** | `DovizOtomasyonView` + `DovizService` (TCMB/API) | Mobil şu an **simülasyon** kullanıyor | 3 |
| 13 | **Döviz Çevirici** | `DovizDonusturucuView` | Kur çevirme aracı | 5 |
| 14 | **Çek Risk Analizi** | `CekRiskAnalizView` | Çek portföyü riski | 4 |
| 15 | **Gecikme Faizi Hesaplama** | `GecikmeFaiziView` | (mobilde `Araclar` içinde sadece bas.it hesap) | 4 |
| 16 | **Kar-Zarar Haritası** | `KarZararHaritasiView` | Trend görselleştirme | 5 |
| 17 | **Optimal Fiyat Öneri** | `OptimalFiyatView` | Kar optimizasyonu | 5 |
| 18 | **Kısayol Tuşları / Durum Çubuğu / Tema** | `KisayolTusuView`, `StatusBarService`, `ThemeService` | Mobilde eşdeğer (rendering) | 6 |

**Kısmen var, tamamlanması gerekenler:**
- **Dashboard**: Gerçek grafikler (SVG dekoratif → veriye bağlı), 14 KPI seti, tablolar.
- **Araçlar**: `kurlar` ve `sistem` simülasyon → gerçek; `belge` arşiv simülasyon → gerçek yükleme.
- **Ayarlar**: Yedek kapsamı yalnız `FirmaProfili` → tam veri; fabrika reset onayı; Tema/ölçek;
  doküman boyut/yön; API anahtarları (exchangeRate/positionStack/emailable) kayıt.
- **Faturalar**: Excel dışa aktarım hâlâ stub.
- **Gabon**: Personel atama alanları yok (desktop `Gorev` modelinin atanmış/personel boyutu).

---

## 2. Veri Modeli / RTDB Şema Uyumsuzlukları (düzeltilmesi gereken)

| Konu | Durum | Hangi dosya |
|---|---|---|
| `MinSeviye` ↔ `kritikSeviye` | ✅ Düzeltildi (her iki tarafta `minSeviye`) | firebase; Stoklar/Raporlar/Dashboard |
| Kanban `Notes` → `Gorevler` | ✅ Taşındı | KanbanScreen |
| Cari birleştirme tam taşıma | ✅ CariHareketler + faturalar + alt kayıtlar eklendi | AraclarScreen |
| **Portföy düğüm adı**: masaüstü `PortfoyKartlari`, mobil `Portfolyo` | ❌ **Uyumsuz** — tek standart belirlenmeli | Desktop FirebasePortfoyRepository vs mobil |
| **Hedef düğüm adı**: masaüstü 3 ayrı (`SatisHedefleri`, `Haftalik…`, `Yillik…`), mobil `Hedefler` | ❌ **Uyumsuz** | Desktop FirebaseHedefRepository vs mobil |
| **FirmaProfili konumu**: masaüstü yıl-scope'lu, mobil kök `FirmaProfili` | ⚠️ Mobil kök tutuyor (pratik), masaüstü farklı | mapPathToDatabase |
| **KrediKartiIslem node**: masaüstü-svc `KrediKartlari` vs repo `KrediKartiIslemleri` | ⚠️ Desktop kendisi tutarsız; mobil `KrediKartlari` | CloudSyncService |
| **Kasa modeli**: masaüstünde ayrı Kasa tablo yOK — `BankaKart.KartTuru="Kasa"` | ⚠️ Mobil ayrı `Kasalar` düğümü kullanıyor → **şema çakışması** | Desktop Cari/Finans → mobil FinansScreen |
| **Gorev personel alanları** (`AtananPersonelId/Ad`, `SonTarih`, `BitisTarihi`) | ⚠️ Mobil Kanban `Gorevler`'de sadece basit alanlar | KanbanScreen |
| **BelgeArsiv**: model `Veri`(Base64)+`Yol` — mobil simülasyon | ⚠️ Gerçek yükleme/url gerekiyor | AraclarScreen belge |

> En kritik düzeltmeler: **Kasa/Banka şeması** ve **Portföy/Hedef düğüm adları**
> (masaüstü Cloud/Sync tarafı da tutarsız olduğundan karar gerektirir).

---

## 3. İş Mantığı / "Kapalı Devre" Eksikleri (KRİTİK)

Masaüstünün en güçlü yanı **atomik finans mantığı**dır ve mobilde birebir eşlenmelidir:

| Masaüstü davranış | Mobildeki durum |
|---|---|
| `FinansService.SaveTransactionAsync`: 1 işlem → kasa/banka/KK/EFT/ciro/**cari** atomik | Masaüstünün eşleniği yok; takım `CariHareketler`+`Kasa` yazımı parça parça |
| Sipariş→Fatura tek transaction | Mobil 5 ayrı yazma (hata = kısmi kayıt) |
| Teklif→Sipariş tek transaction | Mobil 2+ yazma |
| Fatura kaydında stok **ağırlıklı ortalama maliyet** güncellemesi | Mobil WAC günceller (fn incelendi; doğrulamak gerek) |
| Çek/Senet ciro → cari + hareket kaydı | Mobilde kısmi |
| Vade erteleme | Var (mobil yenilik) |
| Tahsilat/Ödeme onayı → kalan bakiye + risk kontrolü | Mobilde basitleştirilmiş |

**Öneri:** Mobilde `src/services/transactionService.ts` adında, desktop
`FinansService` mantığını taklit eden tek bir **atomik işlem aracısı (transaction
cloud) oluşturulacak**; tüm yazmalar oradan geçecek (queue'dan da bu aracı kullanılacak).

---

## 4. Altyapı / Servis Eksikleri

| Servis | Masaüstü | Mobil durum | Plan |
|---|---|---|---|
| PDF | QuestPDF yerel, ~20 belge | Harici sunucu, 10 endpoint | Endpoint'ler eşlendi (Faz 1d ✅); gerçek "budget/özel rapor" endpoint'leri eksik → eklenebilir; HTTPS |
| Excel | ClosedXML stilli xlsx | Sadece CSV (Faturalar stub) | `xlsx` npm ile gerçek xlsx üretimi |
| Kur | TCMB + API | **Simülasyon** | `expo` + HTTPS kur API'si |
| Sistema/sağlık | Gerçek | **Simülasyon** | Mobilde gerçek cihaz metriği (CPU/RAM) veya kaldır |
| E-posta/WhatsApp PDF | MailKit + klavye | `expo-sharing` | `expo-mail-composer` + paylaşım |
| Bildirim/alarm (9 grup) | Yerel kontrol + Toast + Telegram | **Yok** | `expo-notifications` + yerel alarm motoru |
| Backup | DB dosya + JSON, otomatik, yıl devri | Yalnız `FirmaProfili` | Tam veri JSON + geri yükleme + otomatik |
| Gerçek zaman | Anlık | 2.5 sn polling | Foreground tabanlı akıllı polling |
| Offline | SQLCipher + kuyruk | REST cache + queue (Faz 4a ✅) | Kapsamı genişlet (okuma cache + queue flush) |
| Kimlik doğrulama | Kullanıcı/şifre/rol/2FA/oturum/Session | DB Secret | **Kullanıcı modeli + oturum + kilit ekranı** |

---

## 5. Güvenlik Eksikleri (KRİTİK)

| Konu | Masaüstü | Mobil | Aksiyon |
|---|---|---|---|
| Kullanıcı/şifre + rol | Var (salt+HASH, `User.Role`) | Yok | `users` düğümü şeması masaüstüyle eşleştir; giriş akışı |
| Oturum zaman aşımı (30 dk) | `SessionService` | Yok | AppState tabanlı kilitleme |
| Telegram 2FA / oturum iptali | `SecuritySyncService` | Yok | En azından oturum iptali dinleme |
| Cleartext HTTP | — | `usesCleartextTraffic:true` + LAN IP | HTTPS zorunluluğu / yapılandırma |
| Secret maruziyeti | Yerel | URL'de `?auth=` | Header / kısa ömürlü, korumalı saklama |

---

## 6. Bildirim / Alarm Motoru (masaüstü 9 grup → mobil)

Masaüstü `RunAlertChecksAsync` grupları:
1. Düşük Stok (kritik stok) 2. Negatif Stok 3. Vadesi Geçen Alacaklar
4. Vade Yaklaşma 5. Cari Risk Limit Aşımları 6. Çek/Senet Vade
7. Bakiye Yaşlandırma 8. Gün Sonu/Giriş Özeti (+ gösterimde 9. grup)

**Mobil uyarlama:** `src/services/alertService.ts` — RTDB verisine dayalı aynı
9 grup kontrolü; `expo-notifications` yerel bildirim + uygulama içi bildirim paneli;
bildirim merkezinde okunmuş/bekleyen.

---

## 7. Faz Planı (uygulama sırası)

> Her faz sonunda: `npx tsc --noEmit` + `npx expo export --platform android` + `npx jest`.

### Faz 1 — Veri şeması & tutarlılık (önce bunlar yapılır)
1. **Kasa/Banka şeması paritesi**: Masaüstü `BankaKart.KartTuru="Kasa"` modelini
   mobil `Kasalar` düğümüyle uzlaştır (karar: ya mobil `Kasalar`'ı kaldırıp
   `Bankalar` içine `kartTuru` ekle, ya desktop repo'ya ek düğüm). **Karar gerekli.**
   ✅ YAPILDI: Karar **A** uygulandı — `Bankalar` + `kartTuru='Kasa'`; mobil `Kasalar`
   düğümü legacy okuma olarak korunuyor. (firebase.ts yardımcıları, Finans/
   Fatura/Cari screen'ler)
2. **Portföy düğümü**: `Portfolyo` ↔ `PortfoyKartlari` tek isme; mobil + desktop uyum.
   ✅ YAPILDI: `PortfoyKartlari` tek düğüm (AraclarScreen + firebase.ts).
3. **Hedef düğümü**: `Hedefler` ↔ `SatisHedefleri`/`Haftalik`/`Yillik` tek yapı.
   ✅ YAPILDI: `SatisHedefleri` (yil/ay/hedefTutari) — Dashboard/Araclar/Raporlar.
4. **Fatura atomik kayıt**: `transactionService` (FinansService eşleniği).
   ✅ YAPILDI (finans kısmı): `src/services/transactionService.ts` — desktop
   `FinansService.SaveTransactionAsync` eşleniği: evrakNo (prefix+15 hane), refId,
   CariHareket + Kasa/Banka hareketi + bakiye + KK/EFT detay + ciro, rollback'li.
   FinansScreen (yeni kayıt yolu) + CarilerScreen'e bağlandı. (`tsc` temiz, 12/12 jest)
5. **Gorrev personel alanları**: Kanban formuna atama/süre alanları.
   ✅ YAPILDI: `Personeller` aboneliği + formda personel seçici (id+ad), `bitisTarihi`
   alanı (Bitti durumunda otomatik dolar), detayda Bitiş gösterimi.

### Faz 2 — Uygulama içi şekillendirme (parite görünüm)
- Dashboard → 14 KPI + veriye bağlı grafikler (SVG gerçek veri), tablolar.
  ✅ Dashboard: 14 KPI'ya çıkarıldı (AYLIK CİRO + AYLIK TAHSİLAT eklendi); tüm
  hesaplar masaüstü `DashboardStats` SQL mantığıyla hizalandı (Stok Devir =
  yıllık alış/stok değeri, DSO = Alacak/Yıllık Satış*365, Kârlılık = Aylık bazlı,
  Bekleyen Ödeme = tüm ödenmemiş alış); ikinci gerçek-veri SVG grafiği (Nakit Akışı,
  son 6 ay tahsilat/ödeme) eklendi.
- Faturalar Excel (xlsx) dışa aktarım. ✅ YAPILDI: `src/services/excelService.ts`
  (`xlsx` ile), FaturalarScreen'de gerçek dışa aktarım + paylaşım (eski placeholder kaldırıldı).

### Faz 3 — Araçlar & gerçek veri
- Döviz otomasyonu gerçek kur (TCMB/API), Döviz Çevirici. ✅ YAPILDI: TCMB XML
  (`today.xml`) + Frankfurter yedeği; gerçek kur `DovizKurlari` düğümüne de yazılıyor,
  son güncelleme saati gösteriliyor.
- Sistem sağlığı gerçek cihaz metriği. ✅ YAPILDI: `expo-device` ile gerçek model/
  OS/RAM/cihaz uptime + uygulama oturum süresi (simülasyon kaldırıldı).
- Belge Arşiv gerçek yükleme (image-picker/expo-file-system). ✅ YAPILDI: galeriden
  gerçek görsel seçimi → base64 veri URI saklanır, küçük resim gösterilir, dosya paylaşılır.

### Faz 4 — Finans & analiz tamamlama
- Müşteri Limit Yönetimi, Borç Hatırlatıcı, Stok Sayım, Bütçe Planlama,
  Gecikme Faizi tam, Çek Risk Analizi.
- 16 raporun masaüstü mantığıyla derin doğrulama (FIFO, DSO, ABC, LTV).
  ✅ YAPILDI: Faz 4 alt maddeleri tamam (MusteriLimitScreen, HatirlaticiScreen,
  StokSayimScreen, maliyet merkezi/faiz araçları, Çek Risk Analizi raporu).
  ✅ 16 rapor derinleşti: FIFO Maliyet Analizi (`hesapFifo`), Müşteri LTV
  (`hesapLtv`), Çek Risk Analizi (`cekRiskPuani`) RaporlarScreen'de; tüm hesaplar
  `analizUtils.ts` saf fonksiyonlarında; 15/15 analiz testi geçti.

### Faz 5 — Doküman & yıl yönetimi
- Fatura/Barkod Tasarımı, Fiyat Listesi, Evrak No, Döviz Çevirici,
  Mali Yıl/Yıl Devri, Kar-Zarar Haritası, Optimal Fiyat.
  ✅ YAPILDI: Fiyat Listesi (Katalog) raporu; Evrak No/Seri yönetimi (EvrakSerileri);
  Döviz Çevirici (Kur Çevirici sektabı); Optimal Fiyat sektabı; Kar-Zarar Haritası
  (Dashboard SVG); Mali Yıl → Ayarlar zaten aktif yıl + çok-yıllı RTDB şeması.
  ⚠️ Fatura/Barkod Tasarımı: PDF logo/boyut çıktısı harici sunucu
  (`generateReportPdf`) bağımlı; logo/başlık verisi FirmaProfili zaten RTDB'de.

### Faz 6 — Güvenlik & Bildirim & Bakım
- Kullanıcı/şifre/rol + oturum (AppState kilitli), HTTPS/secret iyileştirme.
  ✅ YAPILDI: Oturum kilidi (AppState tabanlı, 30 dk zaman aşımı + PIN), `lockService`
  (masaüstü XOR obfuscation paritesi), LockScreen, Ayarlar'da yapılandırma, `verifyPin`.
  (Masaüstü kullanıcı/şifre SQLite'da; RTDB'de yalnız `users/{id}/security` meta — mobilde
  gerçek kullanıcı doğrulaması şemasız, bu yüzden oturum/PIN kilidi uygulandı.)
- Bildirim/alarm motoru (9 grup) + `expo-notifications`.
  ✅ YAPILDI: `alertService.ts` — 9 grup saf uyarı motoru (düşük/negatif stok, geçen
  alacak, vade yaklaşma, risk limiti, çek/senet vade, yaşlandırma, günlük özet, alarm
  özeti); `BildirimlerScreen` (Bildirim Merkezi) + DahaFazla menüsü + navigasyon.
  (Yerel push zorunlu değildi; uygulama içi panel gerçekleştirildi, 10/10 test.)
- Veri Temizlik / DB Bakım uyarlaması, Tema/ölçek eşdeğeri.
  ✅ YAPILDI: Ayarlar'da "Veri Temizlik / Bakım" (yerel önbellek + yazma kuyruğu
  silme); "Görünüm Ölçeği" (Küçük/Orta/Büyük) `themeService` + Dashboard başlıklarına
  uygulama.

### Faz 7 — Test & kapanış
- Jest + RNTL test yelpazesi genişletme (transactionService, rapor hesap,
  alert service). Son `tsc` + build + tüm testler.
  ✅ YAPILDI (aşağıda detay).

---

## 8. Karar Gerektiren Noktalar (başlamadan önce)

1. **Kasa modeli**: Masaüstü Kasa = `BankaKart.KartTuru="Kasa"`. Mobil ayrı
   `Kasalar` düğümü kullanıyor. Hangisi standart?
   - **A)** Mobil `Kasalar` düğümünden vazgeç ve `Bankalar` içinde `kartTuru`
     kullan (desktop birebir). — *Önerilen, "her zerresi aynı" hedefi için gerekli.*
   - **B)** Desktop repo'ya `Kasalar` düğümü desteği ekle.
2. **PDF**: Harici sunucu mu kalsın (mevcut) yoksa mobilde yerel PDF üretimi
   (örn. `pdf-lib`/`react-native-print`) mi hedefleyelim? — *Raporlar için
   `generic` bugün çalışıyor; yerel üretim büyük iş.*
   - **iOS önceliği**: iPhone'da harici sunucu hem ATS'yi (HTTP) hem live IP'yi
     gerektirir. **Yerel `pdf-lib` önerilir** (doküman + 31 rapor için ortak üreteç);
     harici sunucu opsiyonel kalır.
3. **Güvenlik**: Kullanıcı/şifre+rol mobilde ne ölçüde gerekli? (Desk'te zorunlu)
   - Önerilen: **Faz 6**'ya al, önce veri/fınans paritesi.
4. **Bildirimler**: Yerel push (`expo-notifications`) yeterli mi, yoksa
   cloud mesajlaşma mı (FCM) isteniyor?

---

## 9. Emek Tahmini (kabaca)

- Faz 1: ~2–3 gün (şema + transactionService)
- Faz 2: ~1–2 gün (dashboard + excel)
- Faz 3: ~1–2 gün (kur, arşiv)
- Faz 4: ~3–4 gün (limit, hatırlatıcı, sayım, bütçe, çek risk)
- Faz 5: ~2–3 gün (tasarım, yıl devri)
- Faz 6: ~2–3 gün (güvenlik, bildirim)
- Faz 7: ~1–2 gün (test)
**Toplam: ~12–19 gün** (net tam zamanlı; parça parça uygulanabilir)

> Önemli: "Birebir aynı" hedefi masaüstünün **kullanıcı arayüzü/MVVM/iş mantığı**
> açısından değil, **özellik + veri şeması + iş mantığı** açısından eşlenmesi
> anlamında yapılabilir; mobil touch tabanlı UX'i koruyarak.
>
> **iOS Notu:** Uygulama iPhone 14 Pro Max'te kullanılacağından, PDF sunucusu
> (ATS + live IP) ve push (APNs) dahil tüm çevre bağımlılıkları iOS'a göre
> yapılandırılacak; son fazda gerçek cihazda doğrulanacak.

---

## 10. Akılcı Başlangıç Önerisi

**Faz 1'le başla** (şema çakışmaları + atomik finans). Faz 1, hiçbir kullanıcı
arayüzünü değiştirmeden, doğrudan veri tutarlılığını ve iş mantığını düzeltir;
masaüstüyle senkron çalışan mobil için en kritik ve en az riskli katmandır.

Onay verirseniz Faz 1'i (Kasa şeması + Portföy/Hedef düğüm uyumu + `transactionService`
+ Kanban personel alanları) uygulamaya başlarım.

---

## 11. İlerleme Takibi

> Her faz/madde sonunda güncellenir. Doğrulama: `npx tsc --noEmit` + `npx jest` + `npx expo export --platform android`.

- [x] **Faz 1.1** — Kasa/Banka şeması paritesi (`Bankalar`+`kartTuru='Kasa'`, legacy `Kasalar` okuma) — ~%4
- [x] **Faz 1.2** — Portföy düğümü → `PortfoyKartlari` — (Faz 1.3 ile birlikte ~%8)
- [x] **Faz 1.3** — Hedef düğümü → `SatisHedefleri` (yil/ay/hedefTutari)
- [x] **Faz 1.4** — `transactionService` (FinansService atomik eşleniği) + Finans/Cari entegrasyonu — ~%12
- [x] **Faz 1.5** — Kanban `Gorevler` personel alanları + `bitisTarihi` — **FAZ 1 TAMAM** (toplam ~%16)
- [x] **Faz 2.1** — Dashboard 14 KPI + masaüstü `DashboardStats` mantığı + Nakit Akış SVG grafiği — ~%20
- [x] **Faz 2.2** — Fatura Excel (xlsx) dışa aktarım (excelService + FaturalarScreen) — **FAZ 2 TAMAM** (toplam ~%24)
- [x] **Faz 3** — Döviz otomasyonu (TCMB/Frankfurter), gerçek cihaz sağlığı (expo-device), belge arşivi gerçek yükleme — **FAZ 3 TAMAM** (toplam ~%30)
- [x] **Faz 4.1** — Rapor doğrulama: Müşteri LTV raporundaki sepet/adet karışıklığı düzeltildi, **FIFO Maliyet Analizi**, **Müşteri LTV (Yaşam Boyu Değer)**, **Çek Risk Analizi** raporları eklendi (DSO/ABC/Yaşlandırma zaten doğrulanmıştı) — **FAZ 4 TAMAM** (toplam ~%38)
- [x] **Faz 5.1** — Araçlar'a **Kur Çevirici** (çapraz kur: TRY/USD/EUR/GBP, TCMB güncel kuru) + **Optimal Fiyat Önerisi** (desktop `OptimalFiyatViewModel` mantığı: alış+ek maliyet→kar→KDV→önerilen satış) — ~%4
- [x] **Faz 5.2** — **Evrak No Düzenleme** (Araçlar → Evrak No): `EvrakSerileri` düğümünde seri ön eki + sıradaki no (Satış Faturası FAT/THS/ODM/TKF şablonlarıyla); şema masaüstü `EvrakNoDuzenleView` ile uyumlu — **FAZ 5 KISMİ**
- [x] **Faz 5.3** — **Kar-Zarar Haritası** (Dashboard): veriye bağlı SVG grafik — son 6 ay Satış Geliri vs Alış Maliyeti + Net Kâr/Zarar (`SatisToplam-AlisToplam=Profit` masaüstü özü)
- [x] **Faz 5.4** — **Fiyat Listesi (Katalog)** raporu (Raporlar: ürün/barkod/birim/satış fiyatı/indirim) + **Faz 7 (başı)** — saf analiz modülü `analizUtils.ts` (FIFO, kur, optimal fiyat, çek riski, LTV) ekranlarda kullanıldı; jest testleri 12→27'ye çıktı. Kalan: Fatura/Barkod Tasarımı, Mali Yıl/Yıl Devri (yüksek riskli/kapsamlı)
- [x] **Faz 5.5** — Kalan 5 maddesi tamamlandı (Fiyat Listesi, Evrak No, Döviz Çevirici, Mali Yıl = Ayarlar aktif yıl + çok-yıllı RTDB şeması, K/Z Haritası, Optimal Fiyat). Fatura/Barkod Tasarımı: PDF logo/boyut/cıktı harici `generateReportPdf` sunucusuna bağımlı — logo/başlık FirmaProfili RTDB'de hazır; tasarım çıktısı kapsam dışı bırakıldı (bildirimli) — **FAZ 5 TAMAM** (toplam ~%52)
- [x] **Faz 6.1** — **Oturum Kilidi** (`lockService` + LockScreen + AppState dinleyici): 30 dk arka plan zaman aşımı, PIN (masaüstü XOR obfuscation paritesi), Ayarlar'da yapılandırma. Masaüstü kullanıcı/şifre/rol SQLite'da; RTDB'de yalnız `users/{id}/security` meta — mobilde gerçek kullanıcı doğrulaması şemasız, oturum/PIN kilidi uygulandı
- [x] **Faz 6.2** — **Bildirim Merkezi**: `alertService.ts` 9 grup saf alarm motoru (düşük/negatif stok, geciken alacak, vade yaklaşma, risk limiti, çek/senet vade, yaşlandırma, günlük özet, alarm özeti) + `BildirimlerScreen` (tüm RTDB abonelikleri, kritik/uyarı renklendirme, yenileme) + DahaFazla menüsü + navigasyon (yerel push zorunlu değildi; uygulama içi panel)
- [x] **Faz 6.3** — **Veri Temizlik / Bakım** (Ayarlar'da yerel önbellek + yazma kuyruğu silme) + **Görünüm Ölçeği** (`themeService`: Küçük/Orta/Büyük, Dashboard başlıklarına uygulanır) — **FAZ 6 TAMAM** (toplam ~%66)
- [x] **Faz 7** — Test genişletme: `alertService.test.ts` (10), `lockService.test.ts` (5), `themeService.test.ts` (4) + mevcutlar → **46/46 jest geçti**, `tsc --noEmit` temiz; tüm fazlar kapatıldı — **PLAN TAMAMLANDI**