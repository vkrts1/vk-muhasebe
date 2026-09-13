using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class SenetRepository : BaseRepository<Senet>, ISenetRepository
{
    public SenetRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<Senet>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Senet>()
            .OrderBy(s => s.VadeTarihi)
            .ToListAsync();
    }

    public override async Task<Senet?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Senet>()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public override async Task<int> SaveAsync(Senet entity)
    {
        var db = await GetConnectionAsync();
        if (entity.Id != 0)
            await db.UpdateAsync(entity);
        else
            await db.InsertAsync(entity);

        await _syncService.SyncSenetAsync(entity);
        return entity.Id;
    }

    public override async Task<int> DeleteAsync(Senet entity)
    {
        var db = await GetConnectionAsync();
        if (entity != null && entity.Id > 0)
        {
            await _syncService.DeleteSenetAsync(entity.Id);
        }
        return await db.DeleteAsync(entity);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        var db = await GetConnectionAsync();
        if (id > 0)
        {
            await _syncService.DeleteSenetAsync(id);
        }
        return await db.ExecuteAsync("DELETE FROM Senet WHERE Id = ?", id);
    }

    public override Task<List<Senet>> GetDeletedAsync() => Task.FromResult(new List<Senet>());
    public override Task RestoreAsync(Senet entity) => Task.CompletedTask;

    public async Task<List<Senet>> GetByCariIdAsync(int cariId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Senet>()
            .Where(s => s.CariId == cariId)
            .ToListAsync();
    }
}
