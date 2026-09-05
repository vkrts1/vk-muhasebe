using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseSenetRepository : BaseFirebaseRepository<Senet>, ISenetRepository
{
    public FirebaseSenetRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Senetler";

    public async Task<List<Senet>> GetByCariIdAsync(int cariId)
    {
        var all = await GetAllAsync();
        return all.Where(s => s.CariId == cariId).ToList();
    }
}
