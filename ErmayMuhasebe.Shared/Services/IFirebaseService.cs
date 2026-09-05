using System.Collections.Generic;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Services;

/// <summary>
/// Firebase servisi için interface
/// Platform-specific implementasyonlar bu interface'i implement eder
/// </summary>
public interface IFirebaseService
{
    bool IsConfigured { get; }
    
    Task<List<T>> GetAllAsync<T>(string resourceName) where T : class;
    Task<List<T>> GetAllNestedAsync<T>(string resourceName) where T : class;
    Task SaveAsync<T>(string resourceName, T item, int id) where T : class;
    Task DeleteAsync(string resourceName, int id);
    Task ProcessSyncQueueAsync();
    void SetConfig(string baseUrl, string secret);
    Task SaveSettingAsync(string key, string value);

    Task<List<FaturaDetay>> GetFaturaDetaylarAsync(int faturaId);
    Task SaveFaturaDetaylarAsync(int faturaId, List<FaturaDetay> details);
    Task<List<SiparisDetay>> GetSiparisDetaylarAsync(int siparisId);
    Task SaveSiparisDetaylarAsync(int siparisId, List<SiparisDetay> details);
    Task<List<TeklifDetay>> GetTeklifDetaylarAsync(int teklifId);
    Task SaveTeklifDetaylarAsync(int teklifId, List<TeklifDetay> details);
    Task<List<int>> GetAvailableYearsAsync();
    Task DeleteYearAsync(int year);
}

