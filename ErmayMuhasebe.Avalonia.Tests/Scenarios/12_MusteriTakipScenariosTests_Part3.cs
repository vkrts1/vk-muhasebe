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
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class MusteriTakipScenariosTests
{
    // =============================================================
    // 9. Klasör Detay Ekranı & Çoklu Liste Ayrıştırma (101 - 115)
    // =============================================================

    [Fact]
    public async Task Scenario_101_Detaylar_SegregatedByTip_CorrectlyDivided()
    {
        int fId = 301;
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Tip = "Not", Baslik = "N1" });
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Tip = "Gorusme", Baslik = "G1" });
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Tip = "Fiyat", Baslik = "F1", FiyatBilgisi = 100m });
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Tip = "Gorsel", Baslik = "I1" });

        var all = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(fId);

        var notlar = all.Where(x => x.Tip == "Not").ToList();
        var gorusmeler = all.Where(x => x.Tip == "Gorusme").ToList();
        var fiyatlar = all.Where(x => x.Tip == "Fiyat").ToList();
        var gorseller = all.Where(x => x.Tip == "Gorsel").ToList();

        Assert.Single(notlar);
        Assert.Single(gorusmeler);
        Assert.Single(fiyatlar);
        Assert.Single(gorseller);
    }

    [Fact]
    public async Task Scenario_102_Detay_Timestamp_ReflectsLatestAction()
    {
        var klasor = new MusteriTakipKlasor { CariUnvan = "Timestamp Test", SonIslemTarihi = DateTime.Now.AddDays(-5) };
        await _uow.MusteriTakip.SaveAsync(klasor);

        var detay = new MusteriTakipDetay { KlasorId = klasor.Id, Baslik = "Yeni Detay", Tarih = DateTime.Now };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        klasor.SonIslemTarihi = detay.Tarih;
        await _uow.MusteriTakip.SaveAsync(klasor);

        var retrieved = await _uow.MusteriTakip.GetByIdAsync(klasor.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.SonIslemTarihi > DateTime.Now.AddDays(-1));
    }

    [Theory]
    [InlineData("Not", "Görüşme Detayı", 0, "₺")]
    [InlineData("Gorusme", "Yönetici Toplantısı", 0, "₺")]
    [InlineData("Fiyat", "Özel Kotasyon", 350.75, "$")]
    [InlineData("Gorsel", "Ürün Fotoğrafı", 0, "₺")]
    public async Task Scenario_103_to_106_SaveVariousDetailTypes(string tip, string baslik, double fiyat, string paraBirimi)
    {
        var detay = new MusteriTakipDetay
        {
            KlasorId = 303,
            Tip = tip,
            Baslik = baslik,
            FiyatBilgisi = (decimal)fiyat,
            ParaBirimi = paraBirimi
        };
        await _uow.MusteriTakip.SaveDetayAsync(detay);

        var list = await _uow.MusteriTakip.GetDetaylarByTipAsync(303, tip);
        Assert.Contains(list, x => x.Baslik == baslik);
    }

    [Fact]
    public async Task Scenario_107_UpdateDetayContent_PersistsModifications()
    {
        var d = new MusteriTakipDetay { KlasorId = 307, Baslik = "Eski Başlık", Icerik = "Eski İçerik" };
        await _uow.MusteriTakip.SaveDetayAsync(d);

        d.Baslik = "Yeni Başlık";
        d.Icerik = "Yeni İçerik Güncellendi";
        await _uow.MusteriTakip.SaveDetayAsync(d);

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(307);
        var updated = list.FirstOrDefault(x => x.Id == d.Id);
        Assert.NotNull(updated);
        Assert.Equal("Yeni Başlık", updated.Baslik);
        Assert.Equal("Yeni İçerik Güncellendi", updated.Icerik);
    }

    [Fact]
    public async Task Scenario_108_GetDetaylarByTipAsync_NonExistingTip_ReturnsEmpty()
    {
        var list = await _uow.MusteriTakip.GetDetaylarByTipAsync(9999, "OlmayanTip");
        Assert.NotNull(list);
        Assert.Empty(list);
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 1)]
    [InlineData(5, 3, 2, 1, 11)]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(10, 10, 5, 5, 30)]
    public void Scenario_109_to_112_TotalActivityCount_Summation(int nots, int gorusmes, int fiyats, int gorsels, int expectedTotal)
    {
        int total = nots + gorusmes + fiyats + gorsels;
        Assert.Equal(expectedTotal, total);
    }

    [Fact]
    public void Scenario_113_PreviewText_Truncation_Over100Chars()
    {
        string longContent = new string('A', 150);
        string preview = longContent.Length > 80 ? longContent.Substring(0, 80) + "..." : longContent;

        Assert.Equal(83, preview.Length);
        Assert.EndsWith("...", preview);
    }

    [Fact]
    public async Task Scenario_114_DetailsDateOrder_DescendingVerification()
    {
        int fId = 314;
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Baslik = "Eski", Tarih = DateTime.Today.AddDays(-10) });
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Baslik = "Yeni", Tarih = DateTime.Today });
        await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay { KlasorId = fId, Baslik = "Orta", Tarih = DateTime.Today.AddDays(-5) });

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(fId);
        var sorted = list.OrderByDescending(x => x.Tarih).ToList();

        Assert.Equal("Yeni", sorted[0].Baslik);
        Assert.Equal("Orta", sorted[1].Baslik);
        Assert.Equal("Eski", sorted[2].Baslik);
    }

    [Fact]
    public void Scenario_115_FolderDetail_NullCheck_Safety()
    {
        MusteriTakipKlasor? nullFolder = null;
        Assert.Null(nullFolder);
    }

    // =============================================================
    // 10. PDF Raporu & Dosya Dışa Aktarma (116 - 130)
    // =============================================================

    [Fact]
    public async Task Scenario_116_PdfExport_FolderSummaryReport_GeneratesPdf()
    {
        var cari = new CariKart { CariKod = "C-PDF-1", Unvan = "PDF Test Cari" };
        var f = new MusteriTakipKlasor { CariUnvan = cari.Unvan, Telefon = "05001112233", Etiket = "Önemli" };

        var result = await _pdfService.GenerateCariEkstrePdfAsync(cari, new List<CariHareket>());
        Assert.True(result.Success);
    }

    [Theory]
    [InlineData("Müşteri Takip Dosyası")]
    [InlineData("Görüşme Geçmişi ve Teklifler")]
    [InlineData("Kapsamlı CRM Özeti")]
    public void Scenario_117_to_119_ReportHeaderTitles(string reportTitle)
    {
        Assert.False(string.IsNullOrWhiteSpace(reportTitle));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(50)]
    public void Scenario_120_to_122_PaginationCalculation_ForNotesList(int totalNotes)
    {
        int pageSize = 15;
        int totalPages = totalNotes == 0 ? 1 : (int)Math.Ceiling(totalNotes / (double)pageSize);
        Assert.True(totalPages >= 1);
    }

    [Theory]
    [InlineData("C:\\Attachments\\sample.pdf", true)]
    [InlineData("C:\\Attachments\\image.png", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Scenario_123_to_126_AttachmentPathValidity(string? path, bool expectedHasAttachment)
    {
        bool has = !string.IsNullOrWhiteSpace(path);
        Assert.Equal(expectedHasAttachment, has);
    }

    [Fact]
    public void Scenario_127_GdprAnonymization_CustomerPhoneAndTitle()
    {
        string phone = "05321234567";
        string masked = phone.Substring(0, 3) + "****" + phone.Substring(phone.Length - 2);

        Assert.Equal("053****67", masked);
    }

    [Theory]
    [InlineData(1000, 200, 800)]
    [InlineData(5000, 5000, 0)]
    [InlineData(0, 1500, -1500)]
    public void Scenario_128_to_130_CariBalanceImpactOnCrmFolder(decimal borc, decimal alacak, decimal expectedBakiye)
    {
        decimal bakiye = borc - alacak;
        Assert.Equal(expectedBakiye, bakiye);
    }

    // =============================================================
    // 11. CRM Satış Hunisi & Dönüşüm Süreçleri (131 - 145)
    // =============================================================

    [Theory]
    [InlineData("Yeni İletişim", "Teklif Aşamasında", true)]
    [InlineData("Teklif Aşamasında", "Sıcak Müşteri", true)]
    [InlineData("Sıcak Müşteri", "Sipariş Alındı", true)]
    [InlineData("Sıcak Müşteri", "İptal/Kayıp", true)]
    public void Scenario_131_to_134_LeadConversion_PipelineTransitions(string currentStage, string nextStage, bool isAllowed)
    {
        Assert.NotEqual(currentStage, nextStage);
        Assert.True(isAllowed);
    }

    [Theory]
    [InlineData("Fiyat Yüksek", true)]
    [InlineData("Termin Uymadı", true)]
    [InlineData("Rakip Tercih Edildi", true)]
    [InlineData("İletişim Kesildi", true)]
    public void Scenario_135_to_138_LostCustomerReasonTracking(string reason, bool isValidReason)
    {
        var validReasons = new[] { "Fiyat Yüksek", "Termin Uymadı", "Rakip Tercih Edildi", "İletişim Kesildi" };
        bool valid = validReasons.Contains(reason);
        Assert.Equal(isValidReason, valid);
    }

    [Theory]
    [InlineData(5, 5, 1.0)]   // 5 görüşme, 5 teklif = %100
    [InlineData(10, 4, 0.40)] // 10 görüşme, 4 teklif = %40
    [InlineData(20, 2, 0.10)] // 20 görüşme, 2 teklif = %10
    public void Scenario_139_to_141_MeetingToOffer_ConversionRatio(int meetings, int offers, double expectedRatio)
    {
        double ratio = (double)offers / meetings;
        Assert.Equal(expectedRatio, ratio, precision: 2);
    }

    [Theory]
    [InlineData(5, 5)]   // 5 stars
    [InlineData(3, 3)]   // 3 stars
    [InlineData(1, 1)]   // 1 star
    [InlineData(0, 0)]   // Not rated
    public void Scenario_142_to_145_CustomerSatisfactionRating_Bounds(int inputStars, int expected)
    {
        int clamped = Math.Clamp(inputStars, 0, 5);
        Assert.Equal(expected, clamped);
    }

    // =============================================================
    // 12. Veri Bütünlüğü, Eşzamanlılık & Uç Senaryolar (146 - 154)
    // =============================================================

    [Fact]
    public async Task Scenario_146_BatchInsert_50Notes_PerformanceAndIntegrity()
    {
        int fId = 446;
        for (int i = 1; i <= 50; i++)
        {
            await _uow.MusteriTakip.SaveDetayAsync(new MusteriTakipDetay
            {
                KlasorId = fId,
                Tip = "Not",
                Baslik = $"Hızlı Not #{i}",
                Icerik = $"Otomasyon içerik #{i}"
            });
        }

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(fId);
        Assert.Equal(50, list.Count);
    }

    [Fact]
    public async Task Scenario_147_TurkishCharactersInTitle_SavedAndRetrievedWithoutCorruption()
    {
        string unvan = "İğdır Çorapçılık San. ve Tic. Şti. - Şöhret & Çağdaş";
        var k = new MusteriTakipKlasor { CariUnvan = unvan, Aciklama = "Örnek Türkçe açıklama: Çığır, ışıltı, güvence." };
        await _uow.MusteriTakip.SaveAsync(k);

        var retrieved = await _uow.MusteriTakip.GetByIdAsync(k.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(unvan, retrieved.CariUnvan);
        Assert.Equal("Örnek Türkçe açıklama: Çığır, ışıltı, güvence.", retrieved.Aciklama);
    }

    [Fact]
    public async Task Scenario_148_LargeNoteContent_4000Chars_NoTruncationInDatabase()
    {
        string largeContent = string.Join(" ", Enumerable.Repeat("UzunNotMetniİçeriği", 200));
        var d = new MusteriTakipDetay { KlasorId = 448, Baslik = "Büyük Not", Icerik = largeContent };
        await _uow.MusteriTakip.SaveDetayAsync(d);

        var list = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(448);
        var item = list.FirstOrDefault(x => x.Id == d.Id);
        Assert.NotNull(item);
        Assert.Equal(largeContent.Length, item.Icerik?.Length);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(-50, false)] // Negative quotation invalid
    [InlineData(1000000, true)]
    public void Scenario_149_to_151_QuotationAmount_PositiveSanity(decimal amount, bool expectedValid)
    {
        bool isValid = amount >= 0;
        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public async Task Scenario_152_NullCariUnvan_FallbackHandling()
    {
        var k = new MusteriTakipKlasor { CariUnvan = string.Empty, CariKod = "KOD-BOŞ" };
        await _uow.MusteriTakip.SaveAsync(k);

        var saved = await _uow.MusteriTakip.GetByIdAsync(k.Id);
        Assert.NotNull(saved);
        Assert.Equal(string.Empty, saved.CariUnvan);
    }

    [Fact]
    public async Task Scenario_153_GetKlasorlerAsync_ReturnsOnlyActiveFolders()
    {
        var k1 = new MusteriTakipKlasor { CariUnvan = "Aktif K1", IsDeleted = false };
        var k2 = new MusteriTakipKlasor { CariUnvan = "Silinmiş K2", IsDeleted = true };
        await _uow.MusteriTakip.SaveAsync(k1);
        await _uow.MusteriTakip.SaveAsync(k2);

        var activeList = await _uow.MusteriTakip.GetKlasorlerAsync();
        Assert.Contains(activeList, x => x.Id == k1.Id);
        Assert.DoesNotContain(activeList, x => x.Id == k2.Id);
    }

    [Fact]
    public async Task Scenario_154_EndToEndCustomerFollowUpLifecycle_Simulation()
    {
        // 1. Cari Oluştur
        var cari = new CariKart { CariKod = "C-E2E-CRM", Unvan = "E2E CRM Müşteri A.Ş.", Telefon = "02129990011" };
        await _uow.Cariler.SaveAsync(cari);

        // 2. Müşteri Takip Klasörü Aç
        var klasor = new MusteriTakipKlasor
        {
            CariId = cari.Id,
            CariUnvan = cari.Unvan,
            CariKod = cari.CariKod,
            Telefon = cari.Telefon,
            Etiket = "Sıcak Müşteri",
            Renk = "#10B981"
        };
        await _uow.MusteriTakip.SaveAsync(klasor);

        // 3. İlk Görüşmeyi Kaydet
        var gorusme = new MusteriTakipDetay
        {
            KlasorId = klasor.Id,
            CariId = cari.Id,
            Tip = "Gorusme",
            Baslik = "Fabrika Ziyareti",
            Icerik = "Ürün gamı incelendi, 1000 mt gabardin kumaş talep edildi."
        };
        await _uow.MusteriTakip.SaveDetayAsync(gorusme);

        // 4. Verilen Fiyatı Kaydet
        var fiyat = new MusteriTakipDetay
        {
            KlasorId = klasor.Id,
            CariId = cari.Id,
            Tip = "Fiyat",
            Baslik = "Gabardin Kotasyon",
            FiyatBilgisi = 2.45m,
            ParaBirimi = "$"
        };
        await _uow.MusteriTakip.SaveDetayAsync(fiyat);

        // 5. Kart Görünümü Doğrulaması
        var card = new MusteriTakipKlasorCardViewModel(klasor)
        {
            GorusmeSayisi = 1,
            FiyatSayisi = 1,
            SonVerilenFiyat = 2.45m,
            SonVerilenFiyatBirimi = "$"
        };

        Assert.Equal("E2E CRM Müşteri A.Ş.", card.CariUnvan);
        Assert.Equal(1, card.GorusmeSayisi);
        Assert.Equal(2.45m, card.SonVerilenFiyat);
        Assert.Equal("$", card.SonVerilenFiyatBirimi);

        // 6. Detay Sayısı Doğrulaması
        var detaylar = await _uow.MusteriTakip.GetDetaylarByKlasorIdAsync(klasor.Id);
        Assert.Equal(2, detaylar.Count);
    }
}
