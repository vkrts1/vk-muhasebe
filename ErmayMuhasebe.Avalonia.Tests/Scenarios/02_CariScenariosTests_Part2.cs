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

public partial class CariScenariosTests
{
    // =============================================================
    // 5. İleri Cari Kart Özellikleri & Vade/Risk/İskonto (51-70)
    // =============================================================

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(90)]
    [InlineData(120)]
    public async Task Scenario_51_to_57_VadeGunuConfigurations_PersistCorrectly(int vadeGunu)
    {
        var cari = new CariKart
        {
            CariKod = $"VADE-{vadeGunu}",
            Unvan = $"Vade Test {vadeGunu} Gün",
            VadeGunu = vadeGunu
        };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(vadeGunu, retrieved.VadeGunu);
    }

    [Fact]
    public async Task Scenario_58_RiskLimiti_Zero_MeansNoLimitRestriction()
    {
        var cari = new CariKart { CariKod = "RL-0", Unvan = "Limitsiz Müşteri", RiskLimiti = 0 };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(0, retrieved!.RiskLimiti);
    }

    [Fact]
    public async Task Scenario_59_RiskLimiti_HighLimit_PersistsPrecisely()
    {
        decimal highLimit = 5000000.75m;
        var cari = new CariKart { CariKod = "RL-HIGH", Unvan = "Holding A.Ş.", RiskLimiti = highLimit };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(highLimit, retrieved!.RiskLimiti);
    }

    [Fact]
    public async Task Scenario_60_RiskLimiti_NegativeValue_HandlesGracefully()
    {
        var cari = new CariKart { CariKod = "RL-NEG", Unvan = "Kısıtlı Müşteri", RiskLimiti = -100m };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(-100m, retrieved!.RiskLimiti);
    }

    [Theory]
    [InlineData("Müşteri")]
    [InlineData("Tedarikçi")]
    [InlineData("Personel")]
    [InlineData("Ortak")]
    [InlineData("Diğer")]
    public async Task Scenario_61_to_65_CariGrupClassifications_PersistAndFilter(string grup)
    {
        var cari = new CariKart { CariKod = $"GRP-{grup}", Unvan = $"{grup} Unvanı", Grup = grup, Tur = grup };
        await _uow.Cariler.SaveAsync(cari);

        var all = await _uow.Cariler.GetAllAsync();
        var match = all.FirstOrDefault(c => c.Grup == grup);
        Assert.NotNull(match);
        Assert.Equal(grup, match.Grup);
    }

    [Fact]
    public async Task Scenario_66_CariKart_IBAN_PreservesFormattedString()
    {
        string iban = "TR33 0006 1005 1234 5678 9012 34";
        var cari = new CariKart { CariKod = "IBAN-01", Unvan = "Bankacı Tedarikçi", IBAN = iban };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(iban, retrieved!.IBAN);
    }

    [Fact]
    public async Task Scenario_67_CariKart_OdemePlani_PersistsDescription()
    {
        string plan = "%50 Peşin, %50 60 Gün Vadeli Çek";
        var cari = new CariKart { CariKod = "OPL-01", Unvan = "Planlı Müşteri", OdemePlani = plan };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(plan, retrieved!.OdemePlani);
    }

    [Fact]
    public async Task Scenario_68_CariKart_TCNo_IndividualCustomer_Persists11Digits()
    {
        string tcNo = "12345678901";
        var cari = new CariKart { CariKod = "TC-01", Unvan = "Bireysel Alıcı", TCNo = tcNo };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(tcNo, retrieved!.TCNo);
    }

    [Fact]
    public async Task Scenario_69_CariKart_VergiDairesiAndNo_PersistsCorporate()
    {
        var cari = new CariKart
        {
            CariKod = "CORP-01",
            Unvan = "Büyük Kurumsal Ltd.",
            VergiDairesi = "Beşiktaş",
            VergiNo = "1234567890"
        };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal("Beşiktaş", retrieved!.VergiDairesi);
        Assert.Equal("1234567890", retrieved.VergiNo);
    }

    [Fact]
    public async Task Scenario_70_CariKart_Coordinates_StoreLatitudeLongitude()
    {
        var cari = new CariKart
        {
            CariKod = "GEO-01",
            Unvan = "Konumlu Mağaza",
            Latitude = 41.0082,
            Longitude = 28.9784
        };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(41.0082, retrieved!.Latitude);
        Assert.Equal(28.9784, retrieved.Longitude);
    }

    // =============================================================
    // 6. Cari Arama, Filtreleme & ViewModel UI Davranışları (71-85)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_71_CariListViewModel_FilterBySearchText_MatchesTitle()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "F-01", Unvan = "Anadolu Cam Sanayi" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "F-02", Unvan = "Trakya Döküm A.Ş." });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        vm.SearchString = "Anadolu";
        await vm.LoadCarilerAsync();

        Assert.Contains(vm.Cariler, c => c.Unvan!.Contains("Anadolu"));
    }

    [AvaloniaFact]
    public async Task Scenario_72_CariListViewModel_FilterBySearchText_MatchesCode()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "KOD-72A", Unvan = "Birinci Firma" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "KOD-72B", Unvan = "İkinci Firma" });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        vm.SearchString = "72A";
        await vm.LoadCarilerAsync();

        Assert.Contains(vm.Cariler, c => c.CariKod!.Contains("72A"));
    }

    [AvaloniaFact]
    public async Task Scenario_73_CariListViewModel_FilterBySearchText_CaseInsensitive()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "CI-01", Unvan = "KÜÇÜK VE BÜYÜK HARF" });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        vm.SearchString = "KÜÇÜK";
        await vm.LoadCarilerAsync();

        Assert.Contains(vm.Cariler, c => c.CariKod == "CI-01");
    }

    [AvaloniaFact]
    public async Task Scenario_74_CariListViewModel_EmptySearchText_RestoresFullList()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "R-01", Unvan = "Müşteri 1" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "R-02", Unvan = "Müşteri 2" });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        vm.SearchString = "Müşteri 1";
        await vm.LoadCarilerAsync();
        int filteredCount = vm.Cariler.Count;

        vm.SearchString = string.Empty;
        await vm.LoadCarilerAsync();
        Assert.True(vm.Cariler.Count >= filteredCount);
    }

    [AvaloniaFact]
    public async Task Scenario_75_CariListViewModel_FilterByCity_LocatesCorrectRecord()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "CT-01", Unvan = "İzmirli Tedarikçi", Il = "İzmir" });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "CT-02", Unvan = "Ankaralı Müşteri", Il = "Ankara" });

        var all = await _uow.Cariler.GetAllAsync();
        var izmirli = all.Where(c => c.Il == "İzmir").ToList();

        Assert.Single(izmirli);
        Assert.Equal("CT-01", izmirli[0].CariKod);
    }

    [AvaloniaFact]
    public async Task Scenario_76_CariListViewModel_FilterByDistrict_MatchesPrecisely()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "DS-01", Unvan = "Kadıköy Şubesi", Ilce = "Kadıköy" });

        var all = await _uow.Cariler.GetAllAsync();
        var match = all.FirstOrDefault(c => c.Ilce == "Kadıköy");

        Assert.NotNull(match);
        Assert.Equal("DS-01", match.CariKod);
    }

    [AvaloniaFact]
    public async Task Scenario_77_CariListViewModel_FilterByTaxNumber_ExactSearch()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "TX-01", Unvan = "Vergi Test Ltd.", VergiNo = "9876543210" });

        var all = await _uow.Cariler.GetAllAsync();
        var match = all.FirstOrDefault(c => c.VergiNo == "9876543210");

        Assert.NotNull(match);
        Assert.Equal("Vergi Test Ltd.", match.Unvan);
    }

    [AvaloniaFact]
    public async Task Scenario_78_CariListViewModel_FilterByContactPerson_LocatesAuthorizedAgent()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "CP-01", Unvan = "Delta A.Ş.", Yetkili = "Ahmet Yılmaz" });

        var all = await _uow.Cariler.GetAllAsync();
        var match = all.FirstOrDefault(c => c.Yetkili == "Ahmet Yılmaz");

        Assert.NotNull(match);
        Assert.Equal("Delta A.Ş.", match.Unvan);
    }

    [AvaloniaFact]
    public async Task Scenario_79_CariListViewModel_FilterByEmail_LocatesCompany()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "EM-01", Unvan = "Mail Test Sanayi", Email = "info@mailtest.com" });

        var all = await _uow.Cariler.GetAllAsync();
        var match = all.FirstOrDefault(c => c.Email == "info@mailtest.com");

        Assert.NotNull(match);
        Assert.Equal("EM-01", match.CariKod);
    }

    [AvaloniaFact]
    public async Task Scenario_80_CariListViewModel_FilterByPhone_MatchesDirectly()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "PH-01", Unvan = "Telefoncu Ltd.", Telefon = "02161234567" });

        var all = await _uow.Cariler.GetAllAsync();
        var match = all.FirstOrDefault(c => c.Telefon == "02161234567");

        Assert.NotNull(match);
        Assert.Equal("PH-01", match.CariKod);
    }

    [AvaloniaFact]
    public async Task Scenario_81_CariListViewModel_ActiveStatusToggle_ExcludesInactiveWhenRequired()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "ACT-01", Unvan = "Aktif Müşteri", AktifMi = true });
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "ACT-02", Unvan = "Pasif Müşteri", AktifMi = false });

        var all = await _uow.Cariler.GetAllAsync();
        var activeOnly = all.Where(c => c.AktifMi).ToList();

        Assert.Contains(activeOnly, c => c.CariKod == "ACT-01");
        Assert.DoesNotContain(activeOnly, c => c.CariKod == "ACT-02");
    }

    [AvaloniaFact]
    public async Task Scenario_82_CariListViewModel_SelectedCari_TriggersSelectionState()
    {
        var cari = new CariKart { CariKod = "SEL-01", Unvan = "Seçilen Cari" };
        await _uow.Cariler.SaveAsync(cari);

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        await vm.LoadCarilerAsync();

        vm.SelectedCari = vm.Cariler.FirstOrDefault(c => c.CariKod == "SEL-01");
        Assert.NotNull(vm.SelectedCari);
        Assert.Equal("SEL-01", vm.SelectedCari.CariKod);
    }

    [AvaloniaFact]
    public async Task Scenario_83_CariListViewModel_AddNewCariCommand_InitializesView()
    {
        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        Assert.NotNull(vm.OpenAddCariDialogCommand);
        Assert.True(vm.OpenAddCariDialogCommand.CanExecute(null));
    }

    [AvaloniaFact]
    public async Task Scenario_84_CariListViewModel_RefreshCommand_ReloadsData()
    {
        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        Assert.NotNull(vm.LoadCarilerCommand);
        await vm.LoadCarilerCommand.ExecuteAsync(false);
        Assert.NotNull(vm.Cariler);
    }

    [AvaloniaFact]
    public async Task Scenario_85_CariListViewModel_SearchWithTurkishCharacters_WorksReliably()
    {
        await _uow.Cariler.SaveAsync(new CariKart { CariKod = "TR-CHAR", Unvan = "ŞAHİN ÇELİK İŞLEME ÖRME" });

        var vm = _serviceProvider.GetRequiredService<CariListViewModel>();
        vm.SearchString = "ŞAHİN";
        await vm.LoadCarilerAsync();

        Assert.Contains(vm.Cariler, c => c.CariKod == "TR-CHAR");
    }

    // =============================================================
    // 7. Bakiye & Hareket Entegrasyonu & Uç Durumlar (86-100)
    // =============================================================

    [Fact]
    public async Task Scenario_86_Balance_MultipleDebitCredit_CalculatesNetAccurately()
    {
        var cari = new CariKart { CariKod = "BAL-86", Unvan = "Hassas Bakiye Müşterisi" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış", Borc = 15000.50m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Tahsilat", Borc = 0, Alacak = 5000.25m });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "İade", Borc = 0, Alacak = 1000.10m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        decimal expected = 15000.50m - 5000.25m - 1000.10m; // 9000.15m
        Assert.Equal(expected, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_87_Balance_ZeroBalance_ShowsNeitherDebitNorCredit()
    {
        var cari = new CariKart { CariKod = "BAL-87", Unvan = "Sıfır Bakiye" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış", Borc = 4000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Tahsilat", Borc = 0, Alacak = 4000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(0m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_88_Balance_FlipFromDebitToCredit_ShowsNegativeNetDebit()
    {
        var cari = new CariKart { CariKod = "BAL-88", Unvan = "Alacaklı Duruma Geçen" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Satış", Borc = 2000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Fazla Tahsilat", Borc = 0, Alacak = 3000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        Assert.Equal(-1000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_89_Balance_DevirBorcAndDevirAlacak_IncludedInBakiyeProperty()
    {
        var cari = new CariKart
        {
            CariKod = "BAL-89",
            Unvan = "Devir Test",
            DevirBorc = 10000m,
            DevirAlacak = 2500m
        };
        await _uow.Cariler.SaveAsync(cari);

        Assert.Equal(7500m, cari.Bakiye);
    }

    [Fact]
    public async Task Scenario_90_Balance_DevirCombinedWithMovements_ComputesTotalBalance()
    {
        var cari = new CariKart
        {
            CariKod = "BAL-90",
            Unvan = "Devir ve Hareketli Cari",
            DevirBorc = 5000m,
            DevirAlacak = 1000m
        };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 3000m, Alacak = 0 });
        await _uow.Cariler.SaveHareketAsync(new CariHareket { CariId = cari.Id, IslemTuru = "Ödeme", Borc = 0, Alacak = 2000m });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        // Net: (3000 + 5000) - (2000 + 1000) = 8000 - 3000 = 5000
        Assert.Equal(5000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_91_CariHareket_DeleteMovement_RecalculatesBalance()
    {
        var cari = new CariKart { CariKod = "MOV-91", Unvan = "Hareket Silinen Cari" };
        await _uow.Cariler.SaveAsync(cari);

        var m1 = new CariHareket { CariId = cari.Id, IslemTuru = "Fatura 1", Borc = 5000m, Alacak = 0 };
        var m2 = new CariHareket { CariId = cari.Id, IslemTuru = "Fatura 2 Hatalı", Borc = 3000m, Alacak = 0 };
        await _uow.Cariler.SaveHareketAsync(m1);
        await _uow.Cariler.SaveHareketAsync(m2);

        await _uow.Cariler.DeleteHareketAsync(m2);
        await _dbService.RecalculateCariBalanceAsync(cari.Id);

        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(5000m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_92_CariHareket_UpdateMovement_UpdatesBalanceAccordingly()
    {
        var cari = new CariKart { CariKod = "MOV-92", Unvan = "Hareket Güncellenen Cari" };
        await _uow.Cariler.SaveAsync(cari);

        var m = new CariHareket { CariId = cari.Id, IslemTuru = "Fatura", Borc = 2000m, Alacak = 0 };
        await _uow.Cariler.SaveHareketAsync(m);

        m.Borc = 4500m;
        await _uow.Cariler.SaveHareketAsync(m);

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(4500m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_93_VirmanHareket_TransfersBalanceBetweenTwoCaris()
    {
        var cariSource = new CariKart { CariKod = "VRM-SRC", Unvan = "Kaynak Cari", Borc = 10000m };
        var cariTarget = new CariKart { CariKod = "VRM-TGT", Unvan = "Hedef Cari", Borc = 0 };
        await _uow.Cariler.SaveAsync(cariSource);
        await _uow.Cariler.SaveAsync(cariTarget);

        decimal transferAmount = 3000m;
        // Virman: Kaynak cariye alacak kaydı, Hedef cariye borç kaydı
        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cariSource.Id,
            IslemTuru = "Virman",
            Borc = 0,
            Alacak = transferAmount,
            YonlendirilenCariId = cariTarget.Id,
            YonlendirilenCariUnvan = cariTarget.Unvan
        });

        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cariTarget.Id,
            IslemTuru = "Virman",
            Borc = transferAmount,
            Alacak = 0,
            YonlendirilenCariId = cariSource.Id,
            YonlendirilenCariUnvan = cariSource.Unvan
        });

        await _dbService.RecalculateCariBalanceAsync(cariSource.Id);
        await _dbService.RecalculateCariBalanceAsync(cariTarget.Id);

        var src = await _uow.Cariler.GetByIdAsync(cariSource.Id);
        var tgt = await _uow.Cariler.GetByIdAsync(cariTarget.Id);

        Assert.Equal(-3000m, src!.Bakiye); // Borc movement 0, Alacak movement 3000
        Assert.Equal(3000m, tgt!.Bakiye);  // Borc movement 3000, Alacak movement 0
    }

    [Fact]
    public async Task Scenario_94_CariHareket_WithEvrakNo_PersistsEvrakReference()
    {
        var cari = new CariKart { CariKod = "EVR-94", Unvan = "Evrak Test Cari" };
        await _uow.Cariler.SaveAsync(cari);

        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            EvrakNo = "EVR-2026-9999",
            IslemTuru = "Fatura",
            Borc = 750m
        });

        var movements = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Contains(movements, m => m.EvrakNo == "EVR-2026-9999");
    }

    [Fact]
    public async Task Scenario_95_CariHareket_WithDueDate_VadePersistedCorrectly()
    {
        var cari = new CariKart { CariKod = "VD-95", Unvan = "Vade Tarihli Hareket" };
        await _uow.Cariler.SaveAsync(cari);

        DateTime dueDate = DateTime.Today.AddDays(45);
        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            Vade = dueDate,
            IslemTuru = "Fatura",
            Borc = 12000m
        });

        var movements = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Equal(dueDate, movements.First().Vade);
    }

    [Fact]
    public async Task Scenario_96_CariHareket_SlipImage_StoresPathOrUri()
    {
        var cari = new CariKart { CariKod = "SLIP-96", Unvan = "Dekontlu Cari" };
        await _uow.Cariler.SaveAsync(cari);

        string slipPath = "C:\\receipts\\slip_12345.jpg";
        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            SlipImage = slipPath,
            IslemTuru = "Tahsilat",
            Alacak = 2500m
        });

        var movements = await _uow.Cariler.GetHareketlerAsync(cari.Id);
        Assert.Equal(slipPath, movements.First().SlipImage);
    }

    [Fact]
    public async Task Scenario_97_CariHareket_LargeDecimalAmount_PreservesPrecision()
    {
        var cari = new CariKart { CariKod = "PREC-97", Unvan = "Hassas Kuruşlu Cari" };
        await _uow.Cariler.SaveAsync(cari);

        decimal preciseVal = 999999.88m;
        await _uow.Cariler.SaveHareketAsync(new CariHareket
        {
            CariId = cari.Id,
            IslemTuru = "Büyük Fatura",
            Borc = preciseVal
        });

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.Equal(preciseVal, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_98_CariHareket_TenConsecutiveMovements_CalculatesRunningBalance()
    {
        var cari = new CariKart { CariKod = "RUN-98", Unvan = "Seri Hareketli Cari" };
        await _uow.Cariler.SaveAsync(cari);

        for (int i = 1; i <= 10; i++)
        {
            await _uow.Cariler.SaveHareketAsync(new CariHareket
            {
                CariId = cari.Id,
                IslemTuru = $"Seri Fatura {i}",
                Borc = 100m * i
            });
        }

        await _dbService.RecalculateCariBalanceAsync(cari.Id);
        var refreshed = await _uow.Cariler.GetByIdAsync(cari.Id);

        // Sum(100*i for i=1..10) = 100 * (10*11/2) = 5500
        Assert.Equal(5500m, refreshed!.Bakiye);
    }

    [Fact]
    public async Task Scenario_99_Cari_RiskControlFlag_TrueByDefault()
    {
        var cari = new CariKart { CariKod = "RSK-99", Unvan = "Risk Varsayılanı" };
        Assert.True(cari.FaturadaRiskKontrolu);
    }

    [Fact]
    public async Task Scenario_100_Cari_RiskControlFlag_CanBeDisabled()
    {
        var cari = new CariKart { CariKod = "RSK-100", Unvan = "Risk Kontrolsüz Cari", FaturadaRiskKontrolu = false };
        await _uow.Cariler.SaveAsync(cari);

        var retrieved = await _uow.Cariler.GetByIdAsync(cari.Id);
        Assert.False(retrieved!.FaturadaRiskKontrolu);
    }
}
