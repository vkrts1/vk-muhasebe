using System.Collections.Generic;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Repositories
{
    public interface IMusteriTakipRepository : IRepository<MusteriTakipKlasor>
    {
        Task<List<MusteriTakipKlasor>> GetKlasorlerAsync();
        Task<MusteriTakipKlasor?> GetByCariIdAsync(int cariId);
        Task<List<MusteriTakipDetay>> GetDetaylarByKlasorIdAsync(int klasorId);
        Task<List<MusteriTakipDetay>> GetDetaylarByTipAsync(int klasorId, string tip);
        Task<int> SaveDetayAsync(MusteriTakipDetay detay);
        Task<int> DeleteDetayAsync(int id);
    }
}
