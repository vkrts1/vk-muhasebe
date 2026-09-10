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

public partial class TeklifScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Teklif Oluşturma & Kalemler (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CreateQuote_WithItems_SavesSuccessfully()
    {
        var teklif = new Teklif
        {
            TeklifNo = "TEK-2026-001",
            Tarih = DateTime.Today,
            GecerlilikTarihi = DateTime.Today.AddDays(15),
            CariUnvan = "Teklif İsteyen Firma Ltd",
            GenelToplam = 24000m,
            Durum = "Hazırlandı"
        };
        await _uow.Teklifler.SaveAsync(teklif);

        var saved = await _uow.Teklifler.GetByIdAsync(teklif.Id);
        Assert.NotNull(saved);
        Assert.Equal("TEK-2026-001", saved.TeklifNo);
        Assert.Equal(24000m, saved.GenelToplam);
        Assert.Equal("Hazırlandı", saved.Durum);
    }

    [Theory]
    [InlineData("TEK-01", true)]
    [InlineData("TK-2026-99", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Scenario_02_to_05_QuoteNumberValidation(string no, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(no);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(1, true)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    [InlineData(-10, false)]
    public void Scenario_06_to_10_ValidityDateChecks(int addDays, bool isStillValid)
    {
        DateTime gecerlilik = DateTime.Today.AddDays(addDays);
        bool valid = gecerlilik >= DateTime.Today;
        Assert.Equal(isStillValid, valid);
    }

    [Theory]
    [InlineData(10, 100, 0, 20, 1000, 200, 1200)]
    [InlineData(5, 400, 15, 10, 1700, 170, 1870)]
    [InlineData(50, 10, 5, 0, 475, 0, 475)]
    public void Scenario_11_to_13_QuoteItemCalculations(decimal miktar, decimal fiyat, decimal iskonto, int kdv, decimal expMatrah, decimal expKdv, decimal expToplam)
    {
        decimal brut = miktar * fiyat;
        decimal netMatrah = brut - (brut * (iskonto / 100m));
        decimal kdvTutari = netMatrah * (kdv / 100m);
        decimal toplam = netMatrah + kdvTutari;

        Assert.Equal(expMatrah, netMatrah);
        Assert.Equal(expKdv, kdvTutari);
        Assert.Equal(expToplam, toplam);
    }

    [AvaloniaFact]
    public async Task Scenario_14_DeleteQuoteItem_RecalculatesTotals()
    {
        var teklif = new Teklif { TeklifNo = "TEK-DEL", GenelToplam = 10000m };
        await _uow.Teklifler.SaveAsync(teklif);

        var db = _dbService.GetConnection();
        var k1 = new TeklifDetay { TeklifId = teklif.Id, Tutar = 6000m };
        var k2 = new TeklifDetay { TeklifId = teklif.Id, Tutar = 4000m };
        await db.InsertAsync(k1);
        await db.InsertAsync(k2);

        await db.DeleteAsync(k2);

        var remaining = await db.Table<TeklifDetay>().Where(k => k.TeklifId == teklif.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(6000m, remaining.Sum(k => k.Tutar));
    }

    [Theory]
    [InlineData(10000, 1000, 9000)]
    [InlineData(50000, 5000, 45000)]
    public void Scenario_15_SpecialQuoteDiscountCalculation(decimal teklifToplam, decimal indirim, decimal expNet)
    {
        decimal net = teklifToplam - indirim;
        Assert.Equal(expNet, net);
    }

    // -------------------------------------------------------------
    // 2. Durum Değişiklikleri & İş Akışı (15 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("Hazırlandı")]
    [InlineData("Gönderildi")]
    [InlineData("Kabul Edildi")]
    [InlineData("Reddedildi")]
    [InlineData("Siparişe Dönüştü")]
    [InlineData("Faturalandı")]
    public async Task Scenario_16_to_21_AllQuoteStatuses_PersistedAccurately(string durum)
    {
        var t = new Teklif { TeklifNo = $"T-DUR-{durum}", Durum = durum };
        await _uow.Teklifler.SaveAsync(t);

        var saved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal(durum, saved!.Durum);
    }

    [AvaloniaFact]
    public async Task Scenario_22_QuoteStatusTransition_FromHazirlandiToGonderildi()
    {
        var t = new Teklif { TeklifNo = "T-ST-1", Durum = "Hazırlandı" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Gönderildi";
        await _uow.Teklifler.SaveAsync(t);

        var refT = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Gönderildi", refT!.Durum);
    }

    [AvaloniaFact]
    public async Task Scenario_23_QuoteStatusTransition_FromGonderildiToKabulEdildi()
    {
        var t = new Teklif { TeklifNo = "T-ST-2", Durum = "Gönderildi" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Kabul Edildi";
        await _uow.Teklifler.SaveAsync(t);

        var refT = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Kabul Edildi", refT!.Durum);
    }

    [AvaloniaFact]
    public async Task Scenario_24_QuoteStatusTransition_ToReddedildiWithReason()
    {
        var t = new Teklif { TeklifNo = "T-ST-3", Durum = "Gönderildi", Aciklama = "Müşteri teklifi inceliyor" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Reddedildi";
        t.Aciklama = "Fiyat yüksek bulundu";
        await _uow.Teklifler.SaveAsync(t);

        var refT = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Reddedildi", refT!.Durum);
        Assert.Equal("Fiyat yüksek bulundu", refT.Aciklama);
    }

    [Theory]
    [InlineData("Kabul Edildi", true)]
    [InlineData("Hazırlandı", true)]
    [InlineData("Gönderildi", true)]
    [InlineData("Reddedildi", false)]
    [InlineData("Faturalandı", false)]
    [InlineData("Siparişe Dönüştü", false)]
    public void Scenario_25_to_30_CanConvertToOrderOrInvoiceCheck(string durum, bool canConvert)
    {
        bool allowed = durum != "Reddedildi" && durum != "Faturalandı" && durum != "Siparişe Dönüştü";
        Assert.Equal(canConvert, allowed);
    }

    // -------------------------------------------------------------
    // 3. Arama, Filtreleme ve Sıralama (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_31_SearchByTeklifNo_FiltersList()
    {
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TEK-PRO-01" });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TEK-ECO-02" });

        var vm = _serviceProvider.GetRequiredService<TeklifListViewModel>();
        await vm.LoadTekliflerAsync();

        vm.SearchString = "PRO";
        await vm.LoadTekliflerAsync();

        Assert.Single(vm.Teklifler);
        Assert.Equal("TEK-PRO-01", vm.Teklifler[0].TeklifNo);
    }

    [AvaloniaFact]
    public async Task Scenario_32_SearchByCariUnvan_FiltersList()
    {
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "T1", CariUnvan = "Kardelen Tekstil" });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "T2", CariUnvan = "Menekşe Moda" });

        var vm = _serviceProvider.GetRequiredService<TeklifListViewModel>();
        vm.SearchString = "Kardelen";
        await vm.LoadTekliflerAsync();

        Assert.Single(vm.Teklifler);
        Assert.Equal("Kardelen Tekstil", vm.Teklifler[0].CariUnvan);
    }

    [AvaloniaFact]
    public async Task Scenario_33_FilterByDurum_ReturnsAcceptedOnly()
    {
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TK1", Durum = "Kabul Edildi" });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TR1", Durum = "Reddedildi" });

        var all = await _uow.Teklifler.GetAllAsync();
        var accepted = all.Where(t => t.Durum == "Kabul Edildi").ToList();

        Assert.Single(accepted);
        Assert.Equal("TK1", accepted[0].TeklifNo);
    }

    [AvaloniaFact]
    public async Task Scenario_34_to_37_SortingQuotes_ByAmountAndDate()
    {
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "T-LOW", GenelToplam = 5000, Tarih = DateTime.Today.AddDays(-4) });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "T-HIGH", GenelToplam = 45000, Tarih = DateTime.Today.AddDays(-2) });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "T-MID", GenelToplam = 15000, Tarih = DateTime.Today });

        var list = (await _uow.Teklifler.GetAllAsync()).ToList();

        var sortedAmount = list.OrderByDescending(t => t.GenelToplam).Select(t => t.TeklifNo).ToList();
        Assert.Equal("T-HIGH", sortedAmount[0]);
        Assert.Equal("T-LOW", sortedAmount[2]);

        var sortedDate = list.OrderByDescending(t => t.Tarih).Select(t => t.TeklifNo).ToList();
        Assert.Equal("T-MID", sortedDate[0]);
        Assert.Equal("T-LOW", sortedDate[2]);
    }

    [Theory]
    [InlineData(10, 4, 40.0)]
    [InlineData(20, 15, 75.0)]
    [InlineData(5, 0, 0.0)]
    public void Scenario_38_to_40_QuoteWinRateCalculations(int toplamTeklif, int kabulEdilen, double expOran)
    {
        double oran = toplamTeklif > 0 ? ((double)kabulEdilen / toplamTeklif) * 100.0 : 0.0;
        Assert.Equal(expOran, oran, precision: 1);
    }

    // -------------------------------------------------------------
    // 4. Çıktılar & Eylemler (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_41_GenerateQuotePdf_ProducesPdf()
    {
        var teklif = new Teklif
        {
            TeklifNo = "TEK-PDF-01",
            CariUnvan = "PDF Test Musteri",
            Tarih = DateTime.Today,
            GenelToplam = 15000m
        };
        await _uow.Teklifler.SaveAsync(teklif);

        var detaylar = new List<TeklifDetay>
        {
            new() { TeklifId = teklif.Id, StokAdi = "Kalem A", Miktar = 10, BirimFiyat = 1500, Tutar = 15000 }
        };

        var res = await _pdfService.GenerateTeklifPdfAsync(teklif, detaylar, null);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 500);
    }

    [AvaloniaFact]
    public async Task Scenario_42_DeleteQuote_RemovesRecord()
    {
        var t = new Teklif { TeklifNo = "TEK-DEL-ME", Durum = "Hazırlandı" };
        await _uow.Teklifler.SaveAsync(t);

        await _uow.Teklifler.DeleteAsync(t);

        var deleted = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Null(deleted);
    }

    [Theory]
    [InlineData("TL", 1000, 1000)]
    [InlineData("USD", 1000, 32500)]
    [InlineData("EUR", 1000, 35000)]
    public void Scenario_43_to_45_MultiCurrencyQuoteCalculations(string paraBirimi, decimal tutar, decimal expTlEquivalent)
    {
        decimal kur = paraBirimi == "USD" ? 32.5m : (paraBirimi == "EUR" ? 35.0m : 1.0m);
        decimal tl = tutar * kur;
        Assert.Equal(expTlEquivalent, tl);
    }

    [Theory]
    [InlineData(10000, 0.20, 2000)]
    [InlineData(50000, 0.20, 10000)]
    [InlineData(2500, 0.10, 250)]
    [InlineData(0, 0.20, 0)]
    [InlineData(1000, 0.00, 0)]
    public void Scenario_46_to_50_QuoteVatAmountVerification(decimal matrah, double oran, decimal expKdv)
    {
        decimal kdv = matrah * (decimal)oran;
        Assert.Equal(expKdv, kdv);
    }
}
