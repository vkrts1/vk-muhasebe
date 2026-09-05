# Repository Pattern Implementation

## 📋 Genel Bakış

Bu klasör, **Repository Pattern** implementasyonunu içerir. Veritabanı işlemlerini düzenli ve bakımı kolay bir yapıya dönüştürür.

## 🏗️ Yapı

```
Repositories/
├── IRepository.cs              # Generic repository interface
├── BaseRepository.cs           # Base repository implementation
├── IUnitOfWork.cs              # Unit of Work pattern interfaces
├── UnitOfWork.cs               # Unit of Work implementation
├── CariRepository.cs           # Cari işlemleri
├── StokRepository.cs           # Stok işlemleri
├── FaturaRepository.cs         # Fatura işlemleri
├── BankaRepository.cs          # Banka işlemleri
└── KasaRepository.cs           # Kasa işlemleri
```

## 📚 Repository'ler

### 1. CariRepository
**Sorumluluk**: Cari hesap işlemleri

**Metodlar**:
- `GetAllAsync()` - Tüm carileri getirir
- `GetByIdAsync(int id)` - ID'ye göre cari getirir
- `SaveAsync(CariKart entity)` - Cari kaydeder/günceller
- `DeleteAsync(CariKart entity)` - Cari siler (soft delete)
- `GetByTurAsync(string tur)` - Türe göre carileri getirir
- `GetHareketsizCarilerAsync(int month)` - Hareketsiz carileri getirir
- `GetHareketlerAsync(int cariId)` - Cari hareketlerini getirir

### 2. StokRepository
**Sorumluluk**: Stok işlemleri

**Metodlar**:
- `GetAllAsync()` - Tüm stokları getirir
- `GetByIdAsync(int id)` - ID'ye göre stok getirir
- `SaveAsync(StokKart entity)` - Stok kaydeder/günceller
- `DeleteAsync(int id)` - Stok siler (hard delete + cascade)
- `GetByKodAsync(string kod)` - Koda göre stok getirir
- `GetByBarkodAsync(string barkod)` - Barkoda göre stok getirir
- `GetByKategoriAsync(string kategori)` - Kategoriye göre stokları getirir
- `GetKritikStoklarAsync()` - Kritik seviyedeki stokları getirir

### 3. FaturaRepository
**Sorumluluk**: Fatura işlemleri

**Metodlar**:
- `GetAllAsync()` - Tüm faturaları getirir
- `GetByIdAsync(int id)` - ID'ye göre fatura getirir
- `SaveAsync(Fatura entity)` - Fatura kaydeder/günceller
- `GetByNoAsync(string faturaNo)` - Fatura numarasına göre getirir
- `GetByCariIdAsync(int cariId)` - Cari'ye göre faturaları getirir
- `GetByTurAsync(string tur)` - Türe göre faturaları getirir
- `GetDetaylarAsync(int faturaId)` - Fatura detaylarını getirir
- `SaveWithDetailsAsync(Fatura, List<FaturaDetay>)` - Fatura ve detaylarını birlikte kaydeder

### 4. BankaRepository
**Sorumluluk**: Banka işlemleri

**Metodlar**:
- `GetAllAsync()` - Tüm bankaları getirir
- `GetByIdAsync(int id)` - ID'ye göre banka getirir
- `SaveAsync(BankaKart entity)` - Banka kaydeder/günceller
- `GetHareketlerAsync(int bankaId)` - Banka hareketlerini getirir
- `GetBakiyeAsync(int bankaId)` - Banka bakiyesini hesaplar

### 5. KasaRepository
**Sorumluluk**: Kasa işlemleri

**Metodlar**:
- `GetAllHareketlerAsync()` - Tüm kasa hareketlerini getirir
- `GetHareketByIdAsync(int id)` - ID'ye göre hareket getirir
- `SaveHareketAsync(KasaHareket hareket)` - Hareket kaydeder
- `GetBakiyeAsync()` - Kasa bakiyesini hesaplar
- `GetHareketlerByTarihAsync(DateTime, DateTime)` - Tarih aralığına göre getirir

## 🔄 Unit of Work Pattern

**UnitOfWork** sınıfı, tüm repository'leri tek bir yerden yönetmeyi sağlar:

```csharp
var uow = new UnitOfWork(dbService);

// Tüm repository'lere erişim
var cariler = await uow.Cariler.GetAllAsync();
var stoklar = await uow.Stoklar.GetAllAsync();
var faturalar = await uow.Faturalar.GetAllAsync();

// Transaction yönetimi
await uow.BeginTransactionAsync();
try
{
    await uow.Cariler.SaveAsync(cari);
    await uow.Stoklar.SaveAsync(stok);
    await uow.SaveChangesAsync();
    await uow.CommitTransactionAsync();
}
catch
{
    await uow.RollbackTransactionAsync();
}
```

## 💡 Kullanım Örnekleri

### Basit Kullanım
```csharp
// Repository'yi oluştur
var dbService = new DatabaseService();
await dbService.InitializeAsync();

var cariRepo = new CariRepository(dbService);

// Cari ekle
var yeniCari = new CariKart
{
    Unvan = "Test A.Ş.",
    Tur = "Alici"
};
int id = await cariRepo.SaveAsync(yeniCari);

// Cari getir
var cari = await cariRepo.GetByIdAsync(id);

// Tüm carileri getir
var tumCariler = await cariRepo.GetAllAsync();
```

### UnitOfWork ile Kullanım
```csharp
var dbService = new DatabaseService();
await dbService.InitializeAsync();

var uow = new UnitOfWork(dbService);

// Birden fazla işlem
var cari = await uow.Cariler.GetByIdAsync(1);
var stok = await uow.Stoklar.GetByKodAsync("STK001");

// Yeni fatura oluştur
var fatura = new Fatura { ... };
var detaylar = new List<FaturaDetay> { ... };
await uow.Faturalar.SaveWithDetailsAsync(fatura, detaylar);
```

## ✅ Avantajlar

1. **Düzenli Kod**: Her tablo için ayrı dosya
2. **Kolay Bakım**: Değişiklikler izole edilmiş
3. **Test Edilebilirlik**: Her repository ayrı test edilebilir
4. **Genişletilebilirlik**: Yeni repository'ler kolayca eklenebilir
5. **Separation of Concerns**: Her repository kendi sorumluluğunda

## 🔄 Migration (Geçiş)

Mevcut `DatabaseService` hala çalışıyor. Yeni kod için repository'leri kullanabilirsiniz:

**Eski Yol**:
```csharp
await dbService.SaveCariKartAsync(cari);
```

**Yeni Yol**:
```csharp
var cariRepo = new CariRepository(dbService);
await cariRepo.SaveAsync(cari);
```

## 📝 Notlar

- Repository'ler `DatabaseService`'i kullanır (dependency injection)
- Bulut senkronizasyonu otomatik yapılır
- Soft delete desteklenir (Cari, Stok, Fatura)
- Hard delete desteklenir (Banka, Kasa)

## 🚀 Gelecek İyileştirmeler

- [ ] SiparisRepository
- [ ] TeklifRepository
- [ ] CekSenetRepository
- [ ] Daha fazla özel metod
- [ ] Caching desteği
- [ ] Pagination desteği

---

**Oluşturulma Tarihi**: 2025-01-27  
**Durum**: ✅ Temel implementasyon tamamlandı

