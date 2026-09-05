using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseCekRepository : BaseFirebaseRepository<Cek>, ICekRepository
{
    public FirebaseCekRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Cekler";

    public async Task<List<Cek>> GetByCariIdAsync(int cariId)
    {
        var all = await GetAllAsync();
        return all.Where(c => c.CariId == cariId).ToList();
    }

    public async Task<int> SaveWithTransactionAsync(Cek cek)
    {
        return await SaveAsync(cek);
    }
}
