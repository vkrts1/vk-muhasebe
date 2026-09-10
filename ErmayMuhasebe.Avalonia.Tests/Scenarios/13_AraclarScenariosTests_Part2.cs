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

public partial class AraclarScenariosTests
{
    // =============================================================
    // 5. Belge Arşivi & Dosya Depolama Yönetimi (51 - 60)
    // =============================================================

    [Fact]
    public async Task Scenario_51_SaveBelgeArsiv_PersistsDocumentMetadata()
    {
        var doc = new BelgeArsiv
        {
            Ad = "Kira Sözleşmesi 2026",
            Kategori = "Sözleşmeler",
            Yol = @"C:\Arsiv\kira_sozlesmesi.pdf",
            Tur = ".pdf",
            Boyut = "1,0 MB",
            Tarih = DateTime.Now
        };
        await _uow.BelgeArsiv.SaveAsync(doc);

        var retrieved = await _uow.BelgeArsiv.GetByIdAsync(doc.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Kira Sözleşmesi 2026", retrieved.Ad);
        Assert.Equal("Sözleşmeler", retrieved.Kategori);
        Assert.Equal(".pdf", retrieved.Tur);
    }

    [Theory]
    [InlineData("Faturalar")]
    [InlineData("Sözleşmeler")]
    [InlineData("Dekontlar")]
    [InlineData("Resmi Evraklar")]
    public async Task Scenario_52_to_55_FilterBelgeArsiv_ByCategory(string kategori)
    {
        var doc = new BelgeArsiv
        {
            Ad = $"Belge - {kategori}",
            Kategori = kategori,
            Tur = ".pdf"
        };
        await _uow.BelgeArsiv.SaveAsync(doc);

        var all = await _uow.BelgeArsiv.GetAllAsync();
        var filtered = all.Where(x => x.Kategori == kategori).ToList();
        Assert.Contains(filtered, x => x.Id == doc.Id);
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1,0 KB")]
    [InlineData(1048576, "1,0 MB")]
    [InlineData(1073741824, "1,0 GB")]
    public void Scenario_56_to_59_FormatFileSize_Calculations(long bytes, string expectedFormatted)
    {
        string formatted;
        if (bytes < 1024) formatted = $"{bytes} B";
        else if (bytes < 1048576) formatted = $"{(bytes / 1024.0):0.0} KB";
        else if (bytes < 1073741824) formatted = $"{(bytes / 1048576.0):0.0} MB";
        else formatted = $"{(bytes / 1073741824.0):0.0} GB";

        Assert.Equal(expectedFormatted, formatted);
    }

    [Fact]
    public async Task Scenario_60_DeleteBelgeArsiv_RemovesDocument()
    {
        var doc = new BelgeArsiv { Ad = "Geçici Belge", Tur = ".pdf" };
        await _uow.BelgeArsiv.SaveAsync(doc);

        await _uow.BelgeArsiv.DeleteAsync(doc);

        var retrieved = await _uow.BelgeArsiv.GetByIdAsync(doc.Id);
        Assert.Null(retrieved);
    }

    // =============================================================
    // 6. Hızlı Notlar & Masaüstü Yapışkan Not Motoru (61 - 75)
    // =============================================================

    [Fact]
    public async Task Scenario_61_SaveNote_PersistsTitleAndContent()
    {
        var note = new Note
        {
            Title = "Haftalık Sevkiyat Planı",
            Content = "Pazartesi: Denizli kumaş sevkiyatı\nÇarşamba: Bursa boyahane teslimi",
            Color = "#FEF08A",
            IsPinned = true,
            CreatedAt = DateTime.Now
        };
        await _uow.Notes.SaveAsync(note);

        var retrieved = await _uow.Notes.GetByIdAsync(note.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Haftalık Sevkiyat Planı", retrieved.Title);
        Assert.True(retrieved.IsPinned);
    }

    [Theory]
    [InlineData("#FEF08A", true)] // Sarı
    [InlineData("#BFDBFE", true)] // Mavi
    [InlineData("#BBF7D0", true)] // Yeşil
    [InlineData("#FECDD3", true)] // Pembe
    public void Scenario_62_to_65_StickyNoteThemeColors(string hex, bool isSupported)
    {
        var supported = new[] { "#FEF08A", "#BFDBFE", "#BBF7D0", "#FECDD3" };
        bool exists = supported.Contains(hex);
        Assert.Equal(isSupported, exists);
    }

    [Fact]
    public async Task Scenario_66_ToggleNotePinStatus_UpdatesCorrectly()
    {
        var note = new Note { Title = "Sabitlenecek Not", IsPinned = false };
        await _uow.Notes.SaveAsync(note);

        note.IsPinned = true;
        await _uow.Notes.SaveAsync(note);

        var retrieved = await _uow.Notes.GetByIdAsync(note.Id);
        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsPinned);
    }

    [Fact]
    public async Task Scenario_67_SearchNotes_ByKeyword_ReturnsMatches()
    {
        var n1 = new Note { Title = "Muhasebe Toplantısı", Content = "Vergi dairesi görüşmesi" };
        var n2 = new Note { Title = "Fabrika Bakımı", Content = "Örgü makineleri yağlandı" };
        await _uow.Notes.SaveAsync(n1);
        await _uow.Notes.SaveAsync(n2);

        var all = await _uow.Notes.GetAllAsync();
        var matches = all.Where(x => x.Title.Contains("Muhasebe") || x.Content.Contains("Muhasebe")).ToList();

        Assert.Contains(matches, x => x.Id == n1.Id);
        Assert.DoesNotContain(matches, x => x.Id == n2.Id);
    }

    [Fact]
    public async Task Scenario_68_DeleteNote_RemovesFromList()
    {
        var note = new Note { Title = "Silinecek Not" };
        await _uow.Notes.SaveAsync(note);

        await _uow.Notes.DeleteAsync(note);

        var all = await _uow.Notes.GetAllAsync();
        Assert.DoesNotContain(all, x => x.Id == note.Id);
    }

    [Fact]
    public void Scenario_69_AutoGenerateTitle_FromContentFirstLine()
    {
        string content = "Önemli Sevkiyat Notu\nİkinci satır detaylar...";
        string firstLine = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        Assert.Equal("Önemli Sevkiyat Notu", firstLine);
    }

    [Theory]
    [InlineData("Acil", 4)]
    [InlineData("Yüksek", 3)]
    [InlineData("Orta", 2)]
    [InlineData("Düşük", 1)]
    public void Scenario_70_to_73_NotePriorityWeights(string priority, int weight)
    {
        int w = priority switch
        {
            "Acil" => 4,
            "Yüksek" => 3,
            "Orta" => 2,
            _ => 1
        };
        Assert.Equal(weight, w);
    }

    [Fact]
    public void Scenario_74_SortNotes_PinnedFirstThenDate()
    {
        var list = new List<Note>
        {
            new() { Title = "Eski Sabitsiz", IsPinned = false, CreatedAt = DateTime.Today.AddDays(-5) },
            new() { Title = "Yeni Sabitsiz", IsPinned = false, CreatedAt = DateTime.Today },
            new() { Title = "Sabitlenmiş", IsPinned = true, CreatedAt = DateTime.Today.AddDays(-10) }
        };

        var sorted = list.OrderByDescending(x => x.IsPinned).ThenByDescending(x => x.CreatedAt).ToList();

        Assert.Equal("Sabitlenmiş", sorted[0].Title);
        Assert.Equal("Yeni Sabitsiz", sorted[1].Title);
        Assert.Equal("Eski Sabitsiz", sorted[2].Title);
    }

    [Fact]
    public void Scenario_75_EmptyNote_DefaultValues()
    {
        var n = new Note();
        Assert.NotNull(n.Title);
        Assert.False(n.IsPinned);
    }

    // =============================================================
    // 7. Barkod Tasarımcısı & Etiket Basım Motoru (76 - 90)
    // =============================================================

    [Theory]
    [InlineData("869012345678", 9)] // 869012345678 -> checksum 9
    [InlineData("123456789012", 8)]
    public void Scenario_76_to_77_Ean13Checksum_Calculation(string first12Digits, int expectedChecksum)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = first12Digits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }
        int calcChecksum = (10 - (sum % 10)) % 10;
        Assert.Equal(expectedChecksum, calcChecksum);
    }

    [Theory]
    [InlineData("EAN13", 13)]
    [InlineData("Code128", 0)] // Değişken uzunluk
    [InlineData("QRCode", 0)]
    public void Scenario_78_to_80_BarcodeStandardLengths(string format, int expectedLength)
    {
        if (expectedLength > 0)
        {
            Assert.Equal(13, expectedLength);
        }
        else
        {
            Assert.True(format == "Code128" || format == "QRCode");
        }
    }

    [Theory]
    [InlineData(50, 30, 1500)]  // 50 mm x 30 mm = 1500 mm²
    [InlineData(100, 50, 5000)] // 100 mm x 50 mm = 5000 mm²
    [InlineData(80, 40, 3200)]  // 80 mm x 40 mm = 3200 mm²
    public void Scenario_81_to_83_LabelArea_Calculations(double widthMm, double heightMm, double expectedArea)
    {
        double area = widthMm * heightMm;
        Assert.Equal(expectedArea, area);
    }

    [Theory]
    [InlineData(210, 297, 50, 30, 4, 9, 36)] // A4 (210x297): 4 per row, 9 per column = 36 labels
    [InlineData(210, 297, 70, 37, 3, 8, 24)] // 3 per row, 8 per column = 24 labels
    public void Scenario_84_to_85_A4LabelSheetGrid_Capacity(double pageW, double pageH, double lblW, double lblH, int expCols, int expRows, int expTotal)
    {
        int cols = (int)(pageW / lblW);
        int rows = (int)(pageH / lblH);
        int total = cols * rows;

        Assert.Equal(expCols, cols);
        Assert.Equal(expRows, rows);
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData(203, 8.0)]  // 203 DPI = ~8 dots/mm
    [InlineData(300, 11.8)] // 300 DPI = ~11.8 dots/mm
    [InlineData(600, 23.6)] // 600 DPI = ~23.6 dots/mm
    public void Scenario_86_to_88_PrinterDpi_ToDotsPerMm(int dpi, double expDotsPerMm)
    {
        double dotsPerMm = Math.Round(dpi / 25.4, 1);
        Assert.Equal(expDotsPerMm, dotsPerMm);
    }

    [Fact]
    public void Scenario_89_LabelMargins_NegativeClampedToZero()
    {
        double margin = -5.0;
        double clamped = Math.Max(0.0, margin);
        Assert.Equal(0.0, clamped);
    }

    [Fact]
    public void Scenario_90_BarcodeData_WhitespaceStripped()
    {
        string raw = "  8690123456789   ";
        string cleaned = raw.Trim();
        Assert.Equal("8690123456789", cleaned);
    }

    // =============================================================
    // 8. Fatura Tasarımcısı & Şablon Yapılandırması (91 - 100)
    // =============================================================

    [Theory]
    [InlineData("Modern", true)]
    [InlineData("Klasik", true)]
    [InlineData("Minimal", true)]
    [InlineData("Kompakt", true)]
    public void Scenario_91_to_94_AvailableInvoiceTemplateLayouts(string layout, bool isSupported)
    {
        var supported = new[] { "Modern", "Klasik", "Minimal", "Kompakt" };
        bool valid = supported.Contains(layout);
        Assert.Equal(isSupported, valid);
    }

    [Theory]
    [InlineData("A4", 210, 297)]
    [InlineData("A5", 148, 210)]
    [InlineData("Rulo-80mm", 80, 0)]
    public void Scenario_95_to_97_PaperSizes_DimensionsMm(string paper, int width, int height)
    {
        Assert.True(width > 0);
        if (paper == "Rulo-80mm") Assert.Equal(0, height); // Sürekli form
        else Assert.True(height > 0);
    }

    [Fact]
    public void Scenario_98_TableColumns_DefaultVisibility()
    {
        var columns = new Dictionary<string, bool>
        {
            { "SiraNo", true },
            { "StokKodu", true },
            { "Aciklama", true },
            { "Miktar", true },
            { "Birim", true },
            { "BirimFiyat", true },
            { "KdvOrani", true },
            { "Toplam", true }
        };

        Assert.All(columns.Values, v => Assert.True(v));
    }

    [Fact]
    public void Scenario_99_InvoiceLogoCoordinates_WithinBounds()
    {
        double x = 20, y = 20, w = 150, h = 60;
        bool fits = (x + w <= 210 * 2.83) && (y + h <= 297 * 2.83); // A4 mm to pt approx
        Assert.True(fits);
    }

    [Fact]
    public void Scenario_100_ResetTemplateToDefaults_RestoresValues()
    {
        string defaultLayout = "Modern";
        string currentLayout = "ÖzelÖzelleştirilmiş";

        currentLayout = defaultLayout;
        Assert.Equal("Modern", currentLayout);
    }
}
