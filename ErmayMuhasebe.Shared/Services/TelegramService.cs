using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Services;

/// <summary>
/// Sends verification codes via Telegram Bot API.
/// Free and reliable alternative to SMS.
/// </summary>
public class TelegramService
{
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Sends a verification code message to the specified Telegram chat.
    /// </summary>
    /// <param name="botToken">Telegram Bot API token from @BotFather</param>
    /// <param name="chatId">Target chat ID</param>
    /// <param name="verificationCode">6-digit verification code</param>
    public static async Task SendVerificationCodeAsync(string botToken, string chatId, string verificationCode)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new Exception("Telegram Bot Token ayarlanmamış. Ayarlar > Güvenlik bölümünden bot token'ı girin.");

        if (string.IsNullOrWhiteSpace(chatId))
            throw new Exception("Telegram Chat ID ayarlanmamış. Ayarlar > Güvenlik bölümünden chat ID'yi girin.");

        string message = $"🔐 Ermay Muhasebe - Doğrulama Kodu\n\n" +
                         $"Şifre sıfırlama doğrulama kodunuz:\n\n" +
                         $"📌 {verificationCode}\n\n" +
                         $"Bu kodu kimseyle paylaşmayın.\n" +
                         $"Eğer bu işlemi siz yapmadıysanız dikkate almayınız.";

        string url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "HTML"
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Telegram mesaj gönderilemedi: {response.StatusCode} - {errorBody}");
        }
    }

    /// <summary>
    /// Sends a test message to verify bot configuration is correct.
    /// </summary>
    public static async Task SendTestMessageAsync(string botToken, string chatId)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new Exception("Telegram Bot Token boş olamaz.");

        if (string.IsNullOrWhiteSpace(chatId))
            throw new Exception("Telegram Chat ID boş olamaz.");

        string message = "✅ Ermay Muhasebe - Telegram Bot Bağlantısı Başarılı!\n\n" +
                         "Bu bot, şifre sıfırlama doğrulama kodlarını size iletmek için kullanılacaktır.";

        string url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = message
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Telegram bağlantı testi başarısız: {response.StatusCode} - {errorBody}");
        }
    }

    /// <summary>
    /// Sends an arbitrary HTML message via Telegram Bot.
    /// </summary>
    public static async Task SendMessageAsync(string botToken, string chatId, string message)
    {
        if (string.IsNullOrWhiteSpace(botToken))
            throw new Exception("Telegram Bot Token ayarlanmamış.");

        if (string.IsNullOrWhiteSpace(chatId))
            throw new Exception("Telegram Chat ID ayarlanmamış.");

        string url = $"https://api.telegram.org/bot{botToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = message,
            parse_mode = "HTML"
        };

        var response = await _httpClient.PostAsJsonAsync(url, payload);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Telegram mesaj gönderilemedi: {response.StatusCode} - {errorBody}");
        }
    }
}
