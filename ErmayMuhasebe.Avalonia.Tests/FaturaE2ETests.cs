using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests;

public class FaturaE2ETests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task EndToEnd_User_Scenario_Login_AddCustomer_AddProduct_CreateInvoice_And_Generate_Pdf()
    {
        // =========================================================================
        // ADIM 1: KULLANICI GİRİŞ YAPAR (AUTH)
        // =========================================================================
        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        Assert.False(mainVm.IsAuthenticated);

        mainVm.LoginViewModel!.Username = "admin";
        mainVm.LoginViewModel.Password = "123";
        await mainVm.LoginViewModel.LoginCommand.ExecuteAsync(null);

        Assert.True(mainVm.IsAuthenticated, "Kullanıcı başarıyla giriş yapmalı.");
        Assert.Same(mainVm, mainVm.ActiveAuthView);

        // =========================================================================
        // ADIM 2: MÜŞTERİ (CARİ) OLUŞTURUR
        // =========================================================================
        var cariVm = _serviceProvider.GetRequiredService<CariListViewModel>();
        cariVm.OpenAddCariDialog();

        cariVm.EditUnvan = "Ermay İthalat İhracat A.Ş.";
        cariVm.EditGrup = "Müşteri";
        cariVm.EditVergiNo = "9988776655";
        cariVm.EditTelefon = "05321112233";
        cariVm.EditIl = "İstanbul";

        await cariVm.SaveCariCommand.ExecuteAsync(null);

        var savedCari = (await _uow.Cariler.GetAllAsync()).FirstOrDefault(c => c.Unvan == "Ermay İthalat İhracat A.Ş.");
        Assert.NotNull(savedCari);
        Assert.Equal(0, savedCari.Bakiye);

        // =========================================================================
        // ADIM 3: STOK KARTI EKLER (50 ADET AÇILIŞ STOKU İLE)
        // =========================================================================
        var stokVm = _serviceProvider.GetRequiredService<StokListViewModel>();
        stokVm.SelectedStok = null;
        stokVm.EditStokKodu = "STK-E2E-CNC";
        stokVm.EditStokAdi = "CNC Kesici Uç Karbür";
        stokVm.EditBirim = "Adet";
        stokVm.EditAlisFiyati = 400.00m;
        stokVm.EditSatisFiyati = 1000.00m;
        stokVm.EditKDV = 20;
        stokVm.EditAcilisBakiye = 50; // 50 adet mevcut

        await stokVm.SaveStokCommand.ExecuteAsync(null);

        var savedStok = (await _uow.Stoklar.GetAllAsync()).FirstOrDefault(s => s.StokKodu == "STK-E2E-CNC");
        Assert.NotNull(savedStok);
        Assert.Equal(50, savedStok.Miktar);

        // =========================================================================
        // ADIM 4: SATIŞ FATURASI DÜZENLER VE KAYDEDER
        // =========================================================================
        var faturaDetayVm = new FaturaDetayViewModel(_uow, _pdfService, savedCari, "Satış");
        await faturaDetayVm.InitializeAsync(null, savedCari.Id, "Satış");
        faturaDetayVm.Cari = savedCari;
        faturaDetayVm.FaturaNo = "FAT-2026-E2E";
        faturaDetayVm.OdemeSekli = "Açık Hesap";

        // Stoğu faturaya ekle
        faturaDetayVm.SelectedStokForAdd = savedStok;
        faturaDetayVm.AddStokToGrid();

        Assert.Single(faturaDetayVm.Items);
        var faturaSatiri = faturaDetayVm.Items.First();

        // Son kullanıcı gibi miktar ve fiyat belirler
        faturaSatiri.Miktar = 5; // 5 adet satılıyor
        faturaSatiri.BirimFiyat = 1000.00m;
        faturaSatiri.KdvOrani = 20;

        // Otomatik hesaplanan tutarları doğrula:
        // Ara Toplam: 5 * 1000 = 5000 TL
        // KDV (%20): 1000 TL
        // Genel Toplam: 6000 TL
        Assert.Equal(5000.00m, faturaDetayVm.AraToplam);
        Assert.Equal(1000.00m, faturaDetayVm.KdvToplam);
        Assert.Equal(6000.00m, faturaDetayVm.GenelToplam);

        // Faturayı kaydet
        await faturaDetayVm.SaveFaturaCommand.ExecuteAsync(null);
        Assert.Null(faturaDetayVm.ErrorMessage);

        // =========================================================================
        // ADIM 5: MUHASEBE ÇAPRAZ ENTEGRASYON KONTROLLERİ
        // =========================================================================
        // 1. Fatura veri tabanına yazılmış mı?
        var faturalar = await _uow.Faturalar.GetAllAsync();
        var kayitliFatura = faturalar.FirstOrDefault(f => f.FaturaNo == "FAT-2026-E2E");
        Assert.NotNull(kayitliFatura);
        Assert.Equal(6000.00m, kayitliFatura.GenelToplam);

        // 2. Müşteri (Cari) bakiyesi fatura tutarı kadar (6000 TL) borçlanmış mı?
        var guncelCari = await _uow.Cariler.GetByIdAsync(savedCari.Id);
        Assert.NotNull(guncelCari);
        Assert.Equal(6000.00m, guncelCari.Borc);
        Assert.Equal(6000.00m, guncelCari.Bakiye);

        // 3. Stok miktarı 50'den 45'e (5 adet çıkış) düşmüş mü?
        var guncelStok = await _uow.Stoklar.GetByIdAsync(savedStok.Id);
        Assert.NotNull(guncelStok);
        Assert.Equal(45, guncelStok.Miktar);

        // =========================================================================
        // ADIM 6: FATURA PDF ÇIKTISI ALINMASI (QUESTPDF MOTORU)
        // =========================================================================
        var detaylar = await _uow.Faturalar.GetDetaylarAsync(kayitliFatura.Id);
        var pdfResult = await _pdfService.GenerateFaturaPdfAsync(kayitliFatura, detaylar);

        Assert.True(pdfResult.Success, $"PDF üretimi başarılı olmalı. Hata: {pdfResult.Error}");
        Assert.NotNull(_testFileService.LastSavedBytes);
        Assert.True(_testFileService.LastSavedBytes.Length > 500, "Geçerli bir PDF bayt dizisi oluşturulmuş olmalı.");
        Assert.Contains("FAT-2026-E2E", _testFileService.LastSavedFileName ?? "");
    }
}
