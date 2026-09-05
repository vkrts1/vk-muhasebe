# Web Versiyonunu Masaüstü ile Birebir Eşleştirme Planı

## 🎯 Hedef
Blazor Web uygulamasını Avalonia Desktop uygulaması ile birebir aynı hale getirmek.

## ✅ Tamamlanan Adımlar

### 1. Design System Oluşturuldu
- ✅ `design-system.css` oluşturuldu
- ✅ Avalonia'daki tüm renkler CSS variables olarak tanımlandı
- ✅ Fluid gradients, glassmorphism, shine effects eklendi
- ✅ Premium button styles, badges, cards hazırlandı
- ✅ Inter font eklendi (Avalonia ile aynı)

## 📋 Yapılacak Adımlar

### 2. ViewModels'leri Shared'a Taşıma
**Öncelik: Yüksek**

#### 2.1 Base ViewModel
- ✅ `ViewModelBase.cs` oluşturuldu (Shared/ViewModels/)

#### 2.2 Core ViewModels (Öncelikli)
Şu ViewModels'leri taşıyacağız:

1. **DashboardViewModel** 
   - Kaynak: `ErmayMuhasebe.Avalonia/ViewModels/DashboardViewModel.cs`
   - Hedef: `ErmayMuhasebe.Shared/ViewModels/DashboardViewModel.cs`
   - Bağımlılıklar: DatabaseService, ThemeService, LiveCharts
   
2. **FaturaListViewModel**
   - Kaynak: `ErmayMuhasebe.Avalonia/ViewModels/FaturaListViewModel.cs`
   - Hedef: `ErmayMuhasebe.Shared/ViewModels/FaturaListViewModel.cs`
   - Bağımlılıklar: DatabaseService, ExcelService, PdfService

3. **CariListViewModel**
   - Kaynak: `ErmayMuhasebe.Avalonia/ViewModels/CariListViewModel.cs`
   - Hedef: `ErmayMuhasebe.Shared/ViewModels/CariListViewModel.cs`

4. **StokListViewModel**
   - Kaynak: `ErmayMuhasebe.Avalonia/ViewModels/StokListViewModel.cs`
   - Hedef: `ErmayMuhasebe.Shared/ViewModels/StokListViewModel.cs`

5. **FinansViewModel**
   - Kaynak: `ErmayMuhasebe.Avalonia/ViewModels/FinansViewModel.cs`
   - Hedef: `ErmayMuhasebe.Shared/ViewModels/FinansViewModel.cs`

**Not**: Avalonia-specific kodlar (Dispatcher, Messaging) platform-agnostic hale getirilecek.

### 3. Blazor Sayfalarını Güncelleme
**Öncelik: Yüksek**

#### 3.1 Ana Sayfa Mapping

| Avalonia View | Blazor Page | Durum |
|---------------|-------------|-------|
| DashboardView.axaml | Home.razor | ⏳ Beklemede |
| FaturaListView.axaml | FaturaList.razor | ⏳ Beklemede |
| CariListView.axaml | CariList.razor | ⏳ Beklemede |
| StokListView.axaml | StokList.razor | ⏳ Beklemede |
| FinansView.axaml | Finans.razor | ⏳ Beklemede |

#### 3.2 Her Sayfa İçin Yapılacaklar
1. Shared ViewModel'i inject et
2. Avalonia AXAML'deki layout'u Blazor'a çevir
3. Aynı renkleri, spacing'leri, font'ları kullan
4. Aynı button'ları, card'ları, panel'leri ekle
5. LiveCharts yerine web uyumlu chart kütüphanesi kullan (ApexCharts veya ChartJS)

### 4. Layout ve Navigation
**Öncelik: Orta**

#### 4.1 MainLayout Güncelleme
- Avalonia'daki sidebar tasarımını kopyala
- Aynı gradient'leri, hover effects'leri ekle
- Navigation menüsünü birebir eşleştir

#### 4.2 Theme Service
- ThemeService'i Shared'a taşı
- Blazor'dan kullanılabilir hale getir

### 5. Components
**Öncelik: Orta**

#### 5.1 Ortak Component'ler Oluştur
- PremiumButton.razor
- GlassPanel.razor
- StatCard.razor (Dashboard için)
- PremiumBadge.razor

### 6. Charts ve Grafikler
**Öncelik: Orta**

#### 6.1 Chart Kütüphanesi Seçimi
- **Seçenek 1**: ApexCharts (Önerilen - Modern, responsive)
- **Seçenek 2**: ChartJS (Hafif, basit)

#### 6.2 Chart Migration
- Avalonia'daki LiveCharts grafiklerini web chart'larına çevir
- Aynı renkleri, stilleri kullan

### 7. Testing ve Refinement
**Öncelik: Düşük**

#### 7.1 Visual Comparison
- Her sayfayı Avalonia ile yan yana karşılaştır
- Pixel-perfect eşleştirme yap

#### 7.2 Responsive Design
- Mobile ve tablet görünümlerini test et
- Gerekirse breakpoint'ler ekle

## 🔧 Teknik Notlar

### Platform-Specific Kod Yönetimi

```csharp
// Avalonia-specific
#if AVALONIA
using Avalonia.Threading;
#endif

// Blazor-specific
#if BLAZOR
using Microsoft.AspNetCore.Components;
#endif

// Shared kod
public class SharedViewModel : ViewModelBase
{
    // Ortak mantık buraya
}
```

### Dependency Injection

**Avalonia**:
```csharp
services.AddSingleton<DashboardViewModel>();
```

**Blazor**:
```csharp
builder.Services.AddScoped<DashboardViewModel>();
```

## 📊 İlerleme Takibi

- [x] 1. Design System Oluşturma (1/1) ✅
- [x] 2. ViewModels'leri Shared'a Taşıma (1/5) 🔄
  - [x] DashboardViewModel
  - [ ] FaturaListViewModel
  - [ ] CariListViewModel
  - [ ] StokListViewModel
  - [ ] FinansViewModel
- [x] 3. Blazor Sayfalarını Güncelleme (1/5) 🔄
  - [x] Home.razor (Dashboard)
  - [ ] FaturaList.razor
  - [ ] CariList.razor
  - [ ] StokList.razor
  - [ ] Finans.razor
- [ ] 4. Layout ve Navigation (0/2)
- [x] 5. Components (1/4) 🔄
  - [x] SimpleBarChart.razor
  - [ ] PremiumButton.razor
  - [ ] GlassPanel.razor
  - [ ] StatCard.razor
- [ ] 6. Charts ve Grafikler (0/2)
- [ ] 7. Testing ve Refinement (0/2)

**Toplam İlerleme**: 4/21 (%19)

## 🚀 Sonraki Adım

**ŞİMDİ YAPILACAK**: DashboardViewModel'i Shared'a taşıyarak başlayacağız.

---

**Son Güncelleme**: 2026-01-22
**Durum**: Aktif Geliştirme
