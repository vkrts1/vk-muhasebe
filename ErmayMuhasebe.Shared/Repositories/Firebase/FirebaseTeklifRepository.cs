using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseTeklifRepository : BaseFirebaseRepository<Teklif>, ITeklifRepository
{
    public FirebaseTeklifRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Teklifler";

    public async Task<List<TeklifDetay>> GetDetaylarAsync(int teklifId)
    {
        return await _firebaseService.GetTeklifDetaylarAsync(teklifId);
    }

    public async Task<List<TeklifDetay>> GetAllDetaylarAsync()
    {
        return await _firebaseService.GetAllNestedAsync<TeklifDetay>("TeklifDetaylar");
    }

    public async Task<int> SaveWithDetailsAsync(Teklif teklif, List<TeklifDetay> detaylar)
    {
        int id = await SaveAsync(teklif);
        await _firebaseService.SaveTeklifDetaylarAsync(id, detaylar);
        return id;
    }

    public override async Task<int> DeleteAsync(int id)
    {
        await _firebaseService.DeleteAsync("TeklifDetaylar", id);
        return await base.DeleteAsync(id);
    }

    public override async Task<int> DeleteAsync(Teklif entity)
    {
        if (entity == null) return 0;
        await _firebaseService.DeleteAsync("TeklifDetaylar", entity.Id);
        return await base.DeleteAsync(entity);
    }
}
