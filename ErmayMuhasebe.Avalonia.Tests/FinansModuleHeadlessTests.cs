using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests;

public class FinansModuleHeadlessTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task Kasa_Create_New_Via_Dialog_Saves_To_Database_And_List()
    {
        // 1. Arrange
        var kasaListVm = _serviceProvider.GetRequiredService<KasaListViewModel>();
        await kasaListVm.LoadKasalarAsync();
        int initialCount = kasaListVm.Kasalar.Count;

        // 2. Act: Son kullanıcı yeni kasa ekleme formunu açar
        kasaListVm.OpenAddKasaDialog();
        Assert.True(kasaListVm.IsEditDialogVisible);

        kasaListVm.EditKasaAdi = "Atölye Nakit Kasası";
        kasaListVm.EditYetkili = "Ahmet Usta";
        kasaListVm.EditDovizTuru = "TL";
        kasaListVm.EditAcilisBakiyesi = 7500.00m;

        await kasaListVm.SaveKasaCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.False(kasaListVm.IsEditDialogVisible);
        Assert.True(string.IsNullOrEmpty(kasaListVm.ErrorMessage));

        var kasalar = await _uow.Bankalar.GetAllAsync();
        var eklenen = kasalar.FirstOrDefault(k => k.BankaAdi == "Atölye Nakit Kasası");

        Assert.NotNull(eklenen);
        Assert.Equal("Kasa", eklenen.KartTuru);
        Assert.Equal(7500.00m, eklenen.AcilisBakiyesi);
        Assert.Equal(initialCount + 1, kasaListVm.Kasalar.Count);
    }

    [AvaloniaFact]
    public async Task KasaDetay_Cash_In_And_Out_Updates_Kasa_Balance_Correctly()
    {
        // 1. Arrange: 10000 TL bakiyeli kasa oluştur
        var kasa = new BankaKart
        {
            BankaAdi = "Fabrika Ana Kasa",
            KartTuru = "Kasa",
            DovizTuru = "TL",
            AcilisBakiyesi = 10000,
            GuncelBakiye = 10000
        };
        await _uow.Bankalar.SaveAsync(kasa);

        var kasaDetayVm = _serviceProvider.GetRequiredService<KasaDetayViewModel>();
        await kasaDetayVm.InitializeAsync(kasa);

        Assert.Equal(10000, kasaDetayVm.GuncelBakiye);

        // 2. Act: 3000 TL Kasa Girişi yap (Masraf iadesi vb.)
        kasaDetayVm.OpenNewGirisDialog();
        Assert.True(kasaDetayVm.IsTransactionDialogVisible);
        kasaDetayVm.TransactionAmount = 3000;
        kasaDetayVm.TransactionDescription = "Nakit sermaye ilavesi";

        await kasaDetayVm.SaveNewTransactionCommand.ExecuteAsync(null);
        Assert.True(string.IsNullOrEmpty(kasaDetayVm.ErrorMessage), $"Kasa işlemi başarısız: {kasaDetayVm.ErrorMessage}");
        Assert.False(kasaDetayVm.IsTransactionDialogVisible);

        // 3. Act: 1500 TL Kasa Çıkışı yap (Kırtasiye/Yemek)
        kasaDetayVm.OpenNewCikisDialog();
        Assert.True(kasaDetayVm.IsTransactionDialogVisible);
        kasaDetayVm.TransactionAmount = 1500;
        kasaDetayVm.TransactionDescription = "Ofis sarf malzeme alımı";

        await kasaDetayVm.SaveNewTransactionCommand.ExecuteAsync(null);
        Assert.False(kasaDetayVm.IsTransactionDialogVisible);

        // 4. Assert: 10000 + 3000 - 1500 = 11500 TL olmalı
        var guncelKasa = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.NotNull(guncelKasa);
        Assert.Equal(11500, guncelKasa.GuncelBakiye);

        var hareketler = await _uow.Kasalar.GetHareketlerAsync(kasa.Id);
        Assert.Equal(2, hareketler.Count);
        Assert.Contains(hareketler, h => h.Giren == 3000 && h.IslemTuru == "Kasa Girişi");
        Assert.Contains(hareketler, h => h.Cikan == 1500 && h.IslemTuru == "Kasa Çıkışı");
    }
}
