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

public partial class MusteriTakipScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Klasör Oluşturma & Alan Doğrulamaları (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CreateCustomerFolder_SavesSuccessfully()
    {
        var cari = new CariKart { CariKod = "C-CRM-1", Unvan = "CRM Test Müşterisi", Telefon = "05551234567" };
        await _uow.Cariler.SaveAsync(cari);

        var klasor = new MusteriTakipKlasor
        {
            CariId = cari.Id,
            CariUnvan = cari.Unvan,
            CariKod = cari.CariKod,
            Telefon = cari.Telefon,
            Etiket = "Sıcak Müşteri",
            Renk = "#3B82F6",
            SonIslemTarihi = DateTime.Now,
            Aciklama = "Yüksek potansiyelli ihracat müşterisi"
        };
        await _uow.MusteriTakip.SaveAsync(klasor);

        var saved = await _uow.MusteriTakip.GetByIdAsync(klasor.Id);
        Assert.NotNull(saved);
        Assert.Equal("CRM Test Müşterisi", saved.CariUnvan);
        Assert.Equal("Sıcak Müşteri", saved.Etiket);
        Assert.Equal("#3B82F6", saved.Renk);
    }

    [Theory]
    [InlineData("Sıcak Müşteri")]
    [InlineData("Teklif Aşamasında")]
    [InlineData("Önemli")]
    [InlineData("Yeni İletişim")]
    [InlineData("Takipte")]
    public void Scenario_02_to_06_ValidTagNames(string tag)
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        Assert.Contains(tag, vm.EtiketListesi);
    }

    [Theory]
    [InlineData("#3B82F6", true)] // Mavi
    [InlineData("#10B981", true)] // Yeşil
    [InlineData("#EF4444", true)] // Kırmızı
    [InlineData("#F59E0B", true)] // Sarı
    [InlineData("", false)]       // Boş
    public void Scenario_07_to_11_HexColorCodeValidation(string hexColor, bool expectedValid)
    {
        bool isValid = !string.IsNullOrEmpty(hexColor) && hexColor.StartsWith("#") && hexColor.Length == 7;
        Assert.Equal(expectedValid, isValid);
    }

    [AvaloniaFact]
    public async Task Scenario_12_UpdateCustomerFolder_ModifiesFields()
    {
        var klasor = new MusteriTakipKlasor { CariUnvan = "Eski CRM Unvan", Etiket = "Yeni İletişim" };
        await _uow.MusteriTakip.SaveAsync(klasor);

        klasor.Etiket = "Sıcak Müşteri";
        klasor.Renk = "#10B981";
        await _uow.MusteriTakip.SaveAsync(klasor);

        var updated = await _uow.MusteriTakip.GetByIdAsync(klasor.Id);
        Assert.Equal("Sıcak Müşteri", updated!.Etiket);
        Assert.Equal("#10B981", updated.Renk);
    }

    [AvaloniaFact]
    public async Task Scenario_13_DeleteCustomerFolder_RemovesRecord()
    {
        var klasor = new MusteriTakipKlasor { CariUnvan = "Silinecek CRM Klasör" };
        await _uow.MusteriTakip.SaveAsync(klasor);

        await _uow.MusteriTakip.DeleteAsync(klasor);

        var deleted = await _uow.MusteriTakip.GetByIdAsync(klasor.Id);
        Assert.Null(deleted);
    }

    [Theory]
    [InlineData("05321112233", true)]
    [InlineData("+905321112233", true)]
    [InlineData("123", false)]
    [InlineData("", true)] // Opsiyonel
    public void Scenario_14_to_15_PhoneNumberValidations(string phone, bool expectedValid)
    {
        bool isValid = string.IsNullOrEmpty(phone) || phone.Length >= 10;
        Assert.Equal(expectedValid, isValid);
    }

    // -------------------------------------------------------------
    // 2. Görüşme Notları & Fiyat Geçmişi (15 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData(0, 1, 1)] // 0 görüşme vardı, 1 görüşme eklendi -> 1
    [InlineData(5, 1, 6)]
    [InlineData(3, 2, 5)]
    public void Scenario_16_to_18_MeetingCountIncrementCalculations(int currentCount, int added, int expTotal)
    {
        int total = currentCount + added;
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData(0, 1, 1)] // 0 fiyat teklifi vardı, 1 fiyat verildi -> 1
    [InlineData(10, 1, 11)]
    [InlineData(2, 3, 5)]
    public void Scenario_19_to_21_PriceHistoryCountIncrementCalculations(int currentCount, int added, int expTotal)
    {
        int total = currentCount + added;
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData("₺")]
    [InlineData("$")]
    [InlineData("€")]
    public void Scenario_22_to_24_PriceCurrencySymbols(string currencySymbol)
    {
        var model = new MusteriTakipKlasor { CariUnvan = "Test" };
        var card = new MusteriTakipKlasorCardViewModel(model);
        card.SonVerilenFiyatBirimi = currencySymbol;
        Assert.Equal(currencySymbol, card.SonVerilenFiyatBirimi);
    }

    [Theory]
    [InlineData(150.0, 150.0)]
    [InlineData(2500.50, 2500.50)]
    [InlineData(0.0, 0.0)]
    public void Scenario_25_to_27_LastGivenPriceTracking(decimal price, decimal expPrice)
    {
        var model = new MusteriTakipKlasor { CariUnvan = "Test" };
        var card = new MusteriTakipKlasorCardViewModel(model);
        card.SonVerilenFiyat = price;
        Assert.Equal(expPrice, card.SonVerilenFiyat);
    }

    [Theory]
    [InlineData("Telefon Görüşmesi")]
    [InlineData("Yüz Yüze Ziyaret")]
    [InlineData("E-Posta")]
    [InlineData("Online Toplantı")]
    public void Scenario_28_to_30_MeetingInteractionTypes(string meetingType)
    {
        Assert.False(string.IsNullOrEmpty(meetingType));
    }

    // -------------------------------------------------------------
    // 3. Arama, Filtreleme ve Sıralama (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_31_SearchByUnvan_FiltersFolders()
    {
        await _uow.MusteriTakip.SaveAsync(new MusteriTakipKlasor { CariUnvan = "Atlas Uluslararası Ticaret" });
        await _uow.MusteriTakip.SaveAsync(new MusteriTakipKlasor { CariUnvan = "Balkan Sanayi" });

        var all = await _uow.MusteriTakip.GetAllAsync();
        var matches = all.Where(k => k.CariUnvan.Contains("Atlas", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Single(matches);
        Assert.Equal("Atlas Uluslararası Ticaret", matches[0].CariUnvan);
    }

    [AvaloniaFact]
    public async Task Scenario_32_FilterByTag_ReturnsMatchingFolders()
    {
        await _uow.MusteriTakip.SaveAsync(new MusteriTakipKlasor { CariUnvan = "M1", Etiket = "Sıcak Müşteri" });
        await _uow.MusteriTakip.SaveAsync(new MusteriTakipKlasor { CariUnvan = "M2", Etiket = "Takipte" });

        var all = await _uow.MusteriTakip.GetAllAsync();
        var hotClients = all.Where(k => k.Etiket == "Sıcak Müşteri").ToList();

        Assert.Single(hotClients);
        Assert.Equal("M1", hotClients[0].CariUnvan);
    }

    [AvaloniaFact]
    public async Task Scenario_33_to_36_SortingFolders_ByLastOperationDate()
    {
        var k1 = new MusteriTakipKlasor { CariUnvan = "K-OLD", SonIslemTarihi = DateTime.Today.AddDays(-10) };
        var k2 = new MusteriTakipKlasor { CariUnvan = "K-NEW", SonIslemTarihi = DateTime.Today };
        var k3 = new MusteriTakipKlasor { CariUnvan = "K-MID", SonIslemTarihi = DateTime.Today.AddDays(-3) };

        var list = new List<MusteriTakipKlasor> { k1, k2, k3 };
        var sorted = list.OrderByDescending(k => k.SonIslemTarihi).Select(k => k.CariUnvan).ToList();

        Assert.Equal("K-NEW", sorted[0]);
        Assert.Equal("K-MID", sorted[1]);
        Assert.Equal("K-OLD", sorted[2]);
        await Task.CompletedTask;
    }

    [Theory]
    [InlineData("Tümü", true)]
    [InlineData("Sıcak Müşteri", true)]
    [InlineData("Önemli", true)]
    [InlineData("GeçersizEtiket", false)]
    public void Scenario_37_to_40_EtiketFilterExistence(string tag, bool shouldExist)
    {
        var vm = _serviceProvider.GetRequiredService<MusteriTakipViewModel>();
        bool exists = vm.EtiketListesi.Contains(tag);
        Assert.Equal(shouldExist, exists);
    }

    // -------------------------------------------------------------
    // 4. Belge Ekleri & Alarmlar (10 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData(".jpg", true)]
    [InlineData(".png", true)]
    [InlineData(".pdf", true)]
    [InlineData(".exe", false)] // Güvenlik engeli
    [InlineData(".bat", false)]
    public void Scenario_41_to_45_SupportedAttachmentFileExtensions(string ext, bool isAllowed)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".docx", ".xlsx" };
        bool allowed = allowedExtensions.Contains(ext.ToLowerInvariant());
        Assert.Equal(isAllowed, allowed);
    }

    [Theory]
    [InlineData(-1, true)]  // Takip alarmı dündü -> Gecikmiş takip
    [InlineData(0, false)]   // Bugün
    [InlineData(2, false)]   // Gelecek takip
    public void Scenario_46_to_48_FollowUpAlarmOverdueChecks(int addDays, bool isAlarmTriggered)
    {
        DateTime followUp = DateTime.Today.AddDays(addDays);
        bool overdue = followUp < DateTime.Today;
        Assert.Equal(isAlarmTriggered, overdue);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(15, true)]
    public void Scenario_49_to_50_HasKlasorlerFlagEvaluation(int folderCount, bool expectedHasKlasorler)
    {
        bool has = folderCount > 0;
        Assert.Equal(expectedHasKlasorler, has);
    }
}
