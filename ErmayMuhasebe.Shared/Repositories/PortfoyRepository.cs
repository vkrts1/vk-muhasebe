using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

public class PortfoyRepository : BaseRepository<PortfoyKart>, IPortfoyRepository
{
    public PortfoyRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<PortfoyKart>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<PortfoyKart>()
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public override async Task<PortfoyKart?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<PortfoyKart>()
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public override async Task<int> SaveAsync(PortfoyKart entity)
    {
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

    public override async Task<int> DeleteAsync(PortfoyKart entity)
    {
        return await DeleteAsync(entity.Id);
    }

    public override async Task<int> DeleteAsync(int id)
    {
         var db = await GetConnectionAsync();
         return await db.DeleteAsync<PortfoyKart>(id);
    }

    public override Task<List<PortfoyKart>> GetDeletedAsync() => Task.FromResult(new List<PortfoyKart>());
    public override Task RestoreAsync(PortfoyKart entity) => Task.CompletedTask;
}
