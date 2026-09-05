# Ermay Muhasebe — Masaüstü (Avalonia) ↔ Mobil (React Native/Expo) Parite Analizi ve Uygulama Planı (v2)

> Bu rapor **güncel kaynak kod üzerinde** doğrulanmıştır (2026-08-10).
> Amaç: Mobil uygulamayı masaüstü versiyonunun **özellik + veri şeması + iş mantığı**
> açısından birebir eşleniği hâline getirmek. Mobil, dokunmatik UX'i koruyarak
> masaüstündeki her modülü karşılamalıdır.
> Hedef cihaz: **iPhone 14 Pro Max (iOS)**.

---

## 1. Yöntem

- Masaüstü `ErmayMuhasebe.Avalonia` (66 View + Shared katman) ile mobil
  `ermaymuhasebe-mobil` (21 ekran + servisler) dosya dosya karşılaştırıldı.
- Masaüstü rapor listesi (`RaporListViewModel.GetAllReports` → 31 rapor) ile mobil
  rapor listesi (`RaporlarScreen.reports` → 35 rapor) birebir eşleştirildi.
- Masaüstü `ToolsViewModel` (11 araç) ile mobil `AraclarScreen` (13 sekme) eşleştirildi.
- Masaüstü `MainViewModel` menü yapısı, `SettingsViewModel` kategorileri ve
  `MainViewModel.RunAlertChecksAsync` (9 alarm grubu) mobil ile karşılaştırıldı.
- PDF altyapısı (`ErmayMuhasebe.Functions` → 21 endpoint) ve mobil `pdfService.ts`
  endpoint kullanımları doğrulandı.

---

## 2. Güncel Durum Özeti (Kodda Doğrulanmış)

| Boyut | Masaüstü | Mobil | Durum |
|---|---|---|---|
| View / Ekran | ~66 View | 21 ekran + 13 araç sekmesi | Çoğu eşlendi; **2 modül eksik** (Fatura Tasarımı, Isı Haritası), 6'sı kısmi |
| Rapor | 31 rapor (lokal QuestPDF) | 35 rapor (harici sunucu `generic`) | İsim paritesi tam (+4 fazla), **1 eksik**; PDF layout derinliği eksik |
| Dashboard | 14 KPI + grafik + tablolar | 14 KPI + veriye bağlı 2 SVG grafik | Tam |
| Araçlar | 11 araç | 13 sekme | Tam (+Kur Çevirici, Optimal Fiyat, Evrak No) |
| Finans kapalı devre | `FinansService.SaveTransactionAsync` atomik | `transactionService.ts` (evrakNo, refId, rollback) | Tam |
| Veri | SQLCipher SQLite (offline-first) | Firebase REST + **offline cache + yazma kuyruğu** | Mimari fark (bilinçli), offline eklenmiş |
| Gerçek zaman | Anlık + senkron kuyruğu | 2.5 sn REST polling | Kısmi (verimlilik) |
| PDF | **KARAR:** masaüstü de tek sunucu (api.bat) → Functions PDF API | Harici sunucu (21 endpoint eşleşiyor) | **iOS'ta kırılır (HTTP/ATS) → Faz 1'de düzeltilir** |
| Güvenlik | Kullanıcı/şifre/rol (yerel SHA256) | PIN kilidi + oturum zaman aşımı | **KARAR:** her ikisi de **Firebase Auth**'a geçer |
| Bildirim | 9 alarm grubu + Toast + Telegram | 9 alarm grubu + **uygulama içi panel** (push yok) | **KARAR:** FCM bulut push eklenecek |
| Yedek | Tam DB + JSON + otomatik + yıl devri | Yalnız `FirmaProfili` + `SistemAyarlari` JSON | **Kapsam çok dar** |
| Test | 70+ xUnit | 46 Jest (5 dosya) | Eşitleniyor |

**Daha önceki raporlarda "kritik" denilen, kodda artık çözülmüş olanlar:**
- Kanban `Notes` → `Gorevler` ✅, kritik stok `minSeviye` ✅, cari birleştirme tam taşıma ✅,
  Kasa = `Bankalar` + `kartTuru='Kasa'` ✅, Portföy = `PortfoyKartlari` ✅, Hedef = `SatisHedefleri` ✅,
  `transactionService` atomik finans ✅, rapor PDF endpoint'leri Functions'ta mevcut ✅,
  TCMB gerçek kur ✅, gerçek cihaz sağlığı ✅, belge arşivi gerçek yükleme ✅,
  Excel `.xlsx` dışa aktarım ✅, oturum kilidi (PIN) ✅, bildirim merkezi (9 grup) ✅,
  offline cache + yazma kuyruğu ✅.

---

## 3. Modül / Ekran Karşılaştırması — Eksik ve Kısmi Olanlar

### 3.1 TAMAMEN EKSİK (masaüstünde var, mobilde yok)

| # | Modül | Masaüstü kaynağı | Önem | Açıklama |
|---|---|---|---|---|
| 1 | **Fatura Tasarımı** | `FaturaTasarimView` | Yüksek | Logo, başlık, kağıt boyutu/yönü kişiselleştirip PDF'e yansıtma. Mobilde **hiç yok** (masaüstü `FaturaTasarimViewModel.Kaydet` şu an stub; tasarım kaydı + tek PDF motoruna yansıtma ikisinde de eklenecek). |
| 2 | **Finansal Isı Haritası raporu** | `RaporListViewModel` → `Finansal Isı Haritası` | Orta | Mobil rapor listesinde **tek eksik rapor**. |

> **Kapsam dışı (kullanıcı kararı):** Barkod Tasarımı ve Rota Planlama mobilde **eklenmeyecek** (masaüstünde de gerçek özellik değil / istenmiyor). Maliyet Merkezi (`MaliyetMerkeziDef`) masaüstünde yalnızca DB tablosudur, UI'sı yoktur → mobilde de eklenmeyecek.

### 3.2 KISMİ OLANLAR (varlar ama masaüstü seviyesinde değil)

| # | Modül | Masaüstü | Mobil durum | Eksik kısım |
|---|---|---|---|---|
| 5 | **Bütçe Planlama** | `ButcePlanlamaView` (yıllık/aylık/haftalık satış hedefi + hedef↔gerçekleşen + grafik) | `Araclar → hedef` yalnızca satış hedefi tutar | **KARAR:** Masaüstündeki yıllık→aylık→haftalık hedef dağılımı, gerçekleşen karşılaştırması ve grafiğin mobil eşleniği |
| 6 | **DB Bakım** | `DbBakimView` (tam yedek, geri yükleme, integrity) | Ayarlar'da yalnız `FirmaProfili`+ayarlar JSON dışa/içe aktarım | **İş verisi (faturalar, cariler, stok…) yedeklenmiyor/geri yüklenmiyor** |
| 7 | **Veri Temizlik** | `VeriTemizlikView` (silinen kayıt temizliği) | Ayarlar'da yalnız lokal önbellek + yazma kuyruğu silme | RTDB'deki soft-deleted kayıt temizliği |
| 8 | **Stok Grup Yönetimi** | `StokGrupDuzenleView` (grup tanımla/taşı/sil) | Stok kartında serbest metin `grup` alanı | Grup yönetim ekranı (grup CRUD + ürünleri gruplar arası taşıma) |
| 9 | **Mali Yıl / Yıl Devri** | `YearSelectionView` + `YearTransferService` (kapanış bakiyeleri devri) | Ayarlar'da aktif yıl değişimi (çok-yıllı RTDB şeması) | **Yıl devri işlemi**: önceki yıl kapanışının yeni yıla devri, devir raporu |
| 10 | **Kısayol Tuşları / Durum Çubuğu** | `KisayolTusuView`, `StatusBarService` | Yok (mobilde doğal karşılığı yok) | Kapsam dışı kabul edilebilir; tema/ölçek eşdeğeri mobilde mevcut |

### 3.3 TAM OLANLAR (parite sağlanmış)

Dashboard (14 KPI + grafik), Cariler (+hızlı tahsilat/ödeme, yaşlandırma, risk), Faturalar (+E-Arşiv, xlsx), Stoklar (+WAC), Finans (Kasa/Banka/Çek-Senet/KK/EFT, 5 sekme), Siparişler→Fatura, Teklifler→Sipariş, Vade Takip (+erteleme, takvim, ciro), Raporlar, Kanban (+personel atama, bitiş tarihi), Hesap Makinesi, Araçlar (13 sekme: kur, toplu fiyat, hedef, cari/ürün birleştirme, sistem sağlığı, gecikme faizi, risk puanlayıcı, portföy, belge arşivi, kur çevirici, optimal fiyat, evrak no), Müşteri Limit, Borç Hatırlatıcı, Stok Sayım, Bildirim Merkezi, Ayarlar, Oturum Kilidi (LockScreen).

---

## 4. Rapor Paritesi

- Masaüstü: **31 rapor**. Mobil: **35 rapor**.
- **Eksik (1):** `Finansal Isı Haritası`.
- **Mobilde fazla (5):** FIFO Maliyet Analizi, Müşteri LTV, Çek Risk Analizi, Senet Detay, Fiyat Listesi (Katalog). Bunlar masaüstündeki ilgili hesapların mobildeki uyarlamasıdır, sorun değil.
- **Derinlik farkı (kritik):** Masaüstü her raporu **özelleştirilmiş QuestPDF layout**u ile basar; mobil tüm raporları tek `/generate/generic` endpoint'ine (genel tablo→PDF) gönderir. Çıktı işlevsel ama masaüstündeki grafikli/zengin layout'tan düşüktür.
- **Doğrulama notu:** Rapordaki hesaplamaların (DSO, ABC, FIFO, LTV, Yaşlandırma, Nakit Akış, Gelir Tablosu, Kar-Zarar, Müşteri Karlılık-WAC) masaüstü `RaporListViewModel` hesabıyla birebir tuttuğu kod incelemesiyle doğrulanmalıdır; testlerin önemli bir kısmı bunu örtmektedir (46 Jest, analizUtils/alertService).

---

## 5. Altyapı ve Güvenlik Eksikleri

| Konu | Masaüstü | Mobil | Risk |
|---|---|---|---|
| **PDF üretimi** | **KARAR: tek altyapı** — masaüstü de `api.bat` PDF API sunucusunu kullanacak (yerel QuestPDF üretimi sunucuya devredilir) | Harici HTTP sunucu (`http://192.168.1.103:5244` varsayılan) | **KRİTİK — iPhone hedefi** |
| **iOS ATS** | — | `app.json`'da iOS `NSAppTransportSecurity` **tanımsız**; HTTP engellenir | **KRİTİK — PDF iPhone'da çalışmaz** |
| **Auth** | **KARAR: Firebase Auth** — kullanıcı adı/şifre, hem masaüstü hem mobil Firebase üzerinden | RTDB `?auth=` Database Secret (URL'de açık) | Yüksek |
| **Kullanıcı/rol** | Firebase kullanıcısı + rol | PIN + 30 dk kilit (kullanıcı/rol yok) | Orta |
| **Telegram 2FA / oturum iptali** | `SecuritySyncService` + Telegram | Yok | Orta |
| **Push bildirim** | Windows Toast + Telegram | Yalnız uygulama içi panel (`expo-notifications` kurulu değil) | Orta |
| **E-posta / WhatsApp paylaşım** | MailKit + klavye simülasyonu | Sadece `expo-sharing` | Düşük |
| **Otomatik yedek** | `BackupService` zamanlayıcı | Manuel + kapsam dar | Orta |
| **Yıl devri** | `YearTransferService` | Yok | Orta |
| **Gerçek zaman** | Anlık yerel + senkron | 2.5 sn polling (tüm abonelikler, arka planda bile) | Orta (akü/ağ) |
| **PDF server adres** | — | AsyncStorage'da ayarlanabilir (varsayılan LAN IP) | Prod'da kırılır |

---

## 6. Öncelikli Eksiklik Listesi (Çalışma Sırası)

### Yüksek Öncelik
1. **PDF stratejisi (tek altyapı)** — KARAR: `api.bat` ile başlatılan Functions PDF API sunucusu **tek PDF motoru**. Masaüstü de bu sunucuyu kullanacak (yerel QuestPDF üretimi sunucuya taşınır; masaüstü `PdfService` → HTTP istemcisi). Tasarım (layout) tek yerde: `ErmayMuhasebe.Functions/PdfService.cs`. Hedef iPhone için `app.json` iOS ATS: `NSAllowsLocalNetworking` + HTTPS (üretimde).
2. **Fatura Tasarımı** ekranı (logo/boyut/yön → RTDB `FaturaTasarimi` kaydı + tek PDF motorunda kullanım) — masaüstü `FaturaTasarimViewModel` stub'ı da gerçek uygulamaya dönüşür.
3. **Firebase Auth** — KARAR: masaüstü + mobil kullanıcı adı/şifre ile **Firebase Auth**; RTDB `?auth=` Database Secret'ın yerine güvenli token; rol eşlemesi.
4. **Bütçe Planlama** — masaüstü `ButcePlanlamaViewModel` mantığı: yıllık hedef → 12 ay dağıtım → haftalık detay, hedef↔gerçekleşen + grafik (mobil eşleniği).

### Orta Öncelik
5. **Finansal Isı Haritası** raporu (masaüstü mantığı → mobil).
6. **Tam veri yedekleme/geri yükleme** — masaüstü `DbBakimView` seviyesi (tüm RTDB dalları: `Fatura`, `Cari`, `Stok`, `Finans`, `PortfoyKartlari`…).
7. **Yıl Devri** işlemi (`YearTransferService` eşleniği: kapanış→devir→yeni yıl başlangıç bakiyeleri).
8. **Veri Temizlik** — soft-deleted kayıtların RTDB'den temizlenmesi.
9. **Push bildirim (FCM)** — KARAR: Firebase Cloud Messaging; 9 alarm grubu (kritik stok, vadesi geçen, çek/senet vade) gerçek push bildirimi (Android: FCM, iOS: APNs). `expo-notifications` kurulur.

### Düşük Öncelik
10. **Stok Grup Yönetimi** ekranı.
11. **Akıllı polling** — yalnız foreground'da, ekran aktifken 2.5s; arka planda durdur.
12. E-posta / WhatsApp paylaşım (`expo-mail-composer` + `expo-sharing`).

> **Kapsam dışı (kullanıcı kararı):** Barkod Tasarımı, Rota Planlama, Maliyet Merkezi UI, Telegram 2FA.

---

## 7. Uygulama Planı (Fazlar)

> Her faz sonu doğrulama: `npm run typecheck` (tsc --noEmit) + `npm test` (jest) + `npx expo export --platform android` (+ iOS'ta `--platform ios`).

### Faz 1 — PDF Tek Altyapı (yüksek öncelik, iOS hedefi)
- **1.1** **Tek PDF motoru**: `ErmayMuhasebe.Functions` (api.bat) tek kaynak. Masaüstü yerel QuestPDF üretimi → sunucuya taşınır; masaüstü `PdfService` → HTTP istemcisi (localhost:5244). Mobil `pdfService.ts` aynı endpoint'lere bağlı kalır. Tüm `/generate/*` (fatura, teklif, sipariş, makbuz, ekstre, stok-list, stok-hareket, generic…) her iki tarafta aynı tasarımla çıkar.
- **1.2** **iOS erişimi**: `app.json` ATS `NSAllowsLocalNetworking` + `NSExceptionDomains` (LAN IP / `192.168.1.103`), üretimde HTTPS; `usesCleartextTraffic` Android ayarı.
- **1.3** **Fatura Tasarımı**: RTDB `FaturaTasarimi` düğümü (logo, başlık, boyut, yön) + tek PDF motorunda `FaturaRequest`'e yansıtma; masaüstü `FaturaTasarimView` stub'ı gerçek uygulamaya dönüşür.

### Faz 2 — Auth (Firebase) & Güvenlik
- **2.1** **Firebase Auth**: masaüstü + mobil e-posta/şifre ile giriş; `firebase-auth` (mobile) ve Firebase Admin/identity API (desktop); kullanıcı → rol eşlemesi.
- **2.2** RTDB güvenlik kuralları + Database Secret'ın isteklerde URL yerine güvenli kullanımı (header token / admin SDK); `?auth=` kalıntılarının temizliği.
- **2.3** Oturum/kilit akışının Firebase kimliğine bağlanması (mevcut PIN kilidi korunur).

### Faz 3 — Bütçe & Veri Yönetimi
- **3.1** **Bütçe Planlama**: masaüstü `ButcePlanlamaViewModel` eşleniği — yıllık hedef, aylık dağılım, haftalık detay, hedef↔gerçekleşen (satış faturalarından), grafik (mobil chart).
- **3.2** **Tam yedekleme/geri yükleme**: tüm RTDB dallarının JSON dışa/içe aktarımı (mevcut `FirmaProfili`-yalnız kısıtının kaldırılması) + dosya olarak paylaşım.
- **3.3** **Yıl Devri**: seçilen yılın kapanış bakiyelerinin yeni yıla devri + devir raporu (masaüstü `YearTransferService` mantığı).
- **3.4** **Veri Temizlik**: silinen (isDeleted) kayıtların RTDB'den kalıcı temizliği.

### Faz 4 — Rapor & Analiz Tamamlama
- **4.1** **Finansal Isı Haritası** raporu (günlük finansal aktivite yoğunluğu, masaüstü mantığı).
- **4.2** Rapor hesaplarının masaüstü `RaporListViewModel` ile satır satır doğrulaması (DSO/ABC/FIFO/LTV/Yaşlandırma/Nakit Akış/Gelir Tablosu/Kar-Zarar/Müşteri Karlılık-WAC) + eksik kalanların `analizUtils.ts`'e taşınması + Jest.
- **4.3** Rapor PDF layout derinliği (opsiyonel): en çok kullanılan raporlar için `generic` yerine özelleştirilmiş çıktı (tek PDF motorunda).

### Faz 5 — Bildirim (FCM) & Polling
- **5.1** **Push bildirim (FCM)**: `expo-notifications` + Firebase Cloud Messaging; 9 alarm grubunun (kritik stok, vadesi geçen, çek/senet vade, borç hatırlatıcı…) gerçek push bildirimine dönüşmesi; Android FCM / iOS APNs yapılandırması.
- **5.2** **Akıllı polling** (foreground/background).

### Faz 6 — Stok Grup & Paylaşım
- **6.1** **Stok Grup Yönetimi** ekranı.
- **6.2** E-posta/WhatsApp paylaşım.

### Faz 7 — Test & Kapanış
- Jest test yelpazesini yeni modüller için genişletme; `tsc --noEmit`, `expo export` (android + ios), gerçek cihaz testi (iPhone 14 Pro Max + Expo Go / dev build); masaüstü PDF sunucu entegrasyon testi.

---

## 8. Onaylanan Kararlar (2026-08-10)

| # | Konu | Karar |
|---|---|---|
| 1 | **PDF stratejisi** | **Tek altyapı**: `api.bat` (ErmayMuhasebe.Functions PDF API) tek PDF motoru; **masaüstü de bu sunucuyu kullanacak** (yerel QuestPDF üretimi sunucuya taşınır). Tek tasarım tek yerde: Functions `PdfService.cs`. |
| 2 | **Bütçe Planlama kapsamı** | Masaüstü `ButcePlanlamaViewModel` eşleniği: yıllık→aylık→haftalık satış hedefi + hedef↔gerçekleşen + grafik. Maliyet merkezi UI'sı eklenmez (masaüstünde de UI yok). |
| 3 | **Push bildirim** | **FCM bulut push** (`expo-notifications` + Firebase Cloud Messaging; Android FCM / iOS APNs). |
| 4 | **Auth** | **Kullanıcı adı + şifre**; **hem masaüstü hem mobil Firebase** üzerinden. |
| 5 | **Öncelik sırası** | Faz 1 = PDF tek altyapı + iOS erişimi + Fatura Tasarımı (onaylı başlangıç). |
| 6 | **Kapsam dışı** | Barkod Tasarımı, Rota Planlama, Maliyet Merkezi UI, Telegram 2FA. |

## 9. Emek Tahmini (kabaca)

- Faz 1 (PDF tek motor + iOS + Fatura Tasarımı): ~3–4 gün
- Faz 2 (Firebase Auth + RTDB güvenlik): ~2–3 gün
- Faz 3 (Bütçe + Yedek + Yıl Devri + Temizlik): ~3–4 gün
- Faz 4 (Isı Haritası + rapor doğrulama): ~2–3 gün
- Faz 5 (FCM push + Akıllı polling): ~2–3 gün
- Faz 6 (Stok Grup + Paylaşım): ~1–2 gün
- Faz 7 (Test + cihaz doğrulama): ~1–2 gün
**Toplam: ~14–21 gün** (tam zamanlı; fazlar bağımsız onaylanabilir)

---

## 10. Mevcut Doğrulama Komutları

```
npm run typecheck   # tsc --noEmit
npm test            # jest (46 test)
npx expo export --platform android
npx expo export --platform ios
```

> Masaüstü PDF sunucu entegrasyonu sonrası doğrulama: `api.bat` çalışır durumda iken masaüstünde herhangi bir rapor/fatura PDF üretimi ve `http://localhost:5244/health`.
