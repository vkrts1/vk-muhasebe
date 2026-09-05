using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class StokSayimRepository : BaseRepository<StokSayimFisi>, IStokSayimRepository
{
    public StokSayimRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<StokSayimFisi>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<StokSayimFisi>()
            .OrderByDescending(x => x.Tarih)
            .ToListAsync();
    }

    public override async Task<StokSayimFisi?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<StokSayimFisi>()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<int> SaveAsync(StokSayimFisi entity)
    {
        // This is simplified, usually assumes details are handled separately or by SaveWithDetailsAsync
        // But for IRepository compatibility:
        var db = await GetConnectionAsync();
        if (entity.Id != 0)
        {
            return await db.UpdateAsync(entity);
        }
        else
        {
            return await db.InsertAsync(entity);
        }
    }

    public override async Task<int> DeleteAsync(StokSayimFisi entity)
    {
        return await DeleteAsync(entity.Id);
    }

    public override async Task<int> DeleteAsync(int id)
    {
         var db = await GetConnectionAsync();
         // Delete details first
         var details = await db.Table<StokSayimDetay>().Where(d => d.FisId == id).ToListAsync();
         foreach(var d in details) await db.DeleteAsync(d);
         
         return await db.DeleteAsync<StokSayimFisi>(id);
    }

    public async Task<List<StokSayimDetay>> GetDetaylarAsync(int fisId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<StokSayimDetay>().Where(x => x.FisId == fisId).ToListAsync();
    }

    public async Task<List<StokSayimDetay>> GetAllDetaylarAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<StokSayimDetay>().ToListAsync();
    }

    public async Task<int> SaveWithDetailsAsync(StokSayimFisi fis, List<StokSayimDetay> detaylar)
    {
        var db = await GetConnectionAsync();
        await db.RunInTransactionAsync(tran =>
        {
            if (fis.Id != 0) tran.Update(fis);
            else tran.Insert(fis);

            // Delete old details
            var oldDetails = tran.Table<StokSayimDetay>().Where(x => x.FisId == fis.Id).ToList();
            foreach (var d in oldDetails) tran.Delete(d);

            // Insert new details
            foreach (var d in detaylar)
            {
                d.FisId = fis.Id;
                tran.Insert(d);
            }
        });
        return fis.Id;
    }

    public override Task<List<StokSayimFisi>> GetDeletedAsync() => Task.FromResult(new List<StokSayimFisi>());
    public override Task RestoreAsync(StokSayimFisi entity) => Task.CompletedTask;
}
