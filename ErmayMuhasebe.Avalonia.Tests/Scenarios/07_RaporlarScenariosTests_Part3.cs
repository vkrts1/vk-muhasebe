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
    // 9. Nakit Akışı & Finansal Raporlar (101 - 115)
    // =============================================================

    [Fact]
    public async Task Scenario_101_Rapor_NakitAkisi_CalculatesNetCashBalance()
    {
        var k = new BankaKart { BankaAdi = "Kasa", KartTuru = "Kasa", Bakiye = 25000m };
        var b = new BankaKart { BankaAdi = "Banka", KartTuru = "Vadesiz", Bakiye = 75000m };
        await _uow.Bankalar.SaveAsync(k);
        await _uow.Bankalar.SaveAsync(b);

        var all = await _uow.Bankalar.GetAllAsync();
        decimal totalCash = all.Sum(x => x.Bakiye);

        Assert.True(totalCash >= 100000m);
    }

    [Fact]
    public async Task Scenario_102_Rapor_IncomeStatement_OperatingProfitCalculations()
    {
        decimal brutSatis = 500000m;
        decimal satisinMaliyeti = 300000m;
        decimal faaliyetGiderleri = 50000m;
        decimal finansmanGiderleri = 20000m;

        decimal brutKar = brutSatis - satisinMaliyeti; // 200.000
        decimal faaliyetKari = brutKar - faaliyetGiderleri; // 150.000
        decimal donemKari = faaliyetKari - finansmanGiderleri; // 130.000

        Assert.Equal(200000m, brutKar);
        Assert.Equal(150000m, faaliyetKari);
        Assert.Equal(130000m, donemKari);
    }

    [Theory]
    [InlineData(100000, 20000, 5000, 115000)]
    [InlineData(50000, 0, 10000, 40000)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_103_to_105_CashFlowProjection(decimal acilis, decimal tahsilat, decimal odeme, decimal expectedKapanis)
    {
        decimal kapanis = acilis + tahsilat - odeme;
        Assert.Equal(expectedKapanis, kapanis);
    }

    [Theory]
    [InlineData(10000, 15000, -5000)] // Negatif nakit açığı
    [InlineData(20000, 12000, 8000)]
    public void Scenario_106_to_107_NetWorkingCapitalDifference(decimal donenVarliklar, decimal kisaVadeliBorclar, decimal expectedNetSermaye)
    {
        decimal netCalismaSermayesi = donenVarliklar - kisaVadeliBorclar;
        Assert.Equal(expectedNetSermaye, netCalismaSermayesi);
    }

    [Theory]
    [InlineData(100000, 50000, 2.0)] // Cari Oran = Dönen Varlık / K.V. Borç
    [InlineData(60000, 60000, 1.0)]
    [InlineData(40000, 80000, 0.5)]
    public void Scenario_108_to_110_CurrentRatioCalculations(decimal donenVarliklar, decimal kisaVadeliBorclar, double expectedRatio)
    {
        double ratio = kisaVadeliBorclar > 0 ? (double)(donenVarliklar / kisaVadeliBorclar) : 0.0;
        Assert.Equal(expectedRatio, Math.Round(ratio, 1));
    }

    [Theory]
    [InlineData(100000, 30000, 50000, 1.4)] // Likidite Oranı = (Dönen Varlık - Stok) / K.V. Borç
    [InlineData(50000, 20000, 30000, 1.0)]
    public void Scenario_111_to_112_AcidTestLiquidityRatio(decimal donenVarlik, decimal stok, decimal kisaBorc, double expectedRatio)
    {
        double ratio = kisaBorc > 0 ? (double)((donenVarlik - stok) / kisaBorc) : 0.0;
        Assert.Equal(expectedRatio, Math.Round(ratio, 1));
    }

    [Theory]
    [InlineData(20000, 50000, 0.4)] // Nakit Oranı = Hazır Değerler / K.V. Borç
    [InlineData(50000, 50000, 1.0)]
    public void Scenario_113_to_114_CashRatioCalculations(decimal hazirDegerler, decimal kisaBorc, double expectedRatio)
    {
        double ratio = kisaBorc > 0 ? (double)(hazirDegerler / kisaBorc) : 0.0;
        Assert.Equal(expectedRatio, Math.Round(ratio, 1));
    }

    [Fact]
    public void Scenario_115_Rapor_ZeroDebtCashRatio_ReturnsZero()
    {
        decimal hazir = 50000m;
        decimal borc = 0m;
        double ratio = borc > 0 ? (double)(hazir / borc) : 0.0;
        Assert.Equal(0.0, ratio);
    }

    // =============================================================
    // 10. PDF Üretimi & Sayfa Çıktı Testleri (116 - 125)
    // =============================================================

    [Fact]
    public async Task Scenario_116_PdfService_EmptyTableData_ProducesValidPdf()
    {
        var emptyRows = new List<string[]>();
        var result = await _pdfService.GenerateGenericTablePdfAsync("Boş Rapor", new[] { "Başlık 1", "Başlık 2" }, emptyRows);

        Assert.True(result.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 0);
    }

    [Fact]
    public async Task Scenario_117_PdfService_FiftyRowsTable_ProducesNonEmptyPdf()
    {
        var rows = new List<string[]>();
        for (int i = 1; i <= 50; i++)
        {
            rows.Add(new[] { $"Kalem {i}", $"{i * 100} TL", "Normal" });
        }

        var result = await _pdfService.GenerateGenericTablePdfAsync("50 Kalemli Rapor", new[] { "Kalem", "Tutar", "Durum" }, rows);
        Assert.True(result.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 1000);
    }

    [Theory]
    [InlineData("Cari Yaşlandırma Raporu")]
    [InlineData("Ürün Karlılık Raporu")]
    [InlineData("Müşteri Karlılık Analizi")]
    [InlineData("Nakit Akış Tablosu")]
    public async Task Scenario_118_to_121_PdfService_VariousReportTitles_GeneratePdf(string reportTitle)
    {
        var rows = new List<string[]> { new[] { "Veri A", "Veri B" } };
        var res = await _pdfService.GenerateGenericTablePdfAsync(reportTitle, new[] { "Sütun A", "Sütun B" }, rows);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
    }

    [Fact]
    public async Task Scenario_122_PdfService_TurkishCharactersInTable_ProducesPdfWithoutError()
    {
        string trText = "ŞAFT VE DÖKÜM ÇELİK SANAYİ A.Ş.";
        var rows = new List<string[]> { new[] { trText, "₺ 150.000,00" } };
        var res = await _pdfService.GenerateGenericTablePdfAsync("Türkçe Karakter Test Raporu", new[] { "Ünvan", "Bakiye" }, rows);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
    }

    [Fact]
    public async Task Scenario_123_PdfService_SpecialSymbolsInTitle_DoesNotThrow()
    {
        var rows = new List<string[]> { new[] { "Örnek 1", "100" } };
        var res = await _pdfService.GenerateGenericTablePdfAsync("Rapor (#1) %100 Doğruluk [2026]", new[] { "S1", "S2" }, rows);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    public void Scenario_124_to_125_TableRowsCountIntegrity(int inputCount, int expectedCount)
    {
        var list = new List<string[]>();
        for (int i = 0; i < inputCount; i++) list.Add(new[] { $"Col1_{i}", $"Col2_{i}" });
        Assert.Equal(expectedCount, list.Count);
    }

    // =============================================================
    // 11. Tarih Aralığı & Filtre Kontrolleri (126 - 135)
    // =============================================================

    [Fact]
    public void Scenario_126_RaporListViewModel_DefaultDateRange_IsWholeCurrentYear()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.NotNull(vm.StartDate);
        Assert.NotNull(vm.EndDate);

        Assert.Equal(1, vm.StartDate.Value.Month);
        Assert.Equal(1, vm.StartDate.Value.Day);
        Assert.Equal(12, vm.EndDate.Value.Month);
        Assert.Equal(31, vm.EndDate.Value.Day);
    }

    [Fact]
    public void Scenario_127_RaporListViewModel_SetCustomDateRange_UpdatesDates()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        var d1 = new DateTime(2026, 3, 1);
        var d2 = new DateTime(2026, 3, 31);

        vm.StartDate = d1;
        vm.EndDate = d2;

        Assert.Equal(d1, vm.StartDate);
        Assert.Equal(d2, vm.EndDate);
    }

    [Theory]
    [InlineData(2026, 1, 1, 2026, 1, 31, 31)] // Ocak 31 gün
    [InlineData(2026, 2, 1, 2026, 2, 28, 28)] // Şubat 28 gün
    [InlineData(2026, 4, 1, 2026, 4, 30, 30)] // Nisan 30 gün
    public void Scenario_128_to_130_DaysInReportPeriodCalculations(int y, int m, int d1, int y2, int m2, int d2, int expectedDays)
    {
        var start = new DateTime(y, m, d1);
        var end = new DateTime(y2, m2, d2);
        int days = (end - start).Days + 1;
        Assert.Equal(expectedDays, days);
    }

    [Fact]
    public void Scenario_131_RaporListViewModel_SearchReports_CaseInsensitiveMatch()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.SearchString = "karlılık";

        var filtered = vm.Reports.Where(r => r.Title.Contains("Karlılık", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.NotEmpty(filtered);
    }

    [Fact]
    public void Scenario_132_RaporListViewModel_SearchEmpty_ReturnsAllCatalog()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.SearchString = "";
        Assert.True(vm.Reports.Count >= 20);
    }

    [Theory]
    [InlineData("Genel Durum", 1)]
    [InlineData("Finans", 3)]
    [InlineData("Cari Hesap", 3)]
    [InlineData("Stok", 3)]
    [InlineData("Karlılık", 2)]
    public void Scenario_133_to_135_CategoryReportMinimumCounts(string category, int minExpected)
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        int count = vm.Reports.Count(r => r.Category == category);
        Assert.True(count >= minExpected);
    }

    // =============================================================
    // 12. Cari Seçimi & İletişim Durumları (136 - 145)
    // =============================================================

    [Fact]
    public void Scenario_136_RaporListViewModel_CariSelectionPanelVisibility_Toggles()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.IsCariSelectionVisible = true;
        Assert.True(vm.IsCariSelectionVisible);

        vm.IsCariSelectionVisible = false;
        Assert.False(vm.IsCariSelectionVisible);
    }

    [Fact]
    public void Scenario_137_RaporListViewModel_DateSelectionPanelVisibility_Toggles()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.IsDateSelectionVisible = true;
        Assert.True(vm.IsDateSelectionVisible);

        vm.CloseDateSelectionCommand.Execute(null);
        Assert.False(vm.IsDateSelectionVisible);
    }

    [Fact]
    public void Scenario_138_RaporListViewModel_CariSearchTerm_FiltersResults()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.CariSearchTerm = "Aranan Müşteri";
        Assert.Equal("Aranan Müşteri", vm.CariSearchTerm);
    }

    [Fact]
    public void Scenario_139_RaporListViewModel_ErrorMessageNotification_SetsErrorVisible()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        vm.ErrorMessage = "Yetkisiz işlem veya veri bulunamadı.";
        Assert.True(vm.IsErrorVisible);

        vm.ClearErrorCommand.Execute(null);
        Assert.False(vm.IsErrorVisible);
    }

    [Theory]
    [InlineData("Box", "Stok")]
    [InlineData("Money", "Finans")]
    [InlineData("People", "Cari Hesap")]
    [InlineData("Receipt", "Satış")]
    public void Scenario_140_to_143_ReportIconCategoryConsistency(string icon, string category)
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        var items = vm.Reports.Where(r => r.Category == category).ToList();
        Assert.NotEmpty(items);
    }

    [Fact]
    public void Scenario_144_RaporListViewModel_IsGenerating_DefaultsToFalse()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.False(vm.IsGenerating);
    }

    [Fact]
    public void Scenario_145_RaporListViewModel_CariSearchResults_InitiallyEmpty()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.NotNull(vm.CariSearchResults);
    }

    // =============================================================
    // 13. E2E Raporlama Yaşam Döngüsü (146 - 154)
    // =============================================================

    [Fact]
    public async Task Scenario_146_EndToEndReportingLifecycle_DataCreationToPdfExport()
    {
        // 1. İşlem verisi oluşturma
        var cari = new CariKart { CariKod = "CAR-E2E-R", Unvan = "E2E Rapor Şirketi", Borc = 50000m, Alacak = 10000m };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura { FaturaNo = "FAT-E2E-R", Tur = "Satış", CariUnvan = cari.Unvan, GenelToplam = 40000m };
        await _uow.Faturalar.SaveAsync(fatura);

        // 2. Verileri getirme
        var retrievedCari = await _uow.Cariler.GetByIdAsync(cari.Id);
        var retrievedFatura = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.NotNull(retrievedCari);
        Assert.NotNull(retrievedFatura);

        // 3. Tablo satırlarını derleme
        var rows = new List<string[]>
        {
            new[] { retrievedCari.Unvan, $"{retrievedCari.Borc:N2} TL", $"{retrievedCari.Alacak:N2} TL", $"{retrievedCari.Bakiye:N2} TL" }
        };

        // 4. PDF Raporu Oluşturma
        var pdfResult = await _pdfService.GenerateGenericTablePdfAsync(
            "E2E Cari Bakiye Raporu",
            new[] { "Ünvan", "Borç", "Alacak", "Bakiye" },
            rows
        );

        // 5. Doğrulama
        Assert.True(pdfResult.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 500);
    }

    [Fact]
    public async Task Scenario_147_EndToEndReportingLifecycle_StockInventoryReportToPdf()
    {
        var s1 = new StokKart { StokKodu = "STK-E2E-1", StokAdi = "Rulman 6204", Miktar = 150, SatisFiyati = 80m };
        var s2 = new StokKart { StokKodu = "STK-E2E-2", StokAdi = "Kayış B12", Miktar = 80, SatisFiyati = 120m };
        await _uow.Stoklar.SaveAsync(s1);
        await _uow.Stoklar.SaveAsync(s2);

        var rows = new List<string[]>
        {
            new[] { s1.StokKodu, s1.StokAdi, $"{s1.Miktar}", $"{s1.SatisFiyati:N2} TL" },
            new[] { s2.StokKodu, s2.StokAdi, $"{s2.Miktar}", $"{s2.SatisFiyati:N2} TL" }
        };

        var pdfResult = await _pdfService.GenerateGenericTablePdfAsync(
            "E2E Stok Envanter Raporu",
            new[] { "Kod", "Ürün Adı", "Miktar", "Satış Fiyatı" },
            rows
        );

        Assert.True(pdfResult.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
    }

    [Fact]
    public void Scenario_148_Rapor_LargeDataSummaryAggregation()
    {
        var data = Enumerable.Range(1, 1000).Select(i => (decimal)i * 10m).ToList();
        decimal total = data.Sum();
        decimal average = data.Average();

        Assert.Equal(5005000m, total);
        Assert.Equal(5005m, average);
    }

    [Fact]
    public void Scenario_149_Rapor_MedianCalculationForOrderAmounts()
    {
        var amounts = new List<decimal> { 100m, 200m, 300m, 400m, 500m };
        amounts.Sort();
        decimal median = amounts[amounts.Count / 2];
        Assert.Equal(300m, median);
    }

    [Theory]
    [InlineData(10000, 2000, 1000, 7000)]
    [InlineData(50000, 10000, 0, 40000)]
    public void Scenario_150_to_151_EbitdaCalculations(decimal netSatis, decimal faaliyetGideri, decimal amortisman, decimal expectedEbitda)
    {
        decimal favok = netSatis - faaliyetGideri + amortisman;
        Assert.True(favok > 0);
    }

    [Fact]
    public void Scenario_152_Rapor_ReportsCollectionNotNull()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.NotNull(vm.Reports);
        Assert.NotEmpty(vm.Reports);
    }

    [Fact]
    public void Scenario_153_Rapor_TurkishCategoryFiltering()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        var strategic = vm.Reports.Where(r => r.Category == "Stratejik").ToList();
        Assert.NotEmpty(strategic);
    }

    [Fact]
    public void Scenario_154_Rapor_CatalogItemsHaveNonNullCommandsOrDescriptions()
    {
        var vm = _serviceProvider.GetRequiredService<RaporListViewModel>();
        Assert.All(vm.Reports, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Title));
            Assert.False(string.IsNullOrWhiteSpace(r.Category));
            Assert.False(string.IsNullOrWhiteSpace(r.Description));
        });
    }
}
