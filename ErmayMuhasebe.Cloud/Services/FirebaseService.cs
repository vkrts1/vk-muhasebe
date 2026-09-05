using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using ErmayMuhasebe.Models;
using Microsoft.Extensions.Configuration;
using Blazored.LocalStorage;

using ErmayMuhasebe.Services;

using Microsoft.JSInterop;

namespace ErmayMuhasebe.Cloud.Services
{
    public class FirebaseService : IFirebaseService
    {
        private FirebaseClient? _firebase;
        private string _baseUrl;
        private readonly BrowserStorageService _browserStorage;
        private readonly ClientAuthService _auth;
        private readonly IYearContext _yearContext;
        private readonly ISyncLocalStorageService _syncStorage;
        
        // In-Memory Fallback for Demo/Unconfigured state (deprecated - now using BrowserStorage)
        private static readonly Dictionary<string, List<object>> _inMemoryDb = new();

        public FirebaseService(IConfiguration config, ILocalStorageService localStorage, ISyncLocalStorageService syncStorage, ClientAuthService auth, IJSRuntime jsRuntime, IYearContext yearContext)
        {
            _auth = auth;
            _yearContext = yearContext;
            _syncStorage = syncStorage;
            _browserStorage = new BrowserStorageService(localStorage, jsRuntime);
            
            string? baseUrl = null;
            string? token = null;

            if (syncStorage.ContainKey("firebase_url"))
            {
                baseUrl = syncStorage.GetItem<string>("firebase_url");
                token = syncStorage.GetItem<string>("firebase_token") ?? "";
            }
            
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = config["Firebase:BaseUrl"];
                token = config["Firebase:AuthSecret"] ?? "";
            }

            _baseUrl = baseUrl ?? "";
            
            if (IsConfigured)
            {
                var options = new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(token ?? "") };
                _firebase = new FirebaseClient(_baseUrl, options);
            }
            else
            {
                 Console.WriteLine("Warning: Firebase is not configured. Using browser storage. (BaseUrl: " + _baseUrl + ")");
            }

            // Always ensure seed data exists for demo purposes if nothing is there
            SeedInMemoryData();
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_baseUrl) && !_baseUrl.Contains("YOUR_PROJECT_ID");

        private void SeedInMemoryData()
        {
            if (!_inMemoryDb.ContainsKey("Cariler") || !_inMemoryDb["Cariler"].Any())
            {
                _inMemoryDb["Cariler"] = new List<object>
                {
                    new CariKart { Id = 1, Unvan = "ERMAY YAZILIM LTD", CariKod = "C-0001", Grup = "Müşteri", Tur = "Alici", Telefon = "0212 111 22 33", Borc = 5000 },
                    new CariKart { Id = 2, Unvan = "DEMO TEDARİK A.Ş.", CariKod = "C-0002", Grup = "Tedarikçi", Tur = "Satici", Alacak = 2500 }
                };
            }
        }

        public void SetConfig(string baseUrl, string secret)
        {
             _baseUrl = baseUrl;
              var options = new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(secret ?? "") };
            _firebase = new FirebaseClient(_baseUrl, options);
        }

        // --- GENERIC METHODS ---
        private string GetYearlyPath(string resourceName)
        {
            // /companies/{companyId}/years/{year}/{resourceName}
            return $"companies/{_auth.ActiveTenantId}/years/{_yearContext.CurrentYear}/{resourceName}";
        }

        public async Task<List<T>> GetAllAsync<T>(string resourceName) where T : class
        {
            if (!IsConfigured) 
            {
                // Use BrowserStorage for persistence
                return await _browserStorage.GetAllAsync<T>(resourceName);
            }
            
            try 
            {
                var collection = await _firebase!
                    .Child(GetYearlyPath(resourceName))
                    .OnceAsync<T>();

                var list = new List<T>();
                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        var obj = item.Object;
                        if (obj != null)
                        {
                            // Safety: Ensure ID is populated from key if missing or zero
                            var prop = obj.GetType().GetProperty("Id");
                            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
                            {
                                int currentId = (int)(prop.GetValue(obj) ?? 0);
                                if (currentId == 0 && int.TryParse(item.Key, out int idFromKey))
                                {
                                    prop.SetValue(obj, idFromKey);
                                }
                            }
                            list.Add(obj);
                        }
                    }
                }

                // --- Multi-Tenant Filtering ---
                if (typeof(ITenantEntity).IsAssignableFrom(typeof(T)))
                {
                    list = list.Where(x => ((ITenantEntity)x).TenantId == _auth.ActiveTenantId).ToList();
                    Console.WriteLine($"[FirebaseService] Multi-Tenant Filter applied for {_auth.ActiveTenantId}. Remaining: {list.Count}");
                }

                Console.WriteLine($"[FirebaseService] Fetched {list.Count} items from Firebase: {resourceName}");
                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase Error ({resourceName}): {ex.Message}");
                return new List<T>();
            }
        }

        public async Task SaveAsync<T>(string resourceName, T item, int id) where T : class
        {
             // 1. ALWAYS persist to LocalStorage first (Source of Truth for Offline-First)
             await _browserStorage.SaveAsync(resourceName, item, id);

             if (!IsConfigured) return;

             if (item is ITenantEntity tenantEntity)
             {
                 tenantEntity.TenantId = _auth.ActiveTenantId;
             }

             try 
             {
                 await _firebase!
                    .Child(GetYearlyPath(resourceName))
                    .Child(id.ToString())
                    .PutAsync(item);
                 Console.WriteLine($"[FirebaseService] Saved to Firebase: {resourceName}/{id}");
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[FirebaseService] Firebase Sync Failed, but saved locally: {ex.Message}");
                 // Future: Add to a more robust sync queue if background sync is needed
                 await AddToSyncQueueAsync(resourceName, "Put", id, item);
             }
        }
        public async Task<List<T>> GetAllNestedAsync<T>(string resourceName) where T : class
        {
            if (!IsConfigured) return new List<T>();
            try
            {
                var collection = await _firebase!
                    .Child(GetYearlyPath(resourceName))
                    .OnceAsync<List<T>>();

                if (collection != null)
                {
                    return collection.SelectMany(x => x.Object ?? new List<T>()).ToList();
                }
                return new List<T>();
            }
            catch { return new List<T>(); }
        }

        public async Task DeleteAsync(string resourceName, int id)
        {
            // 1. ALWAYS delete from LocalStorage first
            await _browserStorage.DeleteAsync(resourceName, id);

            if (!IsConfigured) return;

            try 
            {
                await _firebase!
                    .Child(GetYearlyPath(resourceName))
                    .Child(id.ToString())
                    .DeleteAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FirebaseService] Firebase Delete Failed, but deleted locally: {ex.Message}");
                await AddToSyncQueueAsync(resourceName, "Delete", id, null);
            }
        }

        private async Task AddToSyncQueueAsync(string resourceName, string operation, int id, object? data)
        {
             try 
             {
                 var queue = await _browserStorage.GetAllAsync<SyncQueueItem>("SyncQueue");
                 queue.Add(new SyncQueueItem 
                 { 
                     EntityType = resourceName, 
                     Operation = operation, 
                     EntityId = id.ToString(),
                     JsonData = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : "",
                     CreatedAt = DateTime.Now
                 });
                 await _browserStorage.SaveListAsync("SyncQueue", queue);
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[FirebaseService] SyncQueue Error: {ex.Message}");
             }
        }

        public async Task ProcessSyncQueueAsync()
        {
            if (!IsConfigured) return;

            var queue = await _browserStorage.GetAllAsync<SyncQueueItem>("SyncQueue");
            if (!queue.Any()) return;

            Console.WriteLine($"[FirebaseService] Processing {queue.Count} items in sync queue...");
            var remaining = new List<SyncQueueItem>();

            foreach (var item in queue)
            {
                try 
                {
                    if (item.Operation == "Put")
                    {
                        // We don't have the original type T here easily without reflection or generic queue
                        // For simplicity in this demo/MVP, we use the string data
                        await _firebase!.Child(GetYearlyPath(item.EntityType)).Child(item.EntityId).PutAsync(item.JsonData);
                    }
                    else if (item.Operation == "Delete")
                    {
                        await _firebase!.Child(GetYearlyPath(item.EntityType)).Child(item.EntityId).DeleteAsync();
                    }
                }
                catch 
                {
                    item.RetryCount++;
                    if (item.RetryCount < 5) remaining.Add(item);
                }
            }

            await _browserStorage.SaveListAsync("SyncQueue", remaining);
        }

        // --- SPECIFIC WRAPPERS ---
        public async Task<List<CariKart>> GetCarilerAsync() => await GetAllAsync<CariKart>("Cariler");
        public async Task SaveCariAsync(CariKart item) => await SaveAsync("Cariler", item, item.Id);
        public async Task DeleteCariAsync(int id) => await DeleteAsync("Cariler", id);

        public async Task<List<StokKart>> GetStoklarAsync() => await GetAllAsync<StokKart>("Stoklar");
        public async Task SaveStokAsync(StokKart item) => await SaveAsync("Stoklar", item, item.Id);
        
        public async Task<List<Fatura>> GetFaturalarAsync() => await GetAllAsync<Fatura>("Faturalar");
        public async Task SaveFaturaAsync(Fatura item) => await SaveAsync("Faturalar", item, item.Id);
        
        public async Task<List<Siparis>> GetSiparislerAsync() => await GetAllAsync<Siparis>("Siparisler");
        public async Task<List<Teklif>> GetTekliflerAsync() => await GetAllAsync<Teklif>("Teklifler");
        public async Task<List<BankaKart>> GetBankalarAsync() => await GetAllAsync<BankaKart>("Bankalar");
        public async Task<List<CariHareket>> GetCariHareketlerAsync() => await GetAllAsync<CariHareket>("CariHareketler");
        public async Task<List<StokHareket>> GetStokHareketlerAsync() => await GetAllAsync<StokHareket>("StokHareketler");

        public async Task<List<FaturaDetay>> GetFaturaDetaylarAsync(int faturaId)
        {
            if (!IsConfigured)
            {
                var key = $"FaturaDetaylar_{faturaId}";
                return await _browserStorage.GetAllAsync<FaturaDetay>(key);
            }
            try
            {
                var collection = await _firebase!
                    .Child(GetYearlyPath("FaturaDetaylar"))
                    .Child(faturaId.ToString())
                    .OnceAsync<FaturaDetay>();
                
                if (collection == null || !collection.Any())
                {
                    // Fallback for different JSON structure (single object array)
                    try {
                        var list = await _firebase!
                            .Child(GetYearlyPath("FaturaDetaylar"))
                            .Child(faturaId.ToString())
                            .OnceSingleAsync<List<FaturaDetay>>();
                        return list ?? new List<FaturaDetay>();
                    } catch { return new List<FaturaDetay>(); }
                }
                return collection.Select(x => x.Object).ToList();
            }
            catch { return new List<FaturaDetay>(); }
        }

        public async Task<List<SiparisDetay>> GetSiparisDetaylarAsync(int id)
        {
            if (!IsConfigured)
            {
                var key = $"SiparisDetaylar_{id}";
                return await _browserStorage.GetAllAsync<SiparisDetay>(key);
            }
            try
            {
                var collection = await _firebase!
                    .Child(GetYearlyPath("SiparisDetaylar"))
                    .Child(id.ToString())
                    .OnceAsync<SiparisDetay>();
                
                if (collection == null || !collection.Any())
                {
                    try {
                        var list = await _firebase!
                            .Child(GetYearlyPath("SiparisDetaylar"))
                            .Child(id.ToString())
                            .OnceSingleAsync<List<SiparisDetay>>();
                        return list ?? new List<SiparisDetay>();
                    } catch { return new List<SiparisDetay>(); }
                }
                return collection.Select(x => x.Object).ToList();
            }
            catch { return new List<SiparisDetay>(); }
        }
        
        public async Task SaveFaturaDetaylarAsync(int faturaId, List<FaturaDetay> details) 
        {
             if (!IsConfigured)
             {
                 var key = $"FaturaDetaylar_{faturaId}";
                 await _browserStorage.SaveListAsync(key, details);
                 return;
             }
              await _firebase!.Child(GetYearlyPath("FaturaDetaylar")).Child(faturaId.ToString()).PutAsync(details);
         }

         public async Task SaveSiparisAsync(Siparis item) => await SaveAsync("Siparisler", item, item.Id);

         public async Task SaveSiparisDetaylarAsync(int siparisId, List<SiparisDetay> details) 
         {
              if (!IsConfigured)
              {
                  var key = $"SiparisDetaylar_{siparisId}";
                  await _browserStorage.SaveListAsync(key, details);
                  return;
              }
              await _firebase!.Child(GetYearlyPath("SiparisDetaylar")).Child(siparisId.ToString()).PutAsync(details);
         }

         public async Task SaveTeklifAsync(Teklif item) => await SaveAsync("Teklifler", item, item.Id);

         public async Task SaveTeklifDetaylarAsync(int teklifId, List<TeklifDetay> details) 
         {
              if (!IsConfigured)
              {
                  var key = $"TeklifDetaylar_{teklifId}";
                  await _browserStorage.SaveListAsync(key, details);
                  return;
              }
              await _firebase!.Child(GetYearlyPath("TeklifDetaylar")).Child(teklifId.ToString()).PutAsync(details);
         }

        public async Task<List<TeklifDetay>> GetTeklifDetaylarAsync(int id)
        {
            if (!IsConfigured)
            {
                var key = $"TeklifDetaylar_{id}";
                return await _browserStorage.GetAllAsync<TeklifDetay>(key);
            }
            try
            {
                var collection = await _firebase!
                    .Child("TeklifDetaylar")
                    .Child(id.ToString())
                    .OnceAsync<TeklifDetay>();
                
                if (collection == null || !collection.Any())
                {
                    try {
                        var list = await _firebase!
                            .Child("TeklifDetaylar")
                            .Child(id.ToString())
                            .OnceSingleAsync<List<TeklifDetay>>();
                        return list ?? new List<TeklifDetay>();
                    } catch { return new List<TeklifDetay>(); }
                }
                return collection.Select(x => x.Object).ToList();
            }
            catch { return new List<TeklifDetay>(); }
        }

        public async Task<List<KasaHareket>> GetKasaHareketlerAsync() => await GetAllAsync<KasaHareket>("KasaHareketler");
        public async Task<List<BankaHareket>> GetBankaHareketlerAsync() => await GetAllAsync<BankaHareket>("BankaHareketler");

        public async Task<List<Cek>> GetCeklerAsync() => await GetAllAsync<Cek>("Cekler");
        public async Task SaveCekAsync(Cek item) => await SaveAsync("Cekler", item, item.Id);
        
        public async Task<List<Senet>> GetSenetlerAsync() => await GetAllAsync<Senet>("Senetler");
        public async Task<List<int>> GetAvailableYearsAsync()
        {
            if (!IsConfigured)
            {
                if (_syncStorage.ContainKey("ermay_registered_years"))
                {
                    var years = _syncStorage.GetItem<List<int>>("ermay_registered_years");
                    if (years != null && years.Any())
                        return years.OrderByDescending(x => x).ToList();
                }
                return new List<int> { 2024, 2025 }; 
            }
            try 
            {
                var years = await _firebase!
                    .Child($"companies/{_auth.ActiveTenantId}/years")
                    .OnceAsync<object>();
                
                var list = new List<int>();
                foreach (var y in years)
                {
                    if (int.TryParse(y.Key, out int year)) list.Add(year);
                }

                // Also ensure local registry is synced for offline use
                if (list.Any())
                {
                    _syncStorage.SetItem("ermay_registered_years", list);
                }

                return list.OrderByDescending(x => x).ToList();
            }
            catch 
            { 
                if (_syncStorage.ContainKey("ermay_registered_years"))
                {
                    var years = _syncStorage.GetItem<List<int>>("ermay_registered_years");
                    return years?.OrderByDescending(x => x).ToList() ?? new List<int> { DateTime.Now.Year };
                }
                return new List<int> { DateTime.Now.Year }; 
            }
        }

        public void RegisterYearLocally(int year)
        {
            var years = new List<int>();
            if (_syncStorage.ContainKey("ermay_registered_years"))
            {
                years = _syncStorage.GetItem<List<int>>("ermay_registered_years") ?? new();
            }
            if (!years.Contains(year))
            {
                years.Add(year);
                _syncStorage.SetItem("ermay_registered_years", years);
            }
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            _syncStorage.SetItem(key, value);
            if (!IsConfigured) return;

            try
            {
                await _firebase!.Child($"companies/{_auth.ActiveTenantId}/settings/{key}").PutAsync(value);
            }
            catch (Exception ex) { Console.WriteLine($"[FirebaseService] SaveSetting Error: {ex.Message}"); }
        }

        public async Task DeleteYearAsync(int year)
        {
            if (!IsConfigured) return;
            try 
            {
                await _firebase!
                    .Child($"companies/{_auth.ActiveTenantId}/years")
                    .Child(year.ToString())
                    .DeleteAsync();
            }
            catch (Exception ex) { Console.WriteLine($"[FirebaseService] DeleteYear Error: {ex.Message}"); }
        }
    }
}
