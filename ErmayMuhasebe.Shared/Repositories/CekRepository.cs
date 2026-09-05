using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class CekRepository : BaseRepository<Cek>, ICekRepository
{
    public CekRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<Cek>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Cek>()
            .OrderBy(c => c.VadeTarihi)
            .ToListAsync();
    }

    public override async Task<Cek?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Cek>()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public override async Task<int> SaveAsync(Cek entity)
    {
        var db = await GetConnectionAsync();
        if (entity.Id != 0)
            await db.UpdateAsync(entity);
        else
            await db.InsertAsync(entity);
        return entity.Id;
    }

    public override async Task<int> DeleteAsync(Cek entity)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync(entity);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.ExecuteAsync("DELETE FROM Cek WHERE Id = ?", id);
    }

    public override Task<List<Cek>> GetDeletedAsync() => Task.FromResult(new List<Cek>());
    public override Task RestoreAsync(Cek entity) => Task.CompletedTask;

    public async Task<List<Cek>> GetByCariIdAsync(int cariId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Cek>()
            .Where(c => c.CariId == cariId)
            .ToListAsync();
    }

    public async Task<int> SaveWithTransactionAsync(Cek cek)
    {
        await _dbService.SaveCekWithTransactionAsync(cek);
        return 1;
    }
}
