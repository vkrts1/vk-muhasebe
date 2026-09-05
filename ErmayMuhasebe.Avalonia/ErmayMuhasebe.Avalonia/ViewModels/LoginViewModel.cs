using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Services;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;
    private readonly Action<string?> _onLoginSuccess;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty] private bool _showResetPasswordPanel;
    [ObservableProperty] private string _resetUsername = "";
    [ObservableProperty] private bool _isResetCodeSent;
    [ObservableProperty] private string _resetVerificationCode = "";
    [ObservableProperty] private bool _isResetCodeVerified;
    [ObservableProperty] private string _resetOneTimePassword = "";
    [ObservableProperty] private string _resetNewPassword = "";
    [ObservableProperty] private string _successMessage = "";

    // İlk Kurulum & Hızlı Bulut Yapılandırması
    [ObservableProperty] private bool _showCloudConfigPanel;
    [ObservableProperty] private string _cloudUrl = "";
    [ObservableProperty] private string _cloudSecret = "";

    [RelayCommand]
    private void ToggleCloudConfig()
    {
        ShowCloudConfigPanel = !ShowCloudConfigPanel;
        ShowResetPasswordPanel = false;
        ErrorMessage = "";
        SuccessMessage = "";
        if (ShowCloudConfigPanel)
        {
            var config = _dbService.GetCloudConfig();
            CloudUrl = config.Url;
            CloudSecret = config.Secret;
        }
    }

    [RelayCommand]
    private void SaveCloudConfig()
    {
        try
        {
            _dbService.SetCloudConfig(CloudUrl?.Trim() ?? "", CloudSecret?.Trim() ?? "");
            SuccessMessage = "Bulut (Firebase) yapılandırması başarıyla kaydedildi.";
            ShowCloudConfigPanel = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Yapılandırma kaydedilemedi: {ex.Message}";
        }
    }


    public LoginViewModel(DatabaseService dbService, Action<string?> onLoginSuccess)
    {
        _dbService = dbService;
        _onLoginSuccess = onLoginSuccess;
        LoadSavedCredentials();
    }

    public void LoadSavedCredentials()
    {
        try
        {
            // Setup kurulumundan gelen kullanıcı/şifre yapılandırmasını kontrol et
            var setupUserPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ErmayMuhasebe",
                "setup_initial_user.json");

            if (System.IO.File.Exists(setupUserPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(setupUserPath);
                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    
                    Task.Run(async () =>
                    {
                        try
                        {
                            var conn = _dbService.GetGlobalConnection();
                            await conn.CreateTableAsync<Models.User>();

                            if (doc.RootElement.TryGetProperty("Users", out var usersArray))
                            {
                                bool hasCustomUser = false;
                                foreach (var userElem in usersArray.EnumerateArray())
                                {
                                    var uName = userElem.GetProperty("Username").GetString()?.Trim();
                                    var uPass = userElem.GetProperty("Password").GetString()?.Trim();

                                    var uEmail = userElem.TryGetProperty("Email", out var emElem) ? emElem.GetString()?.Trim() : null;

                                    if (!string.IsNullOrEmpty(uName) && !string.IsNullOrEmpty(uPass))
                                    {
                                        var existing = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Username == uName.ToLower());
                                        var salt = AuthService.GenerateSalt();
                                        var hash = AuthService.HashPassword(uPass, salt);

                                        if (existing != null)
                                        {
                                            existing.Password = hash;
                                            existing.PasswordSalt = salt;
                                            if (!string.IsNullOrEmpty(uEmail)) existing.Email = uEmail;
                                            await conn.UpdateAsync(existing);
                                        }
                                        else
                                        {
                                            await conn.InsertAsync(new Models.User
                                            {
                                                Username = uName.ToLower(),
                                                Password = hash,
                                                PasswordSalt = salt,
                                                Email = uEmail,
                                                Role = "Admin",
                                                CreatedAt = DateTime.Now
                                            });
                                        }

                                        if (uName.ToLower() != "admin")
                                        {
                                            hasCustomUser = true;
                                        }
                                    }
                                }

                                // Özel kullanıcılar girilmişse varsayılan admin/123 hesabını sil
                                if (hasCustomUser)
                                {
                                    var defaultAdmin = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Username == "admin");
                                    if (defaultAdmin != null)
                                    {
                                        await conn.DeleteAsync(defaultAdmin);
                                    }
                                }
                            }

                            // Fabrika Ayarları Sıfırlama Şifresi, SMTP ve Telegram Yapılandırması
                            var profil = await _dbService.GetFirmaProfiliAsync();
                            if (profil != null)
                            {
                                bool updated = false;
                                if (doc.RootElement.TryGetProperty("FactoryResetPassword", out var frpElem) && frpElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = frpElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) { profil.FactoryResetPassword = val; updated = true; }
                                }
                                if (doc.RootElement.TryGetProperty("SmtpEmail", out var seElem) && seElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = seElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) 
                                    { 
                                        profil.SmtpUser = val; 
                                        profil.SmtpHost = "smtp.gmail.com";
                                        profil.SmtpPort = 587;
                                        profil.SmtpSsl = true;
                                        updated = true; 
                                    }
                                }
                                if (doc.RootElement.TryGetProperty("SmtpPass", out var spElem) && spElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = spElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) { profil.SmtpPass = val; updated = true; }
                                }
                                if (doc.RootElement.TryGetProperty("GoogleClientId", out var gcElem) && gcElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = gcElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) { profil.GoogleClientId = val; updated = true; }
                                }
                                if (doc.RootElement.TryGetProperty("GoogleClientSecret", out var gcsElem) && gcsElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = gcsElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) { profil.GoogleClientSecret = val; updated = true; }
                                }
                                if (doc.RootElement.TryGetProperty("TelegramBotToken", out var tbElem) && tbElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = tbElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) { profil.TelegramBotToken = val; updated = true; }
                                }
                                if (doc.RootElement.TryGetProperty("TelegramChatId", out var tcElem) && tcElem.ValueKind == System.Text.Json.JsonValueKind.String)
                                {
                                    var val = tcElem.GetString()?.Trim();
                                    if (!string.IsNullOrEmpty(val)) { profil.TelegramChatId = val; updated = true; }
                                }
                                if (updated)
                                {
                                    await _dbService.SaveFirmaProfiliAsync(profil);
                                }
                            }
                        }
                        catch { }
                    });

                    // İlk kullanıcıyı form alanlarına doldur
                    if (doc.RootElement.TryGetProperty("Users", out var uArr) && uArr.GetArrayLength() > 0)
                    {
                        var first = uArr[0];
                        Username = first.GetProperty("Username").GetString() ?? "";
                        Password = first.GetProperty("Password").GetString() ?? "";
                        RememberMe = true;
                    }

                    // İşlendikten sonra geçici setup dosyasını temizle
                    System.IO.File.Delete(setupUserPath);
                }
                catch { }
            }

            var path = GetCredentialsPath();
            if (System.IO.File.Exists(path) && string.IsNullOrEmpty(Username))
            {
                var lines = System.IO.File.ReadAllLines(path);
                if (lines.Length >= 2)
                {
                    Username = AuthService.Decrypt(lines[0]);
                    Password = AuthService.Decrypt(lines[1]);
                    RememberMe = true;
                }
            }
        }
        catch { }
    }

    private void SaveCredentials()
    {
        try
        {
            var path = GetCredentialsPath();
            if (RememberMe)
            {
                var dir = System.IO.Path.GetDirectoryName(path);
                if (dir != null && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                
                System.IO.File.WriteAllLines(path, new[] 
                { 
                    AuthService.Encrypt(Username), 
                    AuthService.Encrypt(Password) 
                });
            }
            else if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch { }
    }

    private string GetCredentialsPath()
    {
        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ErmayMuhasebe",
            "login_settings.txt");
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        var trimmedUsername = Username.Trim().ToLower();
        var trimmedPassword = Password.Trim();

        if (string.IsNullOrEmpty(trimmedUsername) || string.IsNullOrEmpty(trimmedPassword))
        {
            ErrorMessage = "Kullanıcı adı ve şifre boş bırakılamaz.";
            return;
        }

        IsBusy = true;
        ErrorMessage = "";

        try
        {
            int retryCount = 0;
            while (retryCount < 3)
            {
                try
                {
                    var user = await _dbService.GetUserByUsernameAsync(trimmedUsername);
                    if (user != null && AuthService.VerifyPassword(trimmedPassword, user.Password!, user.PasswordSalt!))
                    {
                        Username = trimmedUsername;
                        Password = trimmedPassword;
                        _dbService.CurrentTenantId = user.TenantId ?? "default";
                        SaveCredentials();

                        _onLoginSuccess?.Invoke(user.Username ?? trimmedUsername);
                        return;
                    }
                    else
                    {
                        ErrorMessage = "Hatalı kullanıcı adı veya şifre.";
                        return;
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("not an error") || ex.Message.Contains("busy") || ex.Message.Contains("locked"))
                {
                    retryCount++;
                    ErrorMessage = $"Veritabanı meşgul, deneme {retryCount}/3...";
                    await Task.Delay(2000);
                    
                    if (retryCount >= 3)
                    {
                        ErrorMessage = $"Veritabanı kilitlendi. Lütfen bilgisayarı yeniden başlatmayı veya 'Ermay' işlemlerini sonlandırmayı deneyin. (Hata: {ex.Message})";
                        break;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Giriş hatası: " + ex.Message;
                    if (ex.InnerException != null) ErrorMessage += " -> " + ex.InnerException.Message;
                    break;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoginWithGoogleAsync()
    {
        IsBusy = true;
        ErrorMessage = "";
        SuccessMessage = "";

        try
        {
            var profil = await _dbService.GetFirmaProfiliAsync();
            var cloudConfig = _dbService.GetFullCloudConfig();

            // Client ID öncelik sırası: 1. FirmaProfili, 2. CloudConfig (kurulum)
            string clientId = profil?.GoogleClientId?.Trim() ?? "";
            if (string.IsNullOrEmpty(clientId)) clientId = cloudConfig?.GoogleClientId?.Trim() ?? "";
            string clientSecret = profil?.GoogleClientSecret?.Trim() ?? cloudConfig?.GoogleClientSecret?.Trim() ?? "";

            if (string.IsNullOrEmpty(clientId))
            {
                ErrorMessage = "Google Client ID tanımlanmamış. Lütfen kurulum esnasında veya Ayarlar menüsünden Google OAuth Client ID bilginizi girin.";
                return;
            }

            // Google OAuth Loopback HTTP Listener (Port 5000)
            int port = 5000;
            string redirectUri = $"http://localhost:{port}/";
            
            string googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=openid%20email%20profile";

            using (var listener = new System.Net.HttpListener())
            {
                listener.Prefixes.Add(redirectUri);
                try
                {
                    listener.Start();
                }
                catch
                {
                    // Port meşgulse alternatif port dene
                    port = 5001;
                    redirectUri = $"http://localhost:{port}/";
                    googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=openid%20email%20profile";
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(redirectUri);
                    listener.Start();
                }

                // Varsayılan tarayıcıda Google giriş sayfasını aç
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = googleAuthUrl,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    ErrorMessage = "Tarayıcı açılamadı. Lütfen varsayılan tarayıcınızı kontrol edin.";
                    listener.Stop();
                    return;
                }

                SuccessMessage = "Lütfen açılan tarayıcı penceresinden Google hesabınızı seçin...";

                // Tarayıcıdan gelecek yanıtı bekle (Maksimum 60 saniye zaman aşımı)
                var contextTask = listener.GetContextAsync();
                var completedTask = await Task.WhenAny(contextTask, Task.Delay(60000));

                if (completedTask != contextTask)
                {
                    listener.Stop();
                    ErrorMessage = "Google ile giriş zaman aşımına uğradı (60 sn).";
                    return;
                }

                var context = await contextTask;
                var request = context.Request;
                var code = request.QueryString["code"];
                var error = request.QueryString["error"];

                // Tarayıcıya şık bir başarı sayfası döndür
                var response = context.Response;
                string responseString = @"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <title>BAWSAQ - Giriş Başarılı</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #0f172a; color: #f8fafc; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }
        .card { background: #1e293b; padding: 40px; border-radius: 16px; text-align: center; max-width: 400px; box-shadow: 0 10px 25px -5px rgba(0,0,0,0.5); border: 1px solid #334155; }
        h1 { color: #38bdf8; font-size: 22px; margin-bottom: 10px; }
        p { color: #94a3b8; font-size: 14px; line-height: 1.5; }
        .success-icon { font-size: 48px; margin-bottom: 16px; }
    </style>
</head>
<body>
    <div class='card'>
        <div class='success-icon'>✨</div>
        <h1>Google ile Giriş Başarılı!</h1>
        <p>BAWSAQ Masaüstü uygulamasına dönebilirsiniz. Bu sekmeyi kapatabilirsiniz.</p>
    </div>
</body>
</html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html; charset=utf-8";
                using (var output = response.OutputStream)
                {
                    await output.WriteAsync(buffer, 0, buffer.Length);
                }
                listener.Stop();

                if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                {
                    ErrorMessage = "Google girişi iptal edildi veya yetki verilmedi.";
                    return;
                }

                // Google Token takası veya doğrudan Google Kullanıcı Profilini al
                string userEmail = "google_user@bawsaq.com";
                string googleUserName = "Google Kullanıcısı";

                try
                {
                    // Code ile token ve kullanıcı profilini sorgula
                    using (var client = new HttpClient())
                    {
                        var postParams = new Dictionary<string, string>
                        {
                            { "code", code },
                            { "client_id", clientId },
                            { "redirect_uri", redirectUri },
                            { "grant_type", "authorization_code" }
                        };
                        if (!string.IsNullOrEmpty(clientSecret))
                        {
                            postParams.Add("client_secret", clientSecret);
                        }

                        var tokenRes = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(postParams));

                        if (tokenRes.IsSuccessStatusCode)
                        {
                            var tokenJson = await tokenRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                            if (tokenJson.TryGetProperty("access_token", out var accTokenElem))
                            {
                                var accToken = accTokenElem.GetString();
                                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accToken);
                                var userinfoRes = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                                if (userinfoRes.IsSuccessStatusCode)
                                {
                                    var userinfo = await userinfoRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                                    if (userinfo.TryGetProperty("email", out var em)) userEmail = em.GetString() ?? userEmail;
                                    if (userinfo.TryGetProperty("name", out var nm)) googleUserName = nm.GetString() ?? googleUserName;
                                }
                            }
                        }
                    }
                }
                catch { }

                // Veritabanında kullanıcıyı kontrol et veya oluştur
                var conn = _dbService.GetGlobalConnection();
                await conn.CreateTableAsync<Models.User>();
                var targetUsername = userEmail.Split('@')[0].ToLower();

                var existingUser = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Email == userEmail || u.Username == targetUsername);
                if (existingUser == null)
                {
                    var salt = AuthService.GenerateSalt();
                    existingUser = new Models.User
                    {
                        Username = targetUsername,
                        Email = userEmail,
                        Role = "Admin",
                        PasswordSalt = salt,
                        Password = AuthService.HashPassword(Guid.NewGuid().ToString(), salt),
                        CreatedAt = DateTime.Now
                    };
                    await conn.InsertAsync(existingUser);
                }

                _dbService.CurrentTenantId = existingUser.TenantId ?? "default";
                SuccessMessage = $"Hoş geldiniz, {googleUserName}!";
                await Task.Delay(800);

                _onLoginSuccess?.Invoke(existingUser.Username ?? targetUsername);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Google ile giriş yapılırken hata oluştu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ForgotPassword()
    {
        ShowResetPasswordPanel = !ShowResetPasswordPanel;
        ErrorMessage = "";
        SuccessMessage = "";
        IsResetCodeSent = false;
        IsResetCodeVerified = false;
        ResetUsername = "";
        ResetVerificationCode = "";
        ResetOneTimePassword = "";
        ResetNewPassword = "";
        _generatedResetCode = "";
        _generatedOneTimePassword = "";
        OnPropertyChanged(nameof(ShowVerifyResetCodePanel));
    }

    public bool ShowVerifyResetCodePanel => IsResetCodeSent && !IsResetCodeVerified;

    private string _generatedResetCode = "";
    private string _generatedOneTimePassword = "";

    private string GenerateTempPassword()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new char[8];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }

    [RelayCommand]
    private async Task SendResetCodeAsync()
    {
        var username = string.IsNullOrWhiteSpace(ResetUsername) ? "admin" : ResetUsername.Trim().ToLower();
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            SuccessMessage = "";
            var conn = _dbService.GetGlobalConnection();
            var user = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                ErrorMessage = "Girdiğiniz kullanıcı adına ait hesap bulunamadı.";
                return;
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ErrorMessage = "Bu kullanıcının e-posta adresi tanımlanmamış. Lütfen yöneticinizle iletişime geçin.";
                return;
            }

            var randomCode = new Random().Next(100000, 999999).ToString();
            
            string subject = "Ermay Muhasebe - Şifre Sıfırlama Doğrulama Kodu";
            string body = $@"Hesap şifrenizi sıfırlamak için doğrulama kodu talep ettiniz.
            
Doğrulama Kodunuz: {randomCode}

Lütfen bu kodu sisteme girerek doğrulamayı tamamlayın.";

            await SendEmailAsync(user.Email.Trim(), subject, body);

            _generatedResetCode = randomCode;
            _generatedOneTimePassword = "";
            ResetOneTimePassword = "";
            ResetVerificationCode = "";
            ResetNewPassword = "";
            IsResetCodeSent = true;
            IsResetCodeVerified = false;
            OnPropertyChanged(nameof(ShowVerifyResetCodePanel));
            SuccessMessage = $"6 haneli doğrulama kodu {user.Email} adresine başarıyla gönderildi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Doğrulama kodu gönderilirken hata oluştu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendResetCodeViaTelegramAsync()
    {
        var username = string.IsNullOrWhiteSpace(ResetUsername) ? "admin" : ResetUsername.Trim().ToLower();
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            SuccessMessage = "";
            var conn = _dbService.GetGlobalConnection();
            var user = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                ErrorMessage = "Girdiğiniz kullanıcı adına ait hesap bulunamadı.";
                return;
            }
            if (string.IsNullOrWhiteSpace(user.TelegramChatId))
            {
                ErrorMessage = "Bu kullanıcının Telegram Chat ID bilgisi tanımlanmamış. Lütfen yöneticinizle iletişime geçin.";
                return;
            }

            var profil = await _dbService.GetFirmaProfiliAsync();
            if (string.IsNullOrWhiteSpace(profil?.TelegramBotToken))
            {
                ErrorMessage = "Sistem Telegram Bot Token tanımlanmamış.";
                return;
            }

            var randomCode = new Random().Next(100000, 999999).ToString();
            
            await TelegramService.SendVerificationCodeAsync(profil.TelegramBotToken, user.TelegramChatId, randomCode);

            _generatedResetCode = randomCode;
            _generatedOneTimePassword = "";
            ResetOneTimePassword = "";
            ResetVerificationCode = "";
            ResetNewPassword = "";
            IsResetCodeSent = true;
            IsResetCodeVerified = false;
            OnPropertyChanged(nameof(ShowVerifyResetCodePanel));
            SuccessMessage = "6 haneli doğrulama kodu Telegram ile başarıyla gönderildi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Telegram ile doğrulama kodu gönderilemedi: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var profil = await _dbService.GetFirmaProfiliAsync();
        if (profil != null && !string.IsNullOrWhiteSpace(profil.SmtpUser) && !string.IsNullOrWhiteSpace(profil.SmtpPass))
        {
            try
            {
                var host = !string.IsNullOrWhiteSpace(profil.SmtpHost) ? profil.SmtpHost : "smtp.gmail.com";
                var port = profil.SmtpPort > 0 ? profil.SmtpPort : 587;
                
                using (var smtp = new System.Net.Mail.SmtpClient(host, port))
                {
                    smtp.EnableSsl = profil.SmtpSsl;
                    smtp.Credentials = new System.Net.NetworkCredential(profil.SmtpUser.Trim(), profil.SmtpPass.Trim());
                    smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                    smtp.Timeout = 15000;

                    using (var msg = new System.Net.Mail.MailMessage())
                    {
                        msg.From = new System.Net.Mail.MailAddress(profil.SmtpUser.Trim(), "BAWSAQ Ön Muhasebe");
                        msg.To.Add(toEmail.Trim());
                        msg.Subject = subject;
                        msg.Body = body;
                        msg.IsBodyHtml = false;

                        await smtp.SendMailAsync(msg);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SMTP Gönderim Hatası: {ex.Message}");
                // SMTP hata verirse formsubmit fallback olarak dene
            }
        }

        // Fallback: FormSubmit Web Servisi
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                _subject = subject,
                email = "noreply@ermaymuhasebe.com",
                message = body,
                _captcha = "false"
            };

            var response = await client.PostAsJsonAsync($"https://formsubmit.co/ajax/{toEmail}", payload);
            if (!response.IsSuccessStatusCode)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"E-posta servisi yanıt vermedi: {response.StatusCode} - {errorResponse}");
            }
        }
    }

    [RelayCommand]
    private async Task VerifyResetCodeAsync()
    {
        if (string.IsNullOrEmpty(ResetVerificationCode))
        {
            ErrorMessage = "Lütfen doğrulama kodunu girin.";
            return;
        }

        if (ResetVerificationCode.Trim() != _generatedResetCode)
        {
            ErrorMessage = "Girdiğiniz doğrulama kodu hatalı.";
            return;
        }

        var username = string.IsNullOrWhiteSpace(ResetUsername) ? "admin" : ResetUsername.Trim().ToLower();
        try
        {
            IsBusy = true;
            ErrorMessage = "";
            SuccessMessage = "";
            var conn = _dbService.GetGlobalConnection();
            var user = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                ErrorMessage = "Kullanıcı bulunamadı.";
                return;
            }
            if (string.IsNullOrWhiteSpace(user.TelegramChatId))
            {
                ErrorMessage = "Tek kullanımlık şifrenin gönderilebilmesi için Telegram Chat ID'nizin kayıtlı olması gerekmektedir.";
                return;
            }

            var profil = await _dbService.GetFirmaProfiliAsync();
            if (string.IsNullOrWhiteSpace(profil?.TelegramBotToken))
            {
                ErrorMessage = "Telegram Bot Token yapılandırılmamış.";
                return;
            }

            var otp = GenerateTempPassword();
            
            string message = $"🔐 <b>Ermay Muhasebe - Tek Kullanımlık Şifre</b>\n\n" +
                             $"Doğrulama başarılı! Şifrenizi güncellemek için kullanacağınız tek kullanımlık şifreniz:\n\n" +
                             $"📌 <code>{otp}</code>\n\n" +
                             $"Lütfen bu şifreyi ve yeni şifrenizi ekrandaki alanlara girerek işlemi tamamlayın.";

            await TelegramService.SendMessageAsync(profil.TelegramBotToken, user.TelegramChatId, message);

            _generatedOneTimePassword = otp;
            IsResetCodeVerified = true;
            OnPropertyChanged(nameof(ShowVerifyResetCodePanel));
            SuccessMessage = "Kod başarıyla doğrulandı. Tek kullanımlık şifreniz Telegram botu üzerinden gönderildi.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Tek kullanımlık şifre gönderilirken hata oluştu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmResetPasswordAsync()
    {
        if (string.IsNullOrEmpty(ResetOneTimePassword))
        {
            ErrorMessage = "Lütfen Telegram botundan aldığınız tek kullanımlık şifreyi girin.";
            return;
        }

        if (ResetOneTimePassword.Trim() != _generatedOneTimePassword)
        {
            ErrorMessage = "Girdiğiniz tek kullanımlık şifre hatalı.";
            return;
        }

        var trimmedNewPassword = ResetNewPassword?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedNewPassword))
        {
            ErrorMessage = "Yeni şifre boş olamaz.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = "";
            SuccessMessage = "";
            var username = string.IsNullOrWhiteSpace(ResetUsername) ? "admin" : ResetUsername.Trim().ToLower();
            var conn = _dbService.GetGlobalConnection();
            var user = await conn.Table<Models.User>().FirstOrDefaultAsync(u => u.Username == username);
            if (user != null)
            {
                var salt = AuthService.GenerateSalt();
                user.Password = AuthService.HashPassword(trimmedNewPassword, salt);
                user.PasswordSalt = salt;
                await conn.UpdateAsync(user);
                
                SuccessMessage = $"'{user.Username}' şifresi başarıyla güncellendi. Yeni şifrenizle giriş yapabilirsiniz.";
                
                ShowResetPasswordPanel = false;
                IsResetCodeSent = false;
                IsResetCodeVerified = false;
                ResetUsername = "";
                ResetVerificationCode = "";
                ResetOneTimePassword = "";
                ResetNewPassword = "";
                _generatedResetCode = "";
                _generatedOneTimePassword = "";
                OnPropertyChanged(nameof(ShowVerifyResetCodePanel));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Şifre güncellenirken hata oluştu: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
