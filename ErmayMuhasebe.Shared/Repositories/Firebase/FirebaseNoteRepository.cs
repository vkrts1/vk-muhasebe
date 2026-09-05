using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseNoteRepository : BaseFirebaseRepository<Note>, INoteRepository
{
    protected override string ResourceName => "Notes";

    public FirebaseNoteRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    public async Task<List<Note>> GetByRelatedAsync(string type, int id)
    {
        var all = await GetAllAsync();
        return all.Where(n => n.RelatedType == type && n.RelatedId == id && !n.IsDeleted)
                  .OrderByDescending(n => n.IsPinned)
                  .ThenByDescending(n => n.CreatedAt)
                  .ToList();
    }
}
