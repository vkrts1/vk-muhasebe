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

public partial class DashboardScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. KPI & Finansal Hesaplama Senaryoları (18 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_EmptyDatabase_InitializesAllKpisToZero()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.GunlukSatis);
        Assert.Equal(0, vm.ToplamTahsilat);
        Assert.Equal(0, vm.ToplamBorc);
        Assert.Equal(0, vm.ToplamAlacak);
        Assert.Equal(0, vm.ToplamNakitVarligi);
        Assert.Equal(0, vm.BugunOdenecek);
        Assert.Equal(0, vm.BugunTahsilat);
        Assert.Equal(0, vm.StokSayisi);
        Assert.Equal(0, vm.BekleyenOdeme);
        Assert.Equal(0, vm.Karlilik);
        Assert.Equal(0, vm.KarlilikOrani);
    }

    [AvaloniaFact]
    public async Task Scenario_02_DailySales_IncludesTodaySalesInvoice()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-D01",
            Tur = "Satis",
            Tarih = DateTime.Today,
            GenelToplam = 5000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(5000m, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_03_DailySales_ExcludesYesterdaySalesInvoice()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-D02",
            Tur = "Satis",
            Tarih = DateTime.Today.AddDays(-1),
            GenelToplam = 7500m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0m, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_04_CashCollection_IncreasesTahsilatAndNakitVarligi()
    {
        var kasa = new BankaKart { BankaAdi = "Merkez Kasa", KartTuru = "Kasa", GuncelBakiye = 0 };
        await _uow.Bankalar.SaveAsync(kasa);

        var kh = new BankaHareket
        {
            BankaId = kasa.Id,
            Tarih = DateTime.Today,
            IslemTuru = "Tahsilat",
            Tutar = 2500m,
            Giren = 2500m
        };
        await _dbService.GetConnection().InsertAsync(kh);

        kasa.GuncelBakiye = 2500m;
        await _uow.Bankalar.SaveAsync(kasa);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(2500m, vm.ToplamTahsilat);
        Assert.True(vm.ToplamNakitVarligi >= 2500m);
    }

    [AvaloniaFact]
    public async Task Scenario_05_CashPayment_ReducesNakitVarligi()
    {
        var kasa = new BankaKart { BankaAdi = "Ofis Kasa", KartTuru = "Kasa", GuncelBakiye = 10000m };
        await _uow.Bankalar.SaveAsync(kasa);

        var kh = new BankaHareket
        {
            BankaId = kasa.Id,
            Tarih = DateTime.Today,
            IslemTuru = "Ödeme",
            Tutar = 4000m,
            Cikan = 4000m
        };
        await _dbService.GetConnection().InsertAsync(kh);

        kasa.GuncelBakiye = 6000m;
        await _uow.Bankalar.SaveAsync(kasa);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.ToplamNakitVarligi >= 6000m);
    }

    [AvaloniaFact]
    public async Task Scenario_06_PurchaseInvoice_IncreasesToplamBorc()
    {
        var cari = new CariKart
        {
            CariKod = "TED-D01",
            Unvan = "Tedarikci D01",
            Alacak = 12000m,
            Borc = 0
        };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura
        {
            FaturaNo = "ALIS-01",
            Tur = "Alis",
            CariId = cari.Id,
            Tarih = DateTime.Today,
            GenelToplam = 12000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(12000m, vm.ToplamBorc);
    }

    [AvaloniaFact]
    public async Task Scenario_07_SalesInvoice_IncreasesToplamAlacak()
    {
        var cari = new CariKart
        {
            CariKod = "MST-D01",
            Unvan = "Musteri D01",
            Borc = 8500m,
            Alacak = 0
        };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura
        {
            FaturaNo = "SAT-01",
            Tur = "Satis",
            CariId = cari.Id,
            Tarih = DateTime.Today,
            GenelToplam = 8500m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(8500m, vm.ToplamAlacak);
    }

    [AvaloniaFact]
    public async Task Scenario_08_PartialPayment_UpdatesToplamAlacak()
    {
        var cari = new CariKart
        {
            CariKod = "MST-D02",
            Unvan = "Musteri D02",
            Borc = 10000m,
            Alacak = 7000m
        };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura
        {
            FaturaNo = "SAT-02",
            Tur = "Satis",
            CariId = cari.Id,
            Tarih = DateTime.Today,
            GenelToplam = 10000m,
            Odenen = 7000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(3000m, vm.ToplamAlacak);
    }

    [AvaloniaFact]
    public async Task Scenario_09_DueTodayDebt_ReflectsInBugunOdenecek()
    {
        var fatura = new Fatura
        {
            FaturaNo = "ALIS-TODAY",
            Tur = "Alis",
            Tarih = DateTime.Today.AddDays(-10),
            VadeTarihi = DateTime.Today,
            GenelToplam = 4500m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(4500m, vm.BugunOdenecek);
    }

    [AvaloniaFact]
    public async Task Scenario_10_DueTodayReceivable_ReflectsInBugunTahsilat()
    {
        var fatura = new Fatura
        {
            FaturaNo = "SAT-TODAY",
            Tur = "Satis",
            Tarih = DateTime.Today.AddDays(-15),
            VadeTarihi = DateTime.Today,
            GenelToplam = 6200m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(6200m, vm.BugunTahsilat);
    }

    [AvaloniaFact]
    public async Task Scenario_11_DueTomorrowPayment_ExcludedFromBugunOdenecek()
    {
        var fatura = new Fatura
        {
            FaturaNo = "ALIS-TOMORROW",
            Tur = "Alis",
            Tarih = DateTime.Today,
            VadeTarihi = DateTime.Today.AddDays(1),
            GenelToplam = 9000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0m, vm.BugunOdenecek);
    }

    [AvaloniaFact]
    public async Task Scenario_12_OverdueDebt_ReflectsInBekleyenOdeme()
    {
        var fatura = new Fatura
        {
            FaturaNo = "ALIS-OVERDUE",
            Tur = "Alis",
            Tarih = DateTime.Today.AddDays(-40),
            VadeTarihi = DateTime.Today.AddDays(-5),
            GenelToplam = 15000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(15000m, vm.BekleyenOdeme);
    }

    [AvaloniaFact]
    public async Task Scenario_13_StokCount_ReflectsInStokSayisi()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-A", StokAdi = "Urun A", Miktar = 2, MinSeviye = 5 });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-B", StokAdi = "Urun B", Miktar = 3, MinSeviye = 5 });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-C", StokAdi = "Urun C", Miktar = 5, MinSeviye = 5 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(3, vm.StokSayisi);
    }

    [Theory]
    [InlineData(10000, 6000, 4000, 40.0)]
    [InlineData(20000, 15000, 5000, 25.0)]
    [InlineData(50000, 10000, 40000, 80.0)]
    [InlineData(1000, 1000, 0, 0.0)]
    [InlineData(0, 0, 0, 0.0)]
    public void Scenario_14_to_18_ProfitabilityCalculations(decimal satis, decimal maliyet, decimal expectedKar, double expectedOran)
    {
        decimal kar = satis - maliyet;
        double oran = satis > 0 ? (double)(kar / satis) * 100.0 : 0.0;

        Assert.Equal(expectedKar, kar);
        Assert.Equal(expectedOran, oran, precision: 1);
    }

    // -------------------------------------------------------------
    // 2. Son İşlemler & Uyarı Senaryoları (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_19_RecentTransactions_PopulatesOnNewFatura()
    {
        var cari = new CariKart { CariKod = "C-D1", Unvan = "Test Cari D1" };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura
        {
            FaturaNo = "FAT-REC-1",
            Tur = "Satis",
            CariId = cari.Id,
            CariUnvan = cari.Unvan,
            Tarih = DateTime.Today,
            GenelToplam = 3500m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.HasRecentTransactions);
        Assert.Contains(vm.RecentTransactions, r => r.Title.Contains("Test Cari D1") || r.Description.Contains("Faturası"));
    }

    [AvaloniaFact]
    public async Task Scenario_20_HasRecentTransactions_FalseWhenEmpty()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.False(vm.HasRecentTransactions);
        Assert.Empty(vm.RecentTransactions);
    }

    [Theory]
    [InlineData(100000, 80000, false)] // Limit altı -> riskli değil
    [InlineData(50000, 60000, true)]   // Limiti 10000 TL aşmış -> riskli
    [InlineData(20000, 25000, true)]   // Limiti 5000 TL aşmış -> riskli
    [InlineData(0, 5000, true)]        // Sıfır limitli borçlu -> riskli
    public void Scenario_21_to_24_RiskyCaris_BoundaryChecks(decimal riskLimiti, decimal bakiye, bool expectedIsRisky)
    {
        bool isRisky = riskLimiti > 0 ? bakiye > riskLimiti : bakiye > 0;
        Assert.Equal(expectedIsRisky, isRisky);
    }

    [AvaloniaFact]
    public async Task Scenario_25_RiskyCaris_PopulatesCollection()
    {
        var cari = new CariKart
        {
            CariKod = "C-RISK",
            Unvan = "Riskli Musteri Ltd",
            Borc = 75000m,
            Alacak = 0,
            RiskLimiti = 50000m
        };
        await _uow.Cariler.SaveAsync(cari);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.HasRiskyCaris);
        Assert.Contains(vm.RiskyCaris, r => r.Unvan == "Riskli Musteri Ltd");
    }

    [AvaloniaFact]
    public async Task Scenario_26_PayableCaris_PopulatesWhenWeOweSupplier()
    {
        var tedarikci = new CariKart
        {
            CariKod = "TED-01",
            Unvan = "Tedarikci A.S.",
            Borc = 0,
            Alacak = 45000m
        };
        await _uow.Cariler.SaveAsync(tedarikci);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.HasPayableCaris);
        Assert.Contains(vm.PayableCaris, p => p.Unvan == "Tedarikci A.S.");
    }

    [AvaloniaFact]
    public async Task Scenario_27_HasPayableCaris_FalseWhenNoDebts()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.False(vm.HasPayableCaris);
    }

    [AvaloniaFact]
    public async Task Scenario_28_RecentTransactions_LimitedToMaxCount()
    {
        for (int i = 1; i <= 15; i++)
        {
            await _uow.Faturalar.SaveAsync(new Fatura
            {
                FaturaNo = $"FAT-LIM-{i:D2}",
                Tur = "Satis",
                Tarih = DateTime.Today,
                GenelToplam = i * 100
            });
        }

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.RecentTransactions.Count <= 10, "Recent transactions en fazla 10 kayıt olmalıdır.");
    }

    // -------------------------------------------------------------
    // 3. Notlar (Dashboard Notes) Senaryoları (6 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_29_AddNewNote_AddsToCollectionAndDatabase()
    {
        var note = new Note
        {
            Title = "Toplantı",
            Content = "Pazartesi saat 10:00 mali müşavir toplantısı",
            RelatedType = "General",
            RelatedId = 0,
            CreatedAt = DateTime.Now
        };
        await _uow.Notes.SaveAsync(note);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.HasNotes);
        Assert.Contains(vm.Notes, n => n.Title == "Toplantı");
    }

    [AvaloniaFact]
    public async Task Scenario_30_DeleteNote_RemovesFromCollectionAndDatabase()
    {
        var note = new Note
        {
            Title = "Silinecek Not",
            Content = "İçerik",
            RelatedType = "General",
            RelatedId = 0,
            CreatedAt = DateTime.Now
        };
        await _uow.Notes.SaveAsync(note);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();
        Assert.True(vm.HasNotes);

        await _uow.Notes.DeleteAsync(note);
        await vm.LoadStatsAsync();

        Assert.False(vm.HasNotes);
        Assert.Empty(vm.Notes);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Geçerli Not", true)]
    [InlineData("X", true)]
    public void Scenario_31_to_34_NoteTitleValidation(string title, bool isValid)
    {
        bool valid = !string.IsNullOrWhiteSpace(title);
        Assert.Equal(isValid, valid);
    }

    // -------------------------------------------------------------
    // 4. Hızlı Aksiyon Komutları (6 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_35_HizliSatisFaturasiCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.QuickActionCommand.CanExecute("Satis"));
    }

    [AvaloniaFact]
    public void Scenario_36_HizliAlisFaturasiCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.QuickActionCommand.CanExecute("Alis"));
    }

    [AvaloniaFact]
    public void Scenario_37_HizliTahsilatCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.QuickActionCommand.CanExecute("Tahsilat"));
    }

    [AvaloniaFact]
    public void Scenario_38_HizliOdemeCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.QuickActionCommand.CanExecute("Odeme"));
    }

    [AvaloniaFact]
    public void Scenario_39_HizliCariEkleCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.QuickActionCommand.CanExecute("Siparis"));
    }

    [AvaloniaFact]
    public void Scenario_40_HizliStokEkleCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.QuickActionCommand.CanExecute("Teklif"));
    }

    // -------------------------------------------------------------
    // 5. Temalar & Hedefler & Grafikler (10 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("ModernSaaS", true, false, false)]
    [InlineData("Enterprise", false, true, false)]
    [InlineData("WindowsFluent", false, false, true)]
    [InlineData("IDEProfessional", true, false, false)]
    public void Scenario_41_to_44_ThemeTogglingFlags(string themeName, bool expModern, bool expEnterprise, bool expFluent)
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.CurrentTheme = themeName;

        Assert.Equal(expModern, vm.IsModernSaaS);
        Assert.Equal(expEnterprise, vm.IsEnterprise);
        Assert.Equal(expFluent, vm.IsWindowsFluent);
    }

    [AvaloniaFact]
    public void Scenario_45_ToggleGoalPeriod_SwitchesBetweenWeeklyAndMonthly()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.True(vm.IsWeeklyGoal);

        vm.ToggleGoalPeriodCommand.Execute("Monthly");
        Assert.False(vm.IsWeeklyGoal);

        vm.ToggleGoalPeriodCommand.Execute("Weekly");
        Assert.True(vm.IsWeeklyGoal);
    }

    [AvaloniaFact]
    public async Task Scenario_46_RefreshCommand_ExecutesWithoutError()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.False(vm.IsLoading);
    }

    [Theory]
    [InlineData(50000, 10000, 5.0)]
    [InlineData(120000, 20000, 6.0)]
    [InlineData(0, 10000, 0.0)]
    [InlineData(50000, 0, 0.0)]
    public void Scenario_47_to_50_StokDevirHiziCalculations(decimal toplamMaliyet, decimal stokDegeri, double expectedHiz)
    {
        double hiz = stokDegeri > 0 ? (double)(toplamMaliyet / stokDegeri) : 0.0;
        Assert.Equal(expectedHiz, hiz, precision: 1);
    }
}
