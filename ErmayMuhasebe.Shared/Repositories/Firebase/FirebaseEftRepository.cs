using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseEftRepository : BaseFirebaseRepository<EftIslem>, IEftRepository
{
    public FirebaseEftRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "EftIslemleri";

    public async Task<EftIslem?> GetByNoAsync(string no)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.DekontNo == no);
    }
}
