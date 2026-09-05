using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class EftRepository : BaseRepository<EftIslem>, IEftRepository
{
    public EftRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<EftIslem>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<EftIslem>()
            .OrderByDescending(x => x.Tarih)
            .ToListAsync();
    }

    public override async Task<EftIslem?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<EftIslem>()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<int> SaveAsync(EftIslem entity)
    {
        return await _dbService.SaveEftIslemAsync(entity);
    }

    public override async Task<int> DeleteAsync(EftIslem entity)
    {
        return await _dbService.DeleteEftIslemAsync(entity.Id);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        return await _dbService.DeleteEftIslemAsync(id);
    }

    public async Task<EftIslem?> GetByNoAsync(string no)
    {
        var db = await GetConnectionAsync();
        return await db.Table<EftIslem>().FirstOrDefaultAsync(x => x.DekontNo == no);
    }

    public override Task<List<EftIslem>> GetDeletedAsync() => Task.FromResult(new List<EftIslem>());
    public override Task RestoreAsync(EftIslem entity) => Task.CompletedTask;
}
