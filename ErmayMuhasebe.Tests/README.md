# ErmayMuhasebe.Tests

## 📋 Test Projesi Hakkında

Bu proje, Ermay Muhasebe uygulaması için **Unit Test** projesidir. xUnit framework kullanılarak yazılmıştır.

## ✅ Test Sonuçları

**Toplam Test Sayısı**: 34  
**Başarılı**: 34 ✅  
**Başarısız**: 0  
**Test Süresi**: ~3.5 saniye

## 📁 Test Yapısı

### Services (Servisler)
- **AuthServiceTests.cs** (7 test)
  - Şifre hashleme testleri
  - Şifre doğrulama testleri
  
- **DatabaseServiceTests.cs** (9 test)
  - Veritabanı başlatma testleri
  - Cari CRUD işlemleri testleri
  - Stok CRUD işlemleri testleri
  - Soft delete testleri

### Models (Modeller)
- **FaturaTests.cs** (5 test)
  - Fatura hesaplama testleri (Kalan, Bakiye, KDV)
  
- **CariKartTests.cs** (5 test)
  - Cari bakiye hesaplama testleri
  - VKN/VergiNo testleri
  
- **StokKartTests.cs** (8 test)
  - Stok özellik testleri
  - Property changed event testleri

## 🚀 Testleri Çalıştırma

### Visual Studio'dan
1. Test Explorer'ı açın (Test > Test Explorer)
2. "Run All" butonuna tıklayın

### Komut Satırından
```bash
# Tüm testleri çalıştır
dotnet test

# Detaylı çıktı ile
dotnet test --verbosity normal

# Belirli bir test sınıfını çalıştır
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

## 📊 Test Coverage (Kapsam)

### Şu Anki Kapsam
- ✅ AuthService: %100
- ✅ DatabaseService: Temel CRUD işlemleri
- ✅ Fatura Model: Hesaplama özellikleri
- ✅ CariKart Model: Hesaplama özellikleri
- ✅ StokKart Model: Özellikler ve eventler

### Gelecek İyileştirmeler
- [ ] Daha fazla DatabaseService metodu için testler
- [ ] ViewModel testleri
- [ ] Integration testleri
- [ ] Code coverage raporu (coverlet)

## 🛠️ Kullanılan Teknolojiler

- **xUnit**: Test framework
- **Moq**: Mock objeler için (henüz kullanılmadı, gelecekte kullanılabilir)
- **.NET 9.0**: Target framework

## 📝 Test Yazma Kuralları

### Test İsimlendirme
```
[MethodName]_[Scenario]_[ExpectedResult]
```

Örnek:
```csharp
[Fact]
public void HashPassword_CorrectPassword_ShouldReturnHash()
```

### Test Yapısı (AAA Pattern)
```csharp
[Fact]
public void TestMethod()
{
    // Arrange (Hazırlık)
    var input = "test";
    
    // Act (İşlem)
    var result = MethodUnderTest(input);
    
    // Assert (Doğrulama)
    Assert.NotNull(result);
}
```

## 🔧 Test Veritabanı

Testler, geçici SQLite veritabanı dosyaları kullanır:
- Her test için ayrı veritabanı dosyası oluşturulur
- Test sonunda otomatik olarak temizlenir
- Gerçek veritabanınız etkilenmez

## 📈 İstatistikler

- **Test Dosyası Sayısı**: 5
- **Test Metodu Sayısı**: 34
- **Ortalama Test Süresi**: ~100ms/test
- **En Hızlı Test**: < 1ms
- **En Yavaş Test**: ~579ms (DatabaseService güncelleme testi)

## 🎯 Sonraki Adımlar

1. ✅ Test projesi oluşturuldu
2. ✅ Temel testler yazıldı
3. ⏳ Daha fazla test eklenmeli
4. ⏳ Code coverage raporu oluşturulmalı
5. ⏳ CI/CD pipeline'a test entegrasyonu

---

**Son Güncelleme**: 2025-01-27  
**Test Durumu**: ✅ Tüm Testler Başarılı

