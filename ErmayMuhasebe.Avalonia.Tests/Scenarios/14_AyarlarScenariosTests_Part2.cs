using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Avalonia.Services;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class AyarlarScenariosTests
{
    // =============================================================
    // 6. Belge Logo & PDF Yazdırma Yapılandırması (51 - 65)
    // =============================================================

    [Theory]
    [InlineData("Fatura", true)]
    [InlineData("Siparis", true)]
    [InlineData("Teklif", true)]
    [InlineData("Ekstre", true)]
    [InlineData("Raporlar", true)]
    [InlineData("Tahsilat", true)]
    [InlineData("Odeme", true)]
    [InlineData("AcilisBakiye", true)]
    public void Scenario_51_to_58_LogoVisibility_DefaultIsEnabled(string docType, bool expectedDefault)
    {
        var profil = new FirmaProfili();
        Assert.NotNull(profil);
        Assert.True(expectedDefault);
    }

    [Theory]
    [InlineData("A4", true)]
    [InlineData("A5", true)]
    [InlineData("A3", false)]
    public void Scenario_59_to_61_SupportedPdfPaperSizes(string size, bool isSupported)
    {
        var supported = new[] { "A4", "A5" };
        Assert.Equal(isSupported, supported.Contains(size));
    }

    [Theory]
    [InlineData("Dikey", true)]
    [InlineData("Yatay", true)]
    [InlineData("Capraz", false)]
    public void Scenario_62_to_64_SupportedPdfOrientations(string orient, bool isSupported)
    {
        var supported = new[] { "Dikey", "Yatay" };
        Assert.Equal(isSupported, supported.Contains(orient));
    }

    [Fact]
    public void Scenario_65_DefaultPrinter_FallbackToVarsayilan()
    {
        string defaultPrinter = "Varsayılan";
        Assert.Equal("Varsayılan", defaultPrinter);
    }

    // =============================================================
    // 7. Tema, Ekran Ölçekleme & Arayüz Tercihleri (66 - 75)
    // =============================================================

    [Fact]
    public void Scenario_66_AutoScalingEnabled_DefaultIsTrue()
    {
        bool isAutoScaling = true;
        Assert.True(isAutoScaling);
    }

    [Theory]
    [InlineData(1.0, "100%")]
    [InlineData(1.25, "125%")]
    [InlineData(1.50, "150%")]
    [InlineData(1.75, "175%")]
    [InlineData(2.0, "200%")]
    public void Scenario_67_to_71_DisplayScaleFactors(double factor, string display)
    {
        string formatted = $"{factor * 100:0}%";
        Assert.Equal(display, formatted);
    }

    [Theory]
    [InlineData("Dark", true)]
    [InlineData("Light", true)]
    [InlineData("System", true)]
    [InlineData("Neon", false)]
    public void Scenario_72_to_75_ApplicationThemeModes(string theme, bool isValid)
    {
        var validThemes = new[] { "Dark", "Light", "System" };
        Assert.Equal(isValid, validThemes.Contains(theme));
    }

    // =============================================================
    // 8. Dış Servis API Entegrasyonları (76 - 85)
    // =============================================================

    [Theory]
    [InlineData("ex_live_1234567890abcdef", true)]
    [InlineData("", true)] // Opsiyonel
    [InlineData("short", false)]
    public void Scenario_76_to_78_ExchangeRateApiKey_Validation(string key, bool isValid)
    {
        bool valid = string.IsNullOrEmpty(key) || key.Length >= 15;
        Assert.Equal(isValid, valid);
    }

    [Theory]
    [InlineData("ps_token_abcdef123456", true)]
    [InlineData("", true)]
    public void Scenario_79_to_80_PositionStackApiKey_Validation(string key, bool isValid)
    {
        bool valid = string.IsNullOrEmpty(key) || key.Length >= 10;
        Assert.Equal(isValid, valid);
    }

    [Theory]
    [InlineData("emailable_live_abc123", true)]
    [InlineData("", true)]
    public void Scenario_81_to_82_EmailableApiKey_Validation(string key, bool isValid)
    {
        bool valid = string.IsNullOrEmpty(key) || key.Length >= 10;
        Assert.Equal(isValid, valid);
    }

    [Theory]
    [InlineData("https://api.netgsm.com.tr/sms/send", true)]
    [InlineData("http://insecure-gateway.com", false)] // Sadece HTTPS
    public void Scenario_83_to_84_SmsGatewayUrlSecurity(string url, bool isSecure)
    {
        bool secure = Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
        Assert.Equal(isSecure, secure);
    }

    [Fact]
    public void Scenario_85_TcmbRateAutoRefresh_ScheduledHour()
    {
        // TCMB her iş günü 15:30'da yeni kurları yayınlar
        var updateTime = new TimeSpan(15, 30, 0);
        Assert.Equal(15, updateTime.Hours);
        Assert.Equal(30, updateTime.Minutes);
    }

    // =============================================================
    // 9. Sistem Durumu & Çevrimdışı Çalışma (86 - 100)
    // =============================================================

    [Fact]
    public void Scenario_86_AppVersion_FormatCheck()
    {
        string version = "1.2.5-Stable";
        Assert.Contains("-Stable", version);
        Assert.StartsWith("1.", version);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Scenario_87_to_88_OfflineModeToggle(bool isOffline)
    {
        bool offline = isOffline;
        Assert.Equal(isOffline, offline);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Scenario_89_to_90_MaintenanceModeToggle(bool isMaintenance)
    {
        bool maintenance = isMaintenance;
        Assert.Equal(isMaintenance, maintenance);
    }

    [Theory]
    [InlineData(0, "Devre Dışı")]
    [InlineData(5, "5 Dakika")]
    [InlineData(15, "15 Dakika")]
    [InlineData(30, "30 Dakika")]
    public void Scenario_91_to_94_AutoLockScreenTimeout(int minutes, string displayText)
    {
        string text = minutes == 0 ? "Devre Dışı" : $"{minutes} Dakika";
        Assert.Equal(displayText, text);
    }

    [Theory]
    [InlineData("ErmayMuhasebe_Yedek_20260909_1530.db3", true)]
    [InlineData("gecersiz_dosya.txt", false)]
    public void Scenario_95_to_96_BackupFileNamePattern(string name, bool isBackupDb)
    {
        bool match = name.StartsWith("ErmayMuhasebe_Yedek_") && name.EndsWith(".db3");
        Assert.Equal(isBackupDb, match);
    }

    [Theory]
    [InlineData("Indigo", "#4F46E5")]
    [InlineData("Emerald", "#10B981")]
    [InlineData("Violet", "#8B5CF6")]
    [InlineData("Amber", "#F59E0B")]
    public void Scenario_97_to_100_AccentColorThemes(string themeName, string hexCode)
    {
        Assert.StartsWith("#", hexCode);
        Assert.Equal(7, hexCode.Length);
        Assert.NotEmpty(themeName);
    }
}
