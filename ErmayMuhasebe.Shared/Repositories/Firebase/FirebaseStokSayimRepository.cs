using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseStokSayimRepository : BaseFirebaseRepository<StokSayimFisi>, IStokSayimRepository
{
    public FirebaseStokSayimRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "StokSayimFisleri";

    public async Task<List<StokSayimDetay>> GetDetaylarAsync(int fisId)
    {
        if (!_firebaseService.IsConfigured) return new List<StokSayimDetay>();
        
        // Use nested list strategy like Fatura/Siparis
        var allInFis = await _firebaseService.GetAllAsync<List<StokSayimDetay>>("StokSayimDetaylar"); 
        // Note: actually FirebaseService doesn't have a GetNestedForItemAsync, 
        // but it has GetFaturaDetaylarAsync etc. 
        // I'll just use the underlying service directly if I can or add a generic GetNestedAsync.
        
        try {
            var list = await _firebaseService.GetAllAsync<StokSayimDetay>($"StokSayimDetaylar/{fisId}");
            return list;
        } catch { return new List<StokSayimDetay>(); }
    }

    public async Task<List<StokSayimDetay>> GetAllDetaylarAsync()
    {
        if (!_firebaseService.IsConfigured) return new List<StokSayimDetay>();
        return await _firebaseService.GetAllNestedAsync<StokSayimDetay>("StokSayimDetaylar");
    }
    
    public async Task<int> SaveWithDetailsAsync(StokSayimFisi fis, List<StokSayimDetay> detaylar)
    {
        int id = await SaveAsync(fis);
        if (_firebaseService.IsConfigured)
        {
            await _firebaseService.SaveAsync("StokSayimDetaylar", detaylar, id);
        }
        return id;
    }
}
