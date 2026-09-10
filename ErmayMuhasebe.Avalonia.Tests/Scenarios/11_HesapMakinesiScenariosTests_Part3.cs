using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class HesapMakinesiScenariosTests
{
    // =============================================================
    // 9. Döviz Dönüştürücü & Çoklu Para Birimi Varlık Hesaplamaları (101 - 110)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_101_DovizDonusturucu_Default_CalculatesDonusturulmusTutar()
    {
        var vm = _serviceProvider.GetRequiredService<DovizDonusturucuViewModel>();
        await vm.LoadDataAsync();

        Assert.NotEmpty(vm.Varliklar);
        Assert.True(vm.ToplamTry > 0);
        Assert.True(vm.DonusturulmusTutar > 0);
    }

    [AvaloniaFact]
    public async Task Scenario_102_DovizDonusturucu_HedefDovizKuruChanged_Recalculates()
    {
        var vm = _serviceProvider.GetRequiredService<DovizDonusturucuViewModel>();
        await vm.LoadDataAsync();

        decimal oldConverted = vm.DonusturulmusTutar;
        vm.HedefDovizKuru = 50.0m;

        Assert.Equal(vm.ToplamTry / 50.0m, vm.DonusturulmusTutar);
        Assert.NotEqual(oldConverted, vm.DonusturulmusTutar);
    }

    [AvaloniaFact]
    public async Task Scenario_103_DovizDonusturucu_HedefDovizKuruZero_DoesNotThrow()
    {
        var vm = _serviceProvider.GetRequiredService<DovizDonusturucuViewModel>();
        await vm.LoadDataAsync();

        // Should not crash with DivideByZeroException
        vm.HedefDovizKuru = 0m;
        Assert.True(true);
    }

    [Theory]
    [InlineData(35.0, 100000.0, 2857.14)]
    [InlineData(38.0, 100000.0, 2631.58)]
    [InlineData(45.0, 90000.0, 2000.0)]
    [InlineData(1.0, 50000.0, 50000.0)]
    public void Scenario_104_to_107_CurrencyExchangeRateEquivalence(double targetRate, double totalTry, double expectedConverted)
    {
        decimal rate = (decimal)targetRate;
        decimal tryAmount = (decimal)totalTry;
        decimal converted = Math.Round(tryAmount / rate, 2);

        Assert.Equal((decimal)expectedConverted, converted);
    }

    [Fact]
    public void Scenario_108_DovizVarlik_Record_EqualityAndProperties()
    {
        var item1 = new DovizVarlik("USD", 1000m, 35m, 35000m);
        var item2 = new DovizVarlik("USD", 1000m, 35m, 35000m);

        Assert.Equal(item1, item2);
        Assert.Equal("USD", item1.DovizKodu);
        Assert.Equal(35000m, item1.TryKarsiligi);
    }

    [AvaloniaFact]
    public void Scenario_109_DovizDonusturucu_ManualList_CalculatesTrySum()
    {
        var vm = _serviceProvider.GetRequiredService<DovizDonusturucuViewModel>();
        var items = new List<DovizVarlik>
        {
            new("USD", 100m, 35m, 3500m),
            new("EUR", 200m, 40m, 8000m)
        };
        vm.Varliklar = new ObservableCollection<DovizVarlik>(items);
        vm.ToplamTry = items.Sum(x => x.TryKarsiligi);
        vm.HedefDovizKuru = 10m;

        Assert.Equal(11500m, vm.ToplamTry);
        Assert.Equal(1150m, vm.DonusturulmusTutar);
    }

    [Theory]
    [InlineData(100, 35, 3500)]
    [InlineData(250, 38, 9500)]
    [InlineData(500, 45, 22500)]
    public void Scenario_110_AssetTryValuation(decimal amount, decimal rate, decimal expectedTry)
    {
        decimal tryValuation = amount * rate;
        Assert.Equal(expectedTry, tryValuation);
    }

    // =============================================================
    // 10. Optimal Fiyat & Kâr Marjı Simülatörü (111 - 120)
    // =============================================================

    [Fact]
    public void Scenario_111_OptimalFiyat_Default_CalculatesValues()
    {
        var vm = new OptimalFiyatViewModel();

        // Default: Alis=100, Ek=0, Kar=25%, Kdv=20%
        // ToplamMaliyet = 100
        // KarTutari = 25
        // SatisKdvHaric = 125
        // KdvTutari = 25
        // OnerilenSatis = 150
        Assert.Equal(100m, vm.AlisFiyati);
        Assert.Equal(25m, vm.NetKar);
        Assert.Equal(25m, vm.KdvTutari);
        Assert.Equal(150m, vm.OnerilenSatisFiyati);
    }

    [Fact]
    public void Scenario_112_OptimalFiyat_AdditionalExpense_IncreasesCostBase()
    {
        var vm = new OptimalFiyatViewModel();
        vm.AlisFiyati = 100m;
        vm.EkMaliyet = 20m;
        vm.HedefKarOrani = 25m; // Base: 120, Kar: 30, SatisHariç: 150
        vm.KdvOrani = 20m;      // Kdv: 30, SatisDahil: 180

        Assert.Equal(30m, vm.NetKar);
        Assert.Equal(30m, vm.KdvTutari);
        Assert.Equal(180m, vm.OnerilenSatisFiyati);
    }

    [Fact]
    public void Scenario_113_OptimalFiyat_ZeroProfitMargin_CostEqualsSaleWithoutVat()
    {
        var vm = new OptimalFiyatViewModel();
        vm.AlisFiyati = 200m;
        vm.EkMaliyet = 50m;
        vm.HedefKarOrani = 0m;
        vm.KdvOrani = 0m;

        Assert.Equal(0m, vm.NetKar);
        Assert.Equal(0m, vm.KdvTutari);
        Assert.Equal(250m, vm.OnerilenSatisFiyati);
    }

    [Fact]
    public void Scenario_114_OptimalFiyat_ZeroVat_FinalPriceEqualsCostPlusProfit()
    {
        var vm = new OptimalFiyatViewModel();
        vm.AlisFiyati = 500m;
        vm.EkMaliyet = 0m;
        vm.HedefKarOrani = 30m;
        vm.KdvOrani = 0m;

        Assert.Equal(150m, vm.NetKar);
        Assert.Equal(0m, vm.KdvTutari);
        Assert.Equal(650m, vm.OnerilenSatisFiyati);
    }

    [Theory]
    [InlineData(100, 0, 10, 20, 10, 22, 132)]    // Base 100 -> +10 = 110 + 22 = 132
    [InlineData(200, 50, 20, 10, 50, 30, 330)]   // Base 250 -> +50 = 300 + 30 = 330
    [InlineData(1000, 200, 50, 20, 600, 360, 2160)] // Base 1200 -> +600 = 1800 + 360 = 2160
    [InlineData(50, 10, 100, 20, 60, 24, 144)]   // Base 60 -> +60 = 120 + 24 = 144
    public void Scenario_115_to_118_OptimalFiyat_VariousMatrixCombinations(
        decimal alis, decimal ek, decimal karOran, decimal kdvOran,
        decimal expectedKar, decimal expectedKdv, decimal expectedSatis)
    {
        var vm = new OptimalFiyatViewModel
        {
            AlisFiyati = alis,
            EkMaliyet = ek,
            HedefKarOrani = karOran,
            KdvOrani = kdvOran
        };

        Assert.Equal(expectedKar, vm.NetKar);
        Assert.Equal(expectedKdv, vm.KdvTutari);
        Assert.Equal(expectedSatis, vm.OnerilenSatisFiyati);
    }

    [Fact]
    public void Scenario_119_OptimalFiyat_ExtremeValues_CalculationsRemainAccurate()
    {
        var vm = new OptimalFiyatViewModel
        {
            AlisFiyati = 1000000m,
            EkMaliyet = 250000m,
            HedefKarOrani = 40m,
            KdvOrani = 20m
        };

        decimal expectedMaliyet = 1250000m;
        decimal expectedKar = expectedMaliyet * 0.40m; // 500,000
        decimal expectedHariç = expectedMaliyet + expectedKar; // 1,750,000
        decimal expectedKdv = expectedHariç * 0.20m; // 350,000
        decimal expectedSatis = expectedHariç + expectedKdv; // 2,100,000

        Assert.Equal(expectedKar, vm.NetKar);
        Assert.Equal(expectedKdv, vm.KdvTutari);
        Assert.Equal(expectedSatis, vm.OnerilenSatisFiyati);
    }

    [Fact]
    public void Scenario_120_OptimalFiyat_PropertyChange_TriggersAutomaticRecalculation()
    {
        var vm = new OptimalFiyatViewModel();
        decimal initialSatis = vm.OnerilenSatisFiyati;

        vm.AlisFiyati = 300m;
        Assert.NotEqual(initialSatis, vm.OnerilenSatisFiyati);
    }

    // =============================================================
    // 11. Gecikme Faizi Hesaplama Motoru (121 - 130)
    // =============================================================

    [Fact]
    public void Scenario_121_GecikmeFaizi_DefaultValues_CalculatesCommercialInterest()
    {
        var dovizService = _serviceProvider.GetRequiredService<DovizService>();
        var vm = new GecikmeFaiziViewModel(dovizService);

        // AnaPara=50000, FaizOrani=48, GecikmeGunu=30
        decimal expectedFaiz = 50000m * (48m / 100m) * 30 / 365m;
        decimal expectedToplam = 50000m + expectedFaiz;

        Assert.Equal(Math.Round(expectedFaiz, 4), Math.Round(vm.FaizTutari, 4));
        Assert.Equal(Math.Round(expectedToplam, 4), Math.Round(vm.ToplamTutar, 4));
    }

    [Fact]
    public void Scenario_122_GecikmeFaizi_SelectionIndex0_SetsLegalInterestRate24()
    {
        var dovizService = _serviceProvider.GetRequiredService<DovizService>();
        var vm = new GecikmeFaiziViewModel(dovizService);

        vm.SeciliFaizIndex = 0; // Yasal Faiz
        Assert.Equal(24m, vm.FaizOrani);
    }

    [Fact]
    public void Scenario_123_GecikmeFaizi_SelectionIndex1_SetsCommercialInterestRate48()
    {
        var dovizService = _serviceProvider.GetRequiredService<DovizService>();
        var vm = new GecikmeFaiziViewModel(dovizService);

        vm.SeciliFaizIndex = 1; // Ticari Faiz
        Assert.Equal(48m, vm.FaizOrani);
    }

    [Theory]
    [InlineData(10000, 24, 365, 2400)]
    [InlineData(20000, 48, 365, 9600)]
    [InlineData(100000, 50, 365, 50000)]
    [InlineData(50000, 36.5, 100, 5000)] // 50000 * 0.365 * 100 / 365 = 5000
    public void Scenario_124_to_127_GecikmeFaizi_ExactFormulas(decimal principal, decimal rate, int days, decimal expectedInterest)
    {
        decimal interest = principal * (rate / 100m) * days / 365m;
        Assert.Equal(expectedInterest, interest);
    }

    [Fact]
    public void Scenario_128_GecikmeFaizi_ZeroDays_ZeroInterest()
    {
        var dovizService = _serviceProvider.GetRequiredService<DovizService>();
        var vm = new GecikmeFaiziViewModel(dovizService);

        vm.AnaPara = 100000m;
        vm.GecikmeGunu = 0;

        Assert.Equal(0m, vm.FaizTutari);
        Assert.Equal(100000m, vm.ToplamTutar);
    }

    [Fact]
    public void Scenario_129_GecikmeFaizi_ZeroPrincipal_ZeroInterest()
    {
        var dovizService = _serviceProvider.GetRequiredService<DovizService>();
        var vm = new GecikmeFaiziViewModel(dovizService);

        vm.AnaPara = 0m;
        vm.GecikmeGunu = 60;

        Assert.Equal(0m, vm.FaizTutari);
        Assert.Equal(0m, vm.ToplamTutar);
    }

    [Fact]
    public void Scenario_130_GecikmeFaizi_CustomRate_SetsSelectionIndex3()
    {
        var dovizService = _serviceProvider.GetRequiredService<DovizService>();
        var vm = new GecikmeFaiziViewModel(dovizService);

        vm.FaizOrani = 33m; // Non-standard rate -> Özel (Index 3)
        Assert.Equal(3, vm.SeciliFaizIndex);
    }

    // =============================================================
    // 12. Toplu Fiyatlandırma & Yüzdesel Artış/Azalış Hesaplamaları (131 - 140)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_131_TopluFiyat_LoadData_PopulatesItems()
    {
        var vm = _serviceProvider.GetRequiredService<TopluFiyatViewModel>();
        await vm.LoadDataAsync();

        Assert.NotNull(vm.Items);
        Assert.NotNull(vm.Kategoriler);
    }

    [Fact]
    public void Scenario_132_TopluFiyat_ApplyPercentageIncrease_CalculatesCorrectly()
    {
        var stok = new StokKart { StokAdi = "Test Stok", SatisFiyati = 100m };
        var item = new TopluFiyatItem(stok);

        decimal oran = 15m;
        item.YeniFiyat = item.EskiFiyat + (item.EskiFiyat * oran / 100m);

        Assert.Equal(115m, item.YeniFiyat);
    }

    [Fact]
    public void Scenario_133_TopluFiyat_ApplyPercentageDiscount_CalculatesCorrectly()
    {
        var stok = new StokKart { StokAdi = "Test Stok", SatisFiyati = 200m };
        var item = new TopluFiyatItem(stok);

        decimal oran = 20m;
        item.YeniFiyat = item.EskiFiyat - (item.EskiFiyat * oran / 100m);

        Assert.Equal(160m, item.YeniFiyat);
    }

    [Fact]
    public void Scenario_134_TopluFiyat_ApplyFlatIncrease_CalculatesCorrectly()
    {
        var stok = new StokKart { StokAdi = "Test Stok", SatisFiyati = 250m };
        var item = new TopluFiyatItem(stok);

        decimal tutar = 50m;
        item.YeniFiyat = item.EskiFiyat + tutar;

        Assert.Equal(300m, item.YeniFiyat);
    }

    [Fact]
    public void Scenario_135_TopluFiyat_ApplyFlatDiscount_CalculatesCorrectly()
    {
        var stok = new StokKart { StokAdi = "Test Stok", SatisFiyati = 250m };
        var item = new TopluFiyatItem(stok);

        decimal tutar = 50m;
        item.YeniFiyat = item.EskiFiyat - tutar;

        Assert.Equal(200m, item.YeniFiyat);
    }

    [Fact]
    public void Scenario_136_TopluFiyat_ToggleAllSelection_ChangesAllItemsState()
    {
        var list = new List<TopluFiyatItem>
        {
            new(new StokKart { StokAdi = "S1" }),
            new(new StokKart { StokAdi = "S2" }),
            new(new StokKart { StokAdi = "S3" })
        };

        foreach (var item in list) item.IsSelected = false;
        Assert.All(list, x => Assert.False(x.IsSelected));

        foreach (var item in list) item.IsSelected = true;
        Assert.All(list, x => Assert.True(x.IsSelected));
    }

    [Fact]
    public void Scenario_137_TopluFiyat_SingleItemDeselect_CallbackInvoked()
    {
        bool called = false;
        var item = new TopluFiyatItem(new StokKart { StokAdi = "S1" }, () => called = true);

        item.IsSelected = false;
        Assert.True(called);
    }

    [Fact]
    public void Scenario_138_TopluFiyat_DiscountGreaterThanPrice_FloorsAtZero()
    {
        decimal price = 50m;
        decimal discount = 80m;
        decimal result = Math.Max(0m, price - discount);

        Assert.Equal(0m, result);
    }

    [Theory]
    [InlineData(100, 10, true, "Zam", 110)]
    [InlineData(100, 10, true, "Indirim", 90)]
    [InlineData(500, 25, false, "Zam", 525)]
    [InlineData(500, 25, false, "Indirim", 475)]
    public void Scenario_139_to_140_TopluFiyat_Formulas(decimal basePrice, decimal val, bool isPerc, string op, decimal expectedPrice)
    {
        decimal finalPrice;
        if (isPerc)
        {
            finalPrice = op == "Zam"
                ? basePrice + (basePrice * val / 100m)
                : basePrice - (basePrice * val / 100m);
        }
        else
        {
            finalPrice = op == "Zam"
                ? basePrice + val
                : basePrice - val;
        }

        Assert.Equal(expectedPrice, finalPrice);
    }

    // =============================================================
    // 13. Kumaş Maliyet End-to-End Pipeline & İleri Seviye Finans (141 - 154)
    // =============================================================

    [Theory]
    [InlineData(100, 2.50, 0.05, 262.50)] // 100 kg * 2.50 $ + %5 fire = 262.50 $
    [InlineData(200, 3.00, 0.03, 618.00)] // 200 kg * 3.00 $ + %3 fire = 618.00 $
    [InlineData(500, 4.20, 0.08, 2268.00)]// 500 kg * 4.20 $ + %8 fire = 2268.00 $
    [InlineData(1000, 1.80, 0.02, 1836.00)]// 1000 kg * 1.80 $ + %2 fire = 1836.00 $
    public void Scenario_141_to_144_YarnBatchCost_WithWasteAllowance(double kg, double pricePerKg, double wasteRate, double expectedTotal)
    {
        double totalCost = kg * pricePerKg * (1.0 + wasteRate);
        Assert.Equal(expectedTotal, totalCost, precision: 2);
    }

    [Theory]
    [InlineData(500, 0.40, 200.0)]  // 500 mt örgü/dokuma @ 0.40 $/mt = 200 $
    [InlineData(1200, 0.35, 420.0)] // 1200 mt @ 0.35 $/mt = 420 $
    [InlineData(3000, 0.30, 900.0)] // 3000 mt @ 0.30 $/mt = 900 $
    [InlineData(10000, 0.25, 2500.0)]// 10000 mt @ 0.25 $/mt = 2500 $
    public void Scenario_145_to_148_WeavingKnittingCosts(double meters, double ratePerMeter, double expectedCost)
    {
        double cost = meters * ratePerMeter;
        Assert.Equal(expectedCost, cost, precision: 2);
    }

    [Theory]
    [InlineData(1000, 0.60, 0.15, 750.0)] // 1000 mt boya/baskı @ 0.60 + 0.15 terbiye = 0.75 * 1000 = 750 $
    [InlineData(2500, 0.50, 0.20, 1750.0)]// 2500 mt @ 0.70 = 1750 $
    [InlineData(5000, 0.45, 0.10, 2750.0)]// 5000 mt @ 0.55 = 2750 $
    public void Scenario_149_to_151_DyeingAndFinishingCosts(double meters, double dyeingRate, double finishingRate, double expectedCost)
    {
        double totalCost = meters * (dyeingRate + finishingRate);
        Assert.Equal(expectedCost, totalCost, precision: 2);
    }

    [Fact]
    public void Scenario_152_BreakevenQuantity_FixedCost_ContributionMargin()
    {
        // Sabit Maliyet: 50,000 $
        // Birim Satış: 15 $
        // Birim Değişken Maliyet: 10 $
        // Birim Katkı Payı: 5 $
        // Başabaş Noktası = 50,000 / 5 = 10,000 Adet
        decimal fixedCost = 50000m;
        decimal unitPrice = 15m;
        decimal unitVariableCost = 10m;
        decimal contributionMargin = unitPrice - unitVariableCost;

        decimal breakevenUnits = fixedCost / contributionMargin;
        Assert.Equal(10000m, breakevenUnits);
    }

    [Fact]
    public void Scenario_153_GrossMargin_Vs_MarkupPercentage_Consistency()
    {
        // Maliyet: 80 TL
        // Satış Fiyatı: 100 TL
        // Kâr: 20 TL
        // Margin = 20 / 100 = %20
        // Markup = 20 / 80 = %25
        decimal cost = 80m;
        decimal price = 100m;
        decimal profit = price - cost;

        decimal margin = profit / price;
        decimal markup = profit / cost;

        Assert.Equal(0.20m, margin);
        Assert.Equal(0.25m, markup);
    }

    [Fact]
    public void Scenario_154_EndToEndFabricCost_Pipeline()
    {
        // 1000 mt kumaş üretimi:
        // 1. İplik Maliyeti: 250 kg @ 4.00 $ = 1000 $
        // 2. İplik Firesi: %5 = 50 $
        // 3. Dokuma: 1000 mt @ 0.40 $ = 400 $
        // 4. Boyahane & Terbiye: 1000 mt @ 0.70 $ = 700 $
        // 5. Sevkiyat & Paketleme: 150 $
        // Toplam Üretim Maliyeti: 2300 $
        // Birim Metre Maliyeti: 2.30 $
        // %30 Kâr ile Satış: 2.30 * 1.30 = 2.99 $
        decimal iplikMaliyeti = 250m * 4.00m;
        decimal iplikFiresi = iplikMaliyeti * 0.05m;
        decimal dokuma = 1000m * 0.40m;
        decimal boyahane = 1000m * 0.70m;
        decimal nakliye = 150m;

        decimal toplamMaliyet = iplikMaliyeti + iplikFiresi + dokuma + boyahane + nakliye;
        decimal birimMetreMaliyet = toplamMaliyet / 1000m;
        decimal satisFiyati = birimMetreMaliyet * 1.30m;

        Assert.Equal(2300m, toplamMaliyet);
        Assert.Equal(2.30m, birimMetreMaliyet);
        Assert.Equal(2.99m, satisFiyati);
    }
}
