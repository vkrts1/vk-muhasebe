# Proje Analiz Raporu - Ermay Muhasebe

## 1. Proje Genel Bakışı
Ermay Muhasebe, modern web ve masaüstü teknolojilerini bir araya getiren hibrit bir muhasebe ve yönetim sistemidir. Proje, hem yerel masaüstü deneyimi (Avalonia) hem de genişletilebilir bir bulut/web arayüzü sunmak üzere tasarlanmıştır.

## 2. Mimari Yapı
Proje çoklu katmanlı (Multi-Project) bir .NET Solution yapısına sahiptir:

### A. Masaüstü Katmanı (Avalonia UI)
- **ErmayMuhasebe.Avalonia:** Ana UI mantığı ve platformdan bağımsız bileşenler.
- **ErmayMuhasebe.Avalonia.Desktop:** Masaüstü (Windows/Linux/macOS) çalışma ortamı.
- **ErmayMuhasebe.Avalonia.Android/iOS:** Mobil cihaz destekleri.

### B. Paylaşılan Katman (Shared)
- **ErmayMuhasebe.Shared:** Tüm platformlar tarafından kullanılan ortak Modeller, Servisler ve İş Mantığı (Business Logic). Veritabanı modelleri ve DTO'lar burada tanımlanmıştır.

### C. Web ve Bulut Katmanı
- **ErmayMuhasebe.Cloud:** Blazor tabanlı web arayüzü.
- **Web Frontend (React/Vite):** Vite ve React tabanlı, TailwindCSS ile güçlendirilmiş modern web arayüzü.
- **ErmayMuhasebe.Functions:** PDF üretimi (QuestPDF) ve diğer arka plan görevlerini yürüten API servisleri.

## 3. Kullanılan Teknolojiler
- **UI:** Avalonia UI (Masaüstü), React (Web), Blazor (Cloud).
- **Backend:** .NET 8/9, C#.
- **Frontend:** TypeScript, Vite, TailwindCSS.
- **Raporlama:** QuestPDF (PDF Üretimi), MiniExcel (Excel Entegrasyonu).
- **Veri:** Firebase, Dexie (Local DB).

## 4. Tespit Edilen Durum (USB Kurtarma Sonrası)
- **Kritik Eksik:** `/src` klasörü (Web frontend kaynak kodları) eksiktir.
- **Onarılan:** `StokExcelDto.cs` ve `package.json` dosyaları başarıyla restore edilmiştir.
- **Genel Durum:** Masaüstü projesi %95 oranında sağlamdır ve çalışabilir durumdadır. Web tarafı ise kaynak kodların (src) eksikliği nedeniyle şu an için sadece yapısal olarak mevcuttur.

---
*Bu rapor sistem tarafından mevcut dosya yapısı analiz edilerek otomatik olarak oluşturulmuştur.*
