using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseBankaRepository : BaseFirebaseRepository<BankaKart>, IBankaRepository
{
    public FirebaseBankaRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Bankalar";

    public async Task<List<BankaHareket>> GetHareketlerAsync(int bankaId)
    {
        var all = await GetAllHareketlerAsync();
        return all.Where(h => h.BankaId == bankaId).ToList();
    }

    public async Task<List<BankaHareket>> GetAllHareketlerAsync()
    {
        return await _firebaseService.GetAllAsync<BankaHareket>("BankaHareketler");
    }

    public async Task<decimal> GetBakiyeAsync(int bankaId)
    {
        var hareketler = await GetHareketlerAsync(bankaId);
        return hareketler.Sum(h => h.Giren - h.Cikan);
    }

    public async Task<int> SaveHareketAsync(BankaHareket hareket)
    {
        if (hareket.Id == 0)
        {
            var all = await _firebaseService.GetAllAsync<BankaHareket>("BankaHareketler");
            hareket.Id = all.Any() ? all.Max(h => h.Id) + 1 : 1;
        }
        await _firebaseService.SaveAsync("BankaHareketler", hareket, hareket.Id);
        return hareket.Id;
    }

    public async Task<int> DeleteHareketAsync(BankaHareket hareket)
    {
        return await DeleteHareketAsync(hareket.Id);
    }

    public async Task<int> DeleteHareketAsync(int id)
    {
        await _firebaseService.DeleteAsync("BankaHareketler", id);
        return 1;
    }

    public async Task<int> RecalculateBalanceAsync(int bankaId)
    {
        try
        {
            var banka = await GetByIdAsync(bankaId);
            if (banka == null) return 0;

            decimal bakiye = banka.AcilisBakiyesi;

            if (banka.KartTuru == "Kasa")
            {
                var hareketler = await _firebaseService.GetAllAsync<KasaHareket>("KasaHareketler");
                bakiye += hareketler.Where(h => h.KasaId == bankaId).Sum(h => h.Giren - h.Cikan);
            }
            else
            {
                var hareketler = await GetHareketlerAsync(bankaId);
                bakiye += hareketler.Sum(h => h.Giren - h.Cikan);
            }

            banka.GuncelBakiye = bakiye;
            await SaveAsync(banka);
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Recalculate Banka Balance Error: {ex.Message}");
            return 0;
        }
    }
}
