using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseHedefRepository : IHedefRepository
{
    private readonly IFirebaseService _firebaseService;

    public FirebaseHedefRepository(IFirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    // --- Aylik ---
    public async Task<List<SatisHedefi>> GetAylikHedeflerAsync(int yil)
    {
        if (!_firebaseService.IsConfigured) return new List<SatisHedefi>();
        var all = await _firebaseService.GetAllAsync<SatisHedefi>("SatisHedefleri");
        return all.Where(x => x.Yil == yil).ToList();
    }

    public async Task<List<SatisHedefi>> GetAllAylikHedeflerAsync()
    {
        if (!_firebaseService.IsConfigured) return new List<SatisHedefi>();
        return await _firebaseService.GetAllAsync<SatisHedefi>("SatisHedefleri");
    }

    public async Task<int> SaveAylikHedefAsync(SatisHedefi hedef)
    {
        if (!_firebaseService.IsConfigured) return 0;
        await _firebaseService.SaveAsync("SatisHedefleri", hedef, hedef.Id);
        return hedef.Id;
    }

    public async Task<int> DeleteAylikHedefAsync(int id)
    {
        if (!_firebaseService.IsConfigured) return 0;
        await _firebaseService.DeleteAsync("SatisHedefleri", id);
        return 1;
    }

    // --- Haftalik ---
    public async Task<List<HaftalikSatisHedefi>> GetHaftalikHedeflerAsync(int yil)
    {
        if (!_firebaseService.IsConfigured) return new List<HaftalikSatisHedefi>();
        var all = await _firebaseService.GetAllAsync<HaftalikSatisHedefi>("HaftalikSatisHedefleri");
        return all.Where(x => x.Yil == yil).ToList();
    }
    
    public async Task<List<HaftalikSatisHedefi>> GetAllHaftalikHedeflerAsync()
    {
        if (!_firebaseService.IsConfigured) return new List<HaftalikSatisHedefi>();
        return await _firebaseService.GetAllAsync<HaftalikSatisHedefi>("HaftalikSatisHedefleri");
    }

    public async Task<int> SaveHaftalikHedefAsync(HaftalikSatisHedefi hedef)
    {
         if (!_firebaseService.IsConfigured) return 0;
         await _firebaseService.SaveAsync("HaftalikSatisHedefleri", hedef, hedef.Id);
         return hedef.Id;
    }

    public async Task<int> DeleteHaftalikHedefAsync(int id)
    {
        if (!_firebaseService.IsConfigured) return 0;
        await _firebaseService.DeleteAsync("HaftalikSatisHedefleri", id);
        return 1;
    }

    // --- Yillik ---
    public async Task<List<YillikSatisHedefi>> GetAllYillikHedeflerAsync()
    {
        if (!_firebaseService.IsConfigured) return new List<YillikSatisHedefi>();
        return await _firebaseService.GetAllAsync<YillikSatisHedefi>("YillikSatisHedefleri");
    }

    public async Task<int> SaveYillikHedefAsync(YillikSatisHedefi hedef)
    {
         if (!_firebaseService.IsConfigured) return 0;
         await _firebaseService.SaveAsync("YillikSatisHedefleri", hedef, hedef.Id);
         return hedef.Id;
    }
}
