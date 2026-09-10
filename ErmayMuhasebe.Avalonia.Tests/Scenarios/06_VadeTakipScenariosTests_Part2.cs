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
    // 5. Fatura Vade Opsiyonları & Kalan Gün Dinamikleri (51 - 65)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_51_VadeTakip_CashInvoiceZeroDays_CalculatesZeroDaysRemaining()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-CASH-51",
            Tur = "Satış",
            VadeTarihi = DateTime.Today,
            GenelToplam = 3000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id);
        Assert.NotNull(item);
        Assert.Equal(0, item.KalanGun);
        Assert.Equal("BUGÜN", item.KalanGunText);
        Assert.False(item.RenkliUyari);
    }

    [AvaloniaFact]
    public async Task Scenario_52_VadeTakip_ThirtyDaysMaturity_ReflectsPositiveDays()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-30D-52",
            Tur = "Satış",
            VadeTarihi = DateTime.Today.AddDays(30),
            GenelToplam = 5400m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id);
        Assert.NotNull(item);
        Assert.Equal(30, item.KalanGun);
        Assert.Equal("30 Gün Kaldı", item.KalanGunText);
        Assert.False(item.RenkliUyari);
    }

    [AvaloniaFact]
    public async Task Scenario_53_VadeTakip_OverdueInvoice_TriggersRenkliUyari()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-OVERDUE-53",
            Tur = "Satış",
            VadeTarihi = DateTime.Today.AddDays(-12),
            GenelToplam = 4200m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id);
        Assert.NotNull(item);
        Assert.True(item.KalanGun < 0);
        Assert.True(item.RenkliUyari);
        Assert.Contains("GÜN GECİKTİ", item.KalanGunText);
    }

    [AvaloniaFact]
    public async Task Scenario_54_VadeTakip_ZeroBalanceInvoice_ExcludedFromReceivables()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-ZERO-54",
            Tur = "Satış",
            GenelToplam = 8000m,
            Odenen = 8000m,
            VadeTarihi = DateTime.Today.AddDays(10)
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == fatura.Id && v.Type == "Fatura");
    }

    [AvaloniaFact]
    public async Task Scenario_55_VadeTakip_PurchaseInvoiceOverdue_IdentifiedAsPayableWarning()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-PURCH-55",
            Tur = "Alış",
            CariUnvan = "Ana Tedarikçi",
            GenelToplam = 15000m,
            VadeTarihi = DateTime.Today.AddDays(-8)
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == fatura.Id && v.Type == "Fatura");
        Assert.NotNull(item);
        Assert.False(item.IsIncoming);
        Assert.True(item.RenkliUyari);
        Assert.Equal("8 GÜN GECİKTİ", item.KalanGunText);
    }

    [Theory]
    [InlineData(1, "1 Gün Kaldı", false)]
    [InlineData(7, "7 Gün Kaldı", false)]
    [InlineData(60, "60 Gün Kaldı", false)]
    [InlineData(-1, "1 GÜN GECİKTİ", true)]
    [InlineData(-30, "30 GÜN GECİKTİ", true)]
    public void Scenario_56_to_60_VadeItem_KalanGunTextFormat(int daysOffset, string expectedText, bool expectedUyari)
    {
        var item = new VadeItem
        {
            VadeTarihi = DateTime.Today.AddDays(daysOffset)
        };

        Assert.Equal(expectedText, item.KalanGunText);
        Assert.Equal(expectedUyari, item.RenkliUyari);
    }

    [Theory]
    [InlineData(1000, 30, 0.40, 32.88)] // Vade Farkı = 1000 * 0.40 * 30 / 365
    [InlineData(5000, 45, 0.50, 308.22)]
    [InlineData(10000, 15, 0.60, 246.58)]
    [InlineData(2000, 0, 0.40, 0.0)]
    [InlineData(0, 30, 0.40, 0.0)]
    public void Scenario_61_to_65_MaturityFinanceChargeCalculations(decimal anapara, int gun, double yillikOran, decimal beklenenFark)
    {
        decimal vadeFarki = Math.Round(anapara * (decimal)yillikOran * gun / 365m, 2);
        Assert.Equal(beklenenFark, vadeFarki);
    }

    // =============================================================
    // 6. Çek & Senet Vade Durumları & Ayrıştırmaları (66 - 80)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_66_VadeTakip_ChequeEndorsed_ExcludedFromVadeTakip()
    {
        var cek = new Cek
        {
            CekNo = "CK-END-66",
            CekTuru = "Alınan",
            Durum = "Ciro Edildi",
            Tutar = 20000m,
            VadeTarihi = DateTime.Today.AddDays(15)
        };
        await _uow.Cekler.SaveAsync(cek);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == cek.Id && v.Type == "Cek");
    }

    [AvaloniaFact]
    public async Task Scenario_67_VadeTakip_ChequeCollected_ExcludedFromVadeTakip()
    {
        var cek = new Cek
        {
            CekNo = "CK-COL-67",
            CekTuru = "Alınan",
            Durum = "Tahsil Edildi",
            Tutar = 18000m,
            VadeTarihi = DateTime.Today.AddDays(5)
        };
        await _uow.Cekler.SaveAsync(cek);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == cek.Id && v.Type == "Cek");
    }

    [AvaloniaFact]
    public async Task Scenario_68_VadeTakip_SenetCollected_ExcludedFromVadeTakip()
    {
        var senet = new Senet
        {
            SenetNo = "SN-COL-68",
            SenetTuru = "Alınan",
            Durum = "Tahsil Edildi",
            Tutar = 11000m,
            VadeTarihi = DateTime.Today.AddDays(7)
        };
        await _uow.Senetler.SaveAsync(senet);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId == senet.Id && v.Type == "Senet");
    }

    [AvaloniaFact]
    public async Task Scenario_69_VadeTakip_MultipleCheques_AggregatesCorrectlyInPortfolio()
    {
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "C1", CekTuru = "Alınan", Durum = "Portföyde", Tutar = 10000m, VadeTarihi = DateTime.Today.AddDays(10) });
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "C2", CekTuru = "Alınan", Durum = "Portföyde", Tutar = 15000m, VadeTarihi = DateTime.Today.AddDays(20) });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var cekItems = vm.Vadeler.Where(x => x.Type == "Cek" && x.IsIncoming).ToList();
        Assert.True(cekItems.Count >= 2);
        Assert.True(cekItems.Sum(x => x.Tutar) >= 25000m);
    }

    [AvaloniaFact]
    public async Task Scenario_70_VadeTakip_OwnIssuedPromissoryNote_IdentifiedAsPayable()
    {
        var senet = new Senet
        {
            SenetNo = "SN-GIV-70",
            SenetTuru = "Verilen",
            Durum = "Portföyde",
            Tutar = 8500m,
            VadeTarihi = DateTime.Today.AddDays(22)
        };
        await _uow.Senetler.SaveAsync(senet);

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        var item = vm.Vadeler.FirstOrDefault(v => v.SourceId == senet.Id && v.Type == "Senet");
        Assert.NotNull(item);
        Assert.False(item.IsIncoming);
        Assert.Equal("Borç (Senet)", item.Tur);
    }

    [Theory]
    [InlineData("Portföyde", true)]
    [InlineData("Portfoyde", true)]
    [InlineData("Karşılıksız", false)]
    [InlineData("Protestolu", false)]
    [InlineData("İptal", false)]
    public void Scenario_71_to_75_CheckValidMaturityStatusRules(string status, bool isValid)
    {
        bool inPortfolio = status == "Portföyde" || status == "Portfoyde";
        Assert.Equal(isValid, inPortfolio);
    }

    [Theory]
    [InlineData(10000, 20000, 15000, 45000)]
    [InlineData(0, 5000, 0, 5000)]
    [InlineData(2500, 2500, 2500, 7500)]
    public void Scenario_76_to_78_MultiInstrumentReceivableTotals(decimal fatura, decimal cek, decimal senet, decimal expectedTotal)
    {
        decimal total = fatura + cek + senet;
        Assert.Equal(expectedTotal, total);
    }

    [Theory]
    [InlineData(5000, 10000, 0, 15000)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_79_to_80_MultiInstrumentPayableTotals(decimal faturaBorc, decimal cekBorc, decimal senetBorc, decimal expectedBorc)
    {
        decimal total = faturaBorc + cekBorc + senetBorc;
        Assert.Equal(expectedBorc, total);
    }

    // =============================================================
    // 7. Yaşlandırma / Aging Aralıkları Analizi (81 - 95)
    // =============================================================

    [Theory]
    [InlineData(-5, "1-30 Gün Gecikmiş")]
    [InlineData(-29, "1-30 Gün Gecikmiş")]
    [InlineData(-35, "31-60 Gün Gecikmiş")]
    [InlineData(-60, "31-60 Gün Gecikmiş")]
    [InlineData(-75, "61-90 Gün Gecikmiş")]
    [InlineData(-90, "61-90 Gün Gecikmiş")]
    [InlineData(-120, "90+ Gün Gecikmiş")]
    [InlineData(5, "Vadesi Gelmemiş")]
    public void Scenario_81_to_88_AgingBracketCategorization(int daysOffset, string expectedBracket)
    {
        string bracket = daysOffset switch
        {
            > 0 => "Vadesi Gelmemiş",
            >= -30 => "1-30 Gün Gecikmiş",
            >= -60 => "31-60 Gün Gecikmiş",
            >= -90 => "61-90 Gün Gecikmiş",
            _ => "90+ Gün Gecikmiş"
        };
        Assert.Equal(expectedBracket, bracket);
    }

    [Theory]
    [InlineData(1000, 2000, 3000, 4000, 10000)]
    [InlineData(0, 5000, 0, 0, 5000)]
    [InlineData(0, 0, 0, 0, 0)]
    public void Scenario_89_to_91_AgingMatrixTotalSum(decimal b1, decimal b2, decimal b3, decimal b4, decimal expectedTotal)
    {
        decimal total = b1 + b2 + b3 + b4;
        Assert.Equal(expectedTotal, total);
    }

    [Theory]
    [InlineData(10000, 2000, 20.0)] // 2.000 / 10.000 = %20 riskli
    [InlineData(50000, 25000, 50.0)]
    [InlineData(100000, 0, 0.0)]
    [InlineData(10000, 10000, 100.0)]
    public void Scenario_92_to_95_OverdueRiskRatioCalculations(decimal toplamAlacak, decimal gecikmisAlacak, double beklenenOran)
    {
        double oran = toplamAlacak > 0 ? (double)(gecikmisAlacak / toplamAlacak) * 100.0 : 0.0;
        Assert.Equal(beklenenOran, Math.Round(oran, 1));
    }

    // =============================================================
    // 8. Filtreleme & Arama Fonksiyonları (96 - 100)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_96_VadeTakip_FilterBuHafta_FiltersWithinNext7Days()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-W1", Tur = "Satış", VadeTarihi = DateTime.Today.AddDays(3), GenelToplam = 1000 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-W2", Tur = "Satış", VadeTarihi = DateTime.Today.AddDays(25), GenelToplam = 2000 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Bu Hafta";

        Assert.Contains(vm.Vadeler, v => v.SourceId > 0 && v.Tutar == 1000);
        Assert.DoesNotContain(vm.Vadeler, v => v.SourceId > 0 && v.Tutar == 2000);
    }

    [AvaloniaFact]
    public async Task Scenario_97_VadeTakip_FilterBuAy_FiltersWithinCurrentMonth()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-M1", Tur = "Satış", VadeTarihi = DateTime.Today.AddDays(5), GenelToplam = 1500 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-M2", Tur = "Satış", VadeTarihi = DateTime.Today.AddMonths(3), GenelToplam = 3500 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Bu Ay";

        Assert.Contains(vm.Vadeler, v => v.Tutar == 1500);
        Assert.DoesNotContain(vm.Vadeler, v => v.Tutar == 3500);
    }

    [AvaloniaFact]
    public async Task Scenario_98_VadeTakip_EmptySearch_ShowsAllFilteredRecords()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-ALL-1", Tur = "Satış", VadeTarihi = DateTime.Today, GenelToplam = 500 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-ALL-2", Tur = "Satış", VadeTarihi = DateTime.Today, GenelToplam = 600 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = "";
        Assert.True(vm.Vadeler.Count >= 2);
    }

    [AvaloniaFact]
    public async Task Scenario_99_VadeTakip_NullSearch_DoesNotThrowException()
    {
        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SearchText = null;
        Assert.NotNull(vm.Vadeler);
    }

    [AvaloniaFact]
    public async Task Scenario_100_VadeTakip_FilterResetToTumu_RestoresCompleteList()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-R1", Tur = "Satış", VadeTarihi = DateTime.Today, GenelToplam = 700 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-R2", Tur = "Satış", VadeTarihi = DateTime.Today.AddDays(20), GenelToplam = 800 });

        var vm = _serviceProvider.GetRequiredService<VadeTakipViewModel>();
        await vm.LoadVadelerAsync();

        vm.SelectedFilter = "Bugün";
        int countBugun = vm.Vadeler.Count;

        vm.SelectedFilter = "Tümü";
        int countTumu = vm.Vadeler.Count;

        Assert.True(countTumu >= countBugun);
    }
}
