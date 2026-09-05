using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseDovizRepository : BaseFirebaseRepository<DovizKur>, IDovizRepository
{
    public FirebaseDovizRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "DovizKurlari";
    
    public async Task<List<DovizKur>> GetLatestAsync()
    {
        var all = await GetAllAsync();
        return all
            .GroupBy(x => x.Kod)
            .Select(g => g.OrderByDescending(x => x.Tarih).First())
            .ToList();
    }
}
