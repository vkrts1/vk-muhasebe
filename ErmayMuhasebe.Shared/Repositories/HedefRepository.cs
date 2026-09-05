using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class HedefRepository : IHedefRepository
{
    private readonly DatabaseService _dbService;

    public HedefRepository(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _dbService.EnsureInitializedAsync();
        return _dbService.GetConnection();
    }

    // --- Aylik ---
    public async Task<List<SatisHedefi>> GetAylikHedeflerAsync(int yil)
    {
        var db = await GetConnectionAsync();
        return await db.Table<SatisHedefi>().Where(x => x.Yil == yil).ToListAsync();
    }

    public async Task<List<SatisHedefi>> GetAllAylikHedeflerAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<SatisHedefi>().ToListAsync();
    }

    public async Task<int> SaveAylikHedefAsync(SatisHedefi hedef)
    {
        var db = await GetConnectionAsync();
        if (hedef.Id != 0) return await db.UpdateAsync(hedef);
        return await db.InsertAsync(hedef);
    }

    public async Task<int> DeleteAylikHedefAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<SatisHedefi>(id);
    }

    // --- Haftalik ---
    public async Task<List<HaftalikSatisHedefi>> GetHaftalikHedeflerAsync(int yil)
    {
        var db = await GetConnectionAsync();
        return await db.Table<HaftalikSatisHedefi>().Where(x => x.Yil == yil).ToListAsync();
    }
    
    public async Task<List<HaftalikSatisHedefi>> GetAllHaftalikHedeflerAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<HaftalikSatisHedefi>().ToListAsync();
    }

    public async Task<int> SaveHaftalikHedefAsync(HaftalikSatisHedefi hedef)
    {
         var db = await GetConnectionAsync();
        if (hedef.Id != 0) return await db.UpdateAsync(hedef);
        return await db.InsertAsync(hedef);
    }

    public async Task<int> DeleteHaftalikHedefAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.DeleteAsync<HaftalikSatisHedefi>(id);
    }

    // --- Yillik ---
    public async Task<List<YillikSatisHedefi>> GetAllYillikHedeflerAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<YillikSatisHedefi>().ToListAsync();
    }

    public async Task<int> SaveYillikHedefAsync(YillikSatisHedefi hedef)
    {
        var db = await GetConnectionAsync();
        if (hedef.Id != 0) return await db.UpdateAsync(hedef);
        return await db.InsertAsync(hedef);
    }
}
