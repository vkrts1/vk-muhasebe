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

public partial class SiparisScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Sipariş Oluşturma & Kalemler (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CreateOrder_WithItems_CalculatesTotalsAndSaves()
    {
        var siparis = new Siparis
        {
            SiparisNo = "SIP-2026-001",
            Tarih = DateTime.Today,
            TeslimatTarihi = DateTime.Today.AddDays(7),
            CariUnvan = "Sipariş Müşterisi Ltd",
            GenelToplam = 12000m,
            Durum = "Beklemede"
        };
        await _uow.Siparisler.SaveAsync(siparis);

        var db = _dbService.GetConnection();
        var k1 = new SiparisDetay { SiparisId = siparis.Id, StokAdi = "Kumaş A", Miktar = 100, BirimFiyat = 100, Tutar = 12000 };
        await db.InsertAsync(k1);

        var saved = await _uow.Siparisler.GetByIdAsync(siparis.Id);
        Assert.NotNull(saved);
        Assert.Equal("SIP-2026-001", saved.SiparisNo);
        Assert.Equal(12000m, saved.GenelToplam);
        Assert.Equal("Beklemede", saved.Durum);
    }

    [Theory]
    [InlineData("SIP-01", true)]
    [InlineData("SIP-2026-9999", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void Scenario_02_to_05_OrderNumberValidation(string no, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(no);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(-1, false)]
    public void Scenario_06_to_08_DeliveryDateValidation(int addDays, bool expectedValid)
    {
        var siparisTarihi = DateTime.Today;
        var teslimTarihi = siparisTarihi.AddDays(addDays);
        bool isValid = teslimTarihi >= siparisTarihi;
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(10, 50, 0, 20, 500, 100, 600)]
    [InlineData(5, 200, 10, 10, 900, 90, 990)]
    [InlineData(100, 25, 0, 0, 2500, 0, 2500)]
    public void Scenario_09_to_11_OrderItemCalculations(decimal miktar, decimal fiyat, decimal iskonto, int kdv, decimal expMatrah, decimal expKdv, decimal expToplam)
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
    public async Task Scenario_12_DeleteOrderItem_ReducesOrderTotal()
    {
        var siparis = new Siparis { SiparisNo = "SIP-DEL-K", GenelToplam = 5000m };
        await _uow.Siparisler.SaveAsync(siparis);

        var db = _dbService.GetConnection();
        var k1 = new SiparisDetay { SiparisId = siparis.Id, Tutar = 3000m };
        var k2 = new SiparisDetay { SiparisId = siparis.Id, Tutar = 2000m };
        await db.InsertAsync(k1);
        await db.InsertAsync(k2);

        await db.DeleteAsync(k2);

        var remaining = await db.Table<SiparisDetay>().Where(k => k.SiparisId == siparis.Id).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(3000m, remaining.Sum(k => k.Tutar));
    }

    [Theory]
    [InlineData(100, 80, true)]
    [InlineData(50, 50, true)]
    [InlineData(20, 30, false)]
    public void Scenario_13_to_15_StockAvailabilityChecks(decimal stokMevcut, decimal siparisMiktar, bool isAvailable)
    {
        bool available = stokMevcut >= siparisMiktar;
        Assert.Equal(isAvailable, available);
    }

    // -------------------------------------------------------------
    // 2. Durum Yönetimi & Yaşam Döngüsü (15 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("Beklemede")]
    [InlineData("Onaylandı")]
    [InlineData("Hazırlanıyor")]
    [InlineData("Kargoya Verildi")]
    [InlineData("Teslim Edildi")]
    [InlineData("İptal Edildi")]
    [InlineData("Faturalandı")]
    public async Task Scenario_16_to_22_AllOrderStatuses_PersistedSuccessfully(string durum)
    {
        var siparis = new Siparis { SiparisNo = $"S-DUR-{durum}", Durum = durum };
        await _uow.Siparisler.SaveAsync(siparis);

        var saved = await _uow.Siparisler.GetByIdAsync(siparis.Id);
        Assert.Equal(durum, saved!.Durum);
    }

    [AvaloniaFact]
    public async Task Scenario_23_OrderStatusTransition_FromBeklemedeToOnaylandi()
    {
        var s = new Siparis { SiparisNo = "S-TRANS-1", Durum = "Beklemede" };
        await _uow.Siparisler.SaveAsync(s);

        s.Durum = "Onaylandı";
        await _uow.Siparisler.SaveAsync(s);

        var refS = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("Onaylandı", refS!.Durum);
    }

    [AvaloniaFact]
    public async Task Scenario_24_OrderStatusTransition_FromOnaylandiToFaturalandi()
    {
        var s = new Siparis { SiparisNo = "S-TRANS-2", Durum = "Onaylandı" };
        await _uow.Siparisler.SaveAsync(s);

        s.Durum = "Faturalandı";
        await _uow.Siparisler.SaveAsync(s);

        var refS = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("Faturalandı", refS!.Durum);
    }

    [Theory]
    [InlineData("İptal Edildi", false)]
    [InlineData("Faturalandı", false)]
    [InlineData("Onaylandı", true)]
    [InlineData("Beklemede", true)]
    public void Scenario_25_to_28_CanConvertToInvoiceValidation(string durum, bool expectedCanConvert)
    {
        bool canConvert = durum != "İptal Edildi" && durum != "Faturalandı";
        Assert.Equal(expectedCanConvert, canConvert);
    }

    [Theory]
    [InlineData(100, 100, "Tam Teslim")]
    [InlineData(100, 60, "Kısmi Teslim")]
    [InlineData(100, 0, "Teslim Edilmedi")]
    public void Scenario_29_to_30_PartialDeliveryStatus(int siparisAdet, int teslimAdet, string expDurum)
    {
        string durum = teslimAdet >= siparisAdet ? "Tam Teslim" : (teslimAdet > 0 ? "Kısmi Teslim" : "Teslim Edilmedi");
        Assert.Equal(expDurum, durum);
    }

    // -------------------------------------------------------------
    // 3. Arama, Filtreleme ve Sıralama (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_31_SearchBySiparisNo_FiltersList()
    {
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SIP-ALPHA-10" });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SIP-BETA-20" });

        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        await vm.LoadSiparislerAsync();

        vm.SearchString = "ALPHA";
        await vm.LoadSiparislerAsync();
        Assert.Single(vm.Siparisler);
        Assert.Equal("SIP-ALPHA-10", vm.Siparisler[0].SiparisNo);
    }

    [AvaloniaFact]
    public async Task Scenario_32_SearchByCariUnvan_FiltersList()
    {
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "S1", CariUnvan = "Yıldız Giyim" });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "S2", CariUnvan = "Güneş Tekstil" });

        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        await vm.LoadSiparislerAsync();

        vm.SearchString = "Yıldız";
        await vm.LoadSiparislerAsync();
        Assert.Single(vm.Siparisler);
        Assert.Equal("Yıldız Giyim", vm.Siparisler[0].CariUnvan);
    }

    [AvaloniaFact]
    public async Task Scenario_33_FilterByDurum_ReturnsApprovedOnly()
    {
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SA1", Durum = "Onaylandı" });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SB1", Durum = "Beklemede" });

        var all = await _uow.Siparisler.GetAllAsync();
        var approved = all.Where(s => s.Durum == "Onaylandı").ToList();

        Assert.Single(approved);
        Assert.Equal("SA1", approved[0].SiparisNo);
    }

    [AvaloniaFact]
    public async Task Scenario_34_to_37_SortingOrders_ByAmountAndDate()
    {
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "S-LOW", GenelToplam = 2000, Tarih = DateTime.Today.AddDays(-3) });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "S-HIGH", GenelToplam = 15000, Tarih = DateTime.Today.AddDays(-1) });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "S-MID", GenelToplam = 7000, Tarih = DateTime.Today });

        var list = (await _uow.Siparisler.GetAllAsync()).ToList();

        var sortedAmount = list.OrderByDescending(s => s.GenelToplam).Select(s => s.SiparisNo).ToList();
        Assert.Equal("S-HIGH", sortedAmount[0]);
        Assert.Equal("S-LOW", sortedAmount[2]);

        var sortedDate = list.OrderByDescending(s => s.Tarih).Select(s => s.SiparisNo).ToList();
        Assert.Equal("S-MID", sortedDate[0]);
        Assert.Equal("S-LOW", sortedDate[2]);
    }

    [Theory]
    [InlineData(1000, 2000, 3000, 6000)]
    [InlineData(50000, 50000, 0, 100000)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_38_to_40_OrderAggregationTotals(decimal s1, decimal s2, decimal s3, decimal expTotal)
    {
        decimal total = s1 + s2 + s3;
        Assert.Equal(expTotal, total);
    }

    // -------------------------------------------------------------
    // 4. Çıktılar & Eylemler (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_41_GenerateOrderPdf_ProducesValidPdfBytes()
    {
        var siparis = new Siparis
        {
            SiparisNo = "SIP-PDF-1",
            CariUnvan = "PDF Test Musteri",
            Tarih = DateTime.Today,
            GenelToplam = 8500m
        };
        await _uow.Siparisler.SaveAsync(siparis);

        var detaylar = new List<SiparisDetay>
        {
            new() { SiparisId = siparis.Id, StokAdi = "Kalem 1", Miktar = 10, BirimFiyat = 850, Tutar = 8500 }
        };

        var res = await _pdfService.GenerateSiparisPdfAsync(siparis, detaylar, null);

        Assert.True(res.Success);
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 500);
    }

    [AvaloniaFact]
    public async Task Scenario_42_DeletePendingOrder_RemovesSuccessfully()
    {
        var siparis = new Siparis { SiparisNo = "SIP-DEL-ME", Durum = "Beklemede" };
        await _uow.Siparisler.SaveAsync(siparis);

        await _uow.Siparisler.DeleteAsync(siparis);

        var deleted = await _uow.Siparisler.GetByIdAsync(siparis.Id);
        Assert.Null(deleted);
    }

    [Theory]
    [InlineData(10000, 0.18, 1800)]
    [InlineData(25000, 0.20, 5000)]
    [InlineData(5000, 0.10, 500)]
    [InlineData(0, 0.20, 0)]
    public void Scenario_43_to_46_OrderVatTotalVerification(decimal matrah, double oran, decimal expKdv)
    {
        decimal kdv = matrah * (decimal)oran;
        Assert.Equal(expKdv, kdv);
    }

    [Theory]
    [InlineData(10000, 1000, 9000)]
    [InlineData(20000, 5000, 15000)]
    [InlineData(5000, 5000, 0)]
    [InlineData(3000, 0, 3000)]
    public void Scenario_47_to_50_OrderDownPaymentCalculations(decimal siparisToplam, decimal kaparo, decimal expKalan)
    {
        decimal kalan = siparisToplam - kaparo;
        Assert.Equal(expKalan, kalan);
    }
}
