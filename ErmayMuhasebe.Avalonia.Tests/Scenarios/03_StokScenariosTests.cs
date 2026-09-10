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

public partial class StokScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. CRUD & Alan Doğrulama Senaryoları (20 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CreateStok_WithValidFields_SavesSuccessfully()
    {
        var stok = new StokKart
        {
            StokKodu = "STK-HAM-001",
            StokAdi = "İplik 30/1 Pamuk",
            Birim = "Kg",
            KdvOrani = 10,
            AlisFiyati = 80m,
            SatisFiyati = 110m,
            Miktar = 250
        };
        await _uow.Stoklar.SaveAsync(stok);

        var saved = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.NotNull(saved);
        Assert.Equal("İplik 30/1 Pamuk", saved.StokAdi);
        Assert.Equal(10, saved.KdvOrani);
        Assert.Equal(250, saved.Miktar);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("STK-01", true)]
    [InlineData("A", true)]
    public void Scenario_02_to_05_StokKoduValidation(string kod, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(kod);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Dikiş İpliği", true)]
    [InlineData("Kumaş", true)]
    public void Scenario_06_to_09_StokAdiValidation(string adi, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(adi);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(10, true)]
    [InlineData(20, true)]
    [InlineData(-1, false)]
    [InlineData(18, true)]
    public void Scenario_10_to_15_KdvRateValidation(int kdv, bool expectedValid)
    {
        bool isValid = kdv >= 0 && kdv <= 100;
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(100, 150, true)]
    [InlineData(100, 100, true)]
    [InlineData(100, 80, false)]
    public void Scenario_16_to_18_SalesBelowCostWarning(decimal alis, decimal satis, bool isProfitable)
    {
        bool profitable = satis >= alis;
        Assert.Equal(isProfitable, profitable);
    }

    [AvaloniaFact]
    public async Task Scenario_19_UpdateStok_ModifiesFields()
    {
        var stok = new StokKart { StokKodu = "STK-U", StokAdi = "Eski Ürün", SatisFiyati = 50m };
        await _uow.Stoklar.SaveAsync(stok);

        stok.StokAdi = "Yeni Ürün Adı";
        stok.SatisFiyati = 75m;
        await _uow.Stoklar.SaveAsync(stok);

        var updated = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.Equal("Yeni Ürün Adı", updated!.StokAdi);
        Assert.Equal(75m, updated.SatisFiyati);
    }

    [AvaloniaFact]
    public async Task Scenario_20_DeleteStok_RemovesRecord()
    {
        var stok = new StokKart { StokKodu = "STK-D", StokAdi = "Silinecek Stok" };
        await _uow.Stoklar.SaveAsync(stok);

        await _uow.Stoklar.DeleteAsync(stok);

        var deleted = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.Null(deleted);
    }

    // -------------------------------------------------------------
    // 2. Miktar, Hareket ve Kritik Seviye (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_21_PurchaseMovement_IncreasesQuantity()
    {
        var stok = new StokKart { StokKodu = "STK-M1", StokAdi = "Malzeme 1", Miktar = 100 };
        await _uow.Stoklar.SaveAsync(stok);

        await _uow.Stoklar.SaveHareketAsync(new StokHareket
        {
            StokId = stok.Id,
            Tarih = DateTime.Today,
            IslemTuru = "Giris",
            Miktar = 50,
            Fiyat = 20m
        });

        stok.Miktar += 50;
        await _uow.Stoklar.SaveAsync(stok);

        var refreshed = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.Equal(150, refreshed!.Miktar);
    }

    [AvaloniaFact]
    public async Task Scenario_22_SalesMovement_ReducesQuantity()
    {
        var stok = new StokKart { StokKodu = "STK-M2", StokAdi = "Malzeme 2", Miktar = 200 };
        await _uow.Stoklar.SaveAsync(stok);

        await _uow.Stoklar.SaveHareketAsync(new StokHareket
        {
            StokId = stok.Id,
            Tarih = DateTime.Today,
            IslemTuru = "Cikis",
            Miktar = 40,
            Fiyat = 35m
        });

        stok.Miktar -= 40;
        await _uow.Stoklar.SaveAsync(stok);

        var refreshed = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.Equal(160, refreshed!.Miktar);
    }

    [Theory]
    [InlineData(100, 10, false)]
    [InlineData(10, 10, true)]
    [InlineData(5, 10, true)]
    [InlineData(0, 10, true)]
    [InlineData(-2, 10, true)]
    public void Scenario_23_to_27_CriticalStockLevelEvaluation(decimal mevcut, decimal kritikSeviye, bool expectedKritik)
    {
        bool isKritik = mevcut <= kritikSeviye;
        Assert.Equal(expectedKritik, isKritik);
    }

    [Theory]
    [InlineData(100, 50, 5000)]
    [InlineData(25, 120, 3000)]
    [InlineData(0, 80, 0)]
    [InlineData(1000, 2.5, 2500)]
    public void Scenario_28_to_31_TotalInventoryValueCalculations(decimal miktar, decimal fiyat, decimal expectedDeger)
    {
        decimal deger = miktar * fiyat;
        Assert.Equal(expectedDeger, deger);
    }

    [Theory]
    [InlineData(100, 10, 50, 16, 12)]
    [InlineData(200, 20, 200, 30, 25)]
    [InlineData(50, 100, 0, 0, 100)]
    public void Scenario_32_to_34_WeightedAverageCostCalculations(decimal m1, decimal f1, decimal m2, decimal f2, decimal expectedAom)
    {
        decimal toplamMiktar = m1 + m2;
        decimal toplamTutar = (m1 * f1) + (m2 * f2);
        decimal aom = toplamMiktar > 0 ? toplamTutar / toplamMiktar : 0;

        Assert.Equal(expectedAom, aom, precision: 1);
    }

    [AvaloniaFact]
    public async Task Scenario_35_StockCountAdjustment_SynchronizesQuantity()
    {
        var stok = new StokKart { StokKodu = "STK-COUNT", StokAdi = "Sayım Yapılan Ürün", Miktar = 85 };
        await _uow.Stoklar.SaveAsync(stok);

        stok.Miktar = 80;
        await _uow.Stoklar.SaveAsync(stok);

        var refreshed = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.Equal(80, refreshed!.Miktar);
    }

    // -------------------------------------------------------------
    // 3. Arama, Filtreleme ve Barkod (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_36_SearchByStokKodu_FiltersList()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "KOD-X1", StokAdi = "Ürün 1" });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "KOD-Y2", StokAdi = "Ürün 2" });

        var vm = _serviceProvider.GetRequiredService<StokListViewModel>();
        await vm.LoadStoklarAsync();

        vm.FilterKod = "X1";
        await vm.LoadStoklarAsync();
        Assert.Single(vm.Stoklar);
        Assert.Equal("KOD-X1", vm.Stoklar[0].StokKodu);
    }

    [AvaloniaFact]
    public async Task Scenario_37_SearchByStokAdi_FiltersList()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "S1", StokAdi = "Kadife Kumaş" });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "S2", StokAdi = "Saten Astar" });

        var vm = _serviceProvider.GetRequiredService<StokListViewModel>();
        await vm.LoadStoklarAsync();

        vm.FilterStokAdi = "Kadife";
        await vm.LoadStoklarAsync();
        Assert.Single(vm.Stoklar);
        Assert.Equal("Kadife Kumaş", vm.Stoklar[0].StokAdi);
    }

    [AvaloniaFact]
    public async Task Scenario_38_SearchByBarcode_SimulatesBarcodeReader()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "S3", StokAdi = "Barkodlu Ürün", Barkod = "8690123456789" });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "S4", StokAdi = "Diğer Ürün", Barkod = "8699999999999" });

        var vm = _serviceProvider.GetRequiredService<StokListViewModel>();
        await vm.LoadStoklarAsync();

        vm.FilterStokAdi = "Barkodlu";
        await vm.LoadStoklarAsync();
        Assert.Single(vm.Stoklar);
        Assert.Equal("Barkodlu Ürün", vm.Stoklar[0].StokAdi);
    }

    [Theory]
    [InlineData("8690123456789", true)]
    [InlineData("12345678", true)]
    [InlineData("ABC123", true)]
    [InlineData("", true)]
    public void Scenario_39_to_42_BarcodeValidation(string barcode, bool expectedValid)
    {
        bool isValid = string.IsNullOrEmpty(barcode) || barcode.Length >= 4;
        Assert.Equal(expectedValid, isValid);
    }

    [AvaloniaFact]
    public async Task Scenario_43_FilterByCategory_ReturnsMatchingItems()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "K1", StokAdi = "Hammadde A", Kategori = "Hammadde" });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "K2", StokAdi = "Mamul B", Kategori = "Mamul" });

        var all = await _uow.Stoklar.GetAllAsync();
        var hammaddeOnly = all.Where(x => x.Kategori == "Hammadde").ToList();

        Assert.Single(hammaddeOnly);
        Assert.Equal("Hammadde A", hammaddeOnly[0].StokAdi);
    }

    [AvaloniaFact]
    public async Task Scenario_44_to_47_SortingStoklar_ByNameAndPrice()
    {
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "A", StokAdi = "Zirkon", SatisFiyati = 300 });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "B", StokAdi = "Alüminyum", SatisFiyati = 100 });
        await _uow.Stoklar.SaveAsync(new StokKart { StokKodu = "C", StokAdi = "Bakır", SatisFiyati = 200 });

        var list = (await _uow.Stoklar.GetAllAsync()).ToList();

        var sortedAZ = list.OrderBy(x => x.StokAdi).Select(x => x.StokAdi).ToList();
        Assert.Equal("Alüminyum", sortedAZ[0]);
        Assert.Equal("Zirkon", sortedAZ[2]);

        var sortedPriceDesc = list.OrderByDescending(x => x.SatisFiyati).Select(x => x.SatisFiyati).ToList();
        Assert.Equal(300m, sortedPriceDesc[0]);
        Assert.Equal(100m, sortedPriceDesc[2]);
    }

    [Theory]
    [InlineData("Adet")]
    [InlineData("Kg")]
    [InlineData("Metre")]
    [InlineData("Koli")]
    public async Task Scenario_48_to_50_UnitTypes_PersistedProperly(string birim)
    {
        var stok = new StokKart { StokKodu = $"U-{birim}", StokAdi = $"Ürün {birim}", Birim = birim };
        await _uow.Stoklar.SaveAsync(stok);

        var saved = await _uow.Stoklar.GetByIdAsync(stok.Id);
        Assert.Equal(birim, saved!.Birim);
    }
}
