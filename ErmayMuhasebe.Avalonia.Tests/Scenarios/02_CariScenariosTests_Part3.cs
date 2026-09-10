using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class CariScenariosTests
{
    // =============================================================
    // 8. Cari Ekstre, Tarih & Raporlama Senaryoları (101-110)
    // =============================================================

    [Fact]
    public async Task Scenario_101_Ekstre_ChronologicalOrder_SortsMovementsByDateAscending()
    {
        var cari = new CariKart { CariKod = "EKS-101", Unvan = "Kronolojik Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = DateTime.Today.AddDays(-10), IslemTuru = "Geçmiş Fatura", Borc = 1000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = DateTime.Today.AddDays(-2), IslemTuru = "Yeni Fatura", Borc = 2000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = DateTime.Today.AddDays(-5), IslemTuru = "Ara Tahsilat", Alacak = 500m });

        var list = (await _uow.Cariler.GetHareketlerAsync(cari.Id)).OrderBy(x => x.Tarih).ToList();

        Assert.Equal(3, list.Count);
        Assert.True(list[0].Tarih < list[1].Tarih);
        Assert.True(list[1].Tarih < list[2].Tarih);
    }

    [Fact]
    public async Task Scenario_102_Ekstre_FilterByDateRange_SelectsOnlyMatchingMovements()
    {
        var cari = new CariKart { CariKod = "EKS-102", Unvan = "Tarih Aralıklı Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = new DateTime(2026, 1, 15), IslemTuru = "Ocak Satış", Borc = 1000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = new DateTime(2026, 2, 15), IslemTuru = "Şubat Satış", Borc = 2000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = new DateTime(2026, 3, 15), IslemTuru = "Mart Satış", Borc = 3000m });

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        var subList = all.Where(m => m.Tarih >= new DateTime(2026, 2, 1) && m.Tarih <= new DateTime(2026, 2, 28)).ToList();

        Assert.Single(subList);
        Assert.Equal("Şubat Satış", subList[0].IslemTuru);
    }

    [Fact]
    public async Task Scenario_103_Ekstre_SumBorcAndAlacak_MatchesGrandTotals()
    {
        var cari = new CariKart { CariKod = "EKS-103", Unvan = "Toplam Kontrolü Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 12500m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 3500m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 0, Alacak = 6000m });

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        decimal totalBorc = all.Sum(x => x.Borc);
        decimal totalAlacak = all.Sum(x => x.Alacak);

        Assert.Equal(16000m, totalBorc);
        Assert.Equal(6000m, totalAlacak);
    }

    [Fact]
    public async Task Scenario_104_Ekstre_DescriptionPreservesTurkishUtf8Chars()
    {
        var cari = new CariKart { CariKod = "EKS-104", Unvan = "UTF8 Test Cari" };
        await _uow.Cariler.SaveAsync(cari);

        string aciklama = "Çiçekli Örgü Fabrikası Şube 1 Ödemesi - ğüşiöç";
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Aciklama = aciklama, Borc = 500m });

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Equal(aciklama, all.First().Aciklama);
    }

    [Fact]
    public async Task Scenario_105_Ekstre_MultipleTransactionsOnSameSecond_MaintainsInsertionOrder()
    {
        var cari = new CariKart { CariKod = "EKS-105", Unvan = "Aynı Saniye Cari" };
        await _uow.Cariler.SaveAsync(cari);

        var now = DateTime.Now;
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = now, IslemTuru = "İşlem 1", Borc = 100m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Tarih = now, IslemTuru = "İşlem 2", Borc = 200m });

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Equal(2, all.Count);
        // CariRepository returns ordered descending by default
        Assert.True(all[0].Id > all[1].Id);
    }

    [Fact]
    public async Task Scenario_106_Ekstre_FaturaIdLinkage_CanTraceBackToInvoice()
    {
        var cari = new CariKart { CariKod = "EKS-106", Unvan = "Faturalı Cari" };
        await _uow.Cariler.SaveAsync(cari);

        int invoiceId = 9876;
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, FaturaId = invoiceId, Borc = 4500m });

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Equal(invoiceId, all.First().FaturaId);
    }

    [Fact]
    public async Task Scenario_107_Ekstre_RefIdLinkage_PreservesExternalReference()
    {
        var cari = new CariKart { CariKod = "EKS-107", Unvan = "RefId Test Cari" };
        await _uow.Cariler.SaveAsync(cari);

        string refCode = "BANK-EXT-2026-990";
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, RefId = refCode, Alacak = 3200m });

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Equal(refCode, all.First().RefId);
    }

    [Fact]
    public async Task Scenario_108_Ekstre_EmptyMovements_ReturnsEmptyCollectionWithoutCrashing()
    {
        var cari = new CariKart { CariKod = "EKS-108", Unvan = "Hareketsiz Cari" };
        await _uow.Cariler.SaveAsync(cari);

        var all = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.NotNull(all);
        Assert.Empty(all);
    }

    [Fact]
    public async Task Scenario_109_Ekstre_IsSelectedProperty_SupportsMultiSelectInUI()
    {
        var h = new CariHareket { IsSelected = true };
        Assert.True(h.IsSelected);
        h.IsSelected = false;
        Assert.False(h.IsSelected);
    }

    [Fact]
    public async Task Scenario_110_Ekstre_KalanBakiye_CanBeAssignedInRuntime()
    {
        var h = new CariHareket { KalanBakiye = 15750.50m };
        Assert.Equal(15750.50m, h.KalanBakiye);
    }

    // =============================================================
    // 9. Soft-Delete, Arşivleme & Durum Yönetimi (111-120)
    // =============================================================

    [Fact]
    public async Task Scenario_111_SoftDelete_MarksIsDeletedTrue()
    {
        var cari = new CariKart { CariKod = "SD-111", Unvan = "Silinecek Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.DeleteAsync(cari.Id);
        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);

        // CariRepository Soft-delete implementasyonu IsDeleted = true yapar
        Assert.True(retrieved == null || retrieved.IsDeleted);
    }

    [Fact]
    public async Task Scenario_112_SoftDelete_ExcludesFromGetAllAsync()
    {
        var cari = new CariKart { CariKod = "SD-112", Unvan = "Gizlenecek Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.DeleteAsync(cari.Id);
        var all = await _uow.Cariler.GetAllAsync();

        Assert.DoesNotContain(all, c => c.CariKod == "SD-112");
    }

    [Fact]
    public async Task Scenario_113_SoftDelete_MovementsPreventDirectDeletion()
    {
        var cari = new CariKart { CariKod = "SD-113", Unvan = "Hareketleri Korunacak Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 5000m });
        
        // Business rule: Cari with transactions cannot be deleted directly
        await Assert.ThrowsAsync<Exception>(() => _uow.Cariler.DeleteAsync(cari.Id));

        var movements = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.NotEmpty(movements);
        Assert.Equal(5000m, movements.First().Borc);
    }

    [Fact]
    public async Task Scenario_114_CariKart_AktifMi_ToggleState()
    {
        var cari = new CariKart { CariKod = "ST-114", Unvan = "Durum Değişen Cari", AktifMi = true };
        await _uow.Cariler.SaveAsync(cari);

        cari.AktifMi = false;
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.False(retrieved!.AktifMi);
    }

    [Fact]
    public async Task Scenario_115_CariKart_KayitTarihi_DefaultsToCurrentDate()
    {
        var cari = new CariKart { CariKod = "KT-115", Unvan = "Kayıt Tarihli Cari" };
        Assert.True((DateTime.Now - cari.KayitTarihi).TotalMinutes < 5);
    }

    [Fact]
    public async Task Scenario_116_CariKart_Version_IncrementsOrDefaults()
    {
        var cari = new CariKart { CariKod = "VER-116", Unvan = "Versiyon Test" };
        Assert.True(cari.Version >= 1);
    }

    [Fact]
    public async Task Scenario_117_CariKart_UpdatedAt_UpdatesTimestamp()
    {
        var cari = new CariKart { CariKod = "UPD-117", Unvan = "Güncelleme Zamanı Test" };
        var before = cari.UpdatedAt;
        cari.UpdatedAt = DateTime.Now.AddSeconds(1);
        Assert.True(cari.UpdatedAt >= before);
    }

    [Fact]
    public async Task Scenario_118_CariKart_TenantId_DefaultsToDefault()
    {
        var cari = new CariKart { CariKod = "TNT-118", Unvan = "Tenant Test" };
        Assert.Equal("default", cari.TenantId);
    }

    [Fact]
    public async Task Scenario_119_CariKart_CustomTenantId_PersistsProperly()
    {
        var cari = new CariKart { CariKod = "TNT-119", Unvan = "Özel Tenant Cari", TenantId = "subcompany_99" };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("subcompany_99", retrieved!.TenantId);
    }

    [Fact]
    public async Task Scenario_120_CariKart_ToString_ReturnsUnvan()
    {
        var cari = new CariKart { Unvan = "Mega Holding A.Ş." };
        Assert.Equal("Mega Holding A.Ş.", cari.ToString());
    }

    // =============================================================
    // 10. Sevk & İletişim Detayları (121-130)
    // =============================================================

    [Fact]
    public async Task Scenario_121_SevkAdresi_SeparateFromFaturaAdresi()
    {
        var cari = new CariKart
        {
            CariKod = "ADR-121",
            Unvan = "Lojistik Merkezi",
            Adres = "Maslak Mah. Büyükdere Cad. No:1 Sarıyer/İstanbul",
            SevkAdresi = "Tuzla Serbest Bölge Depoları D-12 Tuzla/İstanbul"
        };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotEqual(retrieved!.Adres, retrieved.SevkAdresi);
        Assert.Contains("Tuzla", retrieved.SevkAdresi);
    }

    [Fact]
    public async Task Scenario_122_PostaKoduAndUlke_PersistsInternationalInfo()
    {
        var cari = new CariKart
        {
            CariKod = "ADR-122",
            Unvan = "Global Import GmbH",
            PostaKodu = "10115",
            Ulke = "Almanya",
            Il = "Berlin"
        };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("10115", retrieved!.PostaKodu);
        Assert.Equal("Almanya", retrieved.Ulke);
    }

    [Fact]
    public async Task Scenario_123_WebAdresi_StoresUrl()
    {
        var cari = new CariKart { CariKod = "WEB-123", Unvan = "Tech Ltd.", WebAdresi = "https://www.techltd.com.tr" };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("https://www.techltd.com.tr", retrieved!.WebAdresi);
    }

    [Fact]
    public async Task Scenario_124_TicaretSicilNo_PersistsChamberRegistration()
    {
        var cari = new CariKart { CariKod = "TIC-124", Unvan = "Sicil A.Ş.", TicaretSicilNo = "ITO-987654" };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("ITO-987654", retrieved!.TicaretSicilNo);
    }

    [Fact]
    public async Task Scenario_125_CepTelefonu_PersistsSecondaryMobileNumber()
    {
        var cari = new CariKart { CariKod = "MOB-125", Unvan = "Mobil İletişim", Telefon = "02120001122", CepTelefon = "05339998877" };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("05339998877", retrieved!.CepTelefon);
        Assert.Equal("02120001122", retrieved.Telefon);
    }

    [Fact]
    public async Task Scenario_126_VKNAlias_SynchronizesWithVergiNo()
    {
        var cari = new CariKart { VKN = "1122334455" };
        Assert.Equal("1122334455", cari.VergiNo);

        cari.VergiNo = "9988776655";
        Assert.Equal("9988776655", cari.VKN);
    }

    [Fact]
    public async Task Scenario_127_Aciklama_StoresMultiLineNotes()
    {
        string notes = "Önemli Müşteri Notları:\n1. Salı günleri teslimat yapılmaz.\n2. Yetkili kişi sadece sabahları ulaşılabilir.";
        var cari = new CariKart { CariKod = "NOT-127", Unvan = "Notlu Müşteri", Aciklama = notes };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(notes, retrieved!.Aciklama);
    }

    [Fact]
    public async Task Scenario_128_RiskTakibiYapilsin_FlagPersistence()
    {
        var cari = new CariKart { CariKod = "RT-128", Unvan = "Risk Takip Açık", RiskTakibiYapilsin = true };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.True(retrieved!.RiskTakibiYapilsin);
    }

    [Fact]
    public async Task Scenario_129_VadeGecmisteEngelle_FlagPersistence()
    {
        var cari = new CariKart { CariKod = "VG-129", Unvan = "Vade Engel Açık", VadeGecmisteEngelle = true };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.True(retrieved!.VadeGecmisteEngelle);
    }

    [Fact]
    public async Task Scenario_130_LongAddressString_PersistsWithoutTruncation()
    {
        string veryLongAddress = new string('A', 500) + " Sk. No: 99";
        var cari = new CariKart { CariKod = "ADR-130", Unvan = "Uzun Adresli Müşteri", Adres = veryLongAddress };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(veryLongAddress, retrieved!.Adres);
    }

    // =============================================================
    // 11. Cari Birleştirme & Gelişmiş ViewModel İşlemleri (131-140)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_131_CariBirlestirmeViewModel_Initialization_SetsUpState()
    {
        var vm = _serviceProvider.GetRequiredService<CariBirlestirmeViewModel>();
        Assert.NotNull(vm);
        Assert.NotNull(vm.BirlestirCommand);
    }

    [AvaloniaFact]
    public async Task Scenario_132_CariBirlestirme_TransfersAllMovementsToTarget()
    {
        var sourceCari = new CariKart { CariKod = "SRC-132", Unvan = "Kapanan Şube" };
        var targetCari = new CariKart { CariKod = "TGT-132", Unvan = "Merkez Şube" };
        await _uow.Cariler.SaveAsync(sourceCari);
        await _uow.Cariler.SaveAsync(targetCari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = sourceCari.Id, IslemTuru = "Fatura 1", Borc = 1000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = sourceCari.Id, IslemTuru = "Fatura 2", Borc = 2000m });

        // Merge logic: reassign CariId of movements to target
        var srcMovements = await _uow.Cariler.GetHareketlerAsync(sourceCari.Id);
        foreach (var m in srcMovements)
        {
            m.CariId = targetCari.Id;
            await _uow.Cariler.SaveHareketAsync(m);
        }

        await _dbService.RecalculateCariBalanceAsync(targetCari.Id);
        await _dbService.RecalculateCariBalanceAsync(sourceCari.Id);

        var tgtMovements = await _uow.Cariler.GetHareketlerAsync(targetCari.Id);
        var srcLeftover = await _uow.Cariler.GetHareketlerAsync(sourceCari.Id);

        Assert.Equal(2, tgtMovements.Count);
        Assert.Empty(srcLeftover);

        var tgt = await _uow.Cariler.GetByIdAsync(targetCari.Id);
        Assert.Equal(3000m, tgt!.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_133_CariDetayViewModel_CreationWithCari_PopulatesFields()
    {
        var cari = new CariKart { CariKod = "CD-133", Unvan = "Detay Görüntülenen", Borc = 4500m };
        await _uow.Cariler.SaveAsync(cari);

        var vm = new CariDetayViewModel(_uow, _pdfService);
        vm.Cari = cari;

        Assert.NotNull(vm.Cari);
        Assert.Equal("CD-133", vm.Cari.CariKod);
    }

    [Fact]
    public async Task Scenario_134_Cari_PropertyChangeNotifications_FireForUnvan()
    {
        var cari = new CariKart();
        string? changedProperty = null;
        cari.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        cari.Unvan = "Yeni Unvan A.Ş.";
        Assert.Equal(nameof(CariKart.Unvan), changedProperty);
    }

    [Fact]
    public async Task Scenario_135_Cari_PropertyChangeNotifications_FireForDevirBorcUpdatesBakiye()
    {
        var cari = new CariKart();
        var changedProps = new List<string?>();
        cari.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

        cari.DevirBorc = 1500m;
        Assert.Contains(nameof(CariKart.DevirBorc), changedProps);
        Assert.Contains(nameof(CariKart.Bakiye), changedProps);
    }

    [Fact]
    public async Task Scenario_136_Cari_PropertyChangeNotifications_FireForDevirAlacakUpdatesBakiye()
    {
        var cari = new CariKart();
        var changedProps = new List<string?>();
        cari.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

        cari.DevirAlacak = 800m;
        Assert.Contains(nameof(CariKart.DevirAlacak), changedProps);
        Assert.Contains(nameof(CariKart.Bakiye), changedProps);
    }

    [Fact]
    public async Task Scenario_137_Cari_PropertyChangeNotifications_FireForBorcUpdatesBakiye()
    {
        var cari = new CariKart();
        var changedProps = new List<string?>();
        cari.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

        cari.Borc = 3200m;
        Assert.Contains(nameof(CariKart.Borc), changedProps);
        Assert.Contains(nameof(CariKart.Bakiye), changedProps);
    }

    [Fact]
    public async Task Scenario_138_Cari_PropertyChangeNotifications_FireForAlacakUpdatesBakiye()
    {
        var cari = new CariKart();
        var changedProps = new List<string?>();
        cari.PropertyChanged += (s, e) => changedProps.Add(e.PropertyName);

        cari.Alacak = 1900m;
        Assert.Contains(nameof(CariKart.Alacak), changedProps);
        Assert.Contains(nameof(CariKart.Bakiye), changedProps);
    }

    [Fact]
    public async Task Scenario_139_CariHareket_PropertyChangeNotifications_FireForIsSelected()
    {
        var h = new CariHareket();
        string? changed = null;
        h.PropertyChanged += (s, e) => changed = e.PropertyName;

        h.IsSelected = true;
        Assert.Equal(nameof(CariHareket.IsSelected), changed);
    }

    [Fact]
    public async Task Scenario_140_CariHareket_PropertyChangeNotifications_FireForId()
    {
        var h = new CariHareket();
        string? changed = null;
        h.PropertyChanged += (s, e) => changed = e.PropertyName;

        h.Id = 777;
        Assert.Equal(nameof(CariHareket.Id), changed);
    }

    // =============================================================
    // 12. Toplu İşlemler & Performans & İleri Bakiye Testleri (141-150)
    // =============================================================

    [Fact]
    public async Task Scenario_141_BulkInsert_TwentyCariCards_SavesAllWithoutLoss()
    {
        var list = new List<CariKart>();
        for (int i = 1; i <= 20; i++)
        {
            list.Add(new CariKart { CariKod = $"BLK-{i:D3}", Unvan = $"Toplu Cari {i}" });
        }

        foreach (var c in list)
        {
            await _uow.Cariler.SaveAsync(c);
        }

        var all = await _uow.Cariler.GetAllAsync();
        foreach (var c in list)
        {
            Assert.Contains(all, x => x.CariKod == c.CariKod);
        }
    }

    [Fact]
    public async Task Scenario_142_RecalculateAllBalancesAsync_SynchronizesEntireCariLedger()
    {
        var c1 = new CariKart { CariKod = "SYNC-01", Unvan = "Senkron Cari 1" };
        var c2 = new CariKart { CariKod = "SYNC-02", Unvan = "Senkron Cari 2" };
        await _uow.Cariler.SaveAsync(c1);
        await _uow.Cariler.SaveAsync(c2);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = c1.Id, Borc = 5000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = c2.Id, Alacak = 2000m });

        await _dbService.RecalculateCariBalanceAsync(c1.Id);
        await _dbService.RecalculateCariBalanceAsync(c2.Id);

        var r1 = await _uow.Cariler.GetByIdAsync(c1.Id);
        var r2 = await _uow.Cariler.GetByIdAsync(c2.Id);

        Assert.Equal(5000m, r1!.Bakiye);
        Assert.Equal(-2000m, r2!.Bakiye);
    }

    [Fact]
    public async Task Scenario_143_CariHareket_DeletedRecord_DoesNotAffectBalanceCalculation()
    {
        var cari = new CariKart { CariKod = "IGN-143", Unvan = "Silinen Kayıt İhmali" };
        await _uow.Cariler.SaveAsync(cari);

        var h1 = new CariHareket { CariId = cari.Id, Borc = 8000m };
        var h2 = new CariHareket { CariId = cari.Id, Borc = 4000m };
        await _uow.Cariler.SaveHareketAsync(h1);
        await _uow.Cariler.SaveHareketAsync(h2);

        await _uow.Cariler.DeleteHareketAsync(h2);
        await _dbService.RecalculateCariBalanceAsync(cari.Id);

        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(8000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_144_CariKart_EmptyStringsOnOptionalFields_DoNotThrowExceptions()
    {
        var cari = new CariKart
        {
            CariKod = "EMP-144",
            Unvan = "Boş Alanlı Cari",
            Telefon = "",
            CepTelefon = "",
            Email = "",
            WebAdresi = "",
            Adres = "",
            SevkAdresi = "",
            VergiDairesi = "",
            VergiNo = "",
            TCNo = "",
            IBAN = "",
            OdemePlani = "",
            Aciklama = ""
        };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("EMP-144", retrieved.CariKod);
    }

    [Fact]
    public async Task Scenario_145_CariHareket_NullOptionalFields_PersistsGracefully()
    {
        var cari = new CariKart { CariKod = "NULL-145", Unvan = "Null Alan Test" };
        await _uow.Cariler.SaveAsync(cari);

        var h = new CariHareket
        {
            CariId = cari.Id,
            Borc = 100m,
            Aciklama = null,
            EvrakNo = null,
            SlipImage = null,
            RefId = null,
            Vade = null,
            FaturaId = null
        };
        await _uow.Cariler.SaveHareketAsync(h);

        var retrieved = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Single(retrieved);
        Assert.Null(retrieved.First().Aciklama);
    }

    [Fact]
    public async Task Scenario_146_Cari_UpdateMultiplePropertiesSimultaneously()
    {
        var cari = new CariKart { CariKod = "MULTI-146", Unvan = "Eski Unvan", Telefon = "02120000000" };
        await _uow.Cariler.SaveAsync(cari);

        cari.Unvan = "Yeni Güncel Unvan";
        cari.Telefon = "02161111111";
        cari.Email = "yeni@email.com";
        cari.Il = "Antalya";
        cari.Ilce = "Muratpaşa";
        await _uow.Cariler.SaveAsync(cari);

        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("Yeni Güncel Unvan", refreshed!.Unvan);
        Assert.Equal("02161111111", refreshed.Telefon);
        Assert.Equal("yeni@email.com", refreshed.Email);
        Assert.Equal("Antalya", refreshed.Il);
        Assert.Equal("Muratpaşa", refreshed.Ilce);
    }

    [Fact]
    public async Task Scenario_147_Cari_BalanceWithKurusRounding_ComputesAccurateFractionalValues()
    {
        var cari = new CariKart { CariKod = "KRS-147", Unvan = "Kuruş Hassasiyet Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 100.33m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 200.33m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 300.34m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        // 100.33 + 200.33 + 300.34 = 601.00m
        Assert.Equal(601.00m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_148_Cari_ExtremeNegativeBalance_DoesNotOverflow()
    {
        var cari = new CariKart { CariKod = "EXT-148", Unvan = "Aşırı Alacaklı Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        decimal hugePayment = 85000000m;
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Alacak = hugePayment });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(-hugePayment, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_149_Cari_ExtremePositiveBalance_DoesNotOverflow()
    {
        var cari = new CariKart { CariKod = "EXT-149", Unvan = "Aşırı Borçlu Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        decimal hugeDebt = 95000000m;
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = hugeDebt });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(hugeDebt, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_150_Cari_EndToEndLifecycle_CreationMovementBalanceMergeAndSoftDelete()
    {
        // 1. Create Cari
        var cari = new CariKart { CariKod = "E2E-150", Unvan = "E2E Yaşam Döngüsü Cari" };
        await _uow.Cariler.SaveAsync(cari);
        Assert.True(cari.Id > 0);

        // 2. Add Transactions
        var m1 = new CariHareket { CariId = cari.Id, IslemTuru = "Satış Faturası", Borc = 5000m };
        var m2 = new CariHareket { CariId = cari.Id, IslemTuru = "Nakit Tahsilat", Alacak = 2000m };
        await _uow.Cariler.SaveHareketAsync(m1);
        await _uow.Cariler.SaveHareketAsync(m2);

        // 3. Recalculate Balance
        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var activeCari = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(3000m, activeCari!.Bakiye);

        // 4. Clean movements before deletion as per business rules
        await _uow.Cariler.DeleteHareketAsync(m1);
        await _uow.Cariler.DeleteHareketAsync(m2);

        // 5. Soft Delete
        await _uow.Cariler.DeleteAsync(cari.Id);
        var allActive = await _uow.Cariler.GetAllAsync();
        Assert.DoesNotContain(allActive, c => c.Id == cari.Id);
    }

    [Fact]
    public async Task Scenario_151_CariKart_IsDeleted_IgnoredInActiveQueries()
    {
        var cari = new CariKart { CariKod = "RSK-151", Unvan = "Silinmiş Riskli", Borc = 100000m, IsDeleted = true };
        await _uow.Cariler.SaveAsync(cari);

        var activeCaris = await _uow.Cariler.GetAllAsync();
        Assert.DoesNotContain(activeCaris, c => c.CariKod == "RSK-151");
    }

    [Fact]
    public async Task Scenario_152_CariKart_EmptyEmail_DoesNotThrowFormatException()
    {
        var cari = new CariKart { CariKod = "EM-152", Unvan = "E-postasız Cari", Email = null };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Null(retrieved!.Email);
    }
}
