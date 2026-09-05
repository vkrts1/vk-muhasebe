using System.Threading.Tasks;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Repositories.DataProviders;

/// <summary>
/// Platform bağımsız veri sağlama arayüzü.
/// Masaüstü (SQLite) ve Web (Firebase) sürümleri bu arayüzü kullanır.
/// </summary>
public interface IDataProvider
{
    ICariRepository Cariler { get; }
    IStokRepository Stoklar { get; }
    IFaturaRepository Faturalar { get; }
    IBankaRepository Bankalar { get; }
    IKasaRepository Kasalar { get; }
    ISiparisRepository Siparisler { get; }
    ITeklifRepository Teklifler { get; }
    ICekRepository Cekler { get; }
    ISenetRepository Senetler { get; }
    IKrediKartiRepository KrediKartlari { get; }
    IEftRepository EftIslemleri { get; }
    IStokSayimRepository StokSayimlar { get; }
    IPortfoyRepository Portfolyo { get; }
    IHedefRepository Hedefler { get; }
    IDovizRepository DovizKurlari { get; }
    IBelgeArsivRepository BelgeArsiv { get; }
    INoteRepository Notes { get; }
    IMusteriTakipRepository MusteriTakip { get; }

    Task<int> SaveChangesAsync();
    
    // Database initialization and cache management (Legacy support moved here)
    Task InitializeAsync();
    Task InitializeAsync(string dbNameOrPath);
    void InvalidateAllCache();
}
