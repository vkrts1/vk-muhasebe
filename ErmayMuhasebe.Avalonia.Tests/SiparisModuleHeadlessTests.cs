using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests;

public class SiparisModuleHeadlessTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task Create_New_Siparis_With_Items_Calculates_Totals_And_Saves_To_Database()
    {
        // 1. Arrange: Müşteri ve Stok oluştur
        var cari = new CariKart { Unvan = "Test Sipariş Müşterisi", Grup = "Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        var stok = new StokKart { StokKodu = "SIP-STK-01", StokAdi = "Test Ürünü", SatisFiyati = 200, Birim = "Adet" };
        await _uow.Stoklar.SaveAsync(stok);

        // 2. Act: Sipariş Detay Formunu doldur
        var siparisDetayVm = new ErmayMuhasebe.Avalonia.ViewModels.SiparisDetayViewModel(_uow, cari);
        siparisDetayVm.SiparisNo = "SIP-2026-001";
        siparisDetayVm.Aciklama = "E2E Test Siparişi";

        siparisDetayVm.SelectedStokForAdd = stok;
        siparisDetayVm.AddStokToGrid();

        Assert.Single(siparisDetayVm.Items);
        var satir = siparisDetayVm.Items.First();
        satir.Miktar = 10;
        satir.BirimFiyat = 250;
        satir.KdvOrani = 20;

        // Hesaplamaları doğrula:
        // Tutar = 10 * 250 = 2500 TL
        // KDV = 500 TL
        // Genel Toplam = 3000 TL
        Assert.Equal(2500m, siparisDetayVm.AraToplam);
        Assert.Equal(500m, siparisDetayVm.KdvToplam);
        Assert.Equal(3000m, siparisDetayVm.GenelToplam);

        await siparisDetayVm.SaveSiparisAsync();

        // 3. Assert: Sipariş veri tabanına yazıldı mı?
        var tumSiparisler = await _uow.Siparisler.GetAllAsync();
        var eklenenSiparis = tumSiparisler.FirstOrDefault(s => s.SiparisNo == "SIP-2026-001");

        Assert.NotNull(eklenenSiparis);
        Assert.Equal("Bekliyor", eklenenSiparis.Durum);
        Assert.Equal(3000m, eklenenSiparis.GenelToplam);

        var detaylar = await _uow.Siparisler.GetDetaylarAsync(eklenenSiparis.Id);
        Assert.Single(detaylar);
        Assert.Equal(10, detaylar[0].Miktar);
        Assert.Equal(250m, detaylar[0].BirimFiyat);
    }

    [AvaloniaFact]
    public async Task Filter_Siparisler_By_SearchString_Returns_Matching_Orders()
    {
        // 1. Arrange: 2 sipariş ekle
        var s1 = new Siparis { SiparisNo = "SIP-ALFA-99", CariUnvan = "Alfa Otomotiv", GenelToplam = 1500, Durum = "Bekliyor" };
        var s2 = new Siparis { SiparisNo = "SIP-BETA-88", CariUnvan = "Beta Gıda", GenelToplam = 2500, Durum = "Tamamlandı" };
        await _uow.Siparisler.SaveAsync(s1);
        await _uow.Siparisler.SaveAsync(s2);

        var siparisListVm = _serviceProvider.GetRequiredService<SiparisListViewModel>();

        // 2. Act: "ALFA" araması yap
        siparisListVm.SearchString = "ALFA";
        await siparisListVm.LoadSiparislerAsync();

        // 3. Assert: Sadece Alfa bulunmalı
        Assert.Contains(siparisListVm.Siparisler, s => s.SiparisNo == "SIP-ALFA-99");
        Assert.DoesNotContain(siparisListVm.Siparisler, s => s.SiparisNo == "SIP-BETA-88");
    }
}
