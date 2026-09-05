using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseSiparisRepository : BaseFirebaseRepository<Siparis>, ISiparisRepository
{
    public FirebaseSiparisRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Siparisler";

    public async Task<List<SiparisDetay>> GetDetaylarAsync(int siparisId)
    {
        return await _firebaseService.GetSiparisDetaylarAsync(siparisId);
    }

    public async Task<List<SiparisDetay>> GetAllDetaylarAsync()
    {
        return await _firebaseService.GetAllNestedAsync<SiparisDetay>("SiparisDetaylar");
    }

    public async Task<int> SaveWithDetailsAsync(Siparis siparis, List<SiparisDetay> detaylar)
    {
        int id = await SaveAsync(siparis);
        await _firebaseService.SaveSiparisDetaylarAsync(id, detaylar);
        return id;
    }

    public override async Task<int> DeleteAsync(int id)
    {
        await _firebaseService.DeleteAsync("SiparisDetaylar", id);
        return await base.DeleteAsync(id);
    }

    public override async Task<int> DeleteAsync(Siparis entity)
    {
        if (entity == null) return 0;
        await _firebaseService.DeleteAsync("SiparisDetaylar", entity.Id);
        return await base.DeleteAsync(entity);
    }
}
