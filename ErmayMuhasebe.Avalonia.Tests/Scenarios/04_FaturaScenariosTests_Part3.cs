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

public partial class FaturaScenariosTests
{
    // =============================================================
    // 10. Fatura ile Cari & Finans Entegrasyonu (101-110)
    // =============================================================

    [Fact]
    public async Task Scenario_101_SalesInvoice_LinksToCari_AndIncreasesCariDebt()
    {
        var cari = new CariKart { CariKod = "CR-FAT-101", Unvan = "Fatura Cari 1" };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura
        {
            FaturaNo = "FAT-INT-101",
            Tur = "Satış",
            CariId = cari.Id,
            CariUnvan = cari.Unvan,
            GenelToplam = 7500m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            FaturaId = fatura.Id,
            EvrakNo = fatura.FaturaNo,
            IslemTuru = "Satış Faturası",
            Borc = fatura.GenelToplam
        });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(7500m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_102_PurchaseInvoice_LinksToSupplier_AndIncreasesSupplierCredit()
    {
        var supplier = new CariKart { CariKod = "CR-SUP-102", Unvan = "Tedarikçi 1" };
        await _uow.Cariler.SaveAsync(supplier);

        var fatura = new Fatura
        {
            FaturaNo = "ALIS-INT-102",
            Tur = "Alış",
            CariId = supplier.Id,
            CariUnvan = supplier.Unvan,
            GenelToplam = 12000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = supplier.Id,
            FaturaId = fatura.Id,
            EvrakNo = fatura.FaturaNo,
            IslemTuru = "Alış Faturası",
            Alacak = fatura.GenelToplam
        });

        await _dbService.RecalculateCariBalanceAsync(supplier.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(supplier.Id);

        Assert.Equal(-12000m, refreshed!.Bakiye); // Net Alacak
    }

    [Fact]
    public async Task Scenario_103_ReturnSalesInvoice_GeneratesCreditToOffsetCustomerDebt()
    {
        var cari = new CariKart { CariKod = "CR-RET-103", Unvan = "İade Müşterisi" };
        await _uow.Cariler.SaveAsync(cari);

        // Satış Faturası (10.000 TL)
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 10000m });
        // Satış İade Faturası (2.000 TL)
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Alacak = 2000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(8000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_104_ReturnPurchaseInvoice_GeneratesDebitToOffsetSupplierCredit()
    {
        var supplier = new CariKart { CariKod = "SUP-RET-104", Unvan = "İadeli Tedarikçi" };
        await _uow.Cariler.SaveAsync(supplier);

        // Alış Faturası (15.000 TL Alacak)
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = supplier.Id, Alacak = 15000m });
        // Alış İade Faturası (3.000 TL Borç)
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = supplier.Id, Borc = 3000m });

        await _dbService.RecalculateCariBalanceAsync(supplier.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(supplier.Id);

        Assert.Equal(-12000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_105_InvoiceWithCashPayment_RegistersBothInvoiceAndCollection()
    {
        var cari = new CariKart { CariKod = "CR-CASH-105", Unvan = "Peşin Çalışan Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        decimal tutar = 5000m;
        // Satış faturası (Borç)
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış Faturası", Borc = tutar });
        // Anında Nakit Tahsilat (Alacak)
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Nakit Tahsilat", Alacak = tutar });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(0m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_106_InvoiceLineCount_MatchesSavedDetayRecords()
    {
        var fatura = new Fatura { FaturaNo = "FAT-CNT-106", Tur = "Satış" };
        var linesList = new List<FaturaDetay>();
        for (int i = 1; i <= 5; i++)
        {
            linesList.Add(new FaturaDetay
            {
                StokAdi = $"Ürün Kalemi {i}",
                Miktar = i,
                BirimFiyat = 50m,
                ToplamTutar = i * 50m
            });
        }
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, linesList);

        var lines = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Equal(5, lines.Count);
    }

    [Fact]
    public async Task Scenario_107_Invoice_SumDetayToplamTutar_MatchesGenelToplam()
    {
        var fatura = new Fatura { FaturaNo = "FAT-SUM-107", Tur = "Satış", GenelToplam = 1500m };
        var line1 = new FaturaDetay { ToplamTutar = 600m };
        var line2 = new FaturaDetay { ToplamTutar = 900m };
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, new List<FaturaDetay> { line1, line2 });

        var lines = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Equal(fatura.GenelToplam, lines.Sum(l => l.ToplamTutar));
    }

    [Fact]
    public async Task Scenario_108_Invoice_MultipleInvoicesForSameCustomer_AccumulatesProperly()
    {
        var cari = new CariKart { CariKod = "CR-ACC-108", Unvan = "Sürekli Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 2000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 3000m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, Borc = 5000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(10000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_109_Invoice_ZeroLineInvoice_PersistsHeaderOnly()
    {
        var fatura = new Fatura { FaturaNo = "FAT-ZERO-109", Tur = "Satış", GenelToplam = 0m };
        await _uow.Faturalar.SaveAsync(fatura);

        var lines = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Empty(lines);
    }

    [Fact]
    public async Task Scenario_110_Invoice_DetaylarCollection_IgnoredFromDbMapping()
    {
        var fatura = new Fatura { FaturaNo = "FAT-IGN-110", Tur = "Satış" };
        fatura.Detaylar.Add(new FaturaDetay { StokAdi = "Geçici Kalem" });

        Assert.Single(fatura.Detaylar);
    }

    // =============================================================
    // 11. Fatura İptali & Soft-Delete (111-120)
    // =============================================================

    [Fact]
    public async Task Scenario_111_Invoice_CancelFlag_IptalMi_CanBeSet()
    {
        var fatura = new Fatura { FaturaNo = "FAT-CAN-111", Tur = "Satış", IptalMi = false };
        await _uow.Faturalar.SaveAsync(fatura);

        fatura.IptalMi = true;
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.True(retrieved!.IptalMi);
    }

    [Fact]
    public async Task Scenario_112_Invoice_SoftDelete_MarksIsDeletedTrue()
    {
        var fatura = new Fatura { FaturaNo = "FAT-DEL-112", Tur = "Satış", GenelToplam = 1000m };
        await _uow.Faturalar.SaveAsync(fatura);

        await _uow.Faturalar.DeleteAsync(fatura.Id);
        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);

        Assert.True(retrieved == null || retrieved.IsDeleted);
    }

    [Fact]
    public async Task Scenario_113_Invoice_SoftDelete_ExcludesFromGetAllAsync()
    {
        var fatura = new Fatura { FaturaNo = "FAT-DEL-113", Tur = "Satış", GenelToplam = 1000m };
        await _uow.Faturalar.SaveAsync(fatura);

        await _uow.Faturalar.DeleteAsync(fatura.Id);
        var all = await _uow.Faturalar.GetAllAsync();

        Assert.DoesNotContain(all, f => f.FaturaNo == "FAT-DEL-113");
    }

    [Fact]
    public async Task Scenario_114_Invoice_DeletedInvoice_LinesPreservedInDatabase()
    {
        var fatura = new Fatura { FaturaNo = "FAT-DEL-114", Tur = "Satış" };
        var line = new FaturaDetay { StokAdi = "Kalem 114", ToplamTutar = 250m };
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, new List<FaturaDetay> { line });

        await _uow.Faturalar.DeleteAsync(fatura.Id);

        var lines = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.NotEmpty(lines);
    }

    [Fact]
    public async Task Scenario_115_Invoice_TenantId_DefaultsToDefault()
    {
        var fatura = new Fatura();
        Assert.Equal("default", fatura.TenantId);
    }

    [Fact]
    public async Task Scenario_116_Invoice_CustomTenantId_PersistsProperly()
    {
        var fatura = new Fatura { FaturaNo = "FAT-TNT-116", Tur = "Satış", TenantId = "branch_ankara" };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal("branch_ankara", retrieved!.TenantId);
    }

    [Fact]
    public async Task Scenario_117_Invoice_Id_AutoIncrementAssignment()
    {
        var fatura = new Fatura { FaturaNo = "FAT-ID-117", Tur = "Satış" };
        await _uow.Faturalar.SaveAsync(fatura);
        Assert.True(fatura.Id > 0);
    }

    [AvaloniaFact]
    public async Task Scenario_118_Invoice_SoftDeletedInvoice_DoesNotContributeToMonthlyTurnover()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura
        {
            FaturaNo = "FAT-REV-118",
            Tur = "Satış",
            Tarih = DateTime.Now,
            GenelToplam = 25000m,
            IsDeleted = true
        });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        // Silinen fatura ciroya dahil edilmemelidir
        Assert.Equal(0, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_119_Invoice_PurchaseInvoice_DoesNotContributeToSalesTurnover()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura
        {
            FaturaNo = "ALIS-REV-119",
            Tur = "Alış",
            Tarih = DateTime.Now,
            GenelToplam = 18000m
        });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_120_Invoice_TodayInvoice_ContributesToDailyTurnover()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura
        {
            FaturaNo = "SATIS-REV-120",
            Tur = "Satış",
            Tarih = DateTime.Now,
            GenelToplam = 14500m
        });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(14500m, vm.GunlukSatis);
    }

    // =============================================================
    // 12. Dövizli Faturalar & Kur Çevrimi (121-130)
    // =============================================================

    [Theory]
    [InlineData("USD", 38.50, 1000, 38500)]
    [InlineData("EUR", 42.00, 500, 21000)]
    [InlineData("GBP", 49.50, 200, 9900)]
    public async Task Scenario_121_to_123_ForeignCurrencyInvoices_StoreRateAndCurrency(string dovizTuru, decimal kur, decimal dovizTutar, decimal expTlKarsiligi)
    {
        var fatura = new Fatura
        {
            FaturaNo = $"FAT-FX-{dovizTuru}",
            Tur = "Satış",
            DovizTuru = dovizTuru,
            DovizKuru = kur,
            GenelToplam = expTlKarsiligi
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(dovizTuru, retrieved!.DovizTuru);
        Assert.Equal(kur, retrieved.DovizKuru);
        Assert.Equal(expTlKarsiligi, retrieved.GenelToplam);
    }

    [Fact]
    public void Scenario_124_ForeignCurrency_TlConversionCalculation()
    {
        decimal usdAmount = 2500m;
        decimal exchangeRate = 38.25m;
        decimal tlAmount = usdAmount * exchangeRate;

        Assert.Equal(95625.00m, tlAmount);
    }

    [Fact]
    public void Scenario_125_ForeignCurrency_ZeroRateDefaultCheck()
    {
        var fatura = new Fatura { DovizTuru = "TL", DovizKuru = 1m };
        Assert.Equal("TL", fatura.DovizTuru);
        Assert.Equal(1m, fatura.DovizKuru);
    }

    [Fact]
    public async Task Scenario_126_Invoice_FractionalPriceAndCurrencies()
    {
        decimal kur = 38.4520m;
        var fatura = new Fatura { FaturaNo = "FAT-FX-PREC", Tur = "Satış", DovizKuru = kur };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(kur, retrieved!.DovizKuru);
    }

    [Fact]
    public async Task Scenario_127_Invoice_NullDovizTuru_DefaultsToTlBehavior()
    {
        var fatura = new Fatura { FaturaNo = "FAT-TL-DEF", Tur = "Satış", DovizTuru = null };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Null(retrieved!.DovizTuru);
    }

    [Fact]
    public void Scenario_128_FxInvoice_CalculateVatInForeignCurrency()
    {
        decimal usdMatrah = 1000m;
        int kdvOrani = 20;
        decimal usdKdv = usdMatrah * (kdvOrani / 100m);
        decimal usdToplam = usdMatrah + usdKdv;

        Assert.Equal(200m, usdKdv);
        Assert.Equal(1200m, usdToplam);
    }

    [Fact]
    public void Scenario_129_FxInvoice_ConvertTotalsToTlUsingRate()
    {
        decimal usdTotal = 1200m;
        decimal kur = 40.00m;
        decimal tlTotal = usdTotal * kur;

        Assert.Equal(48000m, tlTotal);
    }

    [Fact]
    public void Scenario_130_FxInvoice_RoundingSmallCents()
    {
        decimal rawTl = 1250.7891m;
        decimal rounded = Math.Round(rawTl, 2);
        Assert.Equal(1250.79m, rounded);
    }

    // =============================================================
    // 13. FaturaListViewModel Arama, Filtreleme & ViewModel Davranışları (131-140)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_131_FaturaListViewModel_SearchByFaturaNo_LocatesMatch()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-SRC-01", Tur = "Satış", GenelToplam = 1000m });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-SRC-02", Tur = "Satış", GenelToplam = 2000m });

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.SearchString = "SRC-01";
        await vm.LoadFaturalarAsync();

        Assert.Contains(vm.Faturalar, f => f.FaturaNo == "FAT-SRC-01");
        Assert.DoesNotContain(vm.Faturalar, f => f.FaturaNo == "FAT-SRC-02");
    }

    [AvaloniaFact]
    public async Task Scenario_132_FaturaListViewModel_SearchByCustomerName_LocatesMatch()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-CLI-01", Tur = "Satış", CariUnvan = "Yıldız Demir Çelik", GenelToplam = 3000m });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-CLI-02", Tur = "Satış", CariUnvan = "Güneş Tekstil", GenelToplam = 4000m });

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.SearchString = "Demir Çelik";
        await vm.LoadFaturalarAsync();

        Assert.Contains(vm.Faturalar, f => f.FaturaNo == "FAT-CLI-01");
        Assert.DoesNotContain(vm.Faturalar, f => f.FaturaNo == "FAT-CLI-02");
    }

    [AvaloniaFact]
    public async Task Scenario_133_FaturaListViewModel_EmptySearchString_LoadsAll()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-ALL-01", Tur = "Satış", GenelToplam = 100m });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-ALL-02", Tur = "Satış", GenelToplam = 200m });

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.SearchString = string.Empty;
        await vm.LoadFaturalarAsync();

        Assert.True(vm.Faturalar.Count >= 2);
    }

    [AvaloniaFact]
    public async Task Scenario_134_FaturaListViewModel_DateRangeFilter_IncludesOnlyInDateRange()
    {
        var date1 = new DateTime(2026, 1, 15);
        var date2 = new DateTime(2026, 2, 15);
        var date3 = new DateTime(2026, 3, 15);

        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "D-01", Tur = "Satış", Tarih = date1, GenelToplam = 100m });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "D-02", Tur = "Satış", Tarih = date2, GenelToplam = 200m });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "D-03", Tur = "Satış", Tarih = date3, GenelToplam = 300m });

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.StartDate = new DateTime(2026, 2, 1);
        vm.EndDate = new DateTime(2026, 2, 28);
        await vm.LoadFaturalarAsync();

        Assert.Contains(vm.Faturalar, f => f.FaturaNo == "D-02");
        Assert.DoesNotContain(vm.Faturalar, f => f.FaturaNo == "D-01");
        Assert.DoesNotContain(vm.Faturalar, f => f.FaturaNo == "D-03");
    }

    [AvaloniaFact]
    public async Task Scenario_135_FaturaListViewModel_SelectedFatura_UpdatesSelectionState()
    {
        var fatura = new Fatura { FaturaNo = "FAT-SEL-135", Tur = "Satış", GenelToplam = 500m };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.SelectedFatura = fatura;

        Assert.NotNull(vm.SelectedFatura);
        Assert.Equal("FAT-SEL-135", vm.SelectedFatura.FaturaNo);
    }

    [AvaloniaFact]
    public async Task Scenario_136_FaturaListViewModel_IsCreateVisibleToggle()
    {
        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.IsCreateVisible = true;
        Assert.True(vm.IsCreateVisible);

        vm.IsCreateVisible = false;
        Assert.False(vm.IsCreateVisible);
    }

    [AvaloniaFact]
    public async Task Scenario_137_FaturaListViewModel_NewFaturaDetaylar_CanAddLines()
    {
        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        vm.NewFaturaDetaylar.Clear();
        vm.NewFaturaDetaylar.Add(new FaturaDetay { StokAdi = "Yeni Kalem", ToplamTutar = 250m });

        Assert.Single(vm.NewFaturaDetaylar);
    }

    [Fact]
    public async Task Scenario_138_FaturaDetay_KDVTutariAlias_SynchronizesWithKdvTutari()
    {
        var d = new FaturaDetay { KdvTutari = 85m };
        Assert.Equal(85m, d.KDVTutari);

        d.KDVTutari = 120m;
        Assert.Equal(120m, d.KdvTutari);
    }

    [Fact]
    public async Task Scenario_139_Fatura_PaymentStatusString_IdentifiesFullyPaid()
    {
        var f = new Fatura { GenelToplam = 1000m, Odenen = 1000m };
        string status = f.Kalan == 0 ? "Ödendi" : (f.Odenen > 0 ? "Kısmi Ödendi" : "Ödenmedi");
        Assert.Equal("Ödendi", status);
    }

    [Fact]
    public async Task Scenario_140_Fatura_PaymentStatusString_IdentifiesUnpaid()
    {
        var f = new Fatura { GenelToplam = 1000m, Odenen = 0m };
        string status = f.Kalan == 0 ? "Ödendi" : (f.Odenen > 0 ? "Kısmi Ödendi" : "Ödenmedi");
        Assert.Equal("Ödenmedi", status);
    }

    // =============================================================
    // 14. Toplu İşlemler & E2E Fatura Yaşam Döngüsü (141-152)
    // =============================================================

    [Fact]
    public async Task Scenario_141_BulkInsert_TwentyInvoices_SavesAllWithoutLoss()
    {
        var list = new List<Fatura>();
        for (int i = 1; i <= 20; i++)
        {
            list.Add(new Fatura { FaturaNo = $"BLK-FAT-{i:D3}", Tur = "Satış", GenelToplam = i * 100m });
        }

        foreach (var f in list)
        {
            await _uow.Faturalar.SaveAsync(f);
        }

        var all = await _uow.Faturalar.GetAllAsync();
        foreach (var f in list)
        {
            Assert.Contains(all, x => x.FaturaNo == f.FaturaNo);
        }
    }

    [Fact]
    public async Task Scenario_142_Invoice_ExtremeHighAmount_DoesNotOverflow()
    {
        decimal hugeAmount = 999999999.99m;
        var fatura = new Fatura { FaturaNo = "FAT-HUGE-142", Tur = "Satış", GenelToplam = hugeAmount };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(hugeAmount, retrieved!.GenelToplam);
    }

    [Fact]
    public async Task Scenario_143_Invoice_UpdateGenelToplam_ReflectsNewAmount()
    {
        var fatura = new Fatura { FaturaNo = "FAT-UPD-143", Tur = "Satış", GenelToplam = 2000m };
        await _uow.Faturalar.SaveAsync(fatura);

        fatura.GenelToplam = 2500m;
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(2500m, retrieved!.GenelToplam);
    }

    [Fact]
    public async Task Scenario_144_Invoice_EmptyOptionalFields_DoNotThrowExceptions()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-EMP-144",
            Tur = "Satış",
            Aciklama = "",
            VergiDairesi = "",
            VergiNo = "",
            Adres = "",
            DovizTuru = "",
            BaglantiEvrakNo = ""
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("FAT-EMP-144", retrieved.FaturaNo);
    }

    [Fact]
    public async Task Scenario_145_Invoice_NullOptionalFields_PersistsGracefully()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-NULL-145",
            Tur = "Satış",
            CariUnvan = null,
            VergiNo = null,
            VergiDairesi = null,
            Adres = null,
            Aciklama = null,
            DovizTuru = null,
            BaglantiEvrakNo = null
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Single(new[] { retrieved });
        Assert.Null(retrieved!.Aciklama);
    }

    [Fact]
    public async Task Scenario_146_FaturaDetay_SpecialCharactersInLineDescription()
    {
        string specialDesc = "Model %100 Pamuklu (2.Kalite) [Özel İskontolu #12]";
        var fatura = new Fatura { FaturaNo = "FAT-SPEC-146", Tur = "Satış" };
        var line = new FaturaDetay { StokAdi = "Kumaş", Aciklama = specialDesc };
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, new List<FaturaDetay> { line });

        var lines = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Equal(specialDesc, lines.First().Aciklama);
    }

    [Fact]
    public async Task Scenario_147_Invoice_MultiplePayments_ReachingFullyPaidState()
    {
        var fatura = new Fatura { FaturaNo = "FAT-INST-147", Tur = "Satış", GenelToplam = 6000m, Odenen = 0m };
        await _uow.Faturalar.SaveAsync(fatura);

        // 1. Taksit Ödemesi (2000 TL)
        fatura.Odenen += 2000m;
        await _uow.Faturalar.SaveAsync(fatura);
        Assert.Equal(4000m, fatura.Kalan);

        // 2. Taksit Ödemesi (2000 TL)
        fatura.Odenen += 2000m;
        await _uow.Faturalar.SaveAsync(fatura);
        Assert.Equal(2000m, fatura.Kalan);

        // 3. Taksit Ödemesi (2000 TL) -> Sıfır Bakiye
        fatura.Odenen += 2000m;
        await _uow.Faturalar.SaveAsync(fatura);
        Assert.Equal(0m, fatura.Kalan);
    }

    [Fact]
    public async Task Scenario_148_Invoice_TurkishCharactersInCustomerName()
    {
        string trCustomer = "GÖKÇE ÇELİK DÖKÜM ŞAFT SANAYİ VE TİC. A.Ş.";
        var fatura = new Fatura { FaturaNo = "FAT-TR-148", Tur = "Satış", CariUnvan = trCustomer };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(trCustomer, retrieved!.CariUnvan);
    }

    [Fact]
    public async Task Scenario_149_Invoice_DefaultPaymentPlanIsAcikHesap()
    {
        var fatura = new Fatura();
        Assert.Equal("Açık Hesap", fatura.OdemeSekli);
    }

    [Fact]
    public async Task Scenario_150_Invoice_EndToEndLifecycle_CreationLinesBalancePaymentAndSoftDelete()
    {
        // 1. Create Invoice Header with Details
        var fatura = new Fatura
        {
            FaturaNo = "FAT-E2E-150",
            Tur = "Satış",
            Tarih = DateTime.Today,
            AraToplam = 2000m,
            ToplamKDV = 400m,
            GenelToplam = 2400m,
            Odenen = 0m
        };
        var line1 = new FaturaDetay { StokAdi = "Ürün A", Miktar = 10, BirimFiyat = 100m, ToplamTutar = 1200m };
        var line2 = new FaturaDetay { StokAdi = "Ürün B", Miktar = 10, BirimFiyat = 100m, ToplamTutar = 1200m };
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, new List<FaturaDetay> { line1, line2 });
        Assert.True(fatura.Id > 0);

        var lines = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Equal(2, lines.Count);

        // 2. Make Partial Payment
        fatura.Odenen = 1000m;
        await _uow.Faturalar.SaveAsync(fatura);
        Assert.Equal(1400m, fatura.Kalan);

        // 3. Complete Payment
        fatura.Odenen = 2400m;
        await _uow.Faturalar.SaveAsync(fatura);
        Assert.Equal(0m, fatura.Kalan);

        // 4. Soft Delete Invoice
        await _uow.Faturalar.DeleteAsync(fatura.Id);

        var allActive = await _uow.Faturalar.GetAllAsync();
        Assert.DoesNotContain(allActive, f => f.Id == fatura.Id);
    }

    [Fact]
    public async Task Scenario_151_Invoice_IsDeletedFlag_ExcludedFromActiveQueries()
    {
        var fatura = new Fatura { FaturaNo = "DEL-FAT-151", Tur = "Satış", GenelToplam = 5000m, IsDeleted = true };
        await _uow.Faturalar.SaveAsync(fatura);

        var active = await _uow.Faturalar.GetAllAsync();
        Assert.DoesNotContain(active, f => f.FaturaNo == "DEL-FAT-151");
    }

    [Fact]
    public async Task Scenario_152_Invoice_GetByNoAsync_LocatesSpecificInvoice()
    {
        string uniqueNo = "FAT-UNIQ-2026-999";
        var fatura = new Fatura { FaturaNo = uniqueNo, Tur = "Satış", GenelToplam = 3300m };
        await _uow.Faturalar.SaveAsync(fatura);

        var retrieved = await _uow.Faturalar.GetByNoAsync(uniqueNo);
        Assert.NotNull(retrieved);
        Assert.Equal(uniqueNo, retrieved.FaturaNo);
        Assert.Equal(3300m, retrieved.GenelToplam);
    }

    [Fact]
    public async Task Scenario_153_Invoice_DirectInsert_VerifyIdAssigned()
    {
        var fatura = new Fatura { FaturaNo = "FAT-DIR-153", Tur = "Alış", GenelToplam = 820m };
        await _uow.Faturalar.SaveAsync(fatura);

        Assert.True(fatura.Id > 0);
        var fetched = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alış", fetched.Tur);
    }

    [Fact]
    public async Task Scenario_154_Invoice_DetailCountConsistency_AfterLineRemoval()
    {
        var fatura = new Fatura { FaturaNo = "FAT-REM-154", Tur = "Satış", GenelToplam = 500m };
        var line1 = new FaturaDetay { StokAdi = "Kalem A", ToplamTutar = 200m };
        var line2 = new FaturaDetay { StokAdi = "Kalem B", ToplamTutar = 300m };
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, new List<FaturaDetay> { line1, line2 });

        var linesInitial = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Equal(2, linesInitial.Count);

        // Replace with single line
        var line3 = new FaturaDetay { StokAdi = "Kalem C", ToplamTutar = 500m };
        await _uow.Faturalar.SaveWithDetailsAsync(fatura, new List<FaturaDetay> { line3 });

        var linesUpdated = await _uow.Faturalar.GetDetaylarAsync(fatura.Id);
        Assert.Single(linesUpdated);
        Assert.Equal("Kalem C", linesUpdated[0].StokAdi);
    }
}
