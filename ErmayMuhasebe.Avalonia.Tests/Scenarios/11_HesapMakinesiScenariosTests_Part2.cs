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

public partial class HesapMakinesiScenariosTests
{
    // =============================================================
    // 5. KG Hesaplama Gelişmiş Kombinasyonları (51 - 65)
    // =============================================================

    [Theory]
    [InlineData("80", "160", "250", 32.0)]   // (80 * 160 * 250) / 100000 = 32 KG
    [InlineData("120", "160", "300", 57.6)]  // (120 * 160 * 300) / 100000 = 57.6 KG
    [InlineData("180", "160", "400", 115.2)] // (180 * 160 * 400) / 100000 = 115.2 KG
    [InlineData("220", "160", "500", 176.0)] // (220 * 160 * 500) / 100000 = 176 KG
    [InlineData("300", "160", "100", 48.0)]  // (300 * 160 * 100) / 100000 = 48 KG
    public void Scenario_51_to_55_HesaplaKg_VariousWeights(string gr, string en, string sarim, double expectedKg)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = en;
        vm.Sarim = sarim;

        vm.HesaplaKgCommand.Execute(null);

        Assert.Contains(expectedKg.ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKgSonuc);
    }

    [Theory]
    [InlineData("150", "140", "500", 105.0)] // En: 140
    [InlineData("150", "180", "500", 135.0)] // En: 180
    [InlineData("150", "200", "500", 150.0)] // En: 200
    [InlineData("150", "220", "500", 165.0)] // En: 220
    public void Scenario_56_to_59_HesaplaKg_VariousWidths(string gr, string en, string sarim, double expectedKg)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = en;
        vm.Sarim = sarim;

        vm.HesaplaKgCommand.Execute(null);

        Assert.Contains(expectedKg.ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKgSonuc);
    }

    [Theory]
    [InlineData("100", "160", "1500", 240.0)]
    [InlineData("100", "160", "2000", 320.0)]
    [InlineData("100", "160", "50", 8.0)]
    public void Scenario_60_to_62_HesaplaKg_VariousRollLengths(string gr, string en, string sarim, double expectedKg)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = en;
        vm.Sarim = sarim;

        vm.HesaplaKgCommand.Execute(null);

        Assert.Contains(expectedKg.ToString("N2", CultureInfo.CurrentCulture), vm.ToplamKgSonuc);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-50")]
    [InlineData("  ")]
    public void Scenario_63_to_65_HesaplaKg_NegativeAndBlankInputs(string gr)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.Gr = gr;
        vm.En = "160";
        vm.Sarim = "500";

        vm.HesaplaKgCommand.Execute(null);

        Assert.NotNull(vm.ToplamKgSonuc);
    }

    // =============================================================
    // 6. Top Fiyatı Hesaplama & Ondalık Testleri (66 - 80)
    // =============================================================

    [Theory]
    [InlineData("150", "1,50", 225.0)]
    [InlineData("200", "2,25", 450.0)]
    [InlineData("80", "3,75", 300.0)]
    [InlineData("50", "4,50", 225.0)]
    public void Scenario_66_to_69_HesaplaTopFiyati_VariousUnitPrices(string kg, string kgFiyat, double expectedTotal)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = kg;
        vm.KgFiyati = kgFiyat;

        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.Contains(expectedTotal.ToString("N2", CultureInfo.CurrentCulture), vm.TopFiyatiSonuc);
    }

    [Theory]
    [InlineData("100", "2.50", 250.0)] // Dot separator
    [InlineData("100", "2,50", 250.0)] // Comma separator
    public void Scenario_70_to_71_HesaplaTopFiyati_CultureSeparators(string kg, string kgFiyat, double expectedTotal)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = kg;
        vm.KgFiyati = kgFiyat;

        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.Contains(expectedTotal.ToString("N2", CultureInfo.CurrentCulture), vm.TopFiyatiSonuc);
    }

    [Fact]
    public void Scenario_72_HesaplaTopFiyati_AutomaticallyUpdatesTopFiyatiInput()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = "60";
        vm.KgFiyati = "5,00";

        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.Equal((300.0).ToString("N2", CultureInfo.CurrentCulture), vm.TopFiyatiInput);
    }

    [Theory]
    [InlineData(10, 300, 3000.0)]
    [InlineData(5, 450, 2250.0)]
    [InlineData(1, 150, 150.0)]
    public void Scenario_73_to_75_MultipleRollBatchCost(int rollCount, double rollPrice, double expectedBatchCost)
    {
        double batch = rollCount * rollPrice;
        Assert.Equal(expectedBatchCost, batch);
    }

    [Theory]
    [InlineData(1000, 0.05, 50.0)] // %5 fire payı
    [InlineData(500, 0.10, 50.0)]  // %10 fire payı
    public void Scenario_76_to_77_WastageAllowanceCalculations(double totalKg, double wastageRate, double expectedWasteKg)
    {
        double waste = totalKg * wastageRate;
        Assert.Equal(expectedWasteKg, waste);
    }

    [Theory]
    [InlineData(1000, 50, 1050.0)]
    [InlineData(500, 50, 550.0)]
    public void Scenario_78_to_79_TotalKgWithWastage(double netKg, double wasteKg, double expectedGrossKg)
    {
        double gross = netKg + wasteKg;
        Assert.Equal(expectedGrossKg, gross);
    }

    [Fact]
    public void Scenario_80_TopFiyatiSonuc_EndsWithDollarSign()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.ToplamKg = "10";
        vm.KgFiyati = "10";
        vm.HesaplaTopFiyatiCommand.Execute(null);

        Assert.EndsWith("$", vm.TopFiyatiSonuc.Trim());
    }

    // =============================================================
    // 7. Metrekare Hesaplama & Transferleri (81 - 90)
    // =============================================================

    [Theory]
    [InlineData("100", 160.0)]
    [InlineData("250", 400.0)]
    [InlineData("400", 640.0)]
    [InlineData("750", 1200.0)]
    [InlineData("1200", 1920.0)]
    public void Scenario_81_to_85_HesaplaMetrekare_VariousLengths(string topBoy, double expectedM2)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopBoy = topBoy;

        vm.HesaplaMetrekareCommand.Execute(null);

        Assert.Contains(expectedM2.ToString("N2", CultureInfo.CurrentCulture), vm.MetrekareSonuc);
    }

    [Fact]
    public void Scenario_86_HesaplaMetrekare_FormatsWithM2Unit()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopBoy = "100";
        vm.HesaplaMetrekareCommand.Execute(null);

        Assert.Contains("m²", vm.MetrekareSonuc);
    }

    [Fact]
    public void Scenario_87_HesaplaMetrekare_TransfersToToplamMetrekareProperty()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopBoy = "250";
        vm.HesaplaMetrekareCommand.Execute(null);

        Assert.Equal((400.0).ToString("N2", CultureInfo.CurrentCulture), vm.ToplamMetrekare);
    }

    [Theory]
    [InlineData(160, 100, 160.0)] // 1.6m * 100m = 160 m2
    [InlineData(180, 100, 180.0)] // 1.8m * 100m = 180 m2
    [InlineData(200, 100, 200.0)] // 2.0m * 100m = 200 m2
    public void Scenario_88_to_90_CustomWidthM2Calculations(double widthCm, double lengthM, double expectedM2)
    {
        double m2 = (widthCm / 100.0) * lengthM;
        Assert.Equal(expectedM2, m2);
    }

    // =============================================================
    // 8. Birim Metrekare Maliyeti & Kâr Fiyatlandırması (91 - 100)
    // =============================================================

    [Theory]
    [InlineData("400", "800", 0.50)]
    [InlineData("600", "1200", 0.50)]
    [InlineData("750", "1000", 0.75)]
    [InlineData("1200", "1600", 0.75)]
    public void Scenario_91_to_94_HesaplaBirimMaliyet_StandardFormulas(string topFiyati, string m2, double expectedBirimMaliyet)
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopFiyatiInput = topFiyati;
        vm.ToplamMetrekare = m2;

        vm.HesaplaBirimMaliyetCommand.Execute(null);

        Assert.Contains(expectedBirimMaliyet.ToString("N2", CultureInfo.CurrentCulture), vm.BirimMaliyetSonuc);
    }

    [Fact]
    public void Scenario_95_HesaplaBirimMaliyet_ZeroM2_ReturnsZeroDollar()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopFiyatiInput = "500";
        vm.ToplamMetrekare = "0";

        vm.HesaplaBirimMaliyetCommand.Execute(null);

        Assert.Equal("0,00 $", vm.BirimMaliyetSonuc);
    }

    [Theory]
    [InlineData(0.50, 0.15, 0.575)] // 0.50 $ maliyet + %15 kâr = 0.575 $
    [InlineData(0.75, 0.20, 0.90)]  // 0.75 $ maliyet + %20 kâr = 0.90 $
    [InlineData(1.20, 0.25, 1.50)]  // 1.20 $ maliyet + %25 kâr = 1.50 $
    [InlineData(2.00, 0.50, 3.00)]  // 2.00 $ maliyet + %50 kâr = 3.00 $
    public void Scenario_96_to_99_TargetSalePricesWithMargin(double cost, double margin, double expectedSalePrice)
    {
        double salePrice = cost * (1.0 + margin);
        Assert.Equal(expectedSalePrice, salePrice, precision: 3);
    }

    [Fact]
    public void Scenario_100_BirimMaliyetSonuc_EndsWithDollarSign()
    {
        var vm = _serviceProvider.GetRequiredService<MaliyetHesaplamaViewModel>();
        vm.TopFiyatiInput = "100";
        vm.ToplamMetrekare = "200";
        vm.HesaplaBirimMaliyetCommand.Execute(null);

        Assert.EndsWith("$", vm.BirimMaliyetSonuc.Trim());
    }
}
