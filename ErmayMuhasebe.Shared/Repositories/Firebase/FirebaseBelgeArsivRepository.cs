using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseBelgeArsivRepository : BaseFirebaseRepository<BelgeArsiv>, IBelgeArsivRepository
{
    public FirebaseBelgeArsivRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "BelgeArsiv";

    public async Task<List<BelgeArsiv>> GetByKategoriAsync(string kategori)
    {
        var all = await GetAllAsync();
        return all.Where(x => x.Kategori == kategori).OrderByDescending(x => x.Tarih).ToList();
    }
}
