using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebasePortfoyRepository : BaseFirebaseRepository<PortfoyKart>, IPortfoyRepository
{
    public FirebasePortfoyRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "PortfoyKartlari";
}
