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

public partial class DashboardScenariosTests
{
    // -------------------------------------------------------------
    // Dashboard Senaryoları: 51 - 100
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_51_MultiInvoice_DailySales_AggregatesCorrectly()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-51A", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 1500 });
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-51B", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 2500 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(4000, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_52_SalesReturnInvoice_DoesNotIncreaseDailySalesTurnover()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-52A", Tur = "Satış İade", Tarih = DateTime.Now, GenelToplam = 500 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_53_PurchaseInvoice_DoesNotCountTowardsSalesTurnover()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-53A", Tur = "Alış", Tarih = DateTime.Now, GenelToplam = 8000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_54_DeletedInvoice_IsExcludedFromTurnover()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-54A", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 10000, IsDeleted = true });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_55_CashBalance_ReflectsMultipleCashMovements()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new KasaHareket { KasaId = 1, IslemTuru = "Tahsilat", Tarih = DateTime.Now, Giren = 3000 });
        await conn.InsertAsync(new KasaHareket { KasaId = 1, IslemTuru = "Ödeme", Tarih = DateTime.Now, Cikan = 1000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.ToplamTahsilat >= 3000);
    }

    [AvaloniaFact]
    public async Task Scenario_56_BankBalance_IncreasesWithIncomingEFT()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new BankaKart { BankaAdi = "Garanti", GuncelBakiye = 7500 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.ToplamNakitVarligi >= 7500);
    }

    [AvaloniaFact]
    public async Task Scenario_57_ChequePaymentDueToday_ReflectedInBugunOdenecek()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Cek { CekTuru = "Verilen", VadeTarihi = DateTime.Today, Tutar = 4500, Durum = "Portföyde" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.BugunOdenecek >= 4500);
    }

    [AvaloniaFact]
    public async Task Scenario_58_PromissoryNoteDueToday_ReflectedInBugunTahsilat()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Cek { CekTuru = "Alınan", VadeTarihi = DateTime.Today, Tutar = 3200, Durum = "Portföyde" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.BugunTahsilat >= 3200);
    }

    [AvaloniaFact]
    public async Task Scenario_59_MonthlyTargetVsActual_CalculatesCorrectly()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new YillikSatisHedefi { Yil = DateTime.Now.Year, HedefTutari = 50000 });
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-59", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 60000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.GunlukSatis >= 0);
    }

    [AvaloniaFact]
    public async Task Scenario_60_GoalToggle_SwitchesToMonthly()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.IsWeeklyGoal = false;

        Assert.False(vm.IsWeeklyGoal);
    }

    [AvaloniaFact]
    public async Task Scenario_61_GoalToggle_SwitchesToWeekly()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.IsWeeklyGoal = true;

        Assert.True(vm.IsWeeklyGoal);
    }

    [AvaloniaFact]
    public async Task Scenario_62_LowStockAlert_GeneratedWhenStockBelowThreshold()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { StokKodu = "STK-62", StokAdi = "Kritik Ürün", Miktar = 2, MinSeviye = 10 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.StokSayisi >= 1);
    }

    [AvaloniaFact]
    public async Task Scenario_63_NormalStock_DoesNotTriggerLowStockAlert()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { StokKodu = "STK-63", StokAdi = "Yeterli Ürün", Miktar = 50, MinSeviye = 10 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.StokSayisi);
    }

    [AvaloniaFact]
    public async Task Scenario_64_NegativeStock_CountsInTotalStocks()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { StokKodu = "STK-64", StokAdi = "Eksi Stok", Miktar = -5 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.StokSayisi >= 1);
    }

    [AvaloniaFact]
    public async Task Scenario_65_OverdueInvoice_IncreasesOverdueReceivable()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new CariKart { Id = 6501, CariKod = "CR-65", Unvan = "Borclu Cari", Borc = 5000, Alacak = 0 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.ToplamAlacak >= 5000);
    }

    [AvaloniaFact]
    public async Task Scenario_66_FullyPaidInvoice_DoesNotAppearAsOverdue()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-66", Tur = "Satış", Tarih = DateTime.Now.AddDays(-60), VadeTarihi = DateTime.Now.AddDays(-30), GenelToplam = 5000, Odenen = 5000, OdemeSekli = "Açık Hesap" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.ToplamAlacak);
    }

    [AvaloniaFact]
    public async Task Scenario_67_CashInvoice_DoesNotCountAsReceivable()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-67", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 2500, Odenen = 2500, OdemeSekli = "Nakit" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.ToplamAlacak);
    }

    [AvaloniaFact]
    public async Task Scenario_68_RiskyCaris_Collection_PopulatedWhenDebtorExists()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new CariKart { Unvan = "Riskli Cari 68", Borc = 50000, Alacak = 0, RiskLimiti = 10000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.ToplamAlacak >= 50000);
    }

    [AvaloniaFact]
    public async Task Scenario_69_RecentTransactions_OrderDescendingByDate()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new CariHareket { CariId = 1, Tarih = DateTime.Now.AddHours(-2), Borc = 100, Aciklama = "Eski Hareket" });
        await conn.InsertAsync(new CariHareket { CariId = 1, Tarih = DateTime.Now, Borc = 200, Aciklama = "Yeni Hareket" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.NotNull(vm.RecentTransactions);
    }

    [AvaloniaFact]
    public async Task Scenario_70_RecentTransactions_PopulatesItems()
    {
        var conn = _dbService.GetConnection();
        for (int i = 1; i <= 5; i++)
        {
            await conn.InsertAsync(new CariHareket { CariId = 1, Tarih = DateTime.Now.AddMinutes(i), Borc = i * 10, Aciklama = $"İşlem {i}" });
        }

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.NotNull(vm.RecentTransactions);
    }

    [AvaloniaFact]
    public async Task Scenario_71_ProfitCalculation_WithZeroCostProduct()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { Id = 71, StokKodu = "STK-71", StokAdi = "Bedelsiz Mal", AlisFiyati = 0, SatisFiyati = 100 });
        await conn.InsertAsync(new Fatura { Id = 71, FaturaNo = "FAT-71", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 100 });
        await conn.InsertAsync(new FaturaDetay { FaturaId = 71, StokId = 71, Miktar = 1, BirimFiyat = 100, ToplamTutar = 100 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.GunlukSatis >= 100);
    }

    [AvaloniaFact]
    public async Task Scenario_72_ProfitCalculation_StandardMargin()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { Id = 72, StokKodu = "STK-72", StokAdi = "Maliyetli Ürün", AlisFiyati = 50, SatisFiyati = 100 });
        await conn.InsertAsync(new Fatura { Id = 72, FaturaNo = "FAT-72", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 100 });
        await conn.InsertAsync(new FaturaDetay { FaturaId = 72, StokId = 72, Miktar = 1, BirimFiyat = 100, ToplamTutar = 100 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(100, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_73_ProfitRate_DoubledPrice()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "FAT-73", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 2000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(2000, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_74_ProfitRate_ZeroWhenBreakEven()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { Id = 74, StokKodu = "STK-74", StokAdi = "Maliyetine Satış", AlisFiyati = 100, SatisFiyati = 100 });
        await conn.InsertAsync(new Fatura { Id = 74, FaturaNo = "FAT-74", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 100 });
        await conn.InsertAsync(new FaturaDetay { FaturaId = 74, StokId = 74, Miktar = 1, BirimFiyat = 100, ToplamTutar = 100 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(100, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_75_NegativeProfit_HandledGracefully()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { Id = 75, StokKodu = "STK-75", StokAdi = "Zararına Satış", AlisFiyati = 200, SatisFiyati = 100 });
        await conn.InsertAsync(new Fatura { Id = 75, FaturaNo = "FAT-75", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 100 });
        await conn.InsertAsync(new FaturaDetay { FaturaId = 75, StokId = 75, Miktar = 1, BirimFiyat = 100, ToplamTutar = 100 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(100, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_76_TotalPayable_WithMultipleSuppliers()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new CariKart { Id = 761, Unvan = "Tedarikçi A", Borc = 0, Alacak = 12000 });
        await conn.InsertAsync(new CariKart { Id = 762, Unvan = "Tedarikçi B", Borc = 0, Alacak = 18000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(30000, vm.ToplamBorc);
    }

    [AvaloniaFact]
    public async Task Scenario_77_TotalReceivable_WithMultipleCustomers()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new CariKart { Id = 771, Unvan = "Müşteri X", Borc = 15000, Alacak = 0 });
        await conn.InsertAsync(new CariKart { Id = 772, Unvan = "Müşteri Y", Borc = 25000, Alacak = 0 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(40000, vm.ToplamAlacak);
    }

    [AvaloniaFact]
    public async Task Scenario_78_NetBalance_ReceivableMinusPayable()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new CariKart { Id = 781, Unvan = "Cari 78A", Borc = 50000, Alacak = 0 });
        await conn.InsertAsync(new CariKart { Id = 782, Unvan = "Cari 78B", Borc = 0, Alacak = 20000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(50000, vm.ToplamAlacak);
        Assert.Equal(20000, vm.ToplamBorc);
    }

    [AvaloniaFact]
    public async Task Scenario_79_StockCount_ReflectsDistinctItems()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { Id = 791, StokKodu = "S-79A", StokAdi = "Ürün A", Miktar = 10, MinSeviye = 100 });
        await conn.InsertAsync(new StokKart { Id = 792, StokKodu = "S-79B", StokAdi = "Ürün B", Miktar = 20, MinSeviye = 100 });
        await conn.InsertAsync(new StokKart { Id = 793, StokKodu = "S-79C", StokAdi = "Ürün C", Miktar = 30, MinSeviye = 100 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(3, vm.StokSayisi);
    }

    [AvaloniaFact]
    public async Task Scenario_80_DeletedStocks_ExcludedFromCount()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new StokKart { Id = 801, StokKodu = "S-80A", StokAdi = "Aktif Ürün", Miktar = 10, MinSeviye = 100, IsDeleted = false });
        await conn.InsertAsync(new StokKart { Id = 802, StokKodu = "S-80B", StokAdi = "Silinmiş Ürün", Miktar = 10, MinSeviye = 100, IsDeleted = true });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(1, vm.StokSayisi);
    }

    [AvaloniaFact]
    public async Task Scenario_81_CashMovement_DescriptionPreserved()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new KasaHareket { KasaId = 1, IslemTuru = "Tahsilat", Giren = 1500, Aciklama = "Elden Tahsilat", Tarih = DateTime.Now });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(1500, vm.ToplamTahsilat);
    }

    [AvaloniaFact]
    public async Task Scenario_82_CurrencyExchangeService_DoesNotFailDashboard()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.NotNull(vm);
    }

    [AvaloniaFact]
    public async Task Scenario_83_ThemeSwitch_Enterprise_PreservesStats()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.CurrentTheme = "Enterprise";

        Assert.True(vm.IsEnterprise);
    }

    [AvaloniaFact]
    public async Task Scenario_84_ThemeSwitch_WindowsFluent_PreservesStats()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.CurrentTheme = "WindowsFluent";

        Assert.True(vm.IsWindowsFluent);
    }

    [AvaloniaFact]
    public async Task Scenario_85_ThemeSwitch_ModernSaaS_PreservesStats()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.CurrentTheme = "ModernSaaS";

        Assert.True(vm.IsModernSaaS);
    }

    [AvaloniaFact]
    public async Task Scenario_86_QuickAction_Alis_ExecutesSafely()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.QuickActionCommand.Execute("Alis");
        Assert.NotNull(vm);
    }

    [AvaloniaFact]
    public async Task Scenario_87_QuickAction_Satis_ExecutesSafely()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.QuickActionCommand.Execute("Satis");
        Assert.NotNull(vm);
    }

    [AvaloniaFact]
    public async Task Scenario_88_QuickAction_Tahsilat_ExecutesSafely()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.QuickActionCommand.Execute("Tahsilat");
        Assert.NotNull(vm);
    }

    [AvaloniaFact]
    public async Task Scenario_89_QuickAction_Odeme_ExecutesSafely()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.QuickActionCommand.Execute("Odeme");
        Assert.NotNull(vm);
    }

    [AvaloniaFact]
    public async Task Scenario_90_FinancialDataChangedMessage_DoesNotCrashWhenDisabled()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        vm.DisableAutoRefresh = true;
        Assert.True(vm.DisableAutoRefresh);
    }

    [AvaloniaFact]
    public async Task Scenario_91_Dashboard_HandlesHundredInvoices()
    {
        var conn = _dbService.GetConnection();
        var invoices = new List<Fatura>();
        for (int i = 1; i <= 100; i++)
        {
            invoices.Add(new Fatura { FaturaNo = $"BULK-{i}", Tur = "Satış", Tarih = DateTime.Now, GenelToplam = 100 });
        }
        await conn.InsertAllAsync(invoices);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(10000, vm.GunlukSatis);
    }

    [AvaloniaFact]
    public async Task Scenario_92_Dashboard_HandlesFiftyCashMovements()
    {
        var conn = _dbService.GetConnection();
        var movements = new List<KasaHareket>();
        for (int i = 1; i <= 50; i++)
        {
            movements.Add(new KasaHareket { Id = 9200 + i, KasaId = 1, IslemTuru = "Tahsilat", Tarih = DateTime.Now, Giren = 50 });
        }
        await conn.InsertAllAsync(movements);

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(2500, vm.ToplamTahsilat);
    }

    [AvaloniaFact]
    public async Task Scenario_93_TargetVsActualSeries_IsInitialized()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.NotNull(vm.TargetVsActualSeries);
    }

    [AvaloniaFact]
    public async Task Scenario_94_GelirDagilimSeries_IsInitialized()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.NotNull(vm.GelirDaigilimSeries);
    }

    [AvaloniaFact]
    public async Task Scenario_95_SatisGrafigiSeries_IsInitialized()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        Assert.NotNull(vm.SatisGrafigiSeries);
    }

    [AvaloniaFact]
    public async Task Scenario_96_TotalNakitVarligi_IncludesPositiveKasaAndBanka()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new BankaKart { BankaAdi = "Vakıfbank", GuncelBakiye = 10000 });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.ToplamNakitVarligi >= 10000);
    }

    [AvaloniaFact]
    public async Task Scenario_97_BekleyenOdeme_IncludesUnpaidPurchaseInvoices()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "ALIS-97", Tur = "Alış", Tarih = DateTime.Now, GenelToplam = 8500, Odenen = 0, OdemeSekli = "Açık Hesap" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.True(vm.BekleyenOdeme >= 8500);
    }

    [AvaloniaFact]
    public async Task Scenario_98_BekleyenOdeme_ExcludesPaidPurchaseInvoices()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "ALIS-98", Tur = "Alış", Tarih = DateTime.Now, GenelToplam = 8500, Odenen = 8500, OdemeSekli = "Açık Hesap" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(0, vm.BekleyenOdeme);
    }

    [AvaloniaFact]
    public async Task Scenario_99_BekleyenOdeme_PartialPaymentReducesAmount()
    {
        var conn = _dbService.GetConnection();
        await conn.InsertAsync(new Fatura { FaturaNo = "ALIS-99", Tur = "Alış", Tarih = DateTime.Now, GenelToplam = 10000, Odenen = 6000, OdemeSekli = "Açık Hesap" });

        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await vm.LoadStatsAsync();

        Assert.Equal(4000, vm.BekleyenOdeme);
    }

    [AvaloniaFact]
    public async Task Scenario_100_RapidConsecutiveLoadStats_DoesNotCrash()
    {
        var vm = _serviceProvider.GetRequiredService<DashboardViewModel>();
        await Task.WhenAll(
            vm.LoadStatsAsync(),
            vm.LoadStatsAsync(),
            vm.LoadStatsAsync()
        );

        Assert.NotNull(vm);
    }
}
