using System.Net.Http.Json;
using Blazored.LocalStorage;

namespace ErmayMuhasebe.Cloud.Services
{
    public class I18nService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;
        private Dictionary<string, string> _translations = new();
        public string CurrentLanguage { get; private set; } = "tr";
        public event Action? OnLanguageChanged;

        public I18nService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            var savedLang = await _localStorage.GetItemAsync<string>("app_lang");
            CurrentLanguage = savedLang ?? "tr";
            await LoadTranslationsAsync(CurrentLanguage);
        }

        public async Task SetLanguageAsync(string lang)
        {
            if (CurrentLanguage == lang) return;
            CurrentLanguage = lang;
            await _localStorage.SetItemAsync("app_lang", lang);
            await LoadTranslationsAsync(lang);
            OnLanguageChanged?.Invoke();
        }

        private async Task LoadTranslationsAsync(string lang)
        {
            try
            {
                var dict = await _http.GetFromJsonAsync<Dictionary<string, string>>($"i18n/{lang}.json");
                if (dict != null)
                {
                    _translations = dict;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"I18n Load Error ({lang}): {ex.Message}");
            }
        }

        public string T(string key)
        {
            if (_translations.TryGetValue(key, out var val))
            {
                return val;
            }
            return key; // Return key if not found
        }
        
        public string this[string key] => T(key);
    }
}
