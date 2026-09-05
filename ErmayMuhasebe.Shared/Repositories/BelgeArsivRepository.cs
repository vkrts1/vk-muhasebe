using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class BelgeArsivRepository : BaseRepository<BelgeArsiv>, IBelgeArsivRepository
{
    public BelgeArsivRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<BelgeArsiv>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<BelgeArsiv>()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.Tarih)
            .ToListAsync();
    }

    public override async Task<BelgeArsiv?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<BelgeArsiv>()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public override async Task<int> SaveAsync(BelgeArsiv entity)
    {
        var db = await GetConnectionAsync();
        if (entity.Id != 0) await db.UpdateAsync(entity);
        else await db.InsertAsync(entity);
        
        await _syncService.SyncGenericAsync("BelgeArsiv", entity, entity.Id);
        return entity.Id;
    }

    public override async Task<int> DeleteAsync(BelgeArsiv entity)
    {
        entity.IsDeleted = true;
        return await SaveAsync(entity);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return 0;
        return await DeleteAsync(entity);
    }

    public override async Task<List<BelgeArsiv>> GetDeletedAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<BelgeArsiv>().Where(x => x.IsDeleted).ToListAsync();
    }

    public override async Task RestoreAsync(BelgeArsiv entity)
    {
        entity.IsDeleted = false;
        await SaveAsync(entity);
    }

    public async Task<List<BelgeArsiv>> GetByKategoriAsync(string kategori)
    {
        var db = await GetConnectionAsync();
        return await db.Table<BelgeArsiv>()
            .Where(x => x.Kategori == kategori && !x.IsDeleted)
            .OrderByDescending(x => x.Tarih)
            .ToListAsync();
    }
}
