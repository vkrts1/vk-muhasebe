using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase
{
    public class FirebaseMusteriTakipRepository : BaseFirebaseRepository<MusteriTakipKlasor>, IMusteriTakipRepository
    {
        protected override string ResourceName => "MusteriTakipKlasorler";
        private const string DetayResourceName = "MusteriTakipDetaylar";

        public FirebaseMusteriTakipRepository(IFirebaseService firebaseService) : base(firebaseService)
        {
        }

        public async Task<List<MusteriTakipKlasor>> GetKlasorlerAsync()
        {
            var list = await GetAllAsync();
            return list.Where(x => !x.IsDeleted).OrderByDescending(x => x.SonIslemTarihi).ToList();
        }

        public async Task<MusteriTakipKlasor?> GetByCariIdAsync(int cariId)
        {
            var list = await GetAllAsync();
            return list.FirstOrDefault(x => x.CariId == cariId && !x.IsDeleted);
        }

        public override async Task<int> SaveAsync(MusteriTakipKlasor entity)
        {
            entity.SonIslemTarihi = DateTime.Now;
            return await base.SaveAsync(entity);
        }

        public async Task<List<MusteriTakipDetay>> GetDetaylarByKlasorIdAsync(int klasorId)
        {
            var list = await _firebaseService.GetAllAsync<MusteriTakipDetay>(DetayResourceName);
            return list.Where(x => x.KlasorId == klasorId && !x.IsDeleted).OrderByDescending(x => x.Tarih).ToList();
        }

        public async Task<List<MusteriTakipDetay>> GetDetaylarByTipAsync(int klasorId, string tip)
        {
            var list = await _firebaseService.GetAllAsync<MusteriTakipDetay>(DetayResourceName);
            return list.Where(x => x.KlasorId == klasorId && x.Tip == tip && !x.IsDeleted).OrderByDescending(x => x.Tarih).ToList();
        }

        public async Task<int> SaveDetayAsync(MusteriTakipDetay detay)
        {
            if (detay.Id == 0)
            {
                var list = await _firebaseService.GetAllAsync<MusteriTakipDetay>(DetayResourceName);
                detay.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1;
                if (detay.Tarih == default) detay.Tarih = DateTime.Now;
            }
            await _firebaseService.SaveAsync(DetayResourceName, detay, detay.Id);

            var klasor = await GetByIdAsync(detay.KlasorId);
            if (klasor != null)
            {
                klasor.SonIslemTarihi = DateTime.Now;
                await SaveAsync(klasor);
            }

            return detay.Id;
        }

        public async Task<int> DeleteDetayAsync(int id)
        {
            var list = await _firebaseService.GetAllAsync<MusteriTakipDetay>(DetayResourceName);
            var item = list.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _firebaseService.SaveAsync(DetayResourceName, item, item.Id);
                return item.Id;
            }
            return 0;
        }
    }
}
