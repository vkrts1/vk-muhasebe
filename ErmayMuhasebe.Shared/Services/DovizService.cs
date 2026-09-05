using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Net.Http;
using System.Linq;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Services
{
    public class DovizService
    {
        private readonly DatabaseService _dbService;
        private readonly HttpClient _http;

        private readonly ExternalApiService _externalApi;

        public DovizService(DatabaseService dbService, HttpClient http, ExternalApiService externalApi)
        {
            _dbService = dbService;
            _http = http;
            _externalApi = externalApi;
        }

        public async Task<decimal> GetLiveRateAsync(string dovizKodu)
        {
            try
            {
                // Önce veritabanına bak
                var kurRecord = await _dbService.GetSonDovizKurAsync(dovizKodu);
                
                // Eğer bugün için kur yoksa, TCMB'den çekmeyi dene
                if (kurRecord == null || kurRecord.Tarih.Date < DateTime.Today)
                {
                    await UpdateRatesFromTcmbAsync();
                    
                    // Eğer TCMB başarısız olduysa harici API'yi dene
                    kurRecord = await _dbService.GetSonDovizKurAsync(dovizKodu);
                    if (kurRecord == null || kurRecord.Tarih.Date < DateTime.Today)
                    {
                        await UpdateRatesFromExternalAsync();
                        kurRecord = await _dbService.GetSonDovizKurAsync(dovizKodu);
                    }
                }

                return kurRecord?.Satis ?? 0;
            }
            catch
            {
                // İnternet yoksa veritabanındaki en son olanı döndür
                var fallback = await _dbService.GetSonDovizKurAsync(dovizKodu);
                return fallback?.Satis ?? 0;
            }
        }

        public async Task UpdateRatesFromTcmbAsync()
        {
            try
            {
                var response = await _http.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml");
                var doc = XDocument.Parse(response);

                var yeniKurlar = doc.Descendants("Currency")
                    .Select(x => new DovizKur
                    {
                        Kod = x.Attribute("CurrencyCode")?.Value ?? "",
                        Isim = x.Element("Isim")?.Value ?? x.Element("CurrencyName")?.Value ?? "",
                        Alis = ParseDecimal(x.Element("ForexBuying")?.Value),
                        Satis = ParseDecimal(x.Element("ForexSelling")?.Value),
                        EfektifAlis = ParseDecimal(x.Element("BanknoteBuying")?.Value),
                        EfektifSatis = ParseDecimal(x.Element("BanknoteSelling")?.Value),
                        Tarih = DateTime.Today
                    })
                    .Where(x => !string.IsNullOrEmpty(x.Kod))
                    .ToList();

                foreach (var k in yeniKurlar)
                {
                    await _dbService.SaveDovizKurAsync(k);
                }
                
                await UpdateSupplementalRatesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TCMB Error: {ex}. Switching to fallback.");
                await UpdateRatesFromExternalAsync(); 
            }
        }

        public async Task UpdateRatesFromExternalAsync()
        {
            var tryBased = await _externalApi.GetExchangeRatesBackupAsync("TRY");
            if (tryBased.Count == 0) return;

            var targetCodes = new Dictionary<string, string> { 
                { "USD", "ABD DOLARI" }, 
                { "EUR", "EURO" }, 
                { "GBP", "İNGİLİZ STERLİNİ" } 
            };

            foreach(var kvp in targetCodes)
            {
                if (tryBased.TryGetValue(kvp.Key, out decimal rateToTry) && rateToTry > 0)
                {
                    decimal tryVal = 1 / rateToTry;
                    await _dbService.SaveDovizKurAsync(new DovizKur {
                        Kod = kvp.Key, 
                        Isim = kvp.Value,
                        Alis = tryVal * 0.998m, 
                        Satis = tryVal, 
                        Tarih = DateTime.Today
                    });
                }
            }

            await UpdateSupplementalRatesAsync();
        }

        public async Task UpdateSupplementalRatesAsync()
        {
            try 
            {
                var tryBased = await _externalApi.GetExchangeRatesBackupAsync("TRY");
                
                // Gold (XAU) and Silver (XAG)
                var metals = new Dictionary<string, string> { 
                    { "XAU", "GRAM ALTIN" }, 
                    { "XAG", "GRAM GÜMÜŞ" } 
                };

                foreach(var kvp in metals)
                {
                    if (tryBased.TryGetValue(kvp.Key, out decimal rateToTry) && rateToTry > 0)
                    {
                        decimal tryVal = 1 / rateToTry;
                        await _dbService.SaveDovizKurAsync(new DovizKur {
                            Kod = kvp.Key, 
                            Isim = kvp.Value,
                            Alis = tryVal * 0.995m, 
                            Satis = tryVal, 
                            Tarih = DateTime.Today
                        });
                    }
                }
            }
            catch { }
        }

        public async Task<decimal> GetTcmbPolicyRateAsync()
        {
            try
            {
                var html = await _http.GetStringAsync("https://www.tcmb.gov.tr/");
                var match = System.Text.RegularExpressions.Regex.Match(html, @"politika faizi olan [^>]*yüzde\s*(\d+(?:[.,]\d+)?)");
                if (match.Success)
                {
                    var rateStr = match.Groups[1].Value.Replace(',', '.');
                    if (decimal.TryParse(rateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal rate))
                    {
                        return rate;
                    }
                }
                
                var matchFallback = System.Text.RegularExpressions.Regex.Match(html, @"yüzde\s*(\d+(?:[.,]\d+)?)[^>]*politika faizi");
                if (matchFallback.Success)
                {
                    var rateStr = matchFallback.Groups[1].Value.Replace(',', '.');
                    if (decimal.TryParse(rateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal rate))
                    {
                        return rate;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to fetch TCMB policy rate: {ex.Message}");
            }

            return 50.0m;
        }

        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            return decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result) ? result : 0;
        }
    }
}
