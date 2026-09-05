using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseKrediKartiRepository : BaseFirebaseRepository<KrediKartiIslem>, IKrediKartiRepository
{
    public FirebaseKrediKartiRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "KrediKartiIslemleri";

    public async Task<KrediKartiIslem?> GetByNoAsync(string no)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.OnayKodu == no);
    }
}
