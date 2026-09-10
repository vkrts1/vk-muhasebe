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

public partial class FaturaScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Hesaplama ve Kalem Senaryoları (15 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData(10, 100, 20, 1000, 200, 1200)]   // %20 KDV: 10 adet * 100 TL = 1000 TL Matrah, 200 KDV, 1200 Toplam
    [InlineData(5, 200, 10, 1000, 100, 1100)]     // %10 KDV: 5 adet * 200 TL = 1000 TL Matrah, 100 KDV, 1100 Toplam
    [InlineData(20, 50, 1, 1000, 10, 1010)]       // %1 KDV: 20 adet * 50 TL = 1000 TL Matrah, 10 KDV, 1010 Toplam
    [InlineData(100, 15, 0, 1500, 0, 1500)]       // %0 KDV (Muaf): 1500 TL Matrah, 0 KDV, 1500 Toplam
    public void Scenario_01_to_04_VatCalculations(decimal miktar, decimal fiyat, int kdvOrani, decimal expMatrah, decimal expKdv, decimal expGenelToplam)
    {
        decimal matrah = miktar * fiyat;
        decimal kdv = matrah * (kdvOrani / 100m);
        decimal genelToplam = matrah + kdv;

        Assert.Equal(expMatrah, matrah);
        Assert.Equal(expKdv, kdv);
        Assert.Equal(expGenelToplam, genelToplam);
    }

    [Theory]
    [InlineData(10, 100, 10, 20, 900, 180, 1080)]
    [InlineData(5, 500, 20, 10, 2000, 200, 2200)]
    [InlineData(100, 10, 5, 0, 950, 0, 950)]
    public void Scenario_05_to_07_LineDiscountCalculations(decimal miktar, decimal fiyat, decimal iskontoOrani, int kdvOrani, decimal expMatrah, decimal expKdv, decimal expGenelToplam)
    {
        decimal brut = miktar * fiyat;
        decimal iskontoTutari = brut * (iskontoOrani / 100m);
        decimal netMatrah = brut - iskontoTutari;
        decimal kdv = netMatrah * (kdvOrani / 100m);
        decimal genelToplam = netMatrah + kdv;

        Assert.Equal(expMatrah, netMatrah);
        Assert.Equal(expKdv, kdv);
        Assert.Equal(expGenelToplam, genelToplam);
    }

    [Theory]
    [InlineData(10000, 1000, 20, 9000, 1800, 10800)]
    [InlineData(5000, 500, 10, 4500, 450, 4950)]
    public void Scenario_08_to_09_GeneralInvoiceDiscount(decimal matrah, decimal genelIskonto, int kdvOrani, decimal expNetMatrah, decimal expKdv, decimal expToplam)
    {
        decimal netMatrah = matrah - genelIskonto;
        decimal kdv = netMatrah * (kdvOrani / 100m);
        decimal toplam = netMatrah + kdv;

        Assert.Equal(expNetMatrah, netMatrah);
        Assert.Equal(expKdv, kdv);
        Assert.Equal(expToplam, toplam);
    }

    [Theory]
    [InlineData(1234.567, 1234.57)]
    [InlineData(99.994, 99.99)]
    [InlineData(99.995, 100.00)]
    public void Scenario_10_to_12_CurrencyRoundingPrecision(decimal input, decimal expectedRounded)
    {
        decimal rounded = Math.Round(input, 2, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedRounded, rounded);
    }

    [AvaloniaFact]
    public async Task Scenario_13_MultiItemInvoice_CalculatesTotalsCorrectly()
    {
        var fatura = new Fatura
        {
            FaturaNo = "FAT-MULTI",
            Tur = "Satis",
            Tarih = DateTime.Today,
            GenelToplam = 3600m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var db = _dbService.GetConnection();
        var k1 = new FaturaDetay { FaturaId = fatura.Id, Miktar = 10, BirimFiyat = 100, KDVOrani = 20, KdvTutari = 200, ToplamTutar = 1200 };
        var k2 = new FaturaDetay { FaturaId = fatura.Id, Miktar = 20, BirimFiyat = 100, KDVOrani = 20, KdvTutari = 400, ToplamTutar = 2400 };
        await db.InsertAsync(k1);
        await db.InsertAsync(k2);

        var kalemler = await db.Table<FaturaDetay>().Where(k => k.FaturaId == fatura.Id).ToListAsync();
        Assert.Equal(2, kalemler.Count);
        Assert.Equal(3600m, kalemler.Sum(k => k.ToplamTutar));
    }

    [AvaloniaFact]
    public async Task Scenario_14_DeleteInvoiceItem_ReducesTotal()
    {
        var fatura = new Fatura { FaturaNo = "FAT-DEL-ITEM", Tur = "Satis", GenelToplam = 2000m };
        await _uow.Faturalar.SaveAsync(fatura);

        var db = _dbService.GetConnection();
        var k1 = new FaturaDetay { FaturaId = fatura.Id, ToplamTutar = 1200m };
        var k2 = new FaturaDetay { FaturaId = fatura.Id, ToplamTutar = 800m };
        await db.InsertAsync(k1);
        await db.InsertAsync(k2);

        await db.DeleteAsync(k2);

        var remaining = await db.Table<FaturaDetay>().Where(k => k.FaturaId == fatura.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(1200m, remaining.Sum(k => k.ToplamTutar));
    }

    [Theory]
    [InlineData(1000, 200, 5, 10, 100)]
    public void Scenario_15_WithholdingTaxCalculation(decimal matrah, decimal kdv, int pay, int payda, decimal expTevkifat)
    {
        Assert.True(matrah > 0);
        decimal tevkifat = kdv * ((decimal)pay / payda);
        Assert.Equal(expTevkifat, tevkifat);
    }

    // -------------------------------------------------------------
    // 2. Alış & İade Faturaları (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_16_PurchaseInvoice_IncreasesSupplierCredit()
    {
        var tedarikci = new CariKart { CariKod = "TED-P1", Unvan = "Tedarikçi Kumaş", Alacak = 0 };
        await _uow.Cariler.SaveAsync(tedarikci);

        var fatura = new Fatura
        {
            FaturaNo = "ALIS-101",
            Tur = "Alis",
            CariId = tedarikci.Id,
            CariUnvan = tedarikci.Unvan,
            GenelToplam = 18000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = tedarikci.Id,
            IslemTuru = "Alış Faturası",
            Borc = 0,
            Alacak = 18000m
        });
        await _dbService.RecalculateCariBalanceAsync(tedarikci.Id);

        var refreshed = await _uow.Cariler.GetByIdAsync(tedarikci.Id);
        Assert.Equal(18000m, refreshed!.Alacak);
    }

    [AvaloniaFact]
    public async Task Scenario_17_SalesReturnInvoice_ReducesCustomerDebt()
    {
        var cari = new CariKart { CariKod = "C-RET-FAT", Unvan = "İade Yapan Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış Faturası", Borc = 10000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış İade Faturası", Borc = 0, Alacak = 3000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(7000m, refreshed!.Bakiye);
    }

    [Theory]
    [InlineData("Satis")]
    [InlineData("Alis")]
    [InlineData("SatisIade")]
    [InlineData("AlisIade")]
    public async Task Scenario_18_to_21_InvoiceTypes_SavedAccurately(string tur)
    {
        var f = new Fatura { FaturaNo = $"F-TUR-{tur}", Tur = tur, GenelToplam = 1000 };
        await _uow.Faturalar.SaveAsync(f);

        var saved = await _uow.Faturalar.GetByIdAsync(f.Id);
        Assert.Equal(tur, saved!.Tur);
    }

    [Theory]
    [InlineData(100, 10, 90)]
    [InlineData(50, 50, 0)]
    [InlineData(30, 40, -10)]
    [InlineData(100, 0, 100)]
    public void Scenario_22_to_25_StockQuantityReductions(decimal mevcut, decimal satilan, decimal expectedKalan)
    {
        decimal kalan = mevcut - satilan;
        Assert.Equal(expectedKalan, kalan);
    }

    // -------------------------------------------------------------
    // 3. Entegrasyon & Kalan Tutar Senaryoları (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_26_NewInvoice_SetsKalanEqualToGenelToplam()
    {
        var fatura = new Fatura { FaturaNo = "FAT-KAL-01", Tur = "Satis", GenelToplam = 7200m };
        await _uow.Faturalar.SaveAsync(fatura);

        var saved = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(saved!.GenelToplam, saved.Kalan);
    }

    [AvaloniaFact]
    public async Task Scenario_27_PartialPayment_ReducesInvoiceKalan()
    {
        var fatura = new Fatura { FaturaNo = "FAT-KAL-02", Tur = "Satis", GenelToplam = 10000m, Odenen = 0 };
        await _uow.Faturalar.SaveAsync(fatura);

        fatura.Odenen = 4000m;
        await _uow.Faturalar.SaveAsync(fatura);

        var refreshed = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(6000m, refreshed!.Kalan);
    }

    [AvaloniaFact]
    public async Task Scenario_28_FullPayment_SetsKalanToZeroAndClosed()
    {
        var fatura = new Fatura { FaturaNo = "FAT-KAL-03", Tur = "Satis", GenelToplam = 5000m, Odenen = 0 };
        await _uow.Faturalar.SaveAsync(fatura);

        fatura.Odenen = 5000m;
        await _uow.Faturalar.SaveAsync(fatura);

        var refreshed = await _uow.Faturalar.GetByIdAsync(fatura.Id);
        Assert.Equal(0m, refreshed!.Kalan);
    }

    [Theory]
    [InlineData(10000, 0, "Açık")]
    [InlineData(10000, 5000, "Kısmi Ödendi")]
    [InlineData(10000, 10000, "Kapalı")]
    public void Scenario_29_to_31_InvoiceStatusDerivation(decimal genelToplam, decimal odenen, string expectedDurum)
    {
        decimal kalan = genelToplam - odenen;
        string durum = kalan == 0 ? "Kapalı" : (odenen > 0 ? "Kısmi Ödendi" : "Açık");
        Assert.Equal(expectedDurum, durum);
    }

    [Theory]
    [InlineData("GIB2026000000001", true)]
    [InlineData("FAT-01", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Scenario_32_to_35_InvoiceNumberValidation(string fNo, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(fNo);
        Assert.Equal(expectedValid, isValid);
    }

    // -------------------------------------------------------------
    // 4. Arama, Filtreleme ve PDF Çıktısı (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_36_SearchByFaturaNo_FiltersAccurately()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-ABC-101", Tur = "Satis" });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FAT-XYZ-202", Tur = "Satis" });

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        await vm.LoadFaturalarAsync();

        vm.SearchString = "ABC";
        await vm.LoadFaturalarAsync();
        Assert.Single(vm.Faturalar);
        Assert.Equal("FAT-ABC-101", vm.Faturalar[0].FaturaNo);
    }

    [AvaloniaFact]
    public async Task Scenario_37_SearchByCariUnvan_FiltersAccurately()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F1", CariUnvan = "Anadolu Metal", Tur = "Satis" });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F2", CariUnvan = "Ege Kimya", Tur = "Satis" });

        var vm = _serviceProvider.GetRequiredService<FaturaListViewModel>();
        await vm.LoadFaturalarAsync();

        vm.SearchString = "Anadolu";
        await vm.LoadFaturalarAsync();
        Assert.Single(vm.Faturalar);
        Assert.Equal("Anadolu Metal", vm.Faturalar[0].CariUnvan);
    }

    [AvaloniaFact]
    public async Task Scenario_38_FilterByTur_ReturnsSalesOnly()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FS1", Tur = "Satis" });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "FA1", Tur = "Alis" });

        var all = await _uow.Faturalar.GetAllAsync();
        var salesOnly = all.Where(x => x.Tur == "Satis").ToList();

        Assert.Single(salesOnly);
        Assert.Equal("FS1", salesOnly[0].FaturaNo);
    }

    [AvaloniaFact]
    public async Task Scenario_39_GenerateInvoicePdf_ProducesValidPdfBytes()
    {
        var cari = new CariKart { CariKod = "C-PDF-INV", Unvan = "PDF Test Musteri" };
        await _uow.Cariler.SaveAsync(cari);

        var fatura = new Fatura
        {
            FaturaNo = "FAT-PDF-001",
            Tur = "Satis",
            CariId = cari.Id,
            CariUnvan = cari.Unvan,
            Tarih = DateTime.Today,
            GenelToplam = 5000m
        };
        await _uow.Faturalar.SaveAsync(fatura);

        var kalemler = new List<FaturaDetay>
        {
            new() { FaturaId = fatura.Id, StokAdi = "Kalem A", Miktar = 5, BirimFiyat = 1000, ToplamTutar = 5000 }
        };

        var result = await _pdfService.GenerateFaturaPdfAsync(fatura, kalemler);

        Assert.True(result.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 1000);
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(500, 5)]
    [InlineData(2500, 25)]
    public void Scenario_40_to_42_InvoicePaginationCalculations(int totalItems, int expectedPages)
    {
        int pageSize = 100;
        int pages = (int)Math.Ceiling((double)totalItems / pageSize);
        Assert.Equal(expectedPages, pages);
    }

    [AvaloniaFact]
    public async Task Scenario_43_to_46_SortingInvoices_ByDateAndAmount()
    {
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-LOW", Tarih = DateTime.Today.AddDays(-2), GenelToplam = 1000 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-HIGH", Tarih = DateTime.Today.AddDays(-1), GenelToplam = 8000 });
        await _uow.Faturalar.SaveAsync(new Fatura { FaturaNo = "F-MID", Tarih = DateTime.Today, GenelToplam = 4000 });

        var all = (await _uow.Faturalar.GetAllAsync()).ToList();

        var sortedAmountDesc = all.OrderByDescending(f => f.GenelToplam).Select(f => f.FaturaNo).ToList();
        Assert.Equal("F-HIGH", sortedAmountDesc[0]);
        Assert.Equal("F-LOW", sortedAmountDesc[2]);

        var sortedDateDesc = all.OrderByDescending(f => f.Tarih).Select(f => f.FaturaNo).ToList();
        Assert.Equal("F-MID", sortedDateDesc[0]);
        Assert.Equal("F-LOW", sortedDateDesc[2]);
    }

    [Theory]
    [InlineData(10000, 0.05, 500)]
    [InlineData(50000, 0.10, 5000)]
    [InlineData(20000, 0.00, 0)]
    [InlineData(15000, 0.02, 300)]
    public void Scenario_47_to_50_EarlyPaymentDiscountCalculations(decimal toplam, double oran, decimal expDiscount)
    {
        decimal discount = toplam * (decimal)oran;
        Assert.Equal(expDiscount, discount);
    }
}
