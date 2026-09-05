using QuestPDF.Infrastructure;
using System.Net.Http;
using System.Threading.Tasks;
using ErmayMuhasebe.Services;
using System;

namespace ErmayMuhasebe.Cloud.Services
{
    public class FontInitializationService
    {
        private readonly HttpClient _httpClient;
        private readonly PdfService _pdfService;

        public FontInitializationService(HttpClient httpClient, PdfService pdfService)
        {
            _httpClient = httpClient;
            _pdfService = pdfService;
        }

        public async Task InitializeFontsAsync()
        {
            try
            {
                // 1. Try to set License First (Isolated)
                try {
                    QuestPDF.Settings.License = LicenseType.Community;
                } catch (Exception ex) {
                    Console.WriteLine("QuestPDF License Initialization Failed (SAFE): " + ex.Message);
                }

                // 2. Wait a bit for WASM runtime to fully settle after UI interaction
                await Task.Delay(5000); 

                // 3. Register fonts ONLY if we can access the FontManager
                // This is a dangerous area, so it has its own catch.
                try {
                    // Pre-check if SkiaSharp is even callable

                    var regularFontBytes = await _httpClient.GetByteArrayAsync("css/fonts/Roboto-Regular.ttf");
                    var boldFontBytes = await _httpClient.GetByteArrayAsync("css/fonts/Roboto-Bold.ttf");

                    using var regularStream = new System.IO.MemoryStream(regularFontBytes);
                    using var boldStream = new System.IO.MemoryStream(boldFontBytes);

                    QuestPDF.Drawing.FontManager.RegisterFont(regularStream);
                    QuestPDF.Drawing.FontManager.RegisterFont(boldStream);
                    Console.WriteLine("Fonts Registered Successfully.");
                } catch (Exception fontEx) {
                    Console.WriteLine("SkiaSharp Font Rendering is NOT available in this session: " + fontEx.Message);
                }
                
                // 4. Load Logo (Independent)
                try {
                    var logoBytes = await _httpClient.GetByteArrayAsync("_content/ErmayMuhasebe.Shared/images/ermay_logo.png");
                    _pdfService.LogoBytes = logoBytes;
                } catch { /* Silent fail for logo */ }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Global Font Service Failure: {ex.Message}");
            }
        }
    }
}
