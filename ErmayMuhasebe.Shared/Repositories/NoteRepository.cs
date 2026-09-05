using System.Collections.Generic;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories;

public class NoteRepository : BaseRepository<Note>, INoteRepository
{
    public NoteRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<Note>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Note>().Where(x => !x.IsDeleted).ToListAsync();
    }

    public override async Task<Note?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Note>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public override async Task<int> SaveAsync(Note entity)
    {
        var db = await GetConnectionAsync();
        if (entity.Id != 0) await db.UpdateAsync(entity); else await db.InsertAsync(entity);
        await _syncService.SyncNoteAsync(entity);
        return entity.Id;
    }

    public override async Task<int> DeleteAsync(Note entity)
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

    public override async Task<List<Note>> GetDeletedAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Note>().Where(x => x.IsDeleted).ToListAsync();
    }

    public override async Task RestoreAsync(Note entity)
    {
        entity.IsDeleted = false;
        await SaveAsync(entity);
    }

    public async Task<List<Note>> GetByRelatedAsync(string type, int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Note>()
                       .Where(n => n.RelatedType == type && n.RelatedId == id && !n.IsDeleted)
                       .OrderByDescending(n => n.IsPinned)
                       .ThenByDescending(n => n.CreatedAt)
                       .ToListAsync();
    }
}
