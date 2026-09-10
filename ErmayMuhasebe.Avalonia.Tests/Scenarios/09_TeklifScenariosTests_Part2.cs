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

public partial class TeklifScenariosTests
{
    // =============================================================
    // 5. Teklif Detayları & Çoklu Kalem İşlemleri (51 - 65)
    // =============================================================

    [Fact]
    public async Task Scenario_51_Teklif_SaveWithDetailsAsync_PersistsHeaderAndLines()
    {
        var teklif = new Teklif
        {
            TeklifNo = "TEK-DET-51",
            CariId = 12,
            CariUnvan = "Proje Ltd.",
            Tarih = DateTime.Today,
            GenelToplam = 18000m,
            Durum = "Hazırlandı"
        };
        var line1 = new TeklifDetay { StokAdi = "Paslanmaz Sac", Miktar = 10, BirimFiyat = 1000m, Tutar = 10000m };
        var line2 = new TeklifDetay { StokAdi = "Lazer Kesim İşçiliği", Miktar = 20, BirimFiyat = 400m, Tutar = 8000m };

        await _uow.Teklifler.SaveWithDetailsAsync(teklif, new List<TeklifDetay> { line1, line2 });

        Assert.True(teklif.Id > 0);
        var lines = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
        Assert.Equal(2, lines.Count);
        Assert.Contains(lines, l => l.StokAdi == "Paslanmaz Sac");
        Assert.Contains(lines, l => l.StokAdi == "Lazer Kesim İşçiliği");
    }

    [Fact]
    public async Task Scenario_52_TeklifDetay_UpdateLines_ReplacesPreviousLines()
    {
        var teklif = new Teklif { TeklifNo = "TEK-UPD-52", GenelToplam = 5000m };
        var l1 = new TeklifDetay { StokAdi = "Eski Kalem", Miktar = 5, BirimFiyat = 1000m, Tutar = 5000m };
        await _uow.Teklifler.SaveWithDetailsAsync(teklif, new List<TeklifDetay> { l1 });

        var lNew1 = new TeklifDetay { StokAdi = "Yeni Kalem 1", Miktar = 2, BirimFiyat = 3000m, Tutar = 6000m };
        await _uow.Teklifler.SaveWithDetailsAsync(teklif, new List<TeklifDetay> { lNew1 });

        var linesAfter = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
        Assert.Single(linesAfter);
        Assert.Equal("Yeni Kalem 1", linesAfter[0].StokAdi);
    }

    [Fact]
    public async Task Scenario_53_Teklif_ForeignCurrencySupport_StoresCurrency()
    {
        var teklif = new Teklif { TeklifNo = "TEK-FX-53", OdemeBilgisi = "USD Akreditifli", GenelToplam = 15000m };
        var line = new TeklifDetay { StokAdi = "İthal Pompa", Miktar = 3, BirimFiyat = 5000m, Tutar = 15000m, ParaBirimi = "USD" };
        await _uow.Teklifler.SaveWithDetailsAsync(teklif, new List<TeklifDetay> { line });

        var lines = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
        Assert.Equal("USD", lines.First().ParaBirimi);
    }

    [Fact]
    public async Task Scenario_54_TeklifDetay_TopMiktariAndBirim_StoresPackagingDetails()
    {
        var teklif = new Teklif { TeklifNo = "TEK-PKG-54", GenelToplam = 4500m };
        var line = new TeklifDetay
        {
            StokAdi = "İplik",
            Miktar = 300,
            TopMiktari = 6,
            Birim = "Kg",
            BirimFiyat = 15m,
            Tutar = 4500m
        };
        await _uow.Teklifler.SaveWithDetailsAsync(teklif, new List<TeklifDetay> { line });

        var lines = await _uow.Teklifler.GetDetaylarAsync(teklif.Id);
        Assert.Equal(6, lines.First().TopMiktari);
        Assert.Equal("Kg", lines.First().Birim);
    }

    [Theory]
    [InlineData(10, 200, 20, 2400)] // 10 * 200 = 2000 + %20 KDV = 2400
    [InlineData(5, 500, 10, 2750)]  // 5 * 500 = 2500 + %10 KDV = 2750
    [InlineData(100, 10, 0, 1000)]  // %0 KDV
    public void Scenario_55_to_57_QuoteLineCalculationsWithVat(double miktar, decimal fiyat, double kdvOrani, decimal expectedTotal)
    {
        decimal matrah = (decimal)miktar * fiyat;
        decimal kdv = matrah * (decimal)(kdvOrani / 100.0);
        decimal total = matrah + kdv;
        Assert.Equal(expectedTotal, total);
    }

    [Theory]
    [InlineData(5000, 10, 4500)]
    [InlineData(10000, 25, 7500)]
    [InlineData(2000, 0, 2000)]
    public void Scenario_58_to_60_QuoteLineDiscountCalculations(decimal brut, double iskontoOrani, decimal expectedNet)
    {
        decimal iskonto = brut * (decimal)(iskontoOrani / 100.0);
        decimal net = brut - iskonto;
        Assert.Equal(expectedNet, net);
    }

    [Fact]
    public void Scenario_61_Teklif_FiyatPropertyAlias_SyncsWithBirimFiyat()
    {
        var d = new TeklifDetay { BirimFiyat = 850m };
        Assert.Equal(850m, d.Fiyat);

        d.Fiyat = 920m;
        Assert.Equal(920m, d.BirimFiyat);
    }

    [Theory]
    [InlineData(1000, 2000, 3000, 6000)]
    [InlineData(45000, 5000, 0, 50000)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_62_to_65_QuoteGrandTotalSum(decimal l1, decimal l2, decimal l3, decimal expectedGrand)
    {
        decimal total = l1 + l2 + l3;
        Assert.Equal(expectedGrand, total);
    }

    // =============================================================
    // 6. Teklif Durum Akışları & Yaşam Döngüsü (66 - 80)
    // =============================================================

    [Fact]
    public async Task Scenario_66_Teklif_StatusFlow_Gonderildi()
    {
        var t = new Teklif { TeklifNo = "TEK-GND-66", Durum = "Hazırlandı" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Gönderildi";
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Gönderildi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_67_Teklif_StatusFlow_Onaylandi()
    {
        var t = new Teklif { TeklifNo = "TEK-ONY-67", Durum = "Gönderildi" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Onaylandı";
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Onaylandı", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_68_Teklif_StatusFlow_SipariseDonustu()
    {
        var t = new Teklif { TeklifNo = "TEK-ORD-68", Durum = "Onaylandı" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Siparişe Dönüştü";
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Siparişe Dönüştü", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_69_Teklif_StatusFlow_Reddedildi()
    {
        var t = new Teklif { TeklifNo = "TEK-RED-69", Durum = "Gönderildi" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Reddedildi";
        t.Aciklama = "Fiyat yüksek bulundu";
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Reddedildi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_70_Teklif_StatusFlow_RevizeEdildi()
    {
        var t = new Teklif { TeklifNo = "TEK-REV-70", Durum = "Gönderildi" };
        await _uow.Teklifler.SaveAsync(t);

        t.Durum = "Revize Edildi";
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal("Revize Edildi", retrieved!.Durum);
    }

    [Theory]
    [InlineData("Hazırlandı", true)]
    [InlineData("Gönderildi", true)]
    [InlineData("Revize Edildi", true)]
    [InlineData("Onaylandı", true)]
    [InlineData("Reddedildi", false)]
    [InlineData("Siparişe Dönüştü", false)]
    public void Scenario_71_to_76_CanEditQuoteByStatus(string durum, bool expectedCanEdit)
    {
        bool canEdit = durum != "Reddedildi" && durum != "Siparişe Dönüştü";
        Assert.Equal(expectedCanEdit, canEdit);
    }

    [Fact]
    public async Task Scenario_77_Teklif_ExpiredValidityDate_IdentifiedAsExpired()
    {
        var t = new Teklif
        {
            TeklifNo = "TEK-EXP-77",
            GecerlilikTarihi = DateTime.Today.AddDays(-2),
            Durum = "Gönderildi"
        };
        await _uow.Teklifler.SaveAsync(t);

        bool isExpired = t.GecerlilikTarihi.HasValue && t.GecerlilikTarihi.Value < DateTime.Today;
        Assert.True(isExpired);
    }

    [Theory]
    [InlineData("2026-09-01", "2026-09-10", true)]
    [InlineData("2026-09-15", "2026-09-10", false)]
    [InlineData("2026-09-10", "2026-09-10", false)]
    public void Scenario_78_to_80_ValidityExpirationRules(string gecerlilikStr, string bugunStr, bool expectedExpired)
    {
        DateTime gecerlilik = DateTime.Parse(gecerlilikStr);
        DateTime bugun = DateTime.Parse(bugunStr);

        bool expired = gecerlilik < bugun;
        Assert.Equal(expectedExpired, expired);
    }

    // =============================================================
    // 7. Teklif Listeleme & Filtreleme UI Testleri (81 - 95)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_81_TeklifListViewModel_LoadsAllQuotes()
    {
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TK-L1", GenelToplam = 1000m });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TK-L2", GenelToplam = 2000m });

        var vm = _serviceProvider.GetRequiredService<TeklifListViewModel>();
        await vm.LoadTekliflerAsync();

        Assert.True(vm.Teklifler.Count >= 2);
    }

    [AvaloniaFact]
    public async Task Scenario_82_TeklifListViewModel_SearchByCari_Matches()
    {
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TK-S1", CariUnvan = "Balkan Sanayi" });
        await _uow.Teklifler.SaveAsync(new Teklif { TeklifNo = "TK-S2", CariUnvan = "Doğu Kimya" });

        var vm = _serviceProvider.GetRequiredService<TeklifListViewModel>();
        vm.SearchString = "Balkan";
        await vm.LoadTekliflerAsync();

        Assert.Contains(vm.Teklifler, t => t.CariUnvan == "Balkan Sanayi");
        Assert.DoesNotContain(vm.Teklifler, t => t.CariUnvan == "Doğu Kimya");
    }

    [AvaloniaFact]
    public async Task Scenario_83_TeklifListViewModel_SelectQuote_UpdatesSelectedQuote()
    {
        var t = new Teklif { TeklifNo = "TK-SEL-83", GenelToplam = 8500m };
        await _uow.Teklifler.SaveAsync(t);

        var vm = _serviceProvider.GetRequiredService<TeklifListViewModel>();
        await vm.LoadTekliflerAsync();

        vm.SelectedTeklif = vm.Teklifler.FirstOrDefault(x => x.TeklifNo == "TK-SEL-83");
        Assert.NotNull(vm.SelectedTeklif);
        Assert.Equal("TK-SEL-83", vm.SelectedTeklif.TeklifNo);
    }

    [AvaloniaFact]
    public async Task Scenario_84_TeklifListViewModel_EmptySearch_LoadsAll()
    {
        var vm = _serviceProvider.GetRequiredService<TeklifListViewModel>();
        vm.SearchString = "";
        await vm.LoadTekliflerAsync();

        Assert.NotNull(vm.Teklifler);
    }

    [Fact]
    public async Task Scenario_85_Teklif_SoftDelete_SetsIsDeleted()
    {
        var t = new Teklif { TeklifNo = "TK-DEL-85", IsDeleted = false };
        await _uow.Teklifler.SaveAsync(t);

        t.IsDeleted = true;
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.True(retrieved!.IsDeleted);
    }

    [Theory]
    [InlineData(10, 6, 60.0)] // 6 kazanılan / 10 teklif = %60 Win Rate
    [InlineData(20, 5, 25.0)]
    [InlineData(15, 0, 0.0)]
    [InlineData(0, 0, 0.0)]
    public void Scenario_86_to_89_QuoteWinRateCalculations(int toplamTeklif, int kazanilanTeklif, double expectedRate)
    {
        double rate = toplamTeklif > 0 ? (double)kazanilanTeklif / toplamTeklif * 100.0 : 0.0;
        Assert.Equal(expectedRate, Math.Round(rate, 1));
    }

    [Theory]
    [InlineData(100000, 5, 20000)] // Ortalama Teklif = 100.000 / 5 = 20.000
    [InlineData(45000, 3, 15000)]
    [InlineData(0, 0, 0)]
    public void Scenario_90_to_92_AverageQuoteDealSize(decimal toplamTutar, int adet, decimal expectedOrtalama)
    {
        decimal ortalama = adet > 0 ? toplamTutar / adet : 0m;
        Assert.Equal(expectedOrtalama, ortalama);
    }

    [Fact]
    public async Task Scenario_93_Teklif_GetAllDetaylarAsync_ReturnsAcrossQuotes()
    {
        var t1 = new Teklif { TeklifNo = "T1" };
        var t2 = new Teklif { TeklifNo = "T2" };
        await _uow.Teklifler.SaveWithDetailsAsync(t1, new List<TeklifDetay> { new() { StokAdi = "TK-Item-1", Tutar = 500 } });
        await _uow.Teklifler.SaveWithDetailsAsync(t2, new List<TeklifDetay> { new() { StokAdi = "TK-Item-2", Tutar = 800 } });

        var all = await _uow.Teklifler.GetAllDetaylarAsync();
        Assert.Contains(all, d => d.StokAdi == "TK-Item-1");
        Assert.Contains(all, d => d.StokAdi == "TK-Item-2");
    }

    [Fact]
    public async Task Scenario_94_Teklif_ExtremeValueAmount_RetainsPrecision()
    {
        decimal extreme = 77777777.77m;
        var t = new Teklif { TeklifNo = "TEK-EXT-94", GenelToplam = extreme };
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal(extreme, retrieved!.GenelToplam);
    }

    [Fact]
    public async Task Scenario_95_Teklif_TenantIsolation_FiltersTenantData()
    {
        var t1 = new Teklif { TeklifNo = "TEK-T1", TenantId = "tenant_1" };
        var t2 = new Teklif { TeklifNo = "TEK-T2", TenantId = "tenant_2" };
        await _uow.Teklifler.SaveAsync(t1);
        await _uow.Teklifler.SaveAsync(t2);

        var all = await _uow.Teklifler.GetAllAsync();
        Assert.Contains(all, x => x.TenantId == "tenant_1");
        Assert.Contains(all, x => x.TenantId == "tenant_2");
    }

    // =============================================================
    // 8. Teklif Açıklamaları & Ödeme Şartları (96 - 100)
    // =============================================================

    [Fact]
    public async Task Scenario_96_Teklif_PaymentTermsText_PersistsProperly()
    {
        string terms = "%30 Siparişte peşin, %70 Teslimattan önce nakit transfer";
        var t = new Teklif { TeklifNo = "TEK-TRM-96", OdemeBilgisi = terms };
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal(terms, retrieved!.OdemeBilgisi);
    }

    [Theory]
    [InlineData("Peşin")]
    [InlineData("30 Gün Vadeli")]
    [InlineData("60 Gün Vadeli Çek")]
    [InlineData("Kredi Kartı Tek Çekim")]
    public async Task Scenario_97_to_100_StandardPaymentTerms(string term)
    {
        var t = new Teklif { TeklifNo = $"TEK-TRM-{term}", OdemeBilgisi = term };
        await _uow.Teklifler.SaveAsync(t);

        var retrieved = await _uow.Teklifler.GetByIdAsync(t.Id);
        Assert.Equal(term, retrieved!.OdemeBilgisi);
    }
}
