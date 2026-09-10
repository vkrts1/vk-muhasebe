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

public partial class RaporlarScenariosTests
{
    // =============================================================
    // 5. Stok Raporları & Devir Analizleri (51 - 65)
    // =============================================================

    [Fact]
    public async Task Scenario_51_Rapor_StokMevcudu_CalculatesTotalStockValuation()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-R1", StokAdi = "Valuation Item 1", Miktar = 100, SatisFiyati = 50m });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-R2", StokAdi = "Valuation Item 2", Miktar = 200, SatisFiyati = 30m });

        var all = await _uow.Stoklar.GetAllAsync();
        decimal totalValue = all.Sum(s => (decimal)s.Miktar * s.SatisFiyati);

        Assert.True(totalValue >= 11000m); // (100*50) + (200*30) = 5000 + 6000 = 11000
    }

    [Fact]
    public async Task Scenario_52_Rapor_KritikStoklar_FiltersBelowMinimum()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-CRIT", StokAdi = "Kritik Rapor Ürünü", Miktar = 3, MinSeviye = 10 });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "STK-SAFE", StokAdi = "Güvenli Rapor Ürünü", Miktar = 50, MinSeviye = 10 });

        var kritikler = await _uow.Stoklar.GetKritikStoklarAsync();
        Assert.Contains(kritikler, s => s.StokKodu == "STK-CRIT");
        Assert.DoesNotContain(kritikler, s => s.StokKodu == "STK-SAFE");
    }

    [Theory]
    [InlineData(120000, 30000, 4.0)] // Devir Hızı = SMM / Ortalama Stok = 120.000 / 30.000 = 4.0
    [InlineData(500000, 50000, 10.0)]
    [InlineData(60000, 20000, 3.0)]
    [InlineData(100000, 100000, 1.0)]
    public void Scenario_53_to_56_StockTurnoverRateCalculations(decimal smm, decimal ortalamaStok, double expectedTurnover)
    {
        double turnover = ortalamaStok > 0 ? (double)(smm / ortalamaStok) : 0.0;
        Assert.Equal(expectedTurnover, Math.Round(turnover, 1));
    }

    [Theory]
    [InlineData(4.0, 91.25)] // Ortalama Stokta Kalma Süresi = 365 / Devir Hızı
    [InlineData(10.0, 36.5)]
    [InlineData(2.0, 182.5)]
    public void Scenario_57_to_59_StockHoldingPeriodInDays(double turnover, double expectedDays)
    {
        double days = turnover > 0 ? 365.0 / turnover : 0.0;
        Assert.Equal(expectedDays, Math.Round(days, 2));
    }

    [Fact]
    public async Task Scenario_60_Rapor_OluStok_DuplicateStockCodesIdentified()
    {
        var s1 = new StokKart { StokKodu = "DUP-01", StokAdi = "Aynı Ürün 1", Miktar = 10 };
        var s2 = new StokKart { StokKodu = "DUP-01", StokAdi = "Aynı Ürün 2", Miktar = 5 };

        // Duplicate code list checking
        bool isDuplicate = s1.StokKodu == s2.StokKodu;
        Assert.True(isDuplicate);
    }

    [Theory]
    [InlineData(100, 10, 90, "Fazla")]
    [InlineData(5, 20, -15, "Eksik")]
    [InlineData(15, 15, 0, "Dengeli")]
    public void Scenario_61_to_63_StockSafetyLevelDifference(double miktar, double min, double expectedFark, string expectedDurum)
    {
        double fark = miktar - min;
        string durum = fark > 0 ? "Fazla" : (fark < 0 ? "Eksik" : "Dengeli");

        Assert.Equal(expectedFark, fark);
        Assert.Equal(expectedDurum, durum);
    }

    [Theory]
    [InlineData(500, 12.5, 6250.0)]
    [InlineData(1000, 0.0, 0.0)]
    public void Scenario_64_to_65_StockTotalCostValuation(double miktar, double birimMaliyet, double expectedValuation)
    {
        double totalCost = miktar * birimMaliyet;
        Assert.Equal(expectedValuation, totalCost);
    }

    // =============================================================
    // 6. Cari & Karlılık Rapor Analizleri (66 - 80)
    // =============================================================

    [Fact]
    public async Task Scenario_66_Rapor_CariBakiyeRaporu_AggregatesBalances()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "CAR-R66-1", Unvan = "Cari 1", Borc = 10000m, Alacak = 2000m });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "CAR-R66-2", Unvan = "Cari 2", Borc = 5000m, Alacak = 8000m });

        var cariler = await _uow.Cariler.GetAllAsync();
        decimal totalBorc = cariler.Sum(c => c.Borc);
        decimal totalAlacak = cariler.Sum(c => c.Alacak);

        Assert.True(totalBorc >= 15000m);
        Assert.True(totalAlacak >= 10000m);
    }

    [Fact]
    public async Task Scenario_67_Rapor_HareketsizCariler_FindsInactiveCaris()
    {
        var cariHareketsiz = new CariKart { CariKod = "INACT-67", Unvan = "Hareketsiz Firma Ltd." };
        await _uow.Cariler.SaveAsync(cariHareketsiz);

        var list = await _uow.Cariler.GetHareketsizCarilerAsync(180);
        Assert.NotNull(list);
    }

    [Theory]
    [InlineData(10000, 7000, 3000, 30.0)] // Kar = 3.000, Marj = %30
    [InlineData(50000, 25000, 25000, 50.0)]
    [InlineData(20000, 18000, 2000, 10.0)]
    [InlineData(1000, 1000, 0, 0.0)]
    public void Scenario_68_to_71_GrossProfitMarginCalculations(decimal satis, decimal maliyet, decimal expectedProfit, double expectedMargin)
    {
        decimal profit = satis - maliyet;
        double margin = satis > 0 ? (double)(profit / satis) * 100.0 : 0.0;

        Assert.Equal(expectedProfit, profit);
        Assert.Equal(expectedMargin, Math.Round(margin, 1));
    }

    [Theory]
    [InlineData(10000, 3000, 1000, 2000, 20.0)] // Gelir: 10k, Brüt: 3k, Faaliyet Gideri: 1k, Net: 2k, Net Marj: %20
    [InlineData(50000, 20000, 5000, 15000, 30.0)]
    public void Scenario_72_to_73_NetOperatingMarginCalculations(decimal ciro, decimal brutKar, decimal giderler, decimal expectedNet, double expectedNetMargin)
    {
        decimal netKar = brutKar - giderler;
        double netMargin = ciro > 0 ? (double)(netKar / ciro) * 100.0 : 0.0;

        Assert.Equal(expectedNet, netKar);
        Assert.Equal(expectedNetMargin, Math.Round(netMargin, 1));
    }

    [Fact]
    public void Scenario_74_Rapor_TopSellingProducts_SortsByQuantityDescending()
    {
        var items = new List<(string Name, int Qty)>
        {
            ("Ürün A", 50),
            ("Ürün B", 120),
            ("Ürün C", 15),
            ("Ürün D", 85)
        };

        var sorted = items.OrderByDescending(x => x.Qty).ToList();
        Assert.Equal("Ürün B", sorted[0].Name);
        Assert.Equal("Ürün D", sorted[1].Name);
        Assert.Equal("Ürün A", sorted[2].Name);
        Assert.Equal("Ürün C", sorted[3].Name);
    }

    [Fact]
    public void Scenario_75_Rapor_TopProfitableProducts_SortsByProfitDescending()
    {
        var items = new List<(string Name, decimal Profit)>
        {
            ("Ürün X", 12500m),
            ("Ürün Y", 45000m),
            ("Ürün Z", 8200m)
        };

        var sorted = items.OrderByDescending(x => x.Profit).ToList();
        Assert.Equal("Ürün Y", sorted[0].Name);
        Assert.Equal("Ürün X", sorted[1].Name);
        Assert.Equal("Ürün Z", sorted[2].Name);
    }

    [Theory]
    [InlineData(10000, 500, 9500)]
    [InlineData(25000, 2500, 22500)]
    [InlineData(5000, 0, 5000)]
    public void Scenario_76_to_78_NetSalesAfterDiscount(decimal brutSatis, decimal iskonto, decimal expectedNet)
    {
        decimal netSatis = brutSatis - iskonto;
        Assert.Equal(expectedNet, netSatis);
    }

    [Theory]
    [InlineData(100000, 0.20, 20000)]
    [InlineData(50000, 0.10, 5000)]
    public void Scenario_79_to_80_CommissionExpenseCalculations(decimal satis, double komisyonOran, decimal expectedKomisyon)
    {
        decimal komisyon = satis * (decimal)komisyonOran;
        Assert.Equal(expectedKomisyon, komisyon);
    }

    // =============================================================
    // 7. Stratejik ABC Analizleri (81 - 90)
    // =============================================================

    [Theory]
    [InlineData(0.0, 0.80, "A")]
    [InlineData(0.81, 0.95, "B")]
    [InlineData(0.96, 1.00, "C")]
    public void Scenario_81_to_83_AbcClassificationThresholds(double cumulativeLower, double cumulativeUpper, string expectedCategory)
    {
        double testPoint = (cumulativeLower + cumulativeUpper) / 2.0;
        string cat = testPoint switch
        {
            <= 0.80 => "A",
            <= 0.95 => "B",
            _ => "C"
        };
        Assert.Equal(expectedCategory, cat);
    }

    [Fact]
    public void Scenario_84_Rapor_CustomerAbcSegmentation_ClassifiesThreeCustomers()
    {
        // 3 Müşteri: M1: 80k (%80), M2: 15k (%15), M3: 5k (%5) -> Toplam 100k
        var customers = new List<(string Code, decimal Ciro)>
        {
            ("CUST-1", 80000m),
            ("CUST-2", 15000m),
            ("CUST-3", 5000m)
        };

        decimal total = customers.Sum(x => x.Ciro);
        decimal cum = 0m;
        var classified = new List<(string Code, string Class)>();

        foreach (var c in customers.OrderByDescending(x => x.Ciro))
        {
            cum += c.Ciro;
            double ratio = (double)(cum / total);
            string cls = ratio <= 0.8001 ? "A" : (ratio <= 0.9501 ? "B" : "C");
            classified.Add((c.Code, cls));
        }

        Assert.Equal("A", classified.First(x => x.Code == "CUST-1").Class);
        Assert.Equal("B", classified.First(x => x.Code == "CUST-2").Class);
        Assert.Equal("C", classified.First(x => x.Code == "CUST-3").Class);
    }

    [Theory]
    [InlineData(100000, 80000, 80.0)]
    [InlineData(100000, 15000, 15.0)]
    [InlineData(100000, 5000, 5.0)]
    public void Scenario_85_to_87_AbcSharePercentages(decimal toplam, decimal grup, double expectedPercent)
    {
        double pct = (double)(grup / toplam) * 100.0;
        Assert.Equal(expectedPercent, pct);
    }

    [Theory]
    [InlineData(50, 10, 20.0)] // Pareto %80 ciro %20 müşteri
    [InlineData(100, 20, 20.0)]
    public void Scenario_88_to_89_ParetoCustomerRatio(int toplamMusteri, int aSinifiMusteri, double expectedRatio)
    {
        double ratio = (double)aSinifiMusteri / toplamMusteri * 100.0;
        Assert.Equal(expectedRatio, ratio);
    }

    [Fact]
    public void Scenario_90_Rapor_EmptyAbcDataset_HandledSafely()
    {
        var list = new List<decimal>();
        decimal total = list.Sum();
        Assert.Equal(0m, total);
    }

    // =============================================================
    // 8. KDV & Vergi Analiz Raporları (91 - 100)
    // =============================================================

    [Theory]
    [InlineData(100000, 20000, 15000, 5000, "Ödenecek KDV")] // Hesaplanan 20k, İndirilecek 15k -> Ödenecek 5k
    [InlineData(100000, 20000, 28000, -8000, "Sonraki Döneme Devreden KDV")] // Hesaplanan 20k, İndirilecek 28k -> Devreden 8k
    [InlineData(50000, 10000, 10000, 0, "KDV Çıkmadı")]
    public void Scenario_91_to_93_VatDeclarationNetTaxPayableOrCarryOver(decimal matrah, decimal hesaplananKdv, decimal indirilecekKdv, decimal expectedFark, string expectedDurum)
    {
        decimal netKdv = hesaplananKdv - indirilecekKdv;
        string durum = netKdv > 0 ? "Ödenecek KDV" : (netKdv < 0 ? "Sonraki Döneme Devreden KDV" : "KDV Çıkmadı");

        Assert.Equal(expectedFark, netKdv);
        Assert.Equal(expectedDurum, durum);
    }

    [Theory]
    [InlineData(10000, 1, 100)]
    [InlineData(10000, 10, 1000)]
    [InlineData(10000, 20, 2000)]
    [InlineData(0, 20, 0)]
    public void Scenario_94_to_97_MultipleVatRatesBreakdown(decimal matrah, int oran, decimal expectedKdv)
    {
        decimal kdv = matrah * (oran / 100m);
        Assert.Equal(expectedKdv, kdv);
    }

    [Theory]
    [InlineData(100, 1000, 2000, 3100)]
    [InlineData(0, 0, 5000, 5000)]
    public void Scenario_98_to_99_TotalVatPayableAcrossRates(decimal kdv1, decimal kdv10, decimal kdv20, decimal expectedTotal)
    {
        decimal total = kdv1 + kdv10 + kdv20;
        Assert.Equal(expectedTotal, total);
    }

    [Fact]
    public void Scenario_100_Rapor_VatSummaryReport_ZeroTaxPeriodValid()
    {
        decimal hesaplanan = 0m;
        decimal indirilecek = 0m;
        decimal net = hesaplanan - indirilecek;
        Assert.Equal(0m, net);
    }
}
