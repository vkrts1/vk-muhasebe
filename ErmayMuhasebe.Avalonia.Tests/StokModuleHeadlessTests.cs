using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests;

public class StokModuleHeadlessTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task Create_New_Stok_Kart_Saves_To_Database_And_Initializes_Stock_Quantity()
    {
        // 1. Arrange
        var stokVm = _serviceProvider.GetRequiredService<StokListViewModel>();
        await stokVm.LoadStoklarAsync();

        // 2. Act: Son kullanıcı gibi yeni stok formu alanlarını doldurur
        stokVm.SelectedStok = null; // Yeni kart modu
        stokVm.EditStokKodu = "STK-TEST-01";
        stokVm.EditStokAdi = "10mm Çelik Somun";
        stokVm.EditKategori = "Hırdavat";
        stokVm.EditBirim = "Adet";
        stokVm.EditAlisFiyati = 15.50m;
        stokVm.EditSatisFiyati = 35.00m;
        stokVm.EditKDV = 20;
        stokVm.EditAcilisBakiye = 250; // 250 adet açılış stoku

        await stokVm.SaveStokCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.Null(stokVm.ErrorMessage);

        var tumStoklar = await _uow.Stoklar.GetAllAsync();
        var eklenen = tumStoklar.FirstOrDefault(s => s.StokKodu == "STK-TEST-01");

        Assert.NotNull(eklenen);
        Assert.Equal("10mm Çelik Somun", eklenen.StokAdi);
        Assert.Equal(35.00m, eklenen.SatisFiyati);
        Assert.Equal(250, eklenen.Miktar);
    }

    [AvaloniaFact]
    public async Task Filter_By_StokAdi_Returns_Matching_Records()
    {
        // 1. Arrange
        var s1 = new StokKart { StokKodu = "S-01", StokAdi = "Bakır Kablo 3x2.5", Kategori = "Elektrik" };
        var s2 = new StokKart { StokKodu = "S-02", StokAdi = "PVC Boru 50mm", Kategori = "Sıhhi Tesisat" };
        await _uow.Stoklar.SaveAsync(s1);
        await _uow.Stoklar.SaveAsync(s2);

        var stokVm = _serviceProvider.GetRequiredService<StokListViewModel>();

        // 2. Act
        stokVm.FilterStokAdi = "Bakır";
        await stokVm.LoadStoklarAsync();

        // 3. Assert
        Assert.Contains(stokVm.Stoklar, s => s.StokAdi == "Bakır Kablo 3x2.5");
        Assert.DoesNotContain(stokVm.Stoklar, s => s.StokAdi == "PVC Boru 50mm");
    }
}
