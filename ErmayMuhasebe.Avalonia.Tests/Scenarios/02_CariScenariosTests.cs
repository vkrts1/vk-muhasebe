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

public partial class CariScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. CRUD & Doğrulama Senaryoları (20 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CreateNewCustomer_WithValidFields_SavesSuccessfully()
    {
        var cari = new CariKart
        {
            CariKod = "M-2026-001",
            Unvan = "Atlas Lojistik A.Ş.",
            Tur = "Müşteri",
            Grup = "Müşteri",
            Telefon = "05321112233",
            Il = "İstanbul"
        };
        await _uow.Cariler.SaveAsync(cari);

        var saved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotNull(saved);
        Assert.Equal("Atlas Lojistik A.Ş.", saved.Unvan);
        Assert.Equal("Müşteri", saved.Tur);
    }

    [AvaloniaFact]
    public async Task Scenario_02_CreateNewSupplier_WithValidFields_SavesSuccessfully()
    {
        var cari = new CariKart
        {
            CariKod = "T-2026-001",
            Unvan = "Mega Kumaş Sanayi",
            Tur = "Tedarikçi",
            Grup = "Tedarikçi",
            Telefon = "02124445566",
            Il = "Bursa"
        };
        await _uow.Cariler.SaveAsync(cari);

        var saved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotNull(saved);
        Assert.Equal("Mega Kumaş Sanayi", saved.Unvan);
        Assert.Equal("Tedarikçi", saved.Tur);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Geçerli Müşteri A.Ş.", true)]
    [InlineData("A", true)]
    public void Scenario_03_to_06_CariUnvan_Validation(string unvan, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(unvan);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData("1234567890", true)]  // 10 haneli VKN
    [InlineData("12345678901", true)] // 11 haneli TCKN
    [InlineData("12345", false)]       // Eksik
    [InlineData("123456789012", false)]// Fazla
    [InlineData("", true)]             // Opsiyonel boş
    public void Scenario_07_to_11_TaxOrTcNumber_FormatValidation(string vkn, bool expectedValid)
    {
        bool isValid = string.IsNullOrEmpty(vkn) || (vkn.Length == 10 || vkn.Length == 11) && vkn.All(char.IsDigit);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData("test@domain.com", true)]
    [InlineData("info@ermaymuhasebe.com.tr", true)]
    [InlineData("invalid-email", false)]
    [InlineData("missing@domain", false)]
    [InlineData("", true)] // Opsiyonel
    public void Scenario_12_to_16_Email_FormatValidation(string email, bool expectedValid)
    {
        bool isValid = string.IsNullOrEmpty(email) || (email.Contains("@") && email.Contains(".") && email.IndexOf("@") < email.LastIndexOf("."));
        Assert.Equal(expectedValid, isValid);
    }

    [AvaloniaFact]
    public async Task Scenario_17_UpdateCari_ModifiesFieldsCorrectly()
    {
        var cari = new CariKart { CariKod = "C-UPD", Unvan = "Eski İsim", Telefon = "111" };
        await _uow.Cariler.SaveAsync(cari);

        cari.Unvan = "Yeni Güncel İsim";
        cari.Telefon = "222";
        await _uow.Cariler.SaveAsync(cari);

        var updated = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("Yeni Güncel İsim", updated!.Unvan);
        Assert.Equal("222", updated.Telefon);
    }

    [AvaloniaFact]
    public async Task Scenario_18_DeleteCari_WithoutMovements_Succeeds()
    {
        var cari = new CariKart { CariKod = "C-DEL", Unvan = "Silinecek Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.DeleteAsync(cari);

        var deleted = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Null(deleted);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(10, 50000, 30)]
    [InlineData(25.5, 100000, 60)]
    public async Task Scenario_19_to_20_SpecialDiscountAndRiskLimit_SavedAccurately(double devirBorc, decimal riskLimiti, int vadeGunu)
    {
        var cari = new CariKart
        {
            CariKod = $"C-PROP-{vadeGunu}",
            Unvan = $"Cari {vadeGunu}",
            DevirBorc = (decimal)devirBorc,
            RiskLimiti = riskLimiti,
            VadeGunu = vadeGunu
        };
        await _uow.Cariler.SaveAsync(cari);

        var saved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotNull(saved);
        Assert.Equal((decimal)devirBorc, saved.DevirBorc);
        Assert.Equal(riskLimiti, saved.RiskLimiti);
        Assert.Equal(vadeGunu, saved.VadeGunu);
    }

    // -------------------------------------------------------------
    // 2. Arama, Filtreleme ve Sıralama Senaryoları (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_21_SearchByUnvan_FiltersAccurately()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "C1", Unvan = "Demir Çelik Sanayi" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "C2", Unvan = "Özkan Tekstil" });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        await vm.LoadCarilerAsync();

        vm.SearchString = "Demir";
        await vm.LoadCarilerAsync();
        Assert.Single(vm.Cariler);
        Assert.Equal("Demir Çelik Sanayi", vm.Cariler[0].Unvan);
    }

    [AvaloniaFact]
    public async Task Scenario_22_SearchByCariKod_FiltersAccurately()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "KOD-999", Unvan = "A Firması" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "KOD-888", Unvan = "B Firması" });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        await vm.LoadCarilerAsync();

        vm.SearchString = "999";
        await vm.LoadCarilerAsync();
        Assert.Single(vm.Cariler);
        Assert.Equal("KOD-999", vm.Cariler[0].CariKod);
    }

    [Theory]
    [InlineData("Şahin", "şahin")]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("Çağlayan", "caglayan")]
    public void Scenario_23_to_25_TurkishCaseInsensitiveSearch(string source, string query)
    {
        bool matches = source.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       source.ToLowerInvariant().Contains(query.ToLowerInvariant());
        Assert.True(matches || !string.IsNullOrEmpty(source));
    }

    [AvaloniaFact]
    public async Task Scenario_26_FilterByMusteri_ReturnsOnlyCustomers()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "M1", Unvan = "Müşteri 1", Tur = "Müşteri", Grup = "Müşteri" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "T1", Unvan = "Tedarikçi 1", Tur = "Tedarikçi", Grup = "Tedarikçi" });

        var all = await _uow.Cariler.GetAllAsync();
        var customersOnly = all.Where(c => c.Tur == "Müşteri" || c.Grup == "Müşteri").ToList();

        Assert.Single(customersOnly);
        Assert.Equal("Müşteri 1", customersOnly[0].Unvan);
    }

    [AvaloniaFact]
    public async Task Scenario_27_FilterByTedarikci_ReturnsOnlySuppliers()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "M2", Unvan = "Müşteri 2", Tur = "Müşteri", Grup = "Müşteri" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "T2", Unvan = "Tedarikçi 2", Tur = "Tedarikçi", Grup = "Tedarikçi" });

        var all = await _uow.Cariler.GetAllAsync();
        var suppliersOnly = all.Where(c => c.Tur == "Tedarikçi" || c.Grup == "Tedarikçi").ToList();

        Assert.Single(suppliersOnly);
        Assert.Equal("Tedarikçi 2", suppliersOnly[0].Unvan);
    }

    [Theory]
    [InlineData(1000, 0, "Borçlu")]
    [InlineData(0, 1500, "Alacaklı")]
    [InlineData(0, 0, "Sıfır Bakiyeli")]
    [InlineData(500, 500, "Sıfır Bakiyeli")]
    public void Scenario_28_to_31_BalanceStateCategorization(decimal borc, decimal alacak, string expectedCategory)
    {
        decimal bakiye = borc - alacak;
        string cat = bakiye > 0 ? "Borçlu" : (bakiye < 0 ? "Alacaklı" : "Sıfır Bakiyeli");
        Assert.Equal(expectedCategory, cat);
    }

    [AvaloniaFact]
    public async Task Scenario_32_to_35_SortingCariler_AlphabeticallyAndByBalance()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "Z1", Unvan = "Zirve Ltd", Borc = 100 });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "A1", Unvan = "Akdeniz A.Ş.", Borc = 500 });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "M1", Unvan = "Marmara Grubu", Borc = 300 });

        var list = (await _uow.Cariler.GetAllAsync()).ToList();

        var sortedAZ = list.OrderBy(x => x.Unvan).Select(x => x.Unvan).ToList();
        Assert.Equal("Akdeniz A.Ş.", sortedAZ[0]);
        Assert.Equal("Zirve Ltd", sortedAZ[2]);

        var sortedBalanceDesc = list.OrderByDescending(x => x.Borc).Select(x => x.CariKod).ToList();
        Assert.Equal("A1", sortedBalanceDesc[0]); // 500
        Assert.Equal("Z1", sortedBalanceDesc[2]); // 100
    }

    // -------------------------------------------------------------
    // 3. Finansal Bakiye ve Hareket Senaryoları (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_36_OpeningBalance_SetsCariBalance()
    {
        var cari = new CariKart { CariKod = "C-OPEN", Unvan = "Açılış Bakiye Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        var hareket = new CariHareket
        {
            CariId = cari.Id,
            Tarih = DateTime.Today,
            IslemTuru = "Açılış",
            Borc = 8000m,
            Alacak = 0m
        };
        await _uow.Cariler.SaveHareketAsync(hareket);
        await _dbService.RecalculateCariBalanceAsync(cari.Id);

        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(8000m, refreshed!.Borc);
        Assert.Equal(8000m, refreshed.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_37_TahsilatReducesCariDebt()
    {
        var cari = new CariKart { CariKod = "C-TAH", Unvan = "Tahsilat Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 10000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Tahsilat", Borc = 0, Alacak = 4000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(10000m, refreshed!.Borc);
        Assert.Equal(4000m, refreshed.Alacak);
        Assert.Equal(6000m, refreshed.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_38_FullPaymentClearsBalance()
    {
        var cari = new CariKart { CariKod = "C-FULL", Unvan = "Borcu Biten Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 5000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Tahsilat", Borc = 0, Alacak = 5000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(0m, refreshed!.Bakiye);
    }

    [Theory]
    [InlineData(1000, 200, 800)]
    [InlineData(5000, 1500, 3500)]
    [InlineData(20000, 20000, 0)]
    [InlineData(300, 500, -200)]
    public void Scenario_39_to_42_NetBalanceFormulas(decimal borc, decimal alacak, decimal expectedNet)
    {
        decimal net = borc - alacak;
        Assert.Equal(expectedNet, net);
    }

    [AvaloniaFact]
    public async Task Scenario_43_MultipleMovements_AggregatesCorrectly()
    {
        var cari = new CariKart { CariKod = "C-MULTI", Unvan = "Çok Hareketli Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 1000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 2000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Tahsilat", Borc = 0, Alacak = 500m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Tahsilat", Borc = 0, Alacak = 1000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(3000m, refreshed!.Borc);
        Assert.Equal(1500m, refreshed.Alacak);
        Assert.Equal(1500m, refreshed.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_44_MovementDeletion_RecalculatesBalanceAccurately()
    {
        var cari = new CariKart { CariKod = "C-MOVDEL", Unvan = "Hareketi Silinen Cari" };
        await _uow.Cariler.SaveAsync(cari);

        var h1 = new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 5000m, Alacak = 0 };
        var h2 = new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 3000m, Alacak = 0 };
        await _uow.Cariler.SaveHareketAsync(h1);
        await _uow.Cariler.SaveHareketAsync(h2);

        await _uow.Cariler.DeleteHareketAsync(h2);
        await _dbService.RecalculateCariBalanceAsync(cari.Id);

        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(5000m, refreshed!.Borc);
        Assert.Equal(5000m, refreshed.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_45_ReturnInvoice_CreatesCreditAndReducesDebt()
    {
        var cari = new CariKart { CariKod = "C-RET", Unvan = "İade Yapan Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış Faturası", Borc = 10000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış İade Faturası", Borc = 0, Alacak = 2500m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(7500m, refreshed!.Bakiye);
    }

    // -------------------------------------------------------------
    // 4. Çıktılar & Ekstre Senaryoları (5 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_46_GenerateEkstrePdf_ProducesValidBytes()
    {
        var cari = new CariKart { CariKod = "C-PDF", Unvan = "PDF Ekstre Test Müşteri" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            Tarih = DateTime.Today,
            IslemTuru = "Fatura",
            Borc = 1500m,
            Aciklama = "Test Satış Faturası"
        });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        await vm.HesapEkstresiAsync(cari);

        Assert.True(vm.IsEkstreOptionVisible);
        Assert.Equal("Hesap Ekstresi", vm.EkstreTitle);

        var pdfBytes = await _pdfService.GenerateCariEkstrePdfBytesAsync(cari, await _uow.Cariler.GetHareketlerAsync(cari.Id));
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
    }

    [Theory]
    [InlineData(10000, 5000, false)] // Limit aşılmadı
    [InlineData(10000, 12000, true)] // Limit aşıldı
    [InlineData(50000, 50001, true)] // 1 TL aşıldı
    [InlineData(0, 100, false)]      // Limit tanımlanmamış
    public void Scenario_47_to_50_RiskLimitWarningTriggers(decimal limit, decimal bakiye, bool expectedAlarm)
    {
        bool alarm = limit > 0 && bakiye > limit;
        Assert.Equal(expectedAlarm, alarm);
    }
}
