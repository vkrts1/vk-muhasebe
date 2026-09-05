using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace ErmayMuhasebe.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _http;
        
        // Bu anahtarlar normalde Settings'den gelecek, şimdilik placeholder.
        public string ExchangeRateApiKey { get; set; } = "82eb1525975168e961cf7904";
        public string PositionStackApiKey { get; set; } = "e56ae2923409f7a5e646314532dc24e6"; 
        public string EmailableApiKey { get; set; } = "live_0a28e1372851b76d24a0";

        public ExternalApiService(HttpClient http)
        {
            _http = http;
        }

        #region Finance & Exchange Rates
        public async Task<Dictionary<string, decimal>> GetExchangeRatesGlobalAsync(string baseCurrency = "USD")
        {
            try
            {
                // Frankfurter is an open-source, neutral global data source
                var url = $"https://api.frankfurter.app/latest?from={baseCurrency}";
                var response = await _http.GetFromJsonAsync<JsonElement>(url);
                
                var rates = response.GetProperty("rates");
                var result = new Dictionary<string, decimal>();
                foreach (var prop in rates.EnumerateObject())
                {
                    result[prop.Name] = prop.Value.GetDecimal();
                }
                result[baseCurrency] = 1.0m;
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Global API Error: {ex.Message}");
            }
            return new();
        }

        public async Task<Dictionary<string, decimal>> GetExchangeRatesBackupAsync(string baseCurrency = "USD")
        {
            try
            {
                if (string.IsNullOrEmpty(ExchangeRateApiKey)) return new();
                var url = $"https://v6.exchangerate-api.com/v6/{ExchangeRateApiKey}/latest/{baseCurrency}";
                var response = await _http.GetFromJsonAsync<JsonElement>(url);
                if (response.GetProperty("result").GetString() == "success")
                {
                    var rates = response.GetProperty("conversion_rates");
                    var result = new Dictionary<string, decimal>();
                    foreach (var prop in rates.EnumerateObject()) { result[prop.Name] = prop.Value.GetDecimal(); }
                    return result;
                }
            }
            catch { }
            return new();
        }
        #endregion

        #region Geocoding
        public async Task<(double lat, double lon)?> GetCoordinatesAsync(string address)
        {
            // Internal use for visualization only, uses private PositionStack API
            try
            {
                if (string.IsNullOrEmpty(PositionStackApiKey)) return null;
                var url = $"http://api.positionstack.com/v1/forward?access_key={PositionStackApiKey}&query={Uri.EscapeDataString(address)}&limit=1";
                var response = await _http.GetFromJsonAsync<JsonElement>(url);
                var data = response.GetProperty("data");
                if (data.GetArrayLength() > 0)
                {
                    var first = data[0];
                    return (first.GetProperty("latitude").GetDouble(), first.GetProperty("longitude").GetDouble());
                }
            }
            catch { }
            return null;
        }
        #endregion

        #region Validation (Internal Checks)
        public Task<bool> ValidateEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return Task.FromResult(false);
            return Task.FromResult(Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"));
        }

        public Task<bool> ValidateVatAsync(string countryCode, string vatNumber)
        {
            if (string.IsNullOrWhiteSpace(vatNumber)) return Task.FromResult(false);
            // Basit bir format kontrolü veya her zaman true dönen bir placeholder
            return Task.FromResult(vatNumber.Length >= 10);
        }

        public bool ValidateIban(string iban)
        {
            if (string.IsNullOrEmpty(iban)) return false;
            iban = iban.Replace(" ", "").ToUpper();
            if (!Regex.IsMatch(iban, "^[A-Z0-9]{15,34}$")) return false;

            string swapped = iban.Substring(4) + iban.Substring(0, 4);
            string numeric = "";
            foreach (char c in swapped) numeric += (char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString());

            System.Numerics.BigInteger ibanNumber = System.Numerics.BigInteger.Parse(numeric);
            return ibanNumber % 97 == 1;
        }

        public bool ValidatePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var cleaned = Regex.Replace(phone, @"[^\d]", "");
            return cleaned.Length >= 10;
        }

        #endregion
    }
}
