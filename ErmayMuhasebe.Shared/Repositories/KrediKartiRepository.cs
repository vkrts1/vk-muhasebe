using System.Collections.Generic;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class KrediKartiRepository : BaseRepository<KrediKartiIslem>, IKrediKartiRepository
{
    public KrediKartiRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<KrediKartiIslem>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<KrediKartiIslem>().ToListAsync();
    }

    public override async Task<KrediKartiIslem?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<KrediKartiIslem>().FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<int> SaveAsync(KrediKartiIslem entity)
    {
        return await _dbService.SaveKrediKartiIslemAsync(entity);
    }

    public override async Task<int> DeleteAsync(KrediKartiIslem entity)
    {
        return await _dbService.DeleteKrediKartiIslemAsync(entity.Id);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        return await _dbService.DeleteKrediKartiIslemAsync(id);
    }

    public async Task<KrediKartiIslem?> GetByNoAsync(string no)
    {
        var db = await GetConnectionAsync();
        return await db.Table<KrediKartiIslem>().FirstOrDefaultAsync(x => x.OnayKodu == no);
    }

    public override Task<List<KrediKartiIslem>> GetDeletedAsync() => Task.FromResult(new List<KrediKartiIslem>());
    public override Task RestoreAsync(KrediKartiIslem entity) => Task.CompletedTask;
}
