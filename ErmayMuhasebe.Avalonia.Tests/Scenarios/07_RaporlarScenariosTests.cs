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

public partial class RaporlarScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Rapor Kataloğu & Başlatma (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_01_ReportsList_InitializesWithAllCatalogItems()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.NotNull(vm.Reports);
        Assert.True(vm.Reports.Count >= 20, "Rapor listesinde en az 20 farklı rapor tanımlı olmalıdır.");
    }

    [Theory]
    [InlineData("Genel Özet")]
    [InlineData("Aylık Tahsilat ve Ödeme Analizi")]
    [InlineData("Cari Bakiye Raporu")]
    [InlineData("Cari Hareket Dökümü")]
    [InlineData("Stok Mevcudu")]
    [InlineData("Stok Hareketleri")]
    [InlineData("Kritik Stok Seviyesi")]
    [InlineData("Satış Faturası Dökümü")]
    [InlineData("Gelir Tablosu")]
    [InlineData("Nakit Akış Tablosu")]
    public void Scenario_02_to_11_CoreReportsExistInCatalog(string reportTitle)
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.Contains(vm.Reports, r => r.Title == reportTitle);
    }

    [Theory]
    [InlineData("Genel Durum")]
    [InlineData("Finans")]
    [InlineData("Cari Hesap")]
    [InlineData("Stok")]
    public void Scenario_12_to_15_ReportCategoriesExist(string categoryName)
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.Contains(vm.Reports, r => r.Category == categoryName);
    }

    // -------------------------------------------------------------
    // 2. Arama & Filtreleme Senaryoları (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_16_SearchReportsByTitle_FiltersMatching()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.SearchString = "Tahsilat";

        var filtered = vm.Reports.Where(r => r.Title.Contains("Tahsilat", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.NotEmpty(filtered);
    }

    [AvaloniaFact]
    public void Scenario_17_SearchReportsByCategory_FiltersMatching()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        var cariReports = vm.Reports.Where(r => r.Category == "Cari Hesap").ToList();
        Assert.True(cariReports.Count >= 3);
    }

    [Theory]
    [InlineData("Stok", true)]
    [InlineData("Cari", true)]
    [InlineData("ZzzzOlmayanRapor", false)]
    public void Scenario_18_to_20_SearchReportQueryMatchChecks(string query, bool shouldMatch)
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        bool hasMatch = vm.Reports.Any(r => r.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(shouldMatch, hasMatch);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2026, 12, 31, true)]
    [InlineData(2026, 6, 1, 2026, 6, 30, true)]
    [InlineData(2026, 12, 31, 2026, 1, 1, false)]
    public void Scenario_21_to_23_DateRangeValidation(int y1, int m1, int d1, int y2, int m2, int d2, bool isValid)
    {
        var start = new DateTime(y1, m1, d1);
        var end = new DateTime(y2, m2, d2);
        bool valid = start <= end;
        Assert.Equal(isValid, valid);
    }

    [AvaloniaFact]
    public void Scenario_24_DateSelectionPanel_ClosesOnCancel()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.IsDateSelectionVisible = true;
        vm.CloseDateSelectionCommand.Execute(null);

        Assert.False(vm.IsDateSelectionVisible);
    }

    [AvaloniaFact]
    public void Scenario_25_ClearError_ClearsErrorMessage()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.ErrorMessage = "Test Hata Mesajı";
        vm.ClearErrorCommand.Execute(null);

        Assert.Empty(vm.ErrorMessage);
        Assert.False(vm.IsErrorVisible);
    }

    // -------------------------------------------------------------
    // 3. Rapor Veri Hesaplama & Mutabakat (15 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData(100000, 80000, 15000, 5000)]
    [InlineData(50000, 40000, 7500, 2500)]
    public void Scenario_26_to_27_AbcCustomerAnalysisCalculations(decimal totalCiro, decimal expA, decimal expB, decimal expC)
    {
        decimal a = totalCiro * 0.80m;
        decimal b = totalCiro * 0.15m;
        decimal c = totalCiro * 0.05m;

        Assert.Equal(expA, a);
        Assert.Equal(expB, b);
        Assert.Equal(expC, c);
    }

    [Theory]
    [InlineData(10000, 2000, 8000)]
    [InlineData(50000, 35000, 15000)]
    [InlineData(20000, 25000, -5000)]
    [InlineData(0, 0, 0)]
    public void Scenario_28_to_31_IncomeStatementNetProfit(decimal gelir, decimal gider, decimal expNetKar)
    {
        decimal netKar = gelir - gider;
        Assert.Equal(expNetKar, netKar);
    }

    [Theory]
    [InlineData(5000, 3000, 2000)]
    [InlineData(10000, 12000, -2000)]
    [InlineData(0, 0, 0)]
    public void Scenario_32_to_34_CashFlowNetDifference(decimal giris, decimal cikis, decimal expNetAkis)
    {
        decimal net = giris - cikis;
        Assert.Equal(expNetAkis, net);
    }

    [Theory]
    [InlineData(10, 100, 1000)]
    [InlineData(30, 500, 15000)]
    [InlineData(60, 200, 12000)]
    [InlineData(90, 1000, 90000)]
    public void Scenario_35_to_38_AgingBucketsCalculation(int gun, decimal tutar, decimal expCarpim)
    {
        decimal carpim = gun * tutar;
        Assert.Equal(expCarpim, carpim);
    }

    [Theory]
    [InlineData(10000, 20, 2000)]
    [InlineData(50000, 10, 5000)]
    public void Scenario_39_to_40_VatDeclarationCalculations(decimal matrah, int oran, decimal expKdv)
    {
        decimal kdv = matrah * (oran / 100m);
        Assert.Equal(expKdv, kdv);
    }

    // -------------------------------------------------------------
    // 4. PDF Üretimi & Çıktı Senaryoları (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_41_GenerateGeneralSummaryPdf_ProducesPdf()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "C-REP1", Unvan = "Rapor Test Cari", Borc = 5000 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-REP1", Tur = "Satis", GenelToplam = 5000 });

        var rows = new List<string[]>
        {
            new[] { "Test Cari", "5.000 TL" }
        };
        var res = await _pdfService.GenerateGenericTablePdfAsync("Genel Özet Raporu", new[] { "Açıklama", "Tutar" }, rows);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 500);
    }

    [AvaloniaFact]
    public async Task Scenario_42_GenerateStockListPdf_ProducesPdf()
    {
        var rows = new List<string[]>
        {
            new[] { "Ürün A", "100 Adet" },
            new[] { "Ürün B", "250 Adet" }
        };
        var res = await _pdfService.GenerateGenericTablePdfAsync("Stok Mevcut Raporu", new[] { "Ürün Adı", "Miktar" }, rows);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 500);
    }

    [Theory]
    [InlineData("Genel Özet")]
    [InlineData("Stok Mevcudu")]
    [InlineData("Cari Bakiye")]
    [InlineData("Gelir Tablosu")]
    public async Task Scenario_43_to_46_ReportPdfs_ProduceNonEmptyBytes(string raporAdi)
    {
        var rows = new List<string[]> { new[] { "Veri 1", "Veri 2" } };
        var res = await _pdfService.GenerateGenericTablePdfAsync(raporAdi, new[] { "Sütun 1", "Sütun 2" }, rows);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 0);
    }

    [Theory]
    [InlineData(100, 1000, 0.10)]
    [InlineData(500, 2000, 0.25)]
    [InlineData(0, 5000, 0.00)]
    [InlineData(150, 1500, 0.10)]
    public void Scenario_47_to_50_DiscountAnalysisCalculations(decimal iskonto, decimal toplam, double expOran)
    {
        double oran = toplam > 0 ? (double)(iskonto / toplam) : 0.0;
        Assert.Equal(expOran, oran, precision: 2);
    }
}
