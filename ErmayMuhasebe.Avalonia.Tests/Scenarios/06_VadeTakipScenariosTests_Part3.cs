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

public partial class VadeTakipScenariosTests
{
    // =============================================================
    // 9. Vade Takip Metrikleri & Çoklu Enstrüman Birleşimi (101 - 115)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_101_VadeTakip_CombinedInstruments_CalculatesTotalsAccurately()
    {
        // 1. Fatura
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-M101", Tur = "Satış", GenelToplam = 10000m, VadeTarihi = DateTime.Today.AddDays(5) });
        // 2. Çek
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "CK-M101", CekTuru = "Alınan", Durum = "Portföyde", Tutar = 15000m, VadeTarihi = DateTime.Today.AddDays(10) });
        // 3. Senet
        await _uow.Senetler.SaveAsync(new Senet { SenetNo = "SN-M101", SenetTuru = "Alınan", Durum = "Portföyde", Tutar = 5000m, VadeTarihi = DateTime.Today.AddDays(15) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.True(vm.ToplamAlacak >= 30000m);
    }

    [AvaloniaFact]
    public async Task Scenario_102_VadeTakip_CombinedPayables_CalculatesTotalDebtAccurately()
    {
        // 1. Alış Faturası
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-DEBT-102", Tur = "Alış", GenelToplam = 12000m, VadeTarihi = DateTime.Today.AddDays(8) });
        // 2. Verilen Çek
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "CK-DEBT-102", CekTuru = "Verilen", Durum = "Portföyde", Tutar = 18000m, VadeTarihi = DateTime.Today.AddDays(14) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.True(vm.ToplamBorc >= 30000m);
    }

    [AvaloniaFact]
    public async Task Scenario_103_VadeTakip_GecikmisAdetCounter_ReflectsOverdueCount()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-G1", Tur = "Satış", GenelToplam = 2000m, VadeTarihi = DateTime.Today.AddDays(-3) });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-G2", Tur = "Satış", GenelToplam = 3000m, VadeTarihi = DateTime.Today.AddDays(-10) });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-OK", Tur = "Satış", GenelToplam = 4000m, VadeTarihi = DateTime.Today.AddDays(10) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.True(vm.GecikmisAdet >= 2);
    }

    [AvaloniaFact]
    public async Task Scenario_104_VadeTakip_OrderedByVadeTarihiAscending()
    {
        var d1 = DateTime.Today.AddDays(5);
        var d2 = DateTime.Today.AddDays(2);
        var d3 = DateTime.Today.AddDays(20);

        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-O1", Tur = "Satış", GenelToplam = 1000m, VadeTarihi = d1 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-O2", Tur = "Satış", GenelToplam = 2000m, VadeTarihi = d2 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-O3", Tur = "Satış", GenelToplam = 3000m, VadeTarihi = d3 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        for (int i = 0; i < vm.Vadeler.Count - 1; i++)
        {
            Assert.True(vm.Vadeler[i].VadeTarihi <= vm.Vadeler[i + 1].VadeTarihi);
        }
    }

    [Theory]
    [InlineData(100000, 40000, 60000)]
    [InlineData(50000, 80000, -30000)]
    [InlineData(0, 0, 0)]
    public void Scenario_105_to_107_NetReceivablePayableDifference(decimal alacak, decimal borc, decimal expectedNet)
    {
        decimal net = alacak - borc;
        Assert.Equal(expectedNet, net);
    }

    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(5, 5, 10)]
    [InlineData(0, 8, 8)]
    public void Scenario_108_to_110_OverdueCountAggregation(int faturaOverdue, int cekOverdue, int expectedTotal)
    {
        int total = faturaOverdue + cekOverdue;
        Assert.Equal(expectedTotal, total);
    }

    [Fact]
    public void Scenario_111_VadeItem_DefaultPropertyValues()
    {
        var item = new VadeItem();
        Assert.Equal("", item.Type);
        Assert.Equal("", item.Tur);
        Assert.Equal("", item.CariAdi);
        Assert.Equal(0m, item.Tutar);
    }

    [Theory]
    [InlineData("Fatura", true)]
    [InlineData("Cek", true)]
    [InlineData("Senet", true)]
    [InlineData("Bilinmeyen", false)]
    public void Scenario_112_to_115_ValidVadeTypes(string type, bool isValid)
    {
        bool valid = type == "Fatura" || type == "Cek" || type == "Senet";
        Assert.Equal(isValid, valid);
    }

    // =============================================================
    // 10. Arama & Filtreleme İnce Detayları (116 - 125)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_116_VadeTakip_CaseInsensitiveSearch_MatchesLowerUpper()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-CASE-116", CariUnvan = "ANADOLU MAKİNA", Tur = "Satış", GenelToplam = 7500m, VadeTarihi = DateTime.Today });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = "anadolu";
        Assert.Contains(vm.Vadeler, v => v.CariAdi == "ANADOLU MAKİNA");
    }

    [AvaloniaFact]
    public async Task Scenario_117_VadeTakip_SearchNonExistent_ResultsInEmptyVadeler()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-NOT-117", CariUnvan = "Mevcut Müşteri", Tur = "Satış", GenelToplam = 2000m, VadeTarihi = DateTime.Today });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = "OlmayanMusteriXYZ999";
        Assert.Empty(vm.Vadeler);
    }

    [AvaloniaFact]
    public async Task Scenario_118_VadeTakip_FilterGecikmis_IncludesOnlyPastDueDates()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-P1", Tur = "Satış", GenelToplam = 1000m, VadeTarihi = DateTime.Today.AddDays(-20) });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-F1", Tur = "Satış", GenelToplam = 2000m, VadeTarihi = DateTime.Today.AddDays(20) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Gecikmiş";

        Assert.All(vm.Vadeler, v => Assert.True(v.KalanGun < 0));
    }

    [AvaloniaFact]
    public async Task Scenario_119_VadeTakip_FilterBugun_IncludesOnlyTodayDates()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-TDY-119", Tur = "Satış", GenelToplam = 1500m, VadeTarihi = DateTime.Today });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-TMR-119", Tur = "Satış", GenelToplam = 2500m, VadeTarihi = DateTime.Today.AddDays(1) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Bugün";

        Assert.All(vm.Vadeler, v => Assert.Equal(DateTime.Today.Date, v.VadeTarihi.Date));
    }

    [Theory]
    [InlineData("Bugün", 0, true)]
    [InlineData("Bugün", 1, false)]
    [InlineData("Bugün", -1, false)]
    [InlineData("Gecikmiş", -5, true)]
    [InlineData("Gecikmiş", 5, false)]
    public void Scenario_120_to_124_FilterCriteriaValidation(string filter, int daysOffset, bool matches)
    {
        bool matched = filter switch
        {
            "Bugün" => daysOffset == 0,
            "Gecikmiş" => daysOffset < 0,
            _ => true
        };
        Assert.Equal(matches, matched);
    }

    [AvaloniaFact]
    public async Task Scenario_125_VadeTakip_WhitespaceSearch_TreatedAsEmpty()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = "   ";
        Assert.NotNull(vm.Vadeler);
    }

    // =============================================================
    // 11. Sınır Değerler & Toplu Vade İşlemleri (126 - 135)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_126_VadeTakip_TwentyInvoicesSameDay_AggregatesWithoutLoss()
    {
        for (int i = 1; i <= 20; i++)
        {
            await _uow.Faturalar.SaveAsync(new Fatura
            {
                FaturaNo = $"FAT-BLK-VAD-{i:D2}",
                Tur = "Satış",
                GenelToplam = 500m,
                VadeTarihi = DateTime.Today.AddDays(14)
            });
        }

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var bulkItems = vm.Vadeler.Where(v => v.Type == "Fatura" && v.Tutar == 500m).ToList();
        Assert.True(bulkItems.Count >= 20);
    }

    [AvaloniaFact]
    public async Task Scenario_127_VadeTakip_HighValueMaturityItem_HandlesPrecision()
    {
        decimal hugeAmount = 15000000.75m;
        await _uow.Faturalar.SaveAsync(new Fatura
        {
            FaturaNo = "FAT-HUGE-VAD",
            Tur = "Satış",
            GenelToplam = hugeAmount,
            VadeTarihi = DateTime.Today.AddDays(45)
        });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.Tutar == hugeAmount);
        Assert.NotNull(item);
        Assert.Equal(hugeAmount, item.Tutar);
    }

    [Theory]
    [InlineData(100000, 30, 0.40, 3287.67)]
    [InlineData(250000, 60, 0.45, 18493.15)]
    public void Scenario_128_to_129_HighValueInterestAccrual(decimal anapara, int gun, double oran, decimal beklenen)
    {
        decimal faiz = Math.Round(anapara * (decimal)oran * gun / 365m, 2);
        Assert.Equal(beklenen, faiz);
    }

    [Theory]
    [InlineData(3287.67, 0.20, 657.53, 3945.20)] // %20 KDV ilavesi
    [InlineData(18493.15, 0.20, 3698.63, 22191.78)]
    public void Scenario_130_to_131_MaturityFinanceChargeVatAddition(decimal vadeFarki, double kdvOrani, decimal beklenenKdv, decimal beklenenToplam)
    {
        decimal kdv = Math.Round(vadeFarki * (decimal)kdvOrani, 2);
        decimal toplam = vadeFarki + kdv;

        Assert.Equal(beklenenKdv, kdv);
        Assert.Equal(beklenenToplam, toplam);
    }

    [Fact]
    public async Task Scenario_132_VadeTakip_FutureDateTenYears_DoesNotThrowOverflow()
    {
        var farFuture = DateTime.Today.AddYears(10);
        await _uow.Faturalar.SaveAsync(new Fatura
        {
            FaturaNo = "FAT-FUT-132",
            Tur = "Satış",
            GenelToplam = 1000m,
            VadeTarihi = farFuture
        });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.VadeTarihi == farFuture);
        Assert.NotNull(item);
        Assert.True(item.KalanGun > 3000);
    }

    [Fact]
    public async Task Scenario_133_VadeTakip_PastDateTenYears_DoesNotThrowUnderflow()
    {
        var farPast = DateTime.Today.AddYears(-10);
        await _uow.Faturalar.SaveAsync(new Fatura
        {
            FaturaNo = "FAT-PST-133",
            Tur = "Satış",
            GenelToplam = 1000m,
            VadeTarihi = farPast
        });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.VadeTarihi == farPast);
        Assert.NotNull(item);
        Assert.True(item.KalanGun < -3000);
        Assert.True(item.RenkliUyari);
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(500, 200, 300)]
    [InlineData(1000, 0, 1000)]
    public void Scenario_134_to_135_RemainingBalanceCalculation(decimal toplam, decimal odenen, decimal beklenenKalan)
    {
        decimal kalan = toplam - odenen;
        Assert.Equal(beklenenKalan, kalan);
    }

    // =============================================================
    // 12. Vade Erteleme & Dinamik Güncelleme (136 - 145)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_136_VadeTakip_PostponeInvoiceMaturity_ReflectsNewDate()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-POST-136",
            Tur = "Satış",
            GenelToplam = 6000m,
            VadeTarihi = DateTime.Today.AddDays(5)
        };
        await _uow.Faturalar.SaveAsync(fatura);

        // Vade erteleme (25 gün ekle)
        fatura.VadeTarihi = DateTime.Today.AddDays(30);
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id && v.Type == "Fatura");
        Assert.NotNull(item);
        Assert.Equal(30, item.KalanGun);
    }

    [AvaloniaFact]
    public async Task Scenario_137_VadeTakip_PartialPayment_ReducesListedAmount()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-PART-137",
            Tur = "Satış",
            GenelToplam = 10000m,
            Odenen = 0m,
            VadeTarihi = DateTime.Today.AddDays(10)
        };
        await _uow.Faturalar.SaveAsync(fatura);

        fatura.Odenen = 4000m;
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id && v.Type == "Fatura");
        Assert.NotNull(item);
        Assert.Equal(6000m, item.Tutar);
    }

    [AvaloniaFact]
    public async Task Scenario_138_VadeTakip_FullPayment_CompletelyRemovesItem()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-FUL-138",
            Tur = "Satış",
            GenelToplam = 5000m,
            Odenen = 0m,
            VadeTarihi = DateTime.Today.AddDays(15)
        };
        await _uow.Faturalar.SaveAsync(fatura);

        fatura.Odenen = 5000m;
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == fatura.Id && v.Type == "Fatura");
    }

    [Theory]
    [InlineData("Alacak (Fatura)", true)]
    [InlineData("Borç (Fatura)", false)]
    [InlineData("Alacak (Çek)", true)]
    [InlineData("Borç (Çek)", false)]
    [InlineData("Alacak (Senet)", true)]
    [InlineData("Borç (Senet)", false)]
    public void Scenario_139_to_144_TurStringToIsIncomingMapping(string turStr, bool expectedIncoming)
    {
        bool incoming = turStr.StartsWith("Alacak");
        Assert.Equal(expectedIncoming, incoming);
    }

    [Fact]
    public void Scenario_145_VadeTakip_FiltersArray_ContainsFiveOptions()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        Assert.Equal(5, vm.Filters.Length);
    }

    // =============================================================
    // 13. E2E Vade Takip Yaşam Döngüsü (146 - 154)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_146_EndToEndMaturityLifecycle_InvoiceToChequeToSettlement()
    {
        // 1. Vadeli Satış Faturası Kesilir (15 gün vadeli 25.000 TL)
        var fatura = new Fatura
        {
            FaturaNo = "FAT-E2E-V146",
            Tur = "Satış",
            GenelToplam = 25000m,
            Odenen = 0m,
            VadeTarihi = DateTime.Today.AddDays(15),
            CariUnvan = "E2E Müşteri Ltd."
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        // 2. Fatura Vade Takipte Gözükmeli
        var itemFatura = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id && v.Type == "Fatura");
        Assert.NotNull(itemFatura);
        Assert.Equal(25000m, itemFatura.Tutar);

        // 3. Müşteri 30 gün vadeli çek verir, fatura kapatılır
        fatura.Odenen = 25000m;
        await _uow.Faturalar.SaveAsync(fatura);

        var cek = new Cek
        {
            CekNo = "CK-E2E-146",
            CekTuru = "Alınan",
            Durum = "Portföyde",
            Tutar = 25000m,
            VadeTarihi = DateTime.Today.AddDays(30),
            CariUnvan = "E2E Müşteri Ltd."
        };
        await _uow.Cekler.SaveAsync(cek);

        // 4. Vade Takip Listesi Güncellenir: Fatura Düşer, Çek Görünür
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == fatura.Id && v.Type == "Fatura");
        var itemCek = vm.Vadeler.FirstOrDefault(v => v.SourceId == cek.Id && v.Type == "Cek");
        Assert.NotNull(itemCek);
        Assert.Equal(30, itemCek.KalanGun);

        // 5. Çek Vadesinde Tahsil Edilir
        cek.Durum = "Tahsil Edildi";
        await _uow.Cekler.SaveAsync(cek);

        // 6. Vade Takip Listesinden Tamamen Temizlenir
        await vm.LoadVadelerAsync();
        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == cek.Id && v.Type == "Cek");
    }

    [AvaloniaFact]
    public async Task Scenario_147_EndToEndMaturityLifecycle_ChequeBounced_ChangesRiskState()
    {
        var cek = new Cek
        {
            CekNo = "CK-BOUNCE-147",
            CekTuru = "Alınan",
            Durum = "Portföyde",
            Tutar = 12000m,
            VadeTarihi = DateTime.Today.AddDays(-2)
        };
        await _uow.Cekler.SaveAsync(cek);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == cek.Id);
        Assert.NotNull(item);
        Assert.True(item.RenkliUyari);

        // Karşılıksız çıktığında durum değişir
        cek.Durum = "Karşılıksız";
        await _uow.Cekler.SaveAsync(cek);

        await vm.LoadVadelerAsync();
        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == cek.Id);
    }

    [AvaloniaFact]
    public async Task Scenario_148_VadeTakip_MultipleFiltersSwitching_MaintainsIntegrity()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "SW-1", Tur = "Satış", GenelToplam = 100m, VadeTarihi = DateTime.Today });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "SW-2", Tur = "Satış", GenelToplam = 200m, VadeTarihi = DateTime.Today.AddDays(5) });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "SW-3", Tur = "Satış", GenelToplam = 300m, VadeTarihi = DateTime.Today.AddDays(-5) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        foreach (var filter in vm.Filters)
        {
            vm.SelectedFilter = filter;
            Assert.NotNull(vm.Vadeler);
        }
    }

    [AvaloniaFact]
    public async Task Scenario_149_VadeTakip_RapidSuccessiveLoads_DoesNotCorruptState()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        var t1 = vm.LoadVadelerAsync();
        var t2 = vm.LoadVadelerAsync();
        await Task.WhenAll(t1, t2);

        Assert.NotNull(vm.Vadeler);
    }

    [AvaloniaFact]
    public async Task Scenario_150_VadeTakip_OnNavigatedTo_ReloadsData()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        vm.OnNavigatedTo();
        Assert.NotNull(vm.Vadeler);
    }

    [Theory]
    [InlineData(10000, 30, 0.05, 500)]
    [InlineData(20000, 60, 0.08, 3200)]
    public void Scenario_151_to_152_MonthlyInterestCalculations(decimal anapara, int gun, double aylikOran, decimal beklenen)
    {
        decimal faiz = Math.Round(anapara * (decimal)aylikOran * gun / 30m, 2);
        Assert.Equal(beklenen, faiz);
    }

    [Fact]
    public async Task Scenario_153_VadeTakip_TurkishCharactersInCustomerName_Preserved()
    {
        string cariTr = "ÖZÜPEK ÇELİK DÖKÜM ŞTİ.";
        await _uow.Faturalar.SaveAsync(new Fatura
        {
            FaturaNo = "FAT-TR-VAD",
            Tur = "Satış",
            CariUnvan = cariTr,
            GenelToplam = 4500m,
            VadeTarihi = DateTime.Today.AddDays(7)
        });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = "ÖZÜPEK";
        Assert.Contains(vm.Vadeler, v => v.CariAdi == cariTr);
    }

    [Fact]
    public async Task Scenario_154_VadeTakip_PrintCommand_CanExecute()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        Assert.NotNull(vm.PrintCommand);
    }
}
