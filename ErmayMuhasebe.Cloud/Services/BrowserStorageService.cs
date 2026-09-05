using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using System.Security.Cryptography;
using System.Text;
using System.IO;

using Microsoft.JSInterop;

namespace ErmayMuhasebe.Cloud.Services
{
    /// <summary>
    /// Browser-based persistent storage using LocalStorage
    /// Used as fallback when Firebase is not configured
    /// </summary>
    public class BrowserStorageService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _jsRuntime;
        private const string DB_PREFIX = "ermay_db_";
        private const string CRYPTO_KEY = "Ermay@2026!SecureKey#99"; 

        public BrowserStorageService(ILocalStorageService localStorage, IJSRuntime jsRuntime)
        {
            _localStorage = localStorage;
            _jsRuntime = jsRuntime;
        }

        private async Task<string> EncryptAsync(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            try
            {
                return await _jsRuntime.InvokeAsync<string>("cryptoHelper.encrypt", plainText, CRYPTO_KEY);
            }
            catch { return plainText; }
        }

        private async Task<string> DecryptAsync(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            try
            {
                return await _jsRuntime.InvokeAsync<string>("cryptoHelper.decrypt", cipherText, CRYPTO_KEY);
            }
            catch { return cipherText; } 
        }

        public async Task<List<T>> GetAllAsync<T>(string resourceName) where T : class
        {
            try
            {
                var key = DB_PREFIX + resourceName;
                if (await _localStorage.ContainKeyAsync(key))
                {
                    var encryptedJson = await _localStorage.GetItemAsStringAsync(key);
                    var json = await DecryptAsync(encryptedJson ?? "");
                    var items = JsonSerializer.Deserialize<List<T>>(json);
                    return items ?? new List<T>();
                }
                return new List<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error loading {resourceName}: {ex.Message}");
                return new List<T>();
            }
        }

        public async Task SaveAsync<T>(string resourceName, T item, int id) where T : class
        {
            try
            {
                var key = DB_PREFIX + resourceName;
                var list = await GetAllAsync<T>(resourceName);
                
                var existingIdx = list.FindIndex(x => {
                    var prop = x.GetType().GetProperty("Id");
                    return prop != null && (int)prop.GetValue(x)! == id;
                });

                if (existingIdx >= 0)
                    list[existingIdx] = item;
                else
                    list.Add(item);

                var json = JsonSerializer.Serialize(list);
                var encryptedJson = await EncryptAsync(json);
                await _localStorage.SetItemAsStringAsync(key, encryptedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error saving to {resourceName}: {ex.Message}");
            }
        }

        public async Task DeleteAsync(string resourceName, int id)
        {
            try
            {
                var key = DB_PREFIX + resourceName;
                if (!await _localStorage.ContainKeyAsync(key)) return;

                var encryptedJson = await _localStorage.GetItemAsStringAsync(key);
                var json = await DecryptAsync(encryptedJson ?? "[]");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                var filteredList = new List<JsonElement>();
                foreach (var element in root.EnumerateArray())
                {
                    if (element.TryGetProperty("Id", out var idProp) && idProp.GetInt32() == id)
                    {
                        continue; 
                    }
                    filteredList.Add(element.Clone());
                }

                var newJson = JsonSerializer.Serialize(filteredList);
                var newEncryptedJson = await EncryptAsync(newJson);
                await _localStorage.SetItemAsStringAsync(key, newEncryptedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error deleting from {resourceName}: {ex.Message}");
            }
        }

        public async Task<T?> GetAsync<T>(string resourceName, int id) where T : class
        {
            var all = await GetAllAsync<T>(resourceName);
            return all.FirstOrDefault(x => {
                var prop = x.GetType().GetProperty("Id");
                return prop != null && (int)prop.GetValue(x)! == id;
            });
        }

        public async Task SaveListAsync<T>(string resourceName, List<T> items) where T : class
        {
            try
            {
                var key = DB_PREFIX + resourceName;
                var json = JsonSerializer.Serialize(items);
                var encryptedJson = await EncryptAsync(json);
                await _localStorage.SetItemAsStringAsync(key, encryptedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error saving list to {resourceName}: {ex.Message}");
            }
        }

        public async Task ClearAllAsync()
        {
            try
            {
                var keys = (await _localStorage.KeysAsync()).Where(k => k.StartsWith(DB_PREFIX)).ToList();
                foreach (var key in keys)
                {
                    await _localStorage.RemoveItemAsync(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BrowserStorage] Error clearing: {ex.Message}");
            }
        }
    }
}
