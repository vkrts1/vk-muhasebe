using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class DovizRepository : BaseRepository<DovizKur>, IDovizRepository
{
    public DovizRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<DovizKur>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<DovizKur>().OrderByDescending(x => x.Tarih).ToListAsync();
    }

    public async Task<List<DovizKur>> GetLatestAsync()
    {
        var db = await GetConnectionAsync();
        var all = await db.Table<DovizKur>().ToListAsync();
        
        return all
            .GroupBy(x => x.Kod)
            .Select(g => g.OrderByDescending(x => x.Tarih).First())
            .ToList();
    }
    public override async Task<DovizKur?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<DovizKur>().FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<int> SaveAsync(DovizKur entity)
    {
        var db = await GetConnectionAsync();
        if (entity.Id != 0)
            return await db.UpdateAsync(entity);
        else
            return await db.InsertAsync(entity);
    }

    public override async Task<int> DeleteAsync(DovizKur entity)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync(entity);
    }
    
    public override async Task<int> DeleteAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<DovizKur>(id);
    }
    
    public override Task<List<DovizKur>> GetDeletedAsync() => Task.FromResult(new List<DovizKur>());
    public override Task RestoreAsync(DovizKur entity) => Task.CompletedTask;
}
