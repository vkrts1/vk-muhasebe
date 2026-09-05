using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseStokRepository : BaseFirebaseRepository<StokKart>, IStokRepository
{
    public FirebaseStokRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Stoklar";

    public async Task<StokKart?> GetByKodAsync(string kod)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(s => s.StokKodu == kod);
    }

    public async Task<StokKart?> GetByBarkodAsync(string barkod)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(s => s.Barkod == barkod);
    }

    public async Task<List<StokKart>> GetByKategoriAsync(string kategori)
    {
        var all = await GetAllAsync();
        return all.Where(s => s.Kategori == kategori).ToList();
    }

    public async Task<List<StokKart>> GetKritikStoklarAsync()
    {
        var all = await GetAllAsync();
        return all.Where(s => s.MinSeviye > 0 && s.Miktar <= s.MinSeviye).ToList();
    }

    public async Task<List<StokHareket>> GetHareketlerAsync(int stokId)
    {
        return await _firebaseService.GetAllAsync<StokHareket>("StokHareketler")
            .ContinueWith(t => t.Result.Where(h => h.StokId == stokId).ToList());
    }

    public async Task<int> SaveHareketAsync(StokHareket hareket)
    {
        if (hareket.Id == 0)
        {
            var all = await _firebaseService.GetAllAsync<StokHareket>("StokHareketler");
            hareket.Id = all.Any() ? all.Max(h => h.Id) + 1 : 1;
        }
        await _firebaseService.SaveAsync("StokHareketler", hareket, hareket.Id);
        return hareket.Id;
    }

    public async Task<int> DeleteHareketAsync(StokHareket hareket)
    {
        await _firebaseService.DeleteAsync("StokHareketler", hareket.Id);
        return 1;
    }

    public override async Task<int> DeleteAsync(StokKart entity)
    {
        await CleanupHareketlerAsync(entity.Id);
        return await base.DeleteAsync(entity);
    }

    public override async Task<int> DeleteAsync(int id)
    {
        await CleanupHareketlerAsync(id);
        return await base.DeleteAsync(id);
    }

    public async Task<List<StokHareket>> GetAllHareketlerAsync()
    {
        return await _firebaseService.GetAllAsync<StokHareket>("StokHareketler");
    }

    public async Task RecalculateCostsAsync(int? stokId = null)
    {
        try 
        {
            List<StokKart> stoklar;
            if (stokId.HasValue)
            {
                var s = await GetByIdAsync(stokId.Value);
                stoklar = s != null ? new List<StokKart> { s } : new List<StokKart>();
            }
            else
            {
                stoklar = await GetAllAsync();
            }
            
            var allHareketler = await GetAllHareketlerAsync();

            foreach (var stok in stoklar)
            {
                var movements = allHareketler.Where(x => x.StokId == stok.Id).OrderBy(x => x.Tarih).ThenBy(x => x.Id).ToList();
                
                decimal currentQuantity = 0;
                decimal currentTotalValue = 0;
                decimal averagePrice = 0;
                
                decimal totalSoldQuantity = 0;
                decimal totalSalesRevenue = 0;
                decimal averageSalesPrice = 0;

                decimal lastPurchasePrice = 0;
                decimal lastSalesPrice = 0;

                foreach (var m in movements)
                {
                    if (m.IslemTuru == "GİRİŞ" || m.IslemTuru == "Alış Faturası") 
                    {
                        decimal qty = m.Miktar > 0 ? m.Miktar : (m.Giren > 0 ? m.Giren : 0);
                        decimal price = m.Fiyat;
                        if (qty > 0)
                        {
                            currentTotalValue += (qty * price);
                            currentQuantity += (decimal)qty;
                            if (currentQuantity > 0) averagePrice = currentTotalValue / currentQuantity;
                            lastPurchasePrice = price;
                        }
                    }
                    else if (m.IslemTuru == "ÇIKIŞ" || m.IslemTuru == "Satış Faturası")
                    {
                        decimal qty = m.Miktar > 0 ? m.Miktar : (m.Cikan > 0 ? m.Cikan : 0);
                        if (qty > 0)
                        {
                            decimal valueRemoved = qty * averagePrice;
                            currentTotalValue -= valueRemoved;
                            currentQuantity -= (decimal)qty;
                            if (currentQuantity <= 0) { currentQuantity = 0; currentTotalValue = 0; }
                            
                            totalSalesRevenue += (qty * m.Fiyat);
                            totalSoldQuantity += qty;
                            if (totalSoldQuantity > 0) averageSalesPrice = totalSalesRevenue / totalSoldQuantity;
                            lastSalesPrice = m.Fiyat;
                        }
                    }
                }

                stok.OrtalamaAlisFiyati = averagePrice;
                stok.OrtalamaSatisFiyati = averageSalesPrice;
                stok.AlisFiyati = lastPurchasePrice;
                stok.SatisFiyati = lastSalesPrice;

                await SaveAsync(stok);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase RecalculateCostsAsync Error: {ex.Message}");
        }
    }

    public async Task MergeStokAsync(int kaynakStokId, int hedefStokId)
    {
        var hedef = await GetByIdAsync(hedefStokId);
        var kaynak = await GetByIdAsync(kaynakStokId);
        if (hedef == null || kaynak == null) return;

        // 1. Hareketleri aktar (StokHareketler)
        var hareketler = await GetAllHareketlerAsync();
        var aktarilacaklar = hareketler.Where(h => h.StokId == kaynakStokId).ToList();
        foreach (var h in aktarilacaklar)
        {
            h.StokId = hedefStokId;
            h.StokKodu = hedef.StokKodu;
            h.StokAdi = hedef.StokAdi;
            await SaveHareketAsync(h);
        }

        // 2. Fatura Detaylarını Güncelle
        var faturaDetaylar = await _firebaseService.GetAllNestedAsync<FaturaDetay>("FaturaDetaylar");
        var affectedFaturaIds = faturaDetaylar.Where(fd => fd.StokId == kaynakStokId).Select(fd => fd.FaturaId).Distinct().ToList();
        foreach (var fId in affectedFaturaIds)
        {
            var details = await _firebaseService.GetFaturaDetaylarAsync(fId);
            bool updated = false;
            foreach (var d in details.Where(x => x.StokId == kaynakStokId))
            {
                d.StokId = hedefStokId;
                d.StokKodu = hedef.StokKodu;
                d.StokAdi = hedef.StokAdi;
                updated = true;
            }
            if (updated)
            {
                await _firebaseService.SaveFaturaDetaylarAsync(fId, details);
            }
        }

        // 3. Sipariş Detaylarını Güncelle
        var siparisDetaylar = await _firebaseService.GetAllNestedAsync<SiparisDetay>("SiparisDetaylar");
        var affectedSiparisIds = siparisDetaylar.Where(sd => sd.StokId == kaynakStokId).Select(sd => sd.SiparisId).Distinct().ToList();
        foreach (var sId in affectedSiparisIds)
        {
            var details = await _firebaseService.GetSiparisDetaylarAsync(sId);
            bool updated = false;
            foreach (var d in details.Where(x => x.StokId == kaynakStokId))
            {
                d.StokId = hedefStokId;
                d.StokAdi = hedef.StokAdi;
                updated = true;
            }
            if (updated)
            {
                await _firebaseService.SaveSiparisDetaylarAsync(sId, details);
            }
        }

        // 4. Teklif Detaylarını Güncelle
        var teklifDetaylar = await _firebaseService.GetAllNestedAsync<TeklifDetay>("TeklifDetaylar");
        var affectedTeklifIds = teklifDetaylar.Where(td => td.StokId == kaynakStokId).Select(td => td.TeklifId).Distinct().ToList();
        foreach (var tId in affectedTeklifIds)
        {
            var details = await _firebaseService.GetTeklifDetaylarAsync(tId);
            bool updated = false;
            foreach (var d in details.Where(x => x.StokId == kaynakStokId))
            {
                d.StokId = hedefStokId;
                d.StokAdi = hedef.StokAdi;
                updated = true;
            }
            if (updated)
            {
                await _firebaseService.SaveTeklifDetaylarAsync(tId, details);
            }
        }

        // 5. Stok Sayım Detaylarını Güncelle
        var sayimDetaylar = await _firebaseService.GetAllNestedAsync<StokSayimDetay>("StokSayimDetaylar");
        var affectedFisIds = sayimDetaylar.Where(sd => sd.StokId == kaynakStokId).Select(sd => sd.FisId).Distinct().ToList();
        foreach (var fisId in affectedFisIds)
        {
            try
            {
                var details = await _firebaseService.GetAllAsync<StokSayimDetay>($"StokSayimDetaylar/{fisId}");
                bool updated = false;
                foreach (var d in details.Where(x => x.StokId == kaynakStokId))
                {
                    d.StokId = hedefStokId;
                    d.StokKodu = hedef.StokKodu;
                    d.StokAdi = hedef.StokAdi;
                    updated = true;
                }
                if (updated)
                {
                    await _firebaseService.SaveAsync("StokSayimDetaylar", details, fisId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase MergeStokAsync StokSayimDetay Error for FisId {fisId}: {ex.Message}");
            }
        }

        // 6. Hedef stok miktarını güncelle
        hedef.Miktar += kaynak.Miktar;
        await SaveAsync(hedef);
        
        // 7. Kaynak stoğu sil
        await DeleteAsync(kaynakStokId);
        
        // 8. Maliyetleri yeniden hesapla
        await RecalculateCostsAsync(hedefStokId);
    }

    public async Task<List<string>> GetGruplarAsync()
    {
        try
        {
            var defs = await _firebaseService.GetAllAsync<StokGrupDef>("StokGruplar");
            var stocks = await GetAllAsync();

            var list = (defs ?? new List<StokGrupDef>())
                .Where(d => !d.IsDeleted && !string.IsNullOrWhiteSpace(d.Ad))
                .Select(d => d.Ad.Trim())
                .Union((stocks ?? new List<StokKart>())
                    .Where(s => !s.IsDeleted && !string.IsNullOrWhiteSpace(s.Kategori))
                    .Select(s => s.Kategori!.Trim()))
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g)
                .ToList();

            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase GetGruplarAsync Error: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<int> SaveGrupAsync(string grupAdi)
    {
        if (string.IsNullOrWhiteSpace(grupAdi)) return 0;
        try
        {
            var trimmed = grupAdi.Trim();
            var all = await _firebaseService.GetAllAsync<StokGrupDef>("StokGruplar") ?? new List<StokGrupDef>();
            var existing = all.FirstOrDefault(x => string.Equals(x.Ad, trimmed, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
            if (existing != null) return existing.Id;

            int newId = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            var def = new StokGrupDef
            {
                Id = newId,
                Ad = trimmed,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };
            await _firebaseService.SaveAsync("StokGruplar", def, newId);
            return newId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase SaveGrupAsync Error: {ex.Message}");
            return 0;
        }
    }

    public async Task<int> DeleteGrupAsync(string grupAdi)
    {
        if (string.IsNullOrWhiteSpace(grupAdi)) return 0;
        try
        {
            var trimmed = grupAdi.Trim();
            var all = await _firebaseService.GetAllAsync<StokGrupDef>("StokGruplar") ?? new List<StokGrupDef>();
            var existing = all.FirstOrDefault(x => string.Equals(x.Ad, trimmed, StringComparison.OrdinalIgnoreCase) && !x.IsDeleted);
            if (existing != null)
            {
                existing.IsDeleted = true;
                existing.UpdatedAt = DateTime.Now;
                await _firebaseService.SaveAsync("StokGruplar", existing, existing.Id);
                return existing.Id;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase DeleteGrupAsync Error: {ex.Message}");
            return 0;
        }
    }

    private async Task CleanupHareketlerAsync(int stokId)
    {
        try
        {
            var hareketler = await GetAllHareketlerAsync();
            var toDelete = hareketler.Where(h => h.StokId == stokId).ToList();
            foreach (var h in toDelete)
            {
                await DeleteHareketAsync(h);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CleanupHareketlerAsync Error: {ex.Message}");
        }
    }
}
