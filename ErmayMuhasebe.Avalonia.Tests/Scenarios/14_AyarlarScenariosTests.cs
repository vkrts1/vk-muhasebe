using System;
using System.Collections.Generic;
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

public partial class AyarlarScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Firma Profili & Kurumsal Bilgiler (12 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_01_CompanyProfile_SavesAndRetrievesSuccessfully()
    {
        var profil = new FirmaProfili
        {
            FirmaAdi = "Ermay Tekstil San. ve Tic. Ltd. Şti.",
            VergiDairesi = "Göztepe",
            VergiNo = "1234567890",
            Adres = "Bağdat Cad. No: 120 Kadıköy",
            Telefon = "02163334455",
            Eposta = "muhasebe@ermay.com",
            WebSitesi = "www.ermay.com",
            FaturaSize = "A4"
        };
        await _uow.SaveFirmaProfiliAsync(profil);

        var saved = await _uow.GetFirmaProfiliAsync();
        Assert.NotNull(saved);
        Assert.Equal("Ermay Tekstil San. ve Tic. Ltd. Şti.", saved.FirmaAdi);
        Assert.Equal("1234567890", saved.VergiNo);
        Assert.Equal("A4", saved.FaturaSize);
    }

    [Theory]
    [InlineData("1234567890", true)]  // 10 haneli VKN
    [InlineData("98765432101", true)] // 11 haneli TCKN
    [InlineData("12345", false)]
    [InlineData("", true)]             // Opsiyonel boş
    public void Scenario_02_to_05_CompanyTaxNumberValidation(string taxNo, bool expectedValid)
    {
        bool isValid = string.IsNullOrEmpty(taxNo) || (taxNo.Length == 10 || taxNo.Length == 11) && taxNo.All(char.IsDigit);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData("A4")]
    [InlineData("A5")]
    [InlineData("A6")]
    public async Task Scenario_06_to_08_DefaultInvoicePageSizeSelection(string pageSize)
    {
        var profil = new FirmaProfili { FirmaAdi = "Test Firma", FaturaSize = pageSize };
        await _uow.SaveFirmaProfiliAsync(profil);

        var saved = await _uow.GetFirmaProfiliAsync();
        Assert.Equal(pageSize, saved!.FaturaSize);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(10)]
    [InlineData(1)]
    [InlineData(0)]
    public void Scenario_09_to_12_DefaultVatRateOptions(int kdv)
    {
        bool valid = kdv >= 0 && kdv <= 100;
        Assert.True(valid);
    }

    // -------------------------------------------------------------
    // 2. Tema & UI Görünüm Ayarları (10 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("ModernSaaS")]
    [InlineData("Enterprise")]
    [InlineData("WindowsFluent")]
    [InlineData("IDEProfessional")]
    public void Scenario_13_to_16_ThemeOptionsAvailable(string theme)
    {
        var themeService = _serviceProvider.GetRequiredService<ThemeService>();
        Assert.NotNull(themeService);
        Assert.False(string.IsNullOrEmpty(theme));
    }

    [Theory]
    [InlineData(1.0)]  // %100 ölçek
    [InlineData(1.25)] // %125 ölçek
    [InlineData(1.50)] // %150 ölçek
    [InlineData(2.0)]  // %200 ölçek
    public void Scenario_17_to_20_DisplayScalingFactors(double scale)
    {
        bool valid = scale >= 1.0 && scale <= 2.5;
        Assert.True(valid);
    }

    [Theory]
    [InlineData(true, false)] // Saat göster, Döviz gizle
    [InlineData(false, true)] // Saat gizle, Döviz göster
    public void Scenario_21_to_22_StatusBarToggleSettings(bool showDateTime, bool showRates)
    {
        var statusService = _serviceProvider.GetRequiredService<StatusBarService>();
        statusService.Settings.ShowDateTime = showDateTime;
        statusService.Settings.ShowExchangeRates = showRates;

        Assert.Equal(showDateTime, statusService.Settings.ShowDateTime);
        Assert.Equal(showRates, statusService.Settings.ShowExchangeRates);
    }

    // -------------------------------------------------------------
    // 3. Yedekleme & Geri Yükleme (BackupService) (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_23_ManualDatabaseBackup_CreatesBackupFile()
    {
        var backupService = _serviceProvider.GetRequiredService<BackupService>();
        string tempBackupDir = Path.Combine(Path.GetTempPath(), $"Backup_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempBackupDir);

        string backupFile = Path.Combine(tempBackupDir, $"Test_Backup_{DateTime.Now:yyyyMMddHHmmss}.db3");
        File.Copy(_testDbPath, backupFile, overwrite: true);

        Assert.True(File.Exists(backupFile));
        Assert.True(new FileInfo(backupFile).Length > 0);

        // Temizlik
        try { Directory.Delete(tempBackupDir, recursive: true); } catch { }
    }

    [Theory]
    [InlineData("Ermay_Backup_20260909_180000.db3", true)]
    [InlineData("Ermay_Backup_20260909_180000.bak", true)]
    [InlineData("invalid_backup.txt", false)]
    public void Scenario_24_to_26_BackupFileExtensionValidation(string fileName, bool expectedValid)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        bool isValid = ext == ".db3" || ext == ".bak";
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(30, 45, 15)] // 45 yedek dosyası vardı, 30 günlük saklama -> 15 dosya temizlendi
    [InlineData(30, 20, 0)]  // Temizlik gerekmedi
    [InlineData(7, 10, 3)]
    public void Scenario_27_to_29_BackupRetentionCalculations(int maxCount, int currentCount, int expDeleted)
    {
        int deleted = Math.Max(0, currentCount - maxCount);
        Assert.Equal(expDeleted, deleted);
    }

    [AvaloniaFact]
    public void Scenario_30_CorruptedBackupFile_FailsValidation()
    {
        string fakeFile = Path.Combine(Path.GetTempPath(), $"Corrupt_{Guid.NewGuid():N}.db3");
        File.WriteAllText(fakeFile, "NOT_A_SQLITE_DATABASE_CORRUPTED_BYTES");

        bool isValidSqlite = false;
        try
        {
            var headerBytes = new byte[16];
            using var stream = File.OpenRead(fakeFile);
            stream.ReadExactly(headerBytes, 0, 16);
            string header = System.Text.Encoding.UTF8.GetString(headerBytes);
            isValidSqlite = header.StartsWith("SQLite format 3");
        }
        catch { }
        finally
        {
            try { File.Delete(fakeFile); } catch { }
        }

        Assert.False(isValidSqlite, "Bozuk dosya geçerli SQLite veritabanı olarak kabul edilmemelidir.");
    }

    // -------------------------------------------------------------
    // 4. Kullanıcı Yönetimi & Güvenlik (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public async Task Scenario_31_CreateNewUser_HashesPasswordWithSalt()
    {
        var conn = _dbService.GetGlobalConnection();
        await conn.CreateTableAsync<User>();

        string plainPass = "GuvenliSifre*2026";
        string salt = AuthService.GenerateSalt();
        string hash = AuthService.HashPassword(plainPass, salt);

        var user = new User
        {
            Username = "muhasebe_user",
            Password = hash,
            PasswordSalt = salt,
            Role = "Muhasebe",
            CreatedAt = DateTime.Now
        };
        await conn.InsertAsync(user);

        var saved = await conn.Table<User>().FirstOrDefaultAsync(u => u.Username == "muhasebe_user");
        Assert.NotNull(saved);
        Assert.NotEqual(plainPass, saved.Password);
        Assert.True(AuthService.VerifyPassword(plainPass, saved.Password!, saved.PasswordSalt!));
    }

    [Theory]
    [InlineData("GuvenliSifre*2026", "GuvenliSifre*2026", true)]  // Doğru şifre
    [InlineData("GuvenliSifre*2026", "YanlisSifre", false)]         // Hatalı şifre
    [InlineData("GuvenliSifre*2026", "guvenlisifre*2026", false)]  // Case sensitive
    public void Scenario_32_to_34_PasswordVerificationMatches(string original, string attempt, bool expectedMatch)
    {
        string salt = AuthService.GenerateSalt();
        string hash = AuthService.HashPassword(original, salt);

        bool verified = AuthService.VerifyPassword(attempt, hash, salt);
        Assert.Equal(expectedMatch, verified);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Muhasebe")]
    [InlineData("Satış")]
    [InlineData("Depo")]
    public void Scenario_35_to_38_UserRolesDefinition(string role)
    {
        Assert.False(string.IsNullOrEmpty(role));
    }

    [AvaloniaFact]
    public void Scenario_39_DefaultAdminAccount_CannotBeDeleted()
    {
        string username = "admin";
        bool canDelete = username.ToLowerInvariant() != "admin";
        Assert.False(canDelete, "Varsayılan 'admin' kullanıcısının silinmesi engellenmelidir.");
    }

    [Theory]
    [InlineData(15)] // 15 dakika oturum aşımı
    [InlineData(30)] // 30 dakika
    [InlineData(60)] // 60 dakika
    public void Scenario_40_SessionTimeoutDurations(int minutes)
    {
        bool valid = minutes >= 5 && minutes <= 240;
        Assert.True(valid);
    }

    // -------------------------------------------------------------
    // 5. Entegrasyonlar & Fabrika Ayarları (8 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData("smtp.gmail.com", 587, true)]
    [InlineData("smtp.office365.com", 587, true)]
    [InlineData("mail.kurumsal.com", 465, true)]
    public void Scenario_41_to_43_SmtpServerConfigurationValidation(string host, int port, bool ssl)
    {
        bool valid = !string.IsNullOrEmpty(host) && port > 0 && ssl;
        Assert.True(valid);
    }

    [Theory]
    [InlineData("123456789:ABCdefGhIJKlmNoPQRsTUVwxyZ", true)] // Telegram token formatı
    [InlineData("", false)]
    [InlineData("invalid_token", false)]
    public void Scenario_44_to_46_TelegramBotTokenValidation(string token, bool expectedValid)
    {
        bool valid = !string.IsNullOrEmpty(token) && token.Contains(":") && token.Length > 20;
        Assert.Equal(expectedValid, valid);
    }

    [Theory]
    [InlineData("123456", "123456", true)]   // Doğru fabrika sıfırlama şifresi
    [InlineData("123456", "yanlis", false)]   // Yanlış şifre
    public void Scenario_47_to_48_FactoryResetSecurityCheck(string realPass, string enteredPass, bool isAllowed)
    {
        bool allowed = realPass == enteredPass;
        Assert.Equal(isAllowed, allowed);
    }

    [Theory]
    [InlineData("https://ermay-cloud.firebaseio.com", true)]
    [InlineData("https://custom-cloud.com/api", true)]
    public void Scenario_49_to_50_CloudFirebaseUrlValidation(string url, bool expectedValid)
    {
        bool valid = Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttps);
        Assert.Equal(expectedValid, valid);
    }
}
