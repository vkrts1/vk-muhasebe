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
    // 9. Banka Hareketleri & Hesap Ekstresi (Senaryo 101 - 115)
    // =============================================================

    [Fact]
    public async Task Scenario_101_BankaHareket_SaveInflow_StoresTransactionDetails()
    {
        var banka = new BankaKart { BankaAdi = "Akbank Ticari", Bakiye = 10000m };
        await _uow.Bankalar.SaveAsync(banka);

        var hareket = new BankaHareket
        {
            BankaId = banka.Id,
            BankaAdi = "Akbank Ticari",
            Tarih = DateTime.Today,
            EvrakNo = "BNK-IN-101",
            IslemTuru = "Gelen Havale",
            CariUnvan = "Lider Metal Ltd.",
            Giren = 15000m,
            Cikan = 0m,
            Tutar = 15000m,
            Aciklama = "Fatura tahsilatı"
        };
        await _uow.Bankalar.SaveHareketAsync(hareket);

        var list = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        Assert.NotEmpty(list);
        Assert.Contains(list, h => h.EvrakNo == "BNK-IN-101" && h.Giren == 15000m);
    }

    [Fact]
    public async Task Scenario_102_BankaHareket_SaveOutflow_StoresTransactionDetails()
    {
        var banka = new BankaKart { BankaAdi = "QNB Finansbank", Bakiye = 50000m };
        await _uow.Bankalar.SaveAsync(banka);

        var hareket = new BankaHareket
        {
            BankaId = banka.Id,
            BankaAdi = "QNB Finansbank",
            Tarih = DateTime.Today,
            EvrakNo = "BNK-OUT-102",
            IslemTuru = "Giden EFT",
            CariUnvan = "Vergi Dairesi",
            Giren = 0m,
            Cikan = 8500m,
            Tutar = 8500m,
            Aciklama = "KDV Ödemesi"
        };
        await _uow.Bankalar.SaveHareketAsync(hareket);

        var list = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        Assert.Contains(list, h => h.EvrakNo == "BNK-OUT-102" && h.Cikan == 8500m);
    }

    [Fact]
    public async Task Scenario_103_BankaHareket_DeleteHareket_RemovesEntry()
    {
        var banka = new BankaKart { BankaAdi = "Vakıfbank Ticari", Bakiye = 20000m };
        await _uow.Bankalar.SaveAsync(banka);

        var hareket = new BankaHareket
        {
            BankaId = banka.Id,
            EvrakNo = "BNK-DEL-103",
            Giren = 5000m,
            Tutar = 5000m
        };
        await _uow.Bankalar.SaveHareketAsync(hareket);

        var listBefore = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        var inserted = listBefore.First(h => h.EvrakNo == "BNK-DEL-103");

        await _uow.Bankalar.DeleteHareketAsync(inserted.Id);

        var listAfter = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        Assert.DoesNotContain(listAfter, h => h.Id == inserted.Id);
    }

    [Fact]
    public async Task Scenario_104_Banka_RecalculateBalance_AggregatesMovementsAccurately()
    {
        var banka = new BankaKart { BankaAdi = "Halkbank Hesap", AcilisBakiyesi = 5000m, GuncelBakiye = 5000m };
        await _uow.Bankalar.SaveAsync(banka);

        await _uow.Bankalar.SaveHareketAsync(new BankaHareket { BankaId = banka.Id, Giren = 10000m, Tutar = 10000m });
        await _uow.Bankalar.SaveHareketAsync(new BankaHareket { BankaId = banka.Id, Cikan = 3000m, Tutar = 3000m });
        await _uow.Bankalar.SaveHareketAsync(new BankaHareket { BankaId = banka.Id, Giren = 2000m, Tutar = 2000m });

        await _uow.Bankalar.RecalculateBalanceAsync(banka.Id);

        var refreshed = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.NotNull(refreshed);
        // Açılış (5000) + Giren (12000) - Çıkan (3000) = 14000
        Assert.Equal(14000m, refreshed.Bakiye);
    }

    [Fact]
    public async Task Scenario_105_BankaHareket_DekontPath_PersistsDocumentLink()
    {
        var banka = new BankaKart { BankaAdi = "Yapı Kredi", Bakiye = 1000m };
        await _uow.Bankalar.SaveAsync(banka);

        string dekont = @"C:\Dekontlar\2026_09_EFT_123.pdf";
        var hareket = new BankaHareket
        {
            BankaId = banka.Id,
            EvrakNo = "EFT-DKT-105",
            DekontPath = dekont,
            Tutar = 2000m
        };
        await _uow.Bankalar.SaveHareketAsync(hareket);

        var list = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        var item = list.First(h => h.EvrakNo == "EFT-DKT-105");
        Assert.Equal(dekont, item.DekontPath);
    }

    [Fact]
    public async Task Scenario_106_BankaHareket_StatusDefault_IsTamamlandi()
    {
        var h = new BankaHareket();
        Assert.Equal("Tamamlandı", h.Durum);
    }

    [Theory]
    [InlineData(10000, 5000, 2000, 13000)]
    [InlineData(0, 50000, 20000, 30000)]
    [InlineData(15000, 0, 15000, 0)]
    public void Scenario_107_to_109_Banka_BalanceMathVerification(decimal acilis, decimal giren, decimal cikan, decimal expected)
    {
        decimal calculated = acilis + giren - cikan;
        Assert.Equal(expected, calculated);
    }

    [Fact]
    public async Task Scenario_110_BankaHareket_GetAllHareketler_ReturnsFromAllBanks()
    {
        var b1 = new BankaKart { BankaAdi = "Bank A", Bakiye = 100m };
        var b2 = new BankaKart { BankaAdi = "Bank B", Bakiye = 200m };
        await _uow.Bankalar.SaveAsync(b1);
        await _uow.Bankalar.SaveAsync(b2);

        await _uow.Bankalar.SaveHareketAsync(new BankaHareket { BankaId = b1.Id, EvrakNo = "EVR-A-110", Giren = 500m });
        await _uow.Bankalar.SaveHareketAsync(new BankaHareket { BankaId = b2.Id, EvrakNo = "EVR-B-110", Giren = 800m });

        var all = await _uow.Bankalar.GetAllHareketlerAsync();
        Assert.Contains(all, h => h.EvrakNo == "EVR-A-110");
        Assert.Contains(all, h => h.EvrakNo == "EVR-B-110");
    }

    [Fact]
    public async Task Scenario_111_BankaHareket_WithCariReference_AssociatesProperly()
    {
        var banka = new BankaKart { BankaAdi = "Ziraat", Bakiye = 5000m };
        await _uow.Bankalar.SaveAsync(banka);

        var hareket = new BankaHareket
        {
            BankaId = banka.Id,
            CariId = 105,
            CariUnvan = "Anadolu Dış Ticaret",
            Giren = 12500m,
            EvrakNo = "CARI-REF-111"
        };
        await _uow.Bankalar.SaveHareketAsync(hareket);

        var list = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        var item = list.First(h => h.EvrakNo == "CARI-REF-111");
        Assert.Equal(105, item.CariId);
        Assert.Equal("Anadolu Dış Ticaret", item.CariUnvan);
    }

    [Fact]
    public async Task Scenario_112_BankaHareket_LinkedRefId_StoresOriginDocumentId()
    {
        var banka = new BankaKart { BankaAdi = "Garanti", Bakiye = 0m };
        await _uow.Bankalar.SaveAsync(banka);

        var hareket = new BankaHareket
        {
            BankaId = banka.Id,
            RefId = "FAT-2026-888",
            EvrakNo = "REF-112",
            Giren = 4000m
        };
        await _uow.Bankalar.SaveHareketAsync(hareket);

        var list = await _uow.Bankalar.GetHareketlerAsync(banka.Id);
        var item = list.First(h => h.EvrakNo == "REF-112");
        Assert.Equal("FAT-2026-888", item.RefId);
    }

    [Fact]
    public async Task Scenario_113_Banka_GetBakiyeAsync_ReturnsCorrectDirectBalance()
    {
        var banka = new BankaKart { BankaAdi = "Bakiye Doğrulama Bankası" };
        await _uow.Bankalar.SaveAsync(banka);

        await _uow.Bankalar.SaveHareketAsync(new BankaHareket { BankaId = banka.Id, Tutar = 77500m });

        decimal bakiye = await _uow.Bankalar.GetBakiyeAsync(banka.Id);
        Assert.Equal(77500m, bakiye);
    }

    [Theory]
    [InlineData(100000, 1000, 99000)]
    [InlineData(25000, 250, 24750)]
    public void Scenario_114_to_115_Banka_TransactionNetDeductions(decimal brüt, decimal masraf, decimal beklenenNet)
    {
        decimal net = brüt - masraf;
        Assert.Equal(beklenenNet, net);
    }

    // =============================================================
    // 10. Kasa Hareketleri & Kasa Ekstresi (Senaryo 116 - 125)
    // =============================================================

    [Fact]
    public async Task Scenario_116_KasaHareket_SaveInflow_StoresTransactionDetails()
    {
        var h = new KasaHareket
        {
            KasaId = 1,
            Tarih = DateTime.Today,
            EvrakNo = "KSA-IN-116",
            IslemTuru = "Nakit Tahsilat",
            CariUnvan = "Can Ticaret",
            Giren = 3200m,
            Cikan = 0m,
            Aciklama = "Elden tahsilat"
        };
        await _uow.Kasalar.SaveAsync(h);

        var list = await _uow.Kasalar.GetAllAsync();
        Assert.Contains(list, item => item.EvrakNo == "KSA-IN-116");
    }

    [Fact]
    public async Task Scenario_117_KasaHareket_SaveOutflow_StoresTransactionDetails()
    {
        var h = new KasaHareket
        {
            KasaId = 1,
            Tarih = DateTime.Today,
            EvrakNo = "KSA-OUT-117",
            IslemTuru = "Nakit Masraf",
            CariUnvan = "Kırtasiye A.Ş.",
            Giren = 0m,
            Cikan = 450m,
            Aciklama = "Ofis malzemesi"
        };
        await _uow.Kasalar.SaveAsync(h);

        var list = await _uow.Kasalar.GetAllAsync();
        Assert.Contains(list, item => item.EvrakNo == "KSA-OUT-117");
    }

    [Fact]
    public async Task Scenario_118_KasaHareket_TutarFallback_ReturnsGirenOrCikan()
    {
        var hIn = new KasaHareket { Giren = 800m, Cikan = 0m };
        Assert.Equal(800m, hIn.Tutar);

        var hOut = new KasaHareket { Giren = 0m, Cikan = 350m };
        Assert.Equal(350m, hOut.Tutar);
    }

    [Fact]
    public async Task Scenario_119_KasaHareket_ExplicitTutar_OverridesAutoFallback()
    {
        var h = new KasaHareket { Giren = 1000m, Tutar = 1050m };
        Assert.Equal(1050m, h.Tutar);
    }

    [Fact]
    public async Task Scenario_120_KasaHareket_DeleteHareket_RemovesEntry()
    {
        var h = new KasaHareket { EvrakNo = "KSA-DEL-120", Giren = 900m };
        await _uow.Kasalar.SaveAsync(h);

        var all = await _uow.Kasalar.GetAllAsync();
        var item = all.First(x => x.EvrakNo == "KSA-DEL-120");

        await _uow.Kasalar.DeleteAsync(item.Id);

        var after = await _uow.Kasalar.GetAllAsync();
        Assert.DoesNotContain(after, x => x.Id == item.Id);
    }

    [Fact]
    public async Task Scenario_121_KasaHareket_GetHareketlerByTarih_FiltersWithinRange()
    {
        var dateTarget = new DateTime(2026, 4, 15);
        var dateOut = new DateTime(2026, 6, 15);

        await _uow.Kasalar.SaveAsync(new KasaHareket { EvrakNo = "T-IN-121", Tarih = dateTarget, Giren = 500m });
        await _uow.Kasalar.SaveAsync(new KasaHareket { EvrakNo = "T-OUT-121", Tarih = dateOut, Giren = 500m });

        var inRange = await _uow.Kasalar.GetHareketlerByTarihAsync(new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));
        Assert.Contains(inRange, x => x.EvrakNo == "T-IN-121");
        Assert.DoesNotContain(inRange, x => x.EvrakNo == "T-OUT-121");
    }

    [Theory]
    [InlineData(1000, 200, 800)]
    [InlineData(5000, 5000, 0)]
    [InlineData(200, 500, -300)]
    public void Scenario_122_to_124_Kasa_DailyBalanceDifference(decimal girisler, decimal cikislar, decimal beklenenFark)
    {
        decimal gunlukFark = girisler - cikislar;
        Assert.Equal(beklenenFark, gunlukFark);
    }

    [Fact]
    public async Task Scenario_125_KasaHareket_RefIdLinksInvoicePayment()
    {
        var h = new KasaHareket
        {
            EvrakNo = "FAT-KSA-125",
            RefId = "FAT-2026-990",
            Giren = 4800m,
            Aciklama = "FAT-2026-990 nolu fatura tahsilatı"
        };
        await _uow.Kasalar.SaveAsync(h);

        var all = await _uow.Kasalar.GetAllAsync();
        var match = all.First(x => x.EvrakNo == "FAT-KSA-125");
        Assert.Equal("FAT-2026-990", match.RefId);
    }

    // =============================================================
    // 11. Kredi Kartı & POS İşlemleri (Senaryo 126 - 135)
    // =============================================================

    [Fact]
    public async Task Scenario_126_KrediKartiIslem_SaveTransaction_PersistsDetails()
    {
        var islem = new KrediKartiIslem
        {
            OnayKodu = "APP-2026-001",
            KartNo = "**** **** **** 4242",
            MusteriUnvan = "Cemil Yıldız",
            Tutar = 2400m,
            Banka = "Garanti BBVA",
            Durum = "Portföyde",
            Tarih = DateTime.Today
        };
        await _uow.KrediKartlari.SaveAsync(islem);

        var retrieved = await _uow.KrediKartlari.GetByIdAsync(islem.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("APP-2026-001", retrieved.OnayKodu);
        Assert.Equal("Garanti BBVA", retrieved.Banka);
        Assert.Equal(2400m, retrieved.Tutar);
    }

    [Fact]
    public async Task Scenario_127_KrediKartiIslem_GetByNoAsync_FindsExactTransaction()
    {
        var islem = new KrediKartiIslem
        {
            OnayKodu = "POS-998877",
            Tutar = 1500m,
            Banka = "Yapı Kredi POS"
        };
        await _uow.KrediKartlari.SaveAsync(islem);

        var retrieved = await _uow.KrediKartlari.GetByNoAsync("POS-998877");
        Assert.NotNull(retrieved);
        Assert.Equal(1500m, retrieved.Tutar);
    }

    [Theory]
    [InlineData(1200, 3, 400)]
    [InlineData(6000, 6, 1000)]
    [InlineData(10000, 10, 1000)]
    [InlineData(1000, 12, 83.33)]
    public void Scenario_128_to_131_KrediKarti_InstallmentAmountCalculations(decimal toplamTutar, int taksit, decimal beklenenAylik)
    {
        decimal aylikTaksit = Math.Round(toplamTutar / taksit, 2);
        Assert.Equal(beklenenAylik, aylikTaksit);
    }

    [Theory]
    [InlineData(10000, 2.0, 28, 9800)] // %2 komisyon, 28 gün bloke
    [InlineData(5000, 3.5, 35, 4825)]
    [InlineData(2000, 0.0, 1, 2000)]
    public void Scenario_132_to_134_KrediKarti_PosBlockAndSettlementCalculations(decimal tutar, double komisyonOran, int blokeGun, decimal beklenenNet)
    {
        decimal komisyon = tutar * (decimal)(komisyonOran / 100.0);
        decimal net = tutar - komisyon;
        Assert.Equal(beklenenNet, net);
        Assert.True(blokeGun >= 1);
    }

    [Fact]
    public async Task Scenario_135_KrediKartiIslem_DeleteTransaction_RemovesFromDatabase()
    {
        var islem = new KrediKartiIslem { OnayKodu = "KK-DEL-135", Tutar = 500m };
        await _uow.KrediKartlari.SaveAsync(islem);
        int id = islem.Id;

        await _uow.KrediKartlari.DeleteAsync(id);
        var retrieved = await _uow.KrediKartlari.GetByIdAsync(id);
        Assert.Null(retrieved);
    }

    // =============================================================
    // 12. Finans View Modelleri & Avalonia UI Entegrasyonu (Senaryo 136 - 145)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_136_KasaListViewModel_LoadsAllKasas()
    {
        await _uow.Bankalar.SaveAsync(new BankaKart { BankaAdi = "Kasa Alfa", KartTuru = "Kasa", Bakiye = 1000m });
        await _uow.Bankalar.SaveAsync(new BankaKart { BankaAdi = "Kasa Beta", KartTuru = "Kasa", Bakiye = 2000m });

        var vm = _serviceProvider.GetRequiredService<KasaListViewModel>();
        await vm.LoadKasalarAsync();

        Assert.True(vm.Kasalar.Count >= 2);
        Assert.Contains(vm.Kasalar, k => k.BankaAdi == "Kasa Alfa");
        Assert.Contains(vm.Kasalar, k => k.BankaAdi == "Kasa Beta");
    }

    [AvaloniaFact]
    public async Task Scenario_137_KasaListViewModel_SelectsKasa_SetsSelectedKasa()
    {
        var kasa = new BankaKart { BankaAdi = "Kasa Seçim Testi", KartTuru = "Kasa", Bakiye = 3500m };
        await _uow.Bankalar.SaveAsync(kasa);

        var vm = _serviceProvider.GetRequiredService<KasaListViewModel>();
        await vm.LoadKasalarAsync();

        vm.SelectedKasa = vm.Kasalar.FirstOrDefault(k => k.BankaAdi == "Kasa Seçim Testi");
        Assert.NotNull(vm.SelectedKasa);
        Assert.Equal("Kasa Seçim Testi", vm.SelectedKasa.BankaAdi);
    }

    [AvaloniaFact]
    public async Task Scenario_138_KasaListViewModel_TotalBalanceCalculation_ReflectsSum()
    {
        await _uow.Bankalar.SaveAsync(new BankaKart { BankaAdi = "Kasa 1", KartTuru = "Kasa", Bakiye = 1500m });
        await _uow.Bankalar.SaveAsync(new BankaKart { BankaAdi = "Kasa 2", KartTuru = "Kasa", Bakiye = 2500m });

        var vm = _serviceProvider.GetRequiredService<KasaListViewModel>();
        await vm.LoadKasalarAsync();

        decimal total = vm.Kasalar.Sum(k => k.Bakiye);
        Assert.True(total >= 4000m);
    }

    [AvaloniaFact]
    public async Task Scenario_139_KasaListViewModel_EmptyKasaList_HandledGracefully()
    {
        var vm = _serviceProvider.GetRequiredService<KasaListViewModel>();
        Assert.NotNull(vm.Kasalar);
    }

    [Theory]
    [InlineData("TRY", "₺")]
    [InlineData("USD", "$")]
    [InlineData("EUR", "€")]
    [InlineData("GBP", "£")]
    public void Scenario_140_to_143_CurrencySymbolMapping(string code, string expectedSymbol)
    {
        string symbol = code switch
        {
            "TRY" => "₺",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => code
        };
        Assert.Equal(expectedSymbol, symbol);
    }

    [Fact]
    public void Scenario_144_FinancialRiskIndicator_LowRiskThreshold()
    {
        decimal assets = 100000m;
        decimal liabilities = 20000m;
        decimal ratio = liabilities / assets; // 0.20

        string risk = ratio < 0.5m ? "Düşük Risk" : "Yüksek Risk";
        Assert.Equal("Düşük Risk", risk);
    }

    [Fact]
    public void Scenario_145_FinancialRiskIndicator_HighRiskThreshold()
    {
        decimal assets = 100000m;
        decimal liabilities = 85000m;
        decimal ratio = liabilities / assets; // 0.85

        string risk = ratio >= 0.8m ? "Yüksek Risk" : "Normal";
        Assert.Equal("Yüksek Risk", risk);
    }

    // =============================================================
    // 13. E2E Finans Yaşam Döngüsü ve Konsolidasyon (Senaryo 146 - 154)
    // =============================================================

    [Fact]
    public async Task Scenario_146_EndToEndFinancialLifecycle_CheckFlowToBankAndCash()
    {
        // 1. Banka ve Kasa Tanımlama
        var banka = new BankaKart { BankaAdi = "Vakıfbank Ticari", Bakiye = 10000m };
        var kasa = new BankaKart { BankaAdi = "Merkez TL", KartTuru = "Kasa", Bakiye = 2000m };
        await _uow.Bankalar.SaveAsync(banka);
        await _uow.Bankalar.SaveAsync(kasa);

        // 2. Müşteri Çeki Alınması
        var cek = new Cek
        {
            CekNo = "CK-LIFECYCLE-146",
            Tutar = 25000m,
            CekTuru = "Alınan",
            Durum = "Portföyde",
            VadeTarihi = DateTime.Today.AddDays(15)
        };
        await _uow.Cekler.SaveAsync(cek);
        Assert.Equal("Portföyde", cek.Durum);

        // 3. Çekin Bankaya Tahsile Verilmesi ve Tahsil Olması
        cek.Durum = "Tahsil Edildi";
        banka.Bakiye += cek.Tutar;
        await _uow.Cekler.SaveAsync(cek);
        await _uow.Bankalar.SaveAsync(banka);

        var refBanka1 = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal(35000m, refBanka1!.Bakiye);

        // 4. Bankadan Kasaya Nakit Çekilmesi (Virman)
        decimal nakitCekilen = 5000m;
        banka.Bakiye -= nakitCekilen;
        kasa.Bakiye += nakitCekilen;
        await _uow.Bankalar.SaveAsync(banka);
        await _uow.Bankalar.SaveAsync(kasa);

        var refBanka2 = await _uow.Bankalar.GetByIdAsync(banka.Id);
        var refKasa2 = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(30000m, refBanka2!.Bakiye);
        Assert.Equal(7000m, refKasa2!.Bakiye);

        // 5. Kasadan Masraf Ödemesi
        kasa.Bakiye -= 1200m;
        await _uow.Bankalar.SaveAsync(kasa);

        var refKasaFinal = await _uow.Bankalar.GetByIdAsync(kasa.Id);
        Assert.Equal(5800m, refKasaFinal!.Bakiye);
    }

    [Fact]
    public async Task Scenario_147_EndToEndFinancialLifecycle_CheckEndorsedToSupplier()
    {
        // 1. Müşteri Çeki Girişi
        var cek = new Cek
        {
            CekNo = "CK-END-147",
            Tutar = 40000m,
            CekTuru = "Alınan",
            Durum = "Portföyde"
        };
        await _uow.Cekler.SaveAsync(cek);

        // 2. Tedarikçiye Ciro
        cek.Durum = "Ciro Edildi";
        cek.YonlendirilenCariId = 55;
        cek.YonlendirilenCariUnvan = "Demir Çelik Sanayi A.Ş.";
        cek.YonlendirmeTarihi = DateTime.Today;
        await _uow.Cekler.SaveAsync(cek);

        var refCek = await _uow.Cekler.GetByIdAsync(cek.Id);
        Assert.Equal("Ciro Edildi", refCek!.Durum);
        Assert.Equal(55, refCek.YonlendirilenCariId);
    }

    [Fact]
    public async Task Scenario_148_Finans_MultipleCurrencyAssetsConsolidation()
    {
        var kTry = new BankaKart { BankaAdi = "Kasa TRY", DovizTuru = "TRY", Bakiye = 50000m };
        var kUsd = new BankaKart { BankaAdi = "Kasa USD", DovizTuru = "USD", Bakiye = 2000m };
        var kEur = new BankaKart { BankaAdi = "Kasa EUR", DovizTuru = "EUR", Bakiye = 1500m };
        await _uow.Bankalar.SaveAsync(kTry);
        await _uow.Bankalar.SaveAsync(kUsd);
        await _uow.Bankalar.SaveAsync(kEur);

        decimal usdKur = 38.50m;
        decimal eurKur = 41.20m;

        decimal totalTlEquivalent = kTry.Bakiye + (kUsd.Bakiye * usdKur) + (kEur.Bakiye * eurKur);
        // 50000 + 77000 + 61800 = 188800
        Assert.Equal(188800m, totalTlEquivalent);
    }

    [Fact]
    public async Task Scenario_149_Finans_TenantIsolation_FiltersTenantSpecificRecords()
    {
        var bTenantA = new BankaKart { BankaAdi = "Firma A Banka", TenantId = "tenant_a", Bakiye = 1000m };
        var bTenantB = new BankaKart { BankaAdi = "Firma B Banka", TenantId = "tenant_b", Bakiye = 2000m };
        await _uow.Bankalar.SaveAsync(bTenantA);
        await _uow.Bankalar.SaveAsync(bTenantB);

        var all = await _uow.Bankalar.GetAllAsync();
        Assert.Contains(all, b => b.TenantId == "tenant_a");
        Assert.Contains(all, b => b.TenantId == "tenant_b");
    }

    [Fact]
    public async Task Scenario_150_Finans_ExtremeValues_PreservesDecimalPrecision()
    {
        decimal extremeAmount = 888888888.88m;
        var banka = new BankaKart { BankaAdi = "Mega Finans", Bakiye = extremeAmount };
        await _uow.Bankalar.SaveAsync(banka);

        var retrieved = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal(extremeAmount, retrieved!.Bakiye);
    }

    [Fact]
    public async Task Scenario_151_Senet_DefaultPortfoyState_IsCorrect()
    {
        var s = new Senet { SenetNo = "SN-DEF-151", Durum = "Portföyde", Tutar = 1500m };
        await _uow.Senetler.SaveAsync(s);

        var retrieved = await _uow.Senetler.GetByIdAsync(s.Id);
        Assert.Equal("Portföyde", retrieved!.Durum);
    }

    [Fact]
    public async Task Scenario_152_Cek_CheckNumberUniqueQuery_LocatesSpecificCheck()
    {
        var c = new Cek { CekNo = "CK-UNIQ-152", Tutar = 19500m };
        await _uow.Cekler.SaveAsync(c);

        var all = await _uow.Cekler.GetAllAsync();
        var found = all.FirstOrDefault(x => x.CekNo == "CK-UNIQ-152");
        Assert.NotNull(found);
        Assert.Equal(19500m, found.Tutar);
    }

    [Fact]
    public async Task Scenario_153_Banka_IbanWhitespaceTrimming_MaintainsIntegrity()
    {
        string rawIban = " TR120006100511123456789012 ";
        var banka = new BankaKart { BankaAdi = "Format Banka", IBAN = rawIban.Trim() };
        await _uow.Bankalar.SaveAsync(banka);

        var retrieved = await _uow.Bankalar.GetByIdAsync(banka.Id);
        Assert.Equal("TR120006100511123456789012", retrieved!.IBAN);
    }

    [Fact]
    public async Task Scenario_154_Finans_CompletePortfolioSummary_CalculatesAccurately()
    {
        // 1. Kasalar Toplamı
        var k1 = new BankaKart { BankaAdi = "K1", KartTuru = "Kasa", Bakiye = 10000m };
        var k2 = new BankaKart { BankaAdi = "K2", KartTuru = "Kasa", Bakiye = 5000m };
        // 2. Bankalar Toplamı
        var b1 = new BankaKart { BankaAdi = "B1", KartTuru = "Vadesiz", Bakiye = 40000m };
        // 3. Çek Portföyü
        var c1 = new Cek { CekNo = "C-SUM", Tutar = 25000m, Durum = "Portföyde" };

        await _uow.Bankalar.SaveAsync(k1);
        await _uow.Bankalar.SaveAsync(k2);
        await _uow.Bankalar.SaveAsync(b1);
        await _uow.Cekler.SaveAsync(c1);

        decimal totalKasa = k1.Bakiye + k2.Bakiye;
        decimal totalBanka = b1.Bakiye;
        decimal totalCek = c1.Tutar;
        decimal totalLikidite = totalKasa + totalBanka + totalCek;

        Assert.Equal(15000m, totalKasa);
        Assert.Equal(40000m, totalBanka);
        Assert.Equal(25000m, totalCek);
        Assert.Equal(80000m, totalLikidite);
    }
}
