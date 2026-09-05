using System.Threading.Tasks;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Repositories.DataProviders;

/// <summary>
/// SQLite (Masaüstü) için IDataProvider implementasyonu.
/// Mevcut UnitOfWork sınıfını sarmalar veya extend eder.
/// </summary>
public class SqliteDataProvider : UnitOfWork, IDataProvider
{
    public SqliteDataProvider(DatabaseService dbService) : base(dbService)
    {
    }

    // All methods are now implemented in the base UnitOfWork class.
    // Overriding is not necessary as the base implementation already delegates to _dbService.
}
