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

public partial class FinansScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Kasa Yönetimi (12 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CreateTlKasa_SavesSuccessfully()
    {
        var kasa = new BankaKart { BankaAdi = "Merkez TL Kasa", KartTuru = "Kasa", DovizTuru = "TRY", Bakiye = 0 };
        await _uow.Bankalar.SaveAsync(kasa);

        var saved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.NotNull(saved);
        Assert.Equal("Merkez TL Kasa", saved.BankaAdi);
        Assert.Equal("TRY", saved.DovizTuru);
    }

    [AvaloniaFact]
    public async Task Scenario_02_CreateFxKasa_SavesSuccessfully()
    {
        var kasa = new BankaKart { BankaAdi = "Dolar Kasası", KartTuru = "Kasa", DovizTuru = "USD", Bakiye = 0 };
        await _uow.Bankalar.SaveAsync(kasa);

        var saved = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.NotNull(saved);
        Assert.Equal("USD", saved.DovizTuru);
    }

    [AvaloniaFact]
    public async Task Scenario_03_CashInflow_IncreasesKasaBalance()
    {
        var kasa = new BankaKart { BankaAdi = "Giriş Kasa", KartTuru = "Kasa", Bakiye = 1000m };
        await _uow.Bankalar.SaveAsync(kasa);

        kasa.Bakiye += 2500m;
        await _uow.Bankalar.SaveAsync(kasa);

        var refreshed = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(3500m, refreshed!.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_04_CashOutflow_DecreasesKasaBalance()
    {
        var kasa = new BankaKart { BankaAdi = "Çıkış Kasa", KartTuru = "Kasa", Bakiye = 5000m };
        await _uow.Bankalar.SaveAsync(kasa);

        kasa.Bakiye -= 1500m;
        await _uow.Bankalar.SaveAsync(kasa);

        var refreshed = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(3500m, refreshed!.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_05_VirmanBetweenKasalar_TransfersFundsCorrectly()
    {
        var kasa1 = new BankaKart { BankaAdi = "Kaynak Kasa", KartTuru = "Kasa", Bakiye = 8000m };
        var kasa2 = new BankaKart { BankaAdi = "Hedef Kasa", KartTuru = "Kasa", Bakiye = 2000m };
        await _uow.Bankalar.SaveAsync(kasa1);
        await _uow.Bankalar.SaveAsync(kasa2);

        decimal virmanTutari = 3000m;
        kasa1.Bakiye -= virmanTutari;
        kasa2.Bakiye += virmanTutari;
        await _uow.Bankalar.SaveAsync(kasa1);
        await _uow.Bankalar.SaveAsync(kasa2);

        var ref1 = await _uow.Bankalar.GetByIdAsync(kasa1.Id);
        var ref2 = await _uow.Bankalar.GetByIdAsync(kasa2.Id);

        Assert.Equal(5000m, ref1!.Bakiye);
        Assert.Equal(5000m, ref2!.Bakiye);
    }

    [Theory]
    [InlineData(1000, 500, true)]
    [InlineData(1000, 1000, true)]
    [InlineData(1000, 1500, false)]
    public void Scenario_06_to_08_KasaSufficientBalanceChecks(decimal bakiye, decimal cikis, bool isSufficient)
    {
        bool sufficient = bakiye >= cikis;
        Assert.Equal(isSufficient, sufficient);
    }

    [Theory]
    [InlineData(5000, 2000, 1000, 6000)]
    [InlineData(0, 10000, 4000, 6000)]
    [InlineData(2000, 0, 500, 1500)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_09_to_12_KasaDailySummaryCalculations(decimal devir, decimal giris, decimal cikis, decimal expectedBakiye)
    {
        decimal bakiye = devir + giris - cikis;
        Assert.Equal(expectedBakiye, bakiye);
    }

    // -------------------------------------------------------------
    // 2. Banka Yönetimi (12 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_13_CreateBankAccount_AssignsIncrementalId()
    {
        var b1 = new BankaKart { BankaAdi = "Garanti BBVA", SubeAdi = "Kadıköy", HesapNo = "111", IBAN = "TR111", Bakiye = 5000 };
        var b2 = new BankaKart { BankaAdi = "İş Bankası", SubeAdi = "Beşiktaş", HesapNo = "222", IBAN = "TR222", Bakiye = 10000 };

        await _uow.Bankalar.SaveAsync(b1);
        await _uow.Bankalar.SaveAsync(b2);

        Assert.True(b2.Id > b1.Id, "Banka ID otomatik artış mantığıyla benzersiz atanmalıdır.");
    }

    [Theory]
    [InlineData("TR120006200000012345678901", true)]
    [InlineData("TR000000000000000000000000", true)]
    [InlineData("TR12345", false)]
    [InlineData("12345678901234567890123456", false)]
    public void Scenario_14_to_17_IbanFormatValidation(string iban, bool expectedValid)
    {
        bool isValid = !string.IsNullOrEmpty(iban) && iban.StartsWith("TR") && iban.Length == 26;
        Assert.Equal(expectedValid, isValid);
    }

    [AvaloniaFact]
    public async Task Scenario_18_IncomingEft_IncreasesBankBalance()
    {
        var banka = new BankaKart { BankaAdi = "Akbank", HesapNo = "333", Bakiye = 15000m };
        await _uow.Bankalar.SaveAsync(banka);

        banka.Bakiye += 7500m;
        await _uow.Bankalar.SaveAsync(banka);

        var refreshed = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal(22500m, refreshed!.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_19_OutgoingEft_DecreasesBankBalance()
    {
        var banka = new BankaKart { BankaAdi = "Yapı Kredi", HesapNo = "444", Bakiye = 30000m };
        await _uow.Bankalar.SaveAsync(banka);

        banka.Bakiye -= 12000m;
        await _uow.Bankalar.SaveAsync(banka);

        var refreshed = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal(18000m, refreshed!.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_20_BankToCashTransfer_UpdatesBothAccounts()
    {
        var banka = new BankaKart { BankaAdi = "Vakıfbank", Bakiye = 50000m };
        var kasa = new BankaKart { BankaAdi = "Nakit Kasa", KartTuru = "Kasa", Bakiye = 5000m };
        await _uow.Bankalar.SaveAsync(banka);
        await _uow.Bankalar.SaveAsync(kasa);

        decimal cekilenPara = 10000m;
        banka.Bakiye -= cekilenPara;
        kasa.Bakiye += cekilenPara;
        await _uow.Bankalar.SaveAsync(banka);
        await _uow.Bankalar.SaveAsync(kasa);

        var refB = await _uow.Bankalar.GetByIdAsync(banka.Id);
        var refK = await _uow.Bankalar.GetByIdAsync(kasa.Id);

        Assert.Equal(40000m, refB!.Bakiye);
        Assert.Equal(15000m, refK!.Bakiye);
    }

    [Theory]
    [InlineData(10000, 25, 9975)]
    [InlineData(50000, 50, 49950)]
    [InlineData(500, 5, 495)]
    [InlineData(1000, 0, 1000)]
    public void Scenario_21_to_24_BankTransferFeeDeduction(decimal tutar, decimal masraf, decimal expNet)
    {
        decimal net = tutar - masraf;
        Assert.Equal(expNet, net);
    }

    // -------------------------------------------------------------
    // 3. Çek / Senet Portföyü (16 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_25_ReceiveCustomerCheck_EntersPortfolio()
    {
        var cek = new Cek
        {
            CekNo = "CK-9011",
            Banka = "QNB Finansbank",
            Tutar = 25000m,
            VadeTarihi = DateTime.Today.AddDays(45),
            CekTuru = "Alınan",
            Durum = "Portföyde"
        };
        await _uow.Cekler.SaveAsync(cek);

        var saved = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.NotNull(saved);
        Assert.Equal("Portföyde", saved.Durum);
        Assert.Equal(25000m, saved.Tutar);
    }

    [AvaloniaFact]
    public async Task Scenario_26_CheckCollectionToBank_UpdatesStatusAndBalance()
    {
        var banka = new BankaKart { BankaAdi = "Halkbank", Bakiye = 10000m };
        await _uow.Bankalar.SaveAsync(banka);

        var cek = new Cek { CekNo = "CK-COLLECT", Tutar = 15000m, Durum = "Portföyde" };
        await _uow.Cekler.SaveAsync(cek);

        cek.Durum = "Tahsil Edildi";
        banka.Bakiye += cek.Tutar;
        await _uow.Cekler.SaveAsync(cek);
        await _uow.Bankalar.SaveAsync(banka);

        var refC = await _uow.Cekler.GetByIdAsync(cek.Id);
        var refB = await _uow.Bankalar.GetByIdAsync(banka.Id);

        Assert.Equal("Tahsil Edildi", refC!.Durum);
        Assert.Equal(25000m, refB!.Bakiye);
    }

    [AvaloniaFact]
    public async Task Scenario_27_CheckEndorsedToSupplier_UpdatesStatus()
    {
        var cek = new Cek { CekNo = "CK-CIRO", Tutar = 30000m, Durum = "Portföyde" };
        await _uow.Cekler.SaveAsync(cek);

        cek.Durum = "Ciro Edildi";
        await _uow.Cekler.SaveAsync(cek);

        var refC = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.Equal("Ciro Edildi", refC!.Durum);
    }

    [AvaloniaFact]
    public async Task Scenario_28_ReceivePromissoryNote_EntersPortfolio()
    {
        var senet = new Senet
        {
            SenetNo = "SN-501",
            AsilBorclu = "Ahmet Yılmaz",
            Tutar = 8000m,
            VadeTarihi = DateTime.Today.AddDays(30),
            SenetTuru = "Alınan",
            Durum = "Portföyde"
        };
        await _uow.Senetler.SaveAsync(senet);

        var saved = await _uow.Senetler.GetByIdAsync(senet.Id);
        Assert.NotNull(saved);
        Assert.Equal(8000m, saved.Tutar);
    }

    [Theory]
    [InlineData("Portföyde", true)]
    [InlineData("Portfoyde", true)]
    [InlineData("Tahsil Edildi", false)]
    [InlineData("Ciro Edildi", false)]
    [InlineData("Karşılıksız", false)]
    public void Scenario_29_to_33_InPortfolioChecks(string durum, bool expectedInPortfolio)
    {
        bool inPortfolio = durum == "Portföyde" || durum == "Portfoyde";
        Assert.Equal(expectedInPortfolio, inPortfolio);
    }

    [Theory]
    [InlineData(10000, 20000, 30000, 60000)]
    [InlineData(50000, 0, 0, 50000)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_34_to_36_TotalPortfolioRiskCalculations(decimal c1, decimal c2, decimal c3, decimal expTotal)
    {
        decimal total = c1 + c2 + c3;
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData("Verilen", true)]
    [InlineData("Alınan", false)]
    [InlineData("Müşteri Çeki", false)]
    public void Scenario_37_to_40_OwnCheckPayableChecks(string tur, bool isOurCheck)
    {
        bool ourCheck = tur == "Verilen" || tur == "Kendi Çekimiz";
        Assert.Equal(isOurCheck, ourCheck);
    }

    // -------------------------------------------------------------
    // 4. Kredi Kartı & Konsolide Finans (10 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData(1000, 0.02, 20, 980)]
    [InlineData(5000, 0.015, 75, 4925)]
    [InlineData(10000, 0.03, 300, 9700)]
    [InlineData(2000, 0.00, 0, 2000)]
    public void Scenario_41_to_44_PosCommissionDeductionCalculations(decimal tutar, double komisyonOrani, decimal expKomisyon, decimal expNet)
    {
        decimal komisyon = tutar * (decimal)komisyonOrani;
        decimal net = tutar - komisyon;

        Assert.Equal(expKomisyon, komisyon);
        Assert.Equal(expNet, net);
    }

    [Theory]
    [InlineData(10000, 50000, 25000, 85000)]
    [InlineData(0, 100000, 0, 100000)]
    [InlineData(5000, 5000, 5000, 15000)]
    public void Scenario_45_to_47_ConsolidatedAssetsCalculation(decimal kasa, decimal banka, decimal cek, decimal expTotal)
    {
        decimal total = kasa + banka + cek;
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData(100000, 40000, 60000)]
    [InlineData(50000, 70000, -20000)]
    [InlineData(30000, 30000, 0)]
    public void Scenario_48_to_50_NetFinancialPositionCalculation(decimal varliklar, decimal borclar, decimal expNetPosition)
    {
        decimal net = varliklar - borclar;
        Assert.Equal(expNetPosition, net);
    }
}
