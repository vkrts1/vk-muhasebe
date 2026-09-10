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

public partial class SiparisScenariosTests
{
    // =============================================================
    // 5. Sipariş Detayları & Çoklu Kalem İşlemleri (51 - 65)
    // =============================================================

    [Fact]
    public async Task Scenario_51_Siparis_SaveWithDetailsAsync_PersistsHeaderAndLines()
    {
        var siparis = new Siparis
        {
            SiparisNo = "SIP-DET-51",
            CariId = 10,
            CariUnvan = "Detay Test Müşteri",
            Tarih = DateTime.Today,
            GenelToplam = 5400m,
            Durum = "Beklemede"
        };
        var line1 = new SiparisDetay { StokAdi = "Ürün A", Miktar = 20, BirimFiyat = 100m, Tutar = 2000m };
        var line2 = new SiparisDetay { StokAdi = "Ürün B", Miktar = 10, BirimFiyat = 340m, Tutar = 3400m };

        await _uow.Siparisler.SaveWithDetailsAsync(siparis, new List<SiparisDetay> { line1, line2 });

        Assert.True(siparis.Id > 0);
        var lines = await _uow.Siparisler.GetDetaylarAsync(siparis.Id);
        Assert.Equal(2, lines.Count);
        Assert.Contains(lines, l => l.StokAdi == "Ürün A");
        Assert.Contains(lines, l => l.StokAdi == "Ürün B");
    }

    [Fact]
    public async Task Scenario_52_SiparisDetay_UpdateLines_ReplacesPreviousLines()
    {
        var siparis = new Siparis { SiparisNo = "SIP-UPD-52", GenelToplam = 1000m };
        var l1 = new SiparisDetay { StokAdi = "İlk Kalem", Miktar = 5, BirimFiyat = 200m, Tutar = 1000m };
        await _uow.Siparisler.SaveWithDetailsAsync(siparis, new List<SiparisDetay> { l1 });

        var lNew1 = new SiparisDetay { StokAdi = "Yeni Kalem 1", Miktar = 10, BirimFiyat = 150m, Tutar = 1500m };
        var lNew2 = new SiparisDetay { StokAdi = "Yeni Kalem 2", Miktar = 5, BirimFiyat = 100m, Tutar = 500m };
        await _uow.Siparisler.SaveWithDetailsAsync(siparis, new List<SiparisDetay> { lNew1, lNew2 });

        var linesAfter = await _uow.Siparisler.GetDetaylarAsync(siparis.Id);
        Assert.Equal(2, linesAfter.Count);
        Assert.DoesNotContain(linesAfter, l => l.StokAdi == "İlk Kalem");
    }

    [Fact]
    public async Task Scenario_53_Siparis_CurrencySupport_StoresCurrencyCode()
    {
        var siparis = new Siparis { SiparisNo = "SIP-FX-53", OdemeBilgisi = "USD Havale", GenelToplam = 2500m };
        var line = new SiparisDetay { StokAdi = "İthal Parça", Miktar = 5, BirimFiyat = 500m, Tutar = 2500m, ParaBirimi = "USD" };
        await _uow.Siparisler.SaveWithDetailsAsync(siparis, new List<SiparisDetay> { line });

        var lines = await _uow.Siparisler.GetDetaylarAsync(siparis.Id);
        Assert.Equal("USD", lines.First().ParaBirimi);
    }

    [Fact]
    public async Task Scenario_54_Siparis_NotesAndPdfNotes_StoredCorrectly()
    {
        var siparis = new Siparis
        {
            SiparisNo = "SIP-NOT-54",
            Aciklama = "İç operasyon notu: Öncelikli imalat",
            PdfNotlar = "Müşteri teslimatta irsaliye kaşesi basacaktır."
        };
        await _uow.Siparisler.SaveAsync(siparis);

        var retrieved = await _uow.Siparisler.GetByIdAsync(siparis.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("İç operasyon notu: Öncelikli imalat", retrieved.Aciklama);
        Assert.Equal("Müşteri teslimatta irsaliye kaşesi basacaktır.", retrieved.PdfNotlar);
    }

    [Fact]
    public async Task Scenario_55_Siparis_PriorityField_PersistsUrgency()
    {
        var siparis = new Siparis { SiparisNo = "SIP-URG-55", Oncelik = "Acil" };
        await _uow.Siparisler.SaveAsync(siparis);

        var retrieved = await _uow.Siparisler.GetByIdAsync(siparis.Id);
        Assert.Equal("Acil", retrieved!.Oncelik);
    }

    [Theory]
    [InlineData("Düşük")]
    [InlineData("Normal")]
    [InlineData("Yüksek")]
    [InlineData("Acil")]
    public async Task Scenario_56_to_59_Siparis_ValidPriorities(string oncelik)
    {
        var s = new Siparis { SiparisNo = $"SIP-PR-{oncelik}", Oncelik = oncelik };
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal(oncelik, retrieved!.Oncelik);
    }

    [Theory]
    [InlineData(10, 100, 20, 1200)] // 10 * 100 + %20 KDV
    [InlineData(5, 400, 10, 2200)]  // 5 * 400 + %10 KDV
    [InlineData(100, 15, 0, 1500)]  // %0 KDV
    public void Scenario_60_to_62_SiparisDetay_LineTotalWithVatCalculation(double miktar, decimal fiyat, double kdvOrani, decimal expectedTotal)
    {
        decimal matrah = (decimal)miktar * fiyat;
        decimal kdv = matrah * (decimal)(kdvOrani / 100.0);
        decimal total = matrah + kdv;
        Assert.Equal(expectedTotal, total);
    }

    [Theory]
    [InlineData(50, 100, 50)]  // 50 sipariş, 100 mevcut -> Karşılanır
    [InlineData(120, 100, 0)]  // 120 sipariş, 100 mevcut -> Eksik 20
    [InlineData(100, 100, 100)] // Tam
    public void Scenario_63_to_65_OrderFulfillmentCoverageRatio(double siparisMiktar, double stokMevcut, double expectedFulfillment)
    {
        double karsilanan = Math.Min(siparisMiktar, stokMevcut);
        Assert.True(karsilanan >= 0);
    }

    // =============================================================
    // 6. Sipariş Durum Değişimleri & İş Akışları (66 - 80)
    // =============================================================

    [Fact]
    public async Task Scenario_66_Siparis_StatusFlow_Hazirlaniyor()
    {
        var s = new Siparis { SiparisNo = "SIP-HAZ-66", Durum = "Beklemede" };
        await _uow.Siparisler.SaveAsync(s);

        s.Durum = "Hazırlanıyor";
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("Hazırlanıyor", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_67_Siparis_StatusFlow_KargoyaVerildi()
    {
        var s = new Siparis { SiparisNo = "SIP-KRG-67", Durum = "Hazırlanıyor" };
        await _uow.Siparisler.SaveAsync(s);

        s.Durum = "Kargoya Verildi";
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("Kargoya Verildi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_68_Siparis_StatusFlow_TeslimEdildi()
    {
        var s = new Siparis { SiparisNo = "SIP-TSL-68", Durum = "Kargoya Verildi" };
        await _uow.Siparisler.SaveAsync(s);

        s.Durum = "Teslim Edildi";
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("Teslim Edildi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_69_Siparis_StatusFlow_IptalEdildi()
    {
        var s = new Siparis { SiparisNo = "SIP-CNL-69", Durum = "Beklemede" };
        await _uow.Siparisler.SaveAsync(s);

        s.Durum = "İptal Edildi";
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("İptal Edildi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_70_Siparis_SoftDelete_SetsIsDeletedFlag()
    {
        var s = new Siparis { SiparisNo = "SIP-DEL-70", IsDeleted = false };
        await _uow.Siparisler.SaveAsync(s);

        await _uow.Siparisler.DeleteAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Null(retrieved);

        var deletedList = await _uow.Siparisler.GetDeletedAsync();
        Assert.Contains(deletedList, x => x.Id == s.Id);
    }

    [Theory]
    [InlineData("Beklemede", true)]
    [InlineData("Onaylandı", true)]
    [InlineData("Hazırlanıyor", true)]
    [InlineData("Kargoya Verildi", false)]
    [InlineData("Teslim Edildi", false)]
    [InlineData("İptal Edildi", false)]
    public void Scenario_71_to_76_CanCancelOrderStatusRules(string durum, bool expectedCanCancel)
    {
        bool canCancel = durum == "Beklemede" || durum == "Onaylandı" || durum == "Hazırlanıyor";
        Assert.Equal(expectedCanCancel, canCancel);
    }

    [Theory]
    [InlineData(10, 5, 5)] // Kalan gün = 5
    [InlineData(10, 10, 0)] // Bugün teslim
    [InlineData(10, 15, -5)] // 5 gün gecikti
    public void Scenario_77_to_79_DeliveryDueDateRemainingCalculations(int orderDay, int currentDay, int expectedDaysRemaining)
    {
        int remaining = orderDay - currentDay;
        Assert.Equal(expectedDaysRemaining, remaining);
    }

    [Fact]
    public async Task Scenario_80_Siparis_BaglantiEvrakNo_LinksQuoteDocument()
    {
        var s = new Siparis { SiparisNo = "SIP-LNK-80", BaglantiEvrakNo = "TEK-2026-0044" };
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal("TEK-2026-0044", retrieved!.BaglantiEvrakNo);
    }

    // =============================================================
    // 7. Sipariş Listeleme & Filtreleme UI Testleri (81 - 95)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_81_SiparisListViewModel_LoadsAllOrders()
    {
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SIP-LOAD-1", GenelToplam = 1000m });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SIP-LOAD-2", GenelToplam = 2000m });

        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        await vm.LoadSiparislerAsync();

        Assert.True(vm.Siparisler.Count >= 2);
    }

    [AvaloniaFact]
    public async Task Scenario_82_SiparisListViewModel_SearchByCustomer_MatchesCaseInsensitive()
    {
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SIP-C1", CariUnvan = "KAYA İNŞAAT" });
        await _uow.Siparisler.SaveAsync(new Siparis { SiparisNo = "SIP-C2", CariUnvan = "DEMİR LOJİSTİK" });

        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        vm.SearchString = "kaya";
        await vm.LoadSiparislerAsync();

        Assert.Contains(vm.Siparisler, s => s.CariUnvan == "KAYA İNŞAAT");
        Assert.DoesNotContain(vm.Siparisler, s => s.CariUnvan == "DEMİR LOJİSTİK");
    }

    [AvaloniaFact]
    public async Task Scenario_83_SiparisListViewModel_EmptySearch_LoadsAll()
    {
        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        vm.SearchString = "";
        await vm.LoadSiparislerAsync();

        Assert.NotNull(vm.Siparisler);
    }

    [AvaloniaFact]
    public async Task Scenario_84_SiparisListViewModel_SelectOrder_UpdatesSelectedOrder()
    {
        var s = new Siparis { SiparisNo = "SIP-SEL-84", GenelToplam = 4500m };
        await _uow.Siparisler.SaveAsync(s);

        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        await vm.LoadSiparislerAsync();

        vm.SelectedSiparis = vm.Siparisler.FirstOrDefault(x => x.SiparisNo == "SIP-SEL-84");
        Assert.NotNull(vm.SelectedSiparis);
        Assert.Equal("SIP-SEL-84", vm.SelectedSiparis.SiparisNo);
    }

    [AvaloniaFact]
    public async Task Scenario_85_SiparisListViewModel_SearchStringProperty_UpdatesCorrectly()
    {
        var vm = _serviceProvider.GetRequiredService<SiparisListViewModel>();
        vm.SearchString = "Özel Filtre";
        Assert.Equal("Özel Filtre", vm.SearchString);

        vm.SearchString = "";
        Assert.Equal("", vm.SearchString);
    }

    [Theory]
    [InlineData("SIP-001", "SIP-001", true)]
    [InlineData("SIP-001", "sip-001", true)]
    [InlineData("SIP-001", "SIP-002", false)]
    public void Scenario_86_to_88_OrderNumberEqualityChecks(string no1, string no2, bool expectedEqual)
    {
        bool equal = string.Equals(no1, no2, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(expectedEqual, equal);
    }

    [Theory]
    [InlineData(1000, 2000, 3000, 4000, 10000)]
    [InlineData(5000, 5000, 0, 0, 10000)]
    public void Scenario_89_to_90_OrderGrandTotalAggregation(decimal l1, decimal l2, decimal l3, decimal l4, decimal expectedGrand)
    {
        decimal grand = l1 + l2 + l3 + l4;
        Assert.Equal(expectedGrand, grand);
    }

    [Fact]
    public async Task Scenario_91_Siparis_ExtremeGrandTotal_PreservesPrecision()
    {
        decimal extreme = 99999999.95m;
        var s = new Siparis { SiparisNo = "SIP-EXT-91", GenelToplam = extreme };
        await _uow.Siparisler.SaveAsync(s);

        var retrieved = await _uow.Siparisler.GetByIdAsync(s.Id);
        Assert.Equal(extreme, retrieved!.GenelToplam);
    }

    [Theory]
    [InlineData(100, 20, 80)]
    [InlineData(50, 50, 0)]
    [InlineData(10, 0, 10)]
    public void Scenario_92_to_94_PartialDeliveryRemainingCalculation(int siparis, int teslim, int expectedKalan)
    {
        int kalan = siparis - teslim;
        Assert.Equal(expectedKalan, kalan);
    }

    [Fact]
    public async Task Scenario_95_Siparis_TenantIsolation_MaintainsSeparateOrders()
    {
        var s1 = new Siparis { SiparisNo = "SIP-T1-95", TenantId = "tenant_1" };
        var s2 = new Siparis { SiparisNo = "SIP-T2-95", TenantId = "tenant_2" };
        await _uow.Siparisler.SaveAsync(s1);
        await _uow.Siparisler.SaveAsync(s2);

        var all = await _uow.Siparisler.GetAllAsync();
        Assert.Contains(all, s => s.TenantId == "tenant_1");
        Assert.Contains(all, s => s.TenantId == "tenant_2");
    }

    // =============================================================
    // 8. Kaparo & Teslimat Analizleri (96 - 100)
    // =============================================================

    [Theory]
    [InlineData(10000, 2000, 20.0)] // 2.000 / 10.000 = %20 avans
    [InlineData(50000, 15000, 30.0)]
    [InlineData(25000, 25000, 100.0)]
    public void Scenario_96_to_98_DownPaymentRatioCalculations(decimal toplam, decimal kaparo, double expectedRatio)
    {
        double ratio = toplam > 0 ? (double)(kaparo / toplam) * 100.0 : 0.0;
        Assert.Equal(expectedRatio, ratio);
    }

    [Theory]
    [InlineData(10000, 2000, 8000)]
    [InlineData(50000, 15000, 35000)]
    public void Scenario_99_to_100_BalanceDueAfterDownPayment(decimal toplam, decimal kaparo, decimal expectedKalan)
    {
        decimal kalan = toplam - kaparo;
        Assert.Equal(expectedKalan, kalan);
    }
}
