using System.Threading.Tasks;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Repositories.Firebase;

namespace ErmayMuhasebe.Repositories.DataProviders;

/// <summary>
/// Firebase (Bulut) için IDataProvider implementasyonu.
/// Mevcut FirebaseUnitOfWork sınıfını sarmalar veya extend eder.
/// </summary>
public class FirebaseDataProvider : FirebaseUnitOfWork, IDataProvider
{
    public FirebaseDataProvider(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    // All methods are now implemented in the base FirebaseUnitOfWork class.
    // Overriding is not necessary as the base implementation already handles common logic.
}
