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

public partial class FinansScenariosTests
{
    // =============================================================
    // 5. Kasa Gelişmiş & Detaylı İşlemler (Senaryo 51 - 65)
    // =============================================================

    [Fact]
    public async Task Scenario_51_Kasa_FxCashDeposit_IncreasesCurrencyBalance()
    {
        var usdKasa = new BankaKart { BankaAdi = "Kasa USD", KartTuru = "Kasa", DovizTuru = "USD", Bakiye = 1500m };
        await _uow.Bankalar.SaveAsync(usdKasa);

        usdKasa.Bakiye += 750m;
        await _uow.Bankalar.SaveAsync(usdKasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(usdKasa.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(2250m, retrieved.Bakiye);
        Assert.Equal("USD", retrieved.DovizTuru);
    }

    [Fact]
    public async Task Scenario_52_Kasa_FxCashWithdrawal_DecreasesCurrencyBalance()
    {
        var eurKasa = new BankaKart { BankaAdi = "Kasa EUR", KartTuru = "Kasa", DovizTuru = "EUR", Bakiye = 3000m };
        await _uow.Bankalar.SaveAsync(eurKasa);

        eurKasa.Bakiye -= 1200m;
        await _uow.Bankalar.SaveAsync(eurKasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(eurKasa.Id);
        Assert.Equal(1800m, retrieved!.Bakiye);
    }

    [Fact]
    public async Task Scenario_53_Kasa_NegativeBalanceAllowance_ReflectsOverdraft()
    {
        var kasa = new BankaKart { BankaAdi = "Şantiye Kasa", KartTuru = "Kasa", Bakiye = 500m };
        await _uow.Bankalar.SaveAsync(kasa);

        kasa.Bakiye -= 1200m;
        await _uow.Bankalar.SaveAsync(kasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(-700m, retrieved!.Bakiye);
    }

    [Fact]
    public async Task Scenario_54_Kasa_SoftDelete_SetsIsDeletedFlag()
    {
        var kasa = new BankaKart { BankaAdi = "Kapatılacak Kasa", KartTuru = "Kasa", Bakiye = 0m, IsDeleted = false };
        await _uow.Bankalar.SaveAsync(kasa);

        kasa.IsDeleted = true;
        await _uow.Bankalar.SaveAsync(kasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.True(retrieved!.IsDeleted);
    }

    [Fact]
    public async Task Scenario_55_Kasa_OpeningBalance_InitializesCorrectly()
    {
        var kasa = new BankaKart
        {
            BankaAdi = "Açılış Kasası",
            KartTuru = "Kasa",
            AcilisBakiyesi = 12500m,
            GuncelBakiye = 12500m
        };
        await _uow.Bankalar.SaveAsync(kasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(12500m, retrieved!.AcilisBakiyesi);
        Assert.Equal(12500m, retrieved.Bakiye);
    }

    [Fact]
    public async Task Scenario_56_Kasa_MultipleDeposits_AccumulatesAccurately()
    {
        var kasa = new BankaKart { BankaAdi = "Hasılat Kasası", KartTuru = "Kasa", Bakiye = 0m };
        await _uow.Bankalar.SaveAsync(kasa);

        decimal[] deposits = { 120.50m, 450.75m, 1200m, 32.25m };
        foreach (var dep in deposits)
        {
            kasa.Bakiye += dep;
        }
        await _uow.Bankalar.SaveAsync(kasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(1803.50m, retrieved!.Bakiye);
    }

    [Fact]
    public async Task Scenario_57_Kasa_MultipleWithdrawals_DeductsAccurately()
    {
        var kasa = new BankaKart { BankaAdi = "Gider Kasası", KartTuru = "Kasa", Bakiye = 5000m };
        await _uow.Bankalar.SaveAsync(kasa);

        decimal[] expenses = { 250m, 620.50m, 110.20m, 18.30m };
        foreach (var exp in expenses)
        {
            kasa.Bakiye -= exp;
        }
        await _uow.Bankalar.SaveAsync(kasa);

        var retrieved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(4001.00m, retrieved!.Bakiye);
    }

    [Fact]
    public async Task Scenario_58_Kasa_MevcutBakiyeProperty_MatchesGuncelBakiye()
    {
        var kasa = new BankaKart { BankaAdi = "Eşitlik Test Kasa", KartTuru = "Kasa", GuncelBakiye = 8950m };
        Assert.Equal(8950m, kasa.MevcutBakiye);
        Assert.Equal(8950m, kasa.Bakiye);

        kasa.MevcutBakiye = 9200m;
        Assert.Equal(9200m, kasa.GuncelBakiye);
    }

    [Theory]
    [InlineData(1000, 100, 200, 900)]
    [InlineData(500, 500, 0, 1000)]
    [InlineData(2500, 0, 2500, 0)]
    [InlineData(0, 10000, 3000, 7000)]
    public void Scenario_59_to_62_Kasa_NetChangeCalculation(decimal baslangic, decimal giren, decimal cikan, decimal beklenen)
    {
        decimal sonBakiye = baslangic + giren - cikan;
        Assert.Equal(beklenen, sonBakiye);
    }

    [Theory]
    [InlineData(10000, 10050, 50, "Kasa Fazlası")]
    [InlineData(10000, 9920, -80, "Kasa Noksanı")]
    [InlineData(10000, 10000, 0, "Mutabık")]
    public void Scenario_63_to_65_Kasa_CountDifferenceStatus(decimal defter, decimal fiili, decimal beklenenFark, string beklenenDurum)
    {
        decimal fark = fiili - defter;
        string durum = fark > 0 ? "Kasa Fazlası" : (fark < 0 ? "Kasa Noksanı" : "Mutabık");

        Assert.Equal(beklenenFark, fark);
        Assert.Equal(beklenenDurum, durum);
    }

    // =============================================================
    // 6. Banka Hesapları & EFT / Havale Süreçleri (Senaryo 66 - 80)
    // =============================================================

    [Fact]
    public async Task Scenario_66_Banka_CreateAccountWithBranchCode_PersistsSuccessfully()
    {
        var banka = new BankaKart
        {
            BankaAdi = "Ziraat Bankası",
            SubeAdi = "Ümraniye Sanayi",
            SubeKodu = "1248",
            HesapNo = "5551234-5001",
            IBAN = "TR120001001248000555123450",
            KartTuru = "Vadesiz",
            Bakiye = 25000m
        };
        await _uow.Bankalar.SaveAsync(banka);

        var retrieved = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("1248", retrieved.SubeKodu);
        Assert.Equal("Vadesiz", retrieved.KartTuru);
    }

    [Fact]
    public async Task Scenario_67_Banka_UpdateAccountDetails_ReflectsCorrectly()
    {
        var banka = new BankaKart { BankaAdi = "İş Bankası", SubeAdi = "Kadıköy", Bakiye = 10000m };
        await _uow.Bankalar.SaveAsync(banka);

        banka.SubeAdi = "Rıhtım Kadıköy";
        banka.Yetkili = "Ayşe Demir";
        banka.Telefon = "0216 333 44 55";
        await _uow.Bankalar.SaveAsync(banka);

        var retrieved = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal("Rıhtım Kadıköy", retrieved!.SubeAdi);
        Assert.Equal("Ayşe Demir", retrieved.Yetkili);
        Assert.Equal("0216 333 44 55", retrieved.Telefon);
    }

    [Fact]
    public async Task Scenario_68_Banka_InterbankTransfer_DebitsAndCreditsProperly()
    {
        var bSource = new BankaKart { BankaAdi = "Garanti BBVA", Bakiye = 100000m };
        var bTarget = new BankaKart { BankaAdi = "Denizbank", Bakiye = 20000m };
        await _uow.Bankalar.SaveAsync(bSource);
        await _uow.Bankalar.SaveAsync(bTarget);

        decimal transferAmount = 35000m;
        bSource.Bakiye -= transferAmount;
        bTarget.Bakiye += transferAmount;
        await _uow.Bankalar.SaveAsync(bSource);
        await _uow.Bankalar.SaveAsync(bTarget);

        var refSource = await _uow.Bankalar.GetByIdAsync(bSource.Id);
        var refTarget = await _uow.Bankalar.GetByIdAsync(bTarget.Id);

        Assert.Equal(65000m, refSource!.Bakiye);
        Assert.Equal(55000m, refTarget!.Bakiye);
    }

    [Fact]
    public async Task Scenario_69_Banka_CrossCurrencyPurchase_ReflectsExchangeTransaction()
    {
        var tlAccount = new BankaKart { BankaAdi = "İş TL", DovizTuru = "TRY", Bakiye = 200000m };
        var usdAccount = new BankaKart { BankaAdi = "İş USD", DovizTuru = "USD", Bakiye = 5000m };
        await _uow.Bankalar.SaveAsync(tlAccount);
        await _uow.Bankalar.SaveAsync(usdAccount);

        decimal boughtUsd = 2000m;
        decimal rate = 38.50m;
        decimal tlSpent = boughtUsd * rate;

        tlAccount.Bakiye -= tlSpent;
        usdAccount.Bakiye += boughtUsd;
        await _uow.Bankalar.SaveAsync(tlAccount);
        await _uow.Bankalar.SaveAsync(usdAccount);

        var refTl = await _uow.Bankalar.GetByIdAsync(tlAccount.Id);
        var refUsd = await _uow.Bankalar.GetByIdAsync(usdAccount.Id);

        Assert.Equal(123000m, refTl!.Bakiye);
        Assert.Equal(7000m, refUsd!.Bakiye);
    }

    [Fact]
    public async Task Scenario_70_Banka_InterestAccrual_IncreasesBalance()
    {
        var banka = new BankaKart { BankaAdi = "Mevduat Hesabı", KartTuru = "Vadeli", Bakiye = 500000m };
        await _uow.Bankalar.SaveAsync(banka);

        decimal netFaiz = 18500m;
        banka.Bakiye += netFaiz;
        await _uow.Bankalar.SaveAsync(banka);

        var retrieved = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal(518500m, retrieved!.Bakiye);
    }

    [Theory]
    [InlineData(100000, 45.0, 30, 3698.63)] // 100.000 * 45% * 30 / 365
    [InlineData(250000, 40.0, 90, 24657.53)]
    [InlineData(50000, 50.0, 7, 479.45)]
    public void Scenario_71_to_73_Banka_TermDepositInterestCalculation(decimal anapara, double faizOrani, int gunSayisi, decimal beklenenBrutFaiz)
    {
        decimal brutFaiz = Math.Round(anapara * (decimal)(faizOrani / 100.0) * gunSayisi / 365m, 2);
        Assert.Equal(beklenenBrutFaiz, brutFaiz);
    }

    [Theory]
    [InlineData(3698.63, 0.075, 277.40, 3421.23)] // %7.5 stopaj
    [InlineData(1000.00, 0.10, 100.00, 900.00)]
    public void Scenario_74_to_75_Banka_WithholdingTaxOnInterest(decimal brutFaiz, double stopajOrani, decimal beklenenStopaj, decimal beklenenNet)
    {
        decimal stopaj = Math.Round(brutFaiz * (decimal)stopajOrani, 2);
        decimal netFaiz = brutFaiz - stopaj;

        Assert.Equal(beklenenStopaj, stopaj);
        Assert.Equal(beklenenNet, netFaiz);
    }

    [Fact]
    public async Task Scenario_76_Banka_DeleteAccount_RemovesRecord()
    {
        var banka = new BankaKart { BankaAdi = "Geçici Banka", Bakiye = 0m };
        await _uow.Bankalar.SaveAsync(banka);
        int id = banka.Id;

        await _uow.Bankalar.DeleteAsync(id);
        var retrieved = await _uow.Bankalar.GetByIdAsync(id);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task Scenario_77_Banka_ListAll_ReturnsAllActiveAccounts()
    {
        await _uow.Bankalar.SaveAsync(new BankaKart { BankaAdi = "Hesap 1", Bakiye = 100m });
        await _uow.Bankalar.SaveAsync(new BankaKart { BankaAdi = "Hesap 2", Bakiye = 200m });

        var all = await _uow.Bankalar.GetAllAsync();
        Assert.True(all.Count >= 2);
        Assert.Contains(all, b => b.BankaAdi == "Hesap 1");
        Assert.Contains(all, b => b.BankaAdi == "Hesap 2");
    }

    [Fact]
    public async Task Scenario_78_Banka_OverdraftCreditLimit_AllowsControlledNegative()
    {
        var kmh = new BankaKart { BankaAdi = "KMH Hesabı", KartTuru = "Ticari KMH", Bakiye = 0m };
        await _uow.Bankalar.SaveAsync(kmh);

        decimal limit = 100000m;
        decimal cekilen = 45000m;
        kmh.Bakiye -= cekilen;
        await _uow.Bankalar.SaveAsync(kmh);

        var retrieved = await _uow.Bankalar.GetByIdAsync(kmh.Id);
        Assert.Equal(-45000m, retrieved!.Bakiye);
        Assert.True(Math.Abs(retrieved.Bakiye) <= limit);
    }

    [Theory]
    [InlineData("TR330006100511123456789012", true)]
    [InlineData("tr330006100511123456789012", false)] // Standard uppercase format check
    public void Scenario_79_to_80_Banka_IbanCaseValidation(string iban, bool expectedValid)
    {
        bool isValid = !string.IsNullOrEmpty(iban) && iban.StartsWith("TR") && iban.Length == 26;
        Assert.Equal(expectedValid, isValid);
    }

    // =============================================================
    // 7. Çek Yönetimi ve Ciro Yaşam Döngüsü (Senaryo 81 - 95)
    // =============================================================

    [Fact]
    public async Task Scenario_81_Cek_SaveWithCompleteDetails_PersistsAllFields()
    {
        var cek = new Cek
        {
            CekNo = "CK-8811",
            PortfoyNo = "P-101",
            SeriNo = "SR-9988",
            CariId = 15,
            CariUnvan = "Alp Lojistik Ltd.",
            Banka = "Garanti BBVA",
            Sube = "Tuzla",
            HesapNo = "444555",
            Borclu = "Alp Lojistik Ltd.",
            Tutar = 45000m,
            VadeTarihi = DateTime.Today.AddDays(60),
            CekTuru = "Alınan",
            Durum = "Portföyde",
            Aciklama = "Sevkiyat karşılığı çek",
            IslemTuru = "Tahsilat"
        };
        await _uow.Cekler.SaveAsync(cek);

        var retrieved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("CK-8811", retrieved.CekNo);
        Assert.Equal("P-101", retrieved.PortfoyNo);
        Assert.Equal("SR-9988", retrieved.SeriNo);
        Assert.Equal(45000m, retrieved.Tutar);
        Assert.Equal("Portföyde", retrieved.Durum);
    }

    [Fact]
    public async Task Scenario_82_Cek_EndorseToSupplier_SetsRedirectFields()
    {
        var cek = new Cek
        {
            CekNo = "CK-CIRO-82",
            Tutar = 32000m,
            CekTuru = "Alınan",
            Durum = "Portföyde"
        };
        await _uow.Cekler.SaveAsync(cek);

        // Cirolama işlemi
        cek.Durum = "Ciro Edildi";
        cek.YonlendirilenCariId = 22;
        cek.YonlendirilenCariUnvan = "Çelik Profil Sanayi A.Ş.";
        cek.YonlendirmeTarihi = DateTime.Today;
        await _uow.Cekler.SaveAsync(cek);

        var retrieved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.Equal("Ciro Edildi", retrieved!.Durum);
        Assert.Equal(22, retrieved.YonlendirilenCariId);
        Assert.Equal("Çelik Profil Sanayi A.Ş.", retrieved.YonlendirilenCariUnvan);
        Assert.NotNull(retrieved.YonlendirmeTarihi);
    }

    [Fact]
    public async Task Scenario_83_Cek_MarkBounced_UpdatesToKarsiliksiz()
    {
        var cek = new Cek { CekNo = "CK-BOUNCE-83", Tutar = 18000m, Durum = "Portföyde" };
        await _uow.Cekler.SaveAsync(cek);

        cek.Durum = "Karşılıksız";
        cek.Aciklama = "Banka karşılıksız kaşesi vurdu.";
        await _uow.Cekler.SaveAsync(cek);

        var retrieved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.Equal("Karşılıksız", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_84_Cek_ImagePaths_CanStoreFrontAndBackImages()
    {
        var cek = new Cek
        {
            CekNo = "CK-IMG-84",
            Tutar = 12000m,
            GorselYoluOn = @"C:\Muhasebe\Cekler\ck84_front.jpg",
            GorselYoluArka = @"C:\Muhasebe\Cekler\ck84_back.jpg",
            GorselYolu = @"C:\Muhasebe\Cekler\ck84.pdf"
        };
        await _uow.Cekler.SaveAsync(cek);

        var retrieved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.Equal(@"C:\Muhasebe\Cekler\ck84_front.jpg", retrieved!.GorselYoluOn);
        Assert.Equal(@"C:\Muhasebe\Cekler\ck84_back.jpg", retrieved.GorselYoluArka);
        Assert.Equal(@"C:\Muhasebe\Cekler\ck84.pdf", retrieved.GorselYolu);
    }

    [Fact]
    public async Task Scenario_85_Cek_GetByCariId_ReturnsOnlyMatchingCariChecks()
    {
        int cariId = 991;
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "CK-M1", CariId = cariId, Tutar = 1000m });
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "CK-M2", CariId = cariId, Tutar = 2000m });
        await _uow.Cekler.SaveAsync(new Cek { CekNo = "CK-OTHER", CariId = 992, Tutar = 3000m });

        var list = await _uow.Cekler.GetByCariIdAsync(cariId);
        Assert.Equal(2, list.Count);
        Assert.All(list, c => Assert.Equal(cariId, c.CariId));
    }

    [Fact]
    public async Task Scenario_86_Cek_OwnIssuedCheck_MarkedPaid_UpdatesState()
    {
        var cek = new Cek
        {
            CekNo = "CK-OWN-86",
            CekTuru = "Verilen",
            Durum = "Portföyde",
            Tutar = 50000m,
            VadeTarihi = DateTime.Today
        };
        await _uow.Cekler.SaveAsync(cek);

        cek.Durum = "Ödendi";
        await _uow.Cekler.SaveAsync(cek);

        var retrieved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.Equal("Ödendi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_87_Cek_DeleteAsync_RemovesFromDatabase()
    {
        var cek = new Cek { CekNo = "CK-DEL-87", Tutar = 7500m };
        await _uow.Cekler.SaveAsync(cek);
        int id = cek.Id;

        await _uow.Cekler.DeleteAsync(id);
        var retrieved = await _uow.Cekler.GetByIdAsync(id);
        Assert.Null(retrieved);
    }

    [Theory]
    [InlineData("2026-09-01", "2026-09-10", true)]
    [InlineData("2026-09-15", "2026-09-10", false)]
    [InlineData("2026-09-10", "2026-09-10", true)]
    public void Scenario_88_to_90_Cek_MaturityComparison(string checkDateStr, string referenceDateStr, bool isDue)
    {
        DateTime checkDate = DateTime.Parse(checkDateStr);
        DateTime refDate = DateTime.Parse(referenceDateStr);

        bool due = checkDate <= refDate;
        Assert.Equal(isDue, due);
    }

    [Theory]
    [InlineData(10000, 30, 0.50, 410.96)] // Reeskont = Tutar * Oran * Gün / 365
    [InlineData(50000, 60, 0.45, 3698.63)]
    public void Scenario_91_to_92_Cek_RediscountInterestCalculation(decimal tutar, int kalanGun, double reeskontOrani, decimal beklenenReeskont)
    {
        decimal reeskont = Math.Round(tutar * (decimal)reeskontOrani * kalanGun / 365m, 2);
        Assert.Equal(beklenenReeskont, reeskont);
    }

    [Theory]
    [InlineData(10000, 410.96, 9589.04)]
    [InlineData(50000, 3698.63, 46301.37)]
    public void Scenario_93_to_94_Cek_NetPresentValueCalculation(decimal nominalTutar, decimal reeskont, decimal beklenenNet)
    {
        decimal netDeger = nominalTutar - reeskont;
        Assert.Equal(beklenenNet, netDeger);
    }

    [Fact]
    public async Task Scenario_95_Cek_SaveWithTransactionAsync_PersistsWithoutError()
    {
        var cek = new Cek { CekNo = "CK-TX-95", Tutar = 15000m, CekTuru = "Alınan", Durum = "Portföyde" };
        int result = await _uow.Cekler.SaveWithTransactionAsync(cek);

        Assert.True(result > 0 || cek.Id > 0);
        var retrieved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.NotNull(retrieved);
    }

    // =============================================================
    // 8. Senet Yönetimi & Protesto Süreçleri (Senaryo 96 - 100)
    // =============================================================

    [Fact]
    public async Task Scenario_96_Senet_SaveWithCompleteDetails_PersistsAllFields()
    {
        var senet = new Senet
        {
            SenetNo = "SN-9901",
            PortfoyNo = "SP-44",
            CariId = 18,
            CariUnvan = "Metin Kaya",
            Borclu = "Metin Kaya",
            Tutar = 14000m,
            VadeTarihi = DateTime.Today.AddDays(40),
            SenetTuru = "Alınan",
            Durum = "Portföyde",
            Aciklama = "Mobilya alımı vadeli senet"
        };
        await _uow.Senetler.SaveAsync(senet);

        var retrieved = await _uow.Senetler.GetByIdAsync(senet.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("SN-9901", retrieved.SenetNo);
        Assert.Equal("Metin Kaya", retrieved.AsilBorclu);
        Assert.Equal(14000m, retrieved.Tutar);
    }

    [Fact]
    public async Task Scenario_97_Senet_MarkProtested_UpdatesToProtestolu()
    {
        var senet = new Senet { SenetNo = "SN-PROT-97", Tutar = 22000m, Durum = "Portföyde" };
        await _uow.Senetler.SaveAsync(senet);

        senet.Durum = "Protestolu";
        senet.Aciklama = "Noter ihtarnamesi çekildi.";
        await _uow.Senetler.SaveAsync(senet);

        var retrieved = await _uow.Senetler.GetByIdAsync(senet.Id);
        Assert.Equal("Protestolu", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_98_Senet_GetByCariId_ReturnsOnlyMatchingCariSenets()
    {
        int cariId = 882;
        await _uow.Senetler.SaveAsync(new Senet { SenetNo = "SN-C1", CariId = cariId, Tutar = 5000m });
        await _uow.Senetler.SaveAsync(new Senet { SenetNo = "SN-C2", CariId = cariId, Tutar = 6000m });
        await _uow.Senetler.SaveAsync(new Senet { SenetNo = "SN-OTHER", CariId = 883, Tutar = 7000m });

        var list = await _uow.Senetler.GetByCariIdAsync(cariId);
        Assert.Equal(2, list.Count);
        Assert.All(list, s => Assert.Equal(cariId, s.CariId));
    }

    [Fact]
    public async Task Scenario_99_Senet_Collection_UpdatesStatusToTahsilEdildi()
    {
        var senet = new Senet { SenetNo = "SN-COL-99", Tutar = 9500m, Durum = "Portföyde" };
        await _uow.Senetler.SaveAsync(senet);

        senet.Durum = "Tahsil Edildi";
        await _uow.Senetler.SaveAsync(senet);

        var retrieved = await _uow.Senetler.GetByIdAsync(senet.Id);
        Assert.Equal("Tahsil Edildi", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_100_Senet_DeleteAsync_RemovesFromDatabase()
    {
        var senet = new Senet { SenetNo = "SN-DEL-100", Tutar = 3000m };
        await _uow.Senetler.SaveAsync(senet);
        int id = senet.Id;

        await _uow.Senetler.DeleteAsync(id);
        var retrieved = await _uow.Senetler.GetByIdAsync(id);
        Assert.Null(retrieved);
    }
}
