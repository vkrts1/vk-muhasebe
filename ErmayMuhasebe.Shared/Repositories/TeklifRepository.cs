using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class TeklifRepository : BaseRepository<Teklif>, ITeklifRepository
{
    public TeklifRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<Teklif>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Teklif>()
            .OrderByDescending(t => t.Tarih)
            .ToListAsync();
    }

    public override async Task<Teklif?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Teklif>()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public override async Task<int> SaveAsync(Teklif entity)
    {
        var db = await GetConnectionAsync();

        if (entity.Id != 0)
        {
            await db.UpdateAsync(entity);
        }
        else
        {
            await db.InsertAsync(entity);
        }

        await _syncService.SyncTeklifAsync(entity);
        return entity.Id;
    }

    public override async Task<int> DeleteAsync(Teklif entity)
    {
        var db = await GetConnectionAsync();
        if (entity != null && entity.Id > 0)
        {
            await _syncService.DeleteTeklifAsync(entity.Id);
            await _syncService.DeleteTeklifDetaylarAsync(entity.Id);
        }
        return await db.DeleteAsync(entity);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        var db = await GetConnectionAsync();
        if (id > 0)
        {
            await _syncService.DeleteTeklifAsync(id);
            await _syncService.DeleteTeklifDetaylarAsync(id);
        }
        return await db.ExecuteAsync("DELETE FROM Teklif WHERE Id = ?", id);
    }

    public override Task<List<Teklif>> GetDeletedAsync() => Task.FromResult(new List<Teklif>());
    public override Task RestoreAsync(Teklif entity) => Task.CompletedTask;

    public async Task<List<TeklifDetay>> GetDetaylarAsync(int teklifId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<TeklifDetay>()
            .Where(d => d.TeklifId == teklifId)
            .ToListAsync();
    }

    public async Task<List<TeklifDetay>> GetAllDetaylarAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<TeklifDetay>().ToListAsync();
    }

    public async Task<int> SaveWithDetailsAsync(Teklif teklif, List<TeklifDetay> detaylar)
    {
        var db = await GetConnectionAsync();
        
        await db.RunInTransactionAsync(tran =>
        {
            if (teklif.Id != 0)
            {
                tran.Update(teklif);
            }
            else
            {
                tran.Insert(teklif);
            }

            // Clear old details
            tran.Execute("DELETE FROM TeklifDetay WHERE TeklifId = ?", teklif.Id);

            foreach (var d in detaylar)
            {
                d.TeklifId = teklif.Id;
                tran.Insert(d);
            }
        });

        await _syncService.SyncTeklifAsync(teklif);
        await _syncService.SyncTeklifDetaylarAsync(teklif.Id, detaylar);
        return teklif.Id;
    }
}
