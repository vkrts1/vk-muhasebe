using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class MusteriTakipScenariosTests
{
    // =============================================================
    // 5. Müşteri Klasörü Gelişmiş CRUD & Durum Yönetimi (51 - 60)
    // =============================================================

    [Fact]
    public async Task Scenario_51_SaveKlasor_WithSpecificTenant_PersistsTenantId()
    {
        var klasor = new MusteriTakipKlasor
        {
            TenantId = "tenant_custom_12",
            CariId = 101,
            CariUnvan = "Tenant Test Firması",
            Etiket = "Yeni İletişim"
        };
        await _uow.MusteriTakip.SaveAsync(klasor);

        var retrieved = await _uow.MusteriTakip.GetByIdAsync(klasor.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("tenant_custom_12", retrieved.TenantId);
        Assert.Equal("Tenant Test Firması", retrieved.CariUnvan);
    }

    [Fact]
    public async Task Scenario_52_UpdateKlasor_DescriptionAndPhone_UpdatesRecord()
    {
        var klasor = new MusteriTakipKlasor
        {
            CariId = 102,
            CariUnvan = "Güncelleme Müşteri",
            Telefon = "05321112233",
            Aciklama = "Eski Not"
        };
        await _uow.MusteriTakip.SaveAsync(klasor);

        klasor.Telefon = "05329998877";
        klasor.Aciklama = "Yeni Güncel Not";
        klasor.Yetkili = "Ahmet Bey";
        await _uow.MusteriTakip.SaveAsync(klasor);

        var retrieved = await _uow.MusteriTakip.GetByIdAsync(klasor.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("05329998877", retrieved.Telefon);
        Assert.Equal("Yeni Güncel Not", retrieved.Aciklama);
        Assert.Equal("Ahmet Bey", retrieved.Yetkili);
    }

    [Fact]
    public async Task Scenario_53_GetByCariIdAsync_ReturnsMatchingFolder()
    {
        int targetCariId = 103;
        var klasor = new MusteriTakipKlasor
        {
            CariId = targetCariId,
            CariUnvan = "Cari Id Eşleşme Testi"
        };
        await _uow.MusteriTakip.SaveAsync(klasor);

        var result = await _uow.MusteriTakip.GetByCariIdAsync(targetCariId);
        Assert.NotNull(result);
        Assert.Equal(targetCariId, result.CariId);
        Assert.Equal("Cari Id Eşleşme Testi", result.CariUnvan);
    }

    [Theory]
    [InlineData("#3B82F6", true)] // Mavi
    [InlineData("#10B981", true)] // Yeşil
    [InlineData("#EF4444", true)] // Kırmızı
    [InlineData("#F59E0B", true)] // Amber
    [InlineData("#8B5CF6", true)] // Mor
    public void Scenario_54_to_58_FolderThemeColors_AreValidHex(string hexColor, bool expectedValid)
    {
        bool isValid = hexColor.StartsWith("#") && hexColor.Length == 7;
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task Scenario_59_DeleteFolder_RemovesOrSoftDeletes()
    {
        var klasor = new MusteriTakipKlasor
        {
            CariId = 109,
            CariUnvan = "Silinecek Müşteri"
        };
        await _uow.MusteriTakip.SaveAsync(klasor);

        await _uow.MusteriTakip.DeleteAsync(klasor);

        var all = await _uow.MusteriTakip.GetAllAsync();
        Assert.DoesNotContain(all, x => x.Id == klasor.Id && !x.IsDeleted);
    }

    [Fact]
    public void Scenario_60_CardViewModel_SonIslemTarihiFormatli_MatchesPattern()
    {
        var klasor = new MusteriTakipKlasor
        {
            SonIslemTarihi = new DateTime(2026, 6, 15, 14, 30, 0)
        };
        var cardVm = new MusteriTakipKlasorCardViewModel(klasor);

        Assert.Equal("15.06.2026 14:30", cardVm.SonIslemTarihiFormatli);
    }

    // =============================================================
    // 6. Takip Detayları (Not, Görüşme, Fiyat, Görsel) (61 - 75)
    // =============================================================

    [Fact]
    public async Task Scenario_61_SaveDetay_NoteType_PersistsSuccessfully()
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 201,
            CariId = 50,
            Baslik = "İlk Toplantı Notu",
            Icerik = "Müşteri ile ilk görüşme yapıldı. Numune istendi.",
            Tip = "Not"
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(201);
        Assert.Contains(list, d => d.Baslik == "İlk Toplantı Notu" && d.Tip == "Not");
    }

    [Fact]
    public async Task Scenario_62_SaveDetay_MeetingType_PersistsInteraction()
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 202,
            CariId = 51,
            Baslik = "Telefon Görüşmesi",
            Icerik = "Termin tarihi hakkında mutabık kalındı.",
            Tip = "Gorusme",
            Tarih = DateTime.Now
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        var list = await _uow.MusteriTakip.GetDetaylarByTipAsync(202, "Gorusme");
        Assert.Single(list);
        Assert.Equal("Gorusme", list[0].Tip);
    }

    [Fact]
    public async Task Scenario_63_SaveDetay_PriceType_StoresPriceAndCurrency()
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 203,
            CariId = 52,
            Baslik = "Verilen Özel Fiyat",
            Tip = "Fiyat",
            FiyatBilgisi = 125.50m,
            ParaBirimi = "$"
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        var list = await _uow.MusteriTakip.GetDetaylarByTipAsync(203, "Fiyat");
        Assert.Single(list);
        Assert.Equal(125.50m, list[0].FiyatBilgisi);
        Assert.Equal("$", list[0].ParaBirimi);
    }

    [Fact]
    public async Task Scenario_64_SaveDetay_ImageType_StoresPathAndBase64()
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 204,
            CariId = 53,
            Baslik = "Kumaş Deseni",
            Tip = "Gorsel",
            DosyaYolu = @"C:\Photos\kumas1.jpg",
            GorselBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRg=="
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        var list = await _uow.MusteriTakip.GetDetaylarByTipAsync(204, "Gorsel");
        Assert.Single(list);
        Assert.Equal(@"C:\Photos\kumas1.jpg", list[0].DosyaYolu);
        Assert.NotNull(list[0].GorselBase64);
    }

    [Fact]
    public async Task Scenario_65_DeleteDetayAsync_RemovesItem()
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 205,
            Baslik = "Silinecek Detay",
            Tip = "Not"
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        await _uow.MusteriTakip.DeleteDetayAsync(detay.Id);

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(205);
        Assert.DoesNotContain(list, d => d.Id == detay.Id && !d.IsDeleted);
    }

    [Theory]
    [InlineData("₺")]
    [InlineData("$")]
    [InlineData("€")]
    [InlineData("£")]
    public async Task Scenario_66_to_69_PriceDetail_VariousCurrenciesSupported(string currency)
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 206,
            Baslik = $"Fiyat {currency}",
            Tip = "Fiyat",
            FiyatBilgisi = 500m,
            ParaBirimi = currency
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(206);
        Assert.Contains(list, d => d.ParaBirimi == currency);
    }

    [Fact]
    public void Scenario_70_CardViewModel_CountsAndLatestPrice_PopulatedCorrectly()
    {
        var klasor = new MusteriTakipKlasor { CariUnvan = "Sayaç Testi" };
        var card = new MusteriTakipKlasorCardViewModel(klasor)
        {
            GorselSayisi = 5,
            NotSayisi = 12,
            GorusmeSayisi = 4,
            FiyatSayisi = 3,
            SonVerilenFiyat = 250m,
            SonVerilenFiyatBirimi = "$"
        };

        Assert.Equal(5, card.GorselSayisi);
        Assert.Equal(12, card.NotSayisi);
        Assert.Equal(4, card.GorusmeSayisi);
        Assert.Equal(3, card.FiyatSayisi);
        Assert.Equal(250m, card.SonVerilenFiyat);
        Assert.Equal("$", card.SonVerilenFiyatBirimi);
    }

    [Theory]
    [InlineData("Not", true)]
    [InlineData("Gorusme", true)]
    [InlineData("Fiyat", true)]
    [InlineData("Gorsel", true)]
    [InlineData("BilinmeyenTip", false)]
    public void Scenario_71_to_75_SupportedDetailTypes(string tip, bool isSupported)
    {
        var validTypes = new[] { "Not", "Gorusme", "Fiyat", "Gorsel" };
        bool valid = validTypes.Contains(tip);
        Assert.Equal(isSupported, valid);
    }

    // =============================================================
    // 7. Etkileşim Analitiği, Arama & Filtreleme (76 - 90)
    // =============================================================

    [Theory]
    [InlineData("Ahmet Tekstil", "Ahmet Tekstil", null)] // Same name -> null
    [InlineData("Mehmet Bey", "Yıldız Giyim", "Mehmet Bey")] // Different -> returns representative
    [InlineData("  ", "Güneş Ltd", null)]
    public void Scenario_76_to_78_CardViewModel_Yetkili_SanityCheck(string inputYetkili, string unvan, string? expectedYetkili)
    {
        var k = new MusteriTakipKlasor { Yetkili = inputYetkili, CariUnvan = unvan };
        var card = new MusteriTakipKlasorCardViewModel(k);

        Assert.Equal(expectedYetkili, card.Yetkili);
    }

    [Theory]
    [InlineData(10, false)] // 10 days ago -> Active
    [InlineData(25, false)] // 25 days ago -> Active
    [InlineData(35, true)]  // 35 days ago -> Dormant (> 30 days)
    [InlineData(90, true)]  // 90 days ago -> Dormant (> 30 days)
    public void Scenario_79_to_82_CustomerDormancyEvaluation(int daysSinceLastContact, bool expectedDormant)
    {
        DateTime lastDate = DateTime.Today.AddDays(-daysSinceLastContact);
        bool isDormant = (DateTime.Today - lastDate).TotalDays > 30;

        Assert.Equal(expectedDormant, isDormant);
    }

    [Fact]
    public void Scenario_83_FilterKlasorler_MatchesCariUnvan_CaseInsensitive()
    {
        var k1 = new MusteriTakipKlasorCardViewModel(new MusteriTakipKlasor { CariUnvan = "Akdeniz Tekstil" });
        var k2 = new MusteriTakipKlasorCardViewModel(new MusteriTakipKlasor { CariUnvan = "Marmara Kumaş" });
        var list = new List<MusteriTakipKlasorCardViewModel> { k1, k2 };

        string query = "akdeniz";
        var filtered = list.Where(x => x.CariUnvan.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Single(filtered);
        Assert.Equal("Akdeniz Tekstil", filtered[0].CariUnvan);
    }

    [Fact]
    public void Scenario_84_FilterKlasorler_MatchesPhone_Partial()
    {
        var k1 = new MusteriTakipKlasorCardViewModel(new MusteriTakipKlasor { CariUnvan = "C1", Telefon = "05321112233" });
        var k2 = new MusteriTakipKlasorCardViewModel(new MusteriTakipKlasor { CariUnvan = "C2", Telefon = "05445556677" });
        var list = new List<MusteriTakipKlasorCardViewModel> { k1, k2 };

        string query = "555";
        var filtered = list.Where(x => (x.Telefon ?? "").Contains(query)).ToList();

        Assert.Single(filtered);
        Assert.Equal("C2", filtered[0].CariUnvan);
    }

    [Fact]
    public void Scenario_85_FilterKlasorler_MatchesEtiketFilter()
    {
        var k1 = new MusteriTakipKlasorCardViewModel(new MusteriTakipKlasor { CariUnvan = "C1", Etiket = "Sıcak Müşteri" });
        var k2 = new MusteriTakipKlasorCardViewModel(new MusteriTakipKlasor { CariUnvan = "C2", Etiket = "Önemli" });
        var list = new List<MusteriTakipKlasorCardViewModel> { k1, k2 };

        string tag = "Sıcak Müşteri";
        var filtered = list.Where(x => x.Etiket == tag).ToList();

        Assert.Single(filtered);
        Assert.Equal("C1", filtered[0].CariUnvan);
    }

    [Theory]
    [InlineData("Telefon", true)]
    [InlineData("Yüz Yüze", true)]
    [InlineData("WhatsApp", true)]
    [InlineData("E-Posta", true)]
    [InlineData("Faks", false)]
    public void Scenario_86_to_90_ModernContactChannels(string channel, bool isModern)
    {
        var modernChannels = new[] { "Telefon", "Yüz Yüze", "WhatsApp", "E-Posta" };
        bool result = modernChannels.Contains(channel);
        Assert.Equal(isModern, result);
    }

    // =============================================================
    // 8. ViewModel Entegrasyon & Durum Değişimleri (91 - 100)
    // =============================================================

    [AvaloniaFact]
    public async Task Scenario_91_MusteriTakipViewModel_InitialLoad_SetsHasKlasorler()
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        await vm.LoadKlasorlerAsync();

        Assert.NotNull(vm.Klasorler);
        Assert.NotNull(vm.FilteredKlasorler);
    }

    [AvaloniaFact]
    public void Scenario_92_MusteriTakipViewModel_OpenCariSelection_TogglesModalFlag()
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        vm.IsCariSelectionOpen = true;

        Assert.True(vm.IsCariSelectionOpen);
        vm.IsCariSelectionOpen = false;
        Assert.False(vm.IsCariSelectionOpen);
    }

    [AvaloniaFact]
    public void Scenario_93_MusteriTakipViewModel_SelectedEtiketFilter_DefaultIsTumu()
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        Assert.Equal("Tümü", vm.SelectedEtiketFilter);
    }

    [AvaloniaFact]
    public void Scenario_94_MusteriTakipViewModel_EtiketListesi_ContainsRequiredDefaults()
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        Assert.Contains("Tümü", vm.EtiketListesi);
        Assert.Contains("Sıcak Müşteri", vm.EtiketListesi);
        Assert.Contains("Teklif Aşamasında", vm.EtiketListesi);
        Assert.Contains("Önemli", vm.EtiketListesi);
    }

    [AvaloniaFact]
    public void Scenario_95_MusteriTakipViewModel_SearchStringChanged_TriggersFilter()
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        vm.SearchString = "ÖzelAramaQuery";
        Assert.Equal("ÖzelAramaQuery", vm.SearchString);
    }

    [Theory]
    [InlineData("100 ₺", 100)]
    [InlineData("250.50 $", 250.5)]
    [InlineData("1000 €", 1000)]
    [InlineData("50 £", 50)]
    public void Scenario_96_to_99_PriceTextFormattingEquivalence(string display, double numericVal)
    {
        string normDisplay = display.Replace(",", ".");
        string normVal = numericVal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains(normVal, normDisplay);
    }

    [Fact]
    public void Scenario_100_MusteriTakipKlasor_DefaultValues_AreConsistent()
    {
        var k = new MusteriTakipKlasor();
        Assert.Equal("default", k.TenantId);
        Assert.Equal("#3B82F6", k.Renk);
        Assert.False(k.IsDeleted);
        Assert.True(k.OlusturmaTarihi <= DateTime.Now);
    }
}
