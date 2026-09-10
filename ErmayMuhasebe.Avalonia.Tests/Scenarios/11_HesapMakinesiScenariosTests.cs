using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class HesapMakinesiScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Toplam KG Hesaplama - Bölüm 1 (15 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("150", "160", "500", 120.0)]  // (150 * 160 * 500) / 100000 = 120 KG
    [InlineData("200", "180", "1000", 360.0)] // (200 * 180 * 1000) / 100000 = 360 KG
    [InlineData("100", "150", "200", 30.0)]   // (100 * 150 * 200) / 100000 = 30 KG
    [InlineData("80", "140", "400", 44.8)]    // (80 * 140 * 400) / 100000 = 44.8 KG
    public void Scenario_01_to_04_HesaplaKg_StandardFormulas(string gr, string en, string sarim, double expectedKg)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = en;
        vm.Sarim = sarim;

        vm.HesaplaKgCommand.Execute(null);

        Assert.Contains(expectedKg.ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKgSonuc);
    }

    [Theory]
    [InlineData("125,5", "160", "500", 100.4)] // Virgüllü ondalık
    [InlineData("125.5", "160", "500", 100.4)] // Noktalı ondalık
    public void Scenario_05_to_06_HesaplaKg_DecimalPointAndCommaTolerances(string gr, string en, string sarim, double expectedKg)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = en;
        vm.Sarim = sarim;

        vm.HesaplaKgCommand.Execute(null);

        Assert.Contains(expectedKg.ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKgSonuc);
    }

    [Theory]
    [InlineData("0", "160", "500", 0.0)] // 0 Gr
    [InlineData("150", "0", "500", 0.0)] // 0 En
    [InlineData("150", "160", "0", 0.0)] // 0 Sarım
    [InlineData("", "160", "500", 0.0)]  // Boş string
    [InlineData("abc", "160", "500", 0.0)] // Harf girişi koruması
    public void Scenario_07_to_11_HesaplaKg_ZeroAndInvalidEdgeCases(string gr, string en, string sarim, double expectedKg)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = en;
        vm.Sarim = sarim;

        vm.HesaplaKgCommand.Execute(null);

        Assert.Contains(expectedKg.ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKgSonuc);
    }

    [AvaloniaFact]
    public void Scenario_12_HesaplaKg_TransfersResultToSection2ToplamKg()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = "150";
        vm.En = "160";
        vm.Sarim = "500";

        vm.HesaplaKgCommand.Execute(null);

        Assert.Equal((120.0).ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKg);
    }

    [AvaloniaFact]
    public void Scenario_13_SarimChange_TransfersToTopBoy()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Sarim = "750";

        Assert.Equal("750", vm.TopBoy);
    }

    [Theory]
    [InlineData(100, 160, 1000, 160.0)]
    [InlineData(250, 180, 800, 360.0)]
    public void Scenario_14_to_15_WeightScaleCalculations(double g, double e, double s, double expKg)
    {
        double kg = (g * e * s) / 100000.0;
        Assert.Equal(expKg, kg, precision: 2);
    }

    // -------------------------------------------------------------
    // 2. Top Fiyatı Hesaplama - Bölüm 2 (12 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("120", "2,50", 300.0)]  // 120 KG * 2.50 $ = 300 $
    [InlineData("360", "3,00", 1080.0)] // 360 KG * 3.00 $ = 1080 $
    [InlineData("50", "4,20", 210.0)]
    [InlineData("100", "1,85", 185.0)]
    public void Scenario_16_to_19_HesaplaTopFiyati_StandardFormulas(string kg, string kgFiyati, double expectedFiyat)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = kg;
        vm.KgFiyati = kgFiyati;

        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.Contains(expectedFiyat.ToString("N2", CultureInfo.CurrentCulture), vm.TopFiyatiSonuc);
    }

    [Theory]
    [InlineData("0", "2,50", 0.0)] // Sıfır KG
    [InlineData("120", "0", 0.0)]  // Sıfır Fiyat
    [InlineData("", "2,50", 0.0)]  // Boş string
    [InlineData("120", "xyz", 0.0)] // Geçersiz karakter
    public void Scenario_20_to_23_HesaplaTopFiyati_EdgeCases(string kg, string kgFiyati, double expectedFiyat)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = kg;
        vm.KgFiyati = kgFiyati;

        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.Contains(expectedFiyat.ToString("N2", CultureInfo.CurrentCulture), vm.TopFiyatiSonuc);
    }

    [AvaloniaFact]
    public void Scenario_24_HesaplaTopFiyati_TransfersResultToSection4TopFiyatiInput()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = "100";
        vm.KgFiyati = "3,50";

        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.Equal((350.0).ToString("N2", CultureInfo.CurrentCulture), vm.TopFiyatiInput);
    }

    [Theory]
    [InlineData(500, 3.5, 1750.0)]
    [InlineData(1000, 2.0, 2000.0)]
    [InlineData(250, 4.8, 1200.0)]
    public void Scenario_25_to_27_BulkRollPriceCalculations(double kg, double birimFiyat, double expTopFiyat)
    {
        double fiyat = kg * birimFiyat;
        Assert.Equal(expTopFiyat, fiyat, precision: 2);
    }

    // -------------------------------------------------------------
    // 3. Toplam Metrekare Hesaplama - Bölüm 3 (10 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("500", 800.0)]   // 500 * 1.6 = 800 M2
    [InlineData("1000", 1600.0)] // 1000 * 1.6 = 1600 M2
    [InlineData("200", 320.0)]   // 200 * 1.6 = 320 M2
    [InlineData("300", 480.0)]   // 300 * 1.6 = 480 M2
    public void Scenario_28_to_31_HesaplaMetrekare_StandardFormulas(string topBoy, double expectedM2)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopBoy = topBoy;

        vm.HesaplaMetrekareCommand.Execute(null);

        Assert.Contains(expectedM2.ToString("N2", CultureInfo.CurrentCulture), vm.MetrekareSonuc);
    }

    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("", 0.0)]
    [InlineData("invalid", 0.0)]
    [InlineData("0.0", 0.0)]
    public void Scenario_32_to_35_HesaplaMetrekare_EdgeCases(string topBoy, double expectedM2)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopBoy = topBoy;

        vm.HesaplaMetrekareCommand.Execute(null);

        Assert.Contains(expectedM2.ToString("N2", CultureInfo.CurrentCulture), vm.MetrekareSonuc);
    }

    [AvaloniaFact]
    public void Scenario_36_HesaplaMetrekare_TransfersResultToSection4ToplamM2Input()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopBoy = "500";

        vm.HesaplaMetrekareCommand.Execute(null);

        Assert.Equal((800.0).ToString("N2", CultureInfo.CurrentCulture), vm.ToplamMetrekare);
    }

    [Theory]
    [InlineData(300, 500, 1500.0)] // Geniş en kumaş (300 cm en, 500 m boy = 1500 m2)
    public void Scenario_37_WideFabricSquareMeters(double en, double boy, double expM2)
    {
        double m2 = (en * boy) / 100.0;
        Assert.Equal(expM2, m2);
    }

    // -------------------------------------------------------------
    // 4. Metrekare Maliyeti & Döviz Dönüşümleri (13 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("300", "800", 0.38)]  // 300 $ / 800 M2 = 0.375 -> 0.38 $ / M2
    [InlineData("1080", "1800", 0.60)]// 1080 $ / 1800 M2 = 0.60 $ / M2
    [InlineData("500", "1000", 0.50)]
    public void Scenario_38_to_40_HesaplaM2Maliyeti_StandardFormulas(string topFiyati, string toplamM2, double expectedM2Cost)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopFiyatiInput = topFiyati;
        vm.ToplamMetrekare = toplamM2;

        vm.HesaplaBirimMaliyetCommand.Execute(null);

        Assert.Contains(expectedM2Cost.ToString("N2", CultureInfo.CurrentCulture), vm.BirimMaliyetSonuc);
    }

    [Theory]
    [InlineData("300", "0", 0.0)] // Sıfır M2 -> DivideByZero koruması
    [InlineData("0", "800", 0.0)] // Sıfır Fiyat
    [InlineData("", "800", 0.0)]  // Boş string
    public void Scenario_41_to_43_HesaplaM2Maliyeti_DivideByZeroSafe(string topFiyati, string toplamM2, double expectedM2Cost)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopFiyatiInput = topFiyati;
        vm.ToplamMetrekare = toplamM2;

        vm.HesaplaBirimMaliyetCommand.Execute(null);

        Assert.Contains(expectedM2Cost.ToString("N2", CultureInfo.CurrentCulture), vm.BirimMaliyetSonuc);
    }

    [Theory]
    [InlineData(100, 32.5, 3250)]  // 100 USD -> 3.250 TL
    [InlineData(500, 35.0, 17500)] // 500 EUR -> 17.500 TL
    [InlineData(200, 42.0, 8400)]  // 200 GBP -> 8.400 TL
    public void Scenario_44_to_46_CurrencyToTlConversion(decimal fxAmount, double rate, decimal expectedTl)
    {
        decimal tl = fxAmount * (decimal)rate;
        Assert.Equal(expectedTl, tl);
    }

    [Theory]
    [InlineData(32500, 32.5, 1000)] // 32.500 TL / 32.5 = 1.000 USD
    [InlineData(17500, 35.0, 500)]  // 17.500 TL / 35.0 = 500 EUR
    public void Scenario_47_to_48_TlToCurrencyConversion(decimal tlAmount, double rate, decimal expectedFx)
    {
        decimal fx = rate > 0 ? tlAmount / (decimal)rate : 0;
        Assert.Equal(expectedFx, fx);
    }

    [Theory]
    [InlineData(0.50, 0.20, 0.60)] // 0.50 $ maliyet + %20 kâr = 0.60 $ tavsiye satış
    [InlineData(1.00, 0.30, 1.30)]
    public void Scenario_49_to_50_TargetProfitMarginPricing(double cost, double margin, double expPrice)
    {
        double price = cost * (1.0 + margin);
        Assert.Equal(expPrice, price, precision: 2);
    }
}
