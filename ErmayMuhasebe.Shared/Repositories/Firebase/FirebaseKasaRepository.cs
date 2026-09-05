using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseKasaRepository : BaseFirebaseRepository<KasaHareket>, IKasaRepository
{
    public FirebaseKasaRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "KasaHareketler";

    public async Task<decimal> GetBakiyeAsync()
    {
        var all = await GetAllAsync();
        return all.Sum(h => h.Giren - h.Cikan);
    }

    public async Task<List<KasaHareket>> GetHareketlerByTarihAsync(DateTime baslangic, DateTime bitis)
    {
        var all = await GetAllAsync();
        return all.Where(h => h.Tarih >= baslangic && h.Tarih <= bitis).OrderByDescending(h => h.Tarih).ToList();
    }

    public async Task<List<KasaHareket>> GetHareketlerAsync(int kasaId)
    {
        var all = await GetAllAsync();
        return all.Where(x => x.KasaId == kasaId).OrderByDescending(x => x.Tarih).ToList();
    }

    public async Task<List<KasaHareket>> GetAllHareketlerAsync()
    {
        return await GetAllAsync();
    }
}
