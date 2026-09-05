using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Repositories.Firebase;

public class FirebaseFaturaRepository : BaseFirebaseRepository<Fatura>, IFaturaRepository
{
    public FirebaseFaturaRepository(IFirebaseService firebaseService) : base(firebaseService)
    {
    }

    protected override string ResourceName => "Faturalar";

    public async Task<Fatura?> GetByNoAsync(string faturaNo)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(f => f.FaturaNo == faturaNo);
    }

    public async Task<List<Fatura>> GetByCariIdAsync(int cariId)
    {
        var all = await GetAllAsync();
        return all.Where(f => f.CariId == cariId).ToList();
    }

    public async Task<List<Fatura>> GetByTurAsync(string tur)
    {
        var all = await GetAllAsync();
        return all.Where(f => f.Tur == tur).ToList();
    }

    public async Task<decimal> GetSumAsync(DateTime? start = null, DateTime? end = null, string? tur = null)
    {
        var all = await GetAllAsync();
        var filtered = all.Where(f => !f.IsDeleted);

        if (start != null) filtered = filtered.Where(f => f.Tarih >= start.Value);
        if (end != null) filtered = filtered.Where(f => f.Tarih <= end.Value);
        
        if (!string.IsNullOrEmpty(tur))
        {
            bool isSatis = tur.Equals("Satış", StringComparison.OrdinalIgnoreCase) || tur.Equals("Satis", StringComparison.OrdinalIgnoreCase);
            bool isAlis = tur.Equals("Alış", StringComparison.OrdinalIgnoreCase) || tur.Equals("Alis", StringComparison.OrdinalIgnoreCase);

            if (isSatis)
            {
                filtered = filtered.Where(f => f.Tur?.Equals("Satış", StringComparison.OrdinalIgnoreCase) == true || 
                                              f.Tur?.Equals("Satis", StringComparison.OrdinalIgnoreCase) == true);
            }
            else if (isAlis)
            {
                filtered = filtered.Where(f => f.Tur?.Equals("Alış", StringComparison.OrdinalIgnoreCase) == true || 
                                              f.Tur?.Equals("Alis", StringComparison.OrdinalIgnoreCase) == true);
            }
            else
            {
                filtered = filtered.Where(f => f.Tur != null && f.Tur.Equals(tur, StringComparison.OrdinalIgnoreCase));
            }
        }

        return filtered.Sum(f => f.GenelToplam);
    }

    public async Task<List<FaturaDetay>> GetDetaylarAsync(int faturaId)
    {
        return await _firebaseService.GetFaturaDetaylarAsync(faturaId);
    }

    public async Task<List<FaturaDetay>> GetAllDetaylarAsync()
    {
        return await _firebaseService.GetAllNestedAsync<FaturaDetay>("FaturaDetaylar");
    }

    public async Task<int> SaveWithDetailsAsync(Fatura fatura, List<FaturaDetay> detaylar)
    {
        int id = await SaveAsync(fatura);
        await _firebaseService.SaveFaturaDetaylarAsync(id, detaylar);
        return id;
    }

    public async Task<int> SaveWithTransactionAsync(Fatura fatura, List<FaturaDetay> detaylar, CariKart cari, bool updateCari, bool updateStok, bool updateStokPrices)
    {
        return await SaveWithTransactionAsyncInternal(fatura, detaylar, cari, updateCari, updateStok, updateStokPrices);
    }

    public async Task<int> SaveWithDetailsAndTransactionAsync(Fatura fatura, List<FaturaDetay> detaylar, bool isSatis, bool updateCari, bool updateStok, bool updateStokPrices)
    {
        return await SaveWithTransactionAsyncInternal(fatura, detaylar, null, updateCari, updateStok, updateStokPrices);
    }

    private async Task<int> SaveWithTransactionAsyncInternal(Fatura fatura, List<FaturaDetay> detaylar, CariKart? cari, bool updateCari, bool updateStok, bool updateStokPrices = false)
    {
        bool isSatis = (fatura.Tur?.Equals("Satış", StringComparison.OrdinalIgnoreCase) == true || 
                        fatura.Tur?.Equals("Satis", StringComparison.OrdinalIgnoreCase) == true);
        
        if (fatura.Id != 0)
        {
            var oldFatura = await GetByIdAsync(fatura.Id);
            if (oldFatura != null)
            {
                bool oldIsSatis = (oldFatura.Tur?.Equals("Satış", StringComparison.OrdinalIgnoreCase) == true || 
                                   oldFatura.Tur?.Equals("Satis", StringComparison.OrdinalIgnoreCase) == true);
                
                if (updateCari)
                {
                    var allCariler = await _firebaseService.GetAllAsync<CariKart>("Cariler");
                    var oldCari = allCariler.FirstOrDefault(c => c.Id == oldFatura.CariId);
                    if (oldCari != null)
                    {
                        if (oldIsSatis) oldCari.Borc -= oldFatura.GenelToplam;
                        else oldCari.Alacak -= oldFatura.GenelToplam;
                        await _firebaseService.SaveAsync("Cariler", oldCari, oldCari.Id);
                    }
                }

                if (updateStok)
                {
                    var oldDetaylar = await GetDetaylarAsync(oldFatura.Id);
                    if (oldDetaylar.Any())
                    {
                        var allStoklar = await _firebaseService.GetAllAsync<StokKart>("Stoklar");
                        foreach (var od in oldDetaylar)
                        {
                            var stok = allStoklar.FirstOrDefault(s => s.Id == od.StokId);
                            if (stok != null)
                            {
                                if (oldIsSatis) stok.Miktar += (double)od.Miktar;
                                else stok.Miktar -= (double)od.Miktar;
                                await _firebaseService.SaveAsync("Stoklar", stok, stok.Id);
                            }
                        }
                    }
                }

                var allCariH = await _firebaseService.GetAllAsync<CariHareket>("CariHareketler");
                var chToDel = allCariH.Where(h => h.FaturaId == oldFatura.Id || (h.EvrakNo == oldFatura.FaturaNo && h.CariId == oldFatura.CariId)).ToList();
                foreach (var ch in chToDel) await _firebaseService.DeleteAsync("CariHareketler", ch.Id);

                var allStokH = await _firebaseService.GetAllAsync<StokHareket>("StokHareketler");
                var shToDel = allStokH.Where(h => h.FaturaId == oldFatura.Id || h.EvrakNo == oldFatura.FaturaNo).ToList();
                foreach (var sh in shToDel) await _firebaseService.DeleteAsync("StokHareketler", sh.Id);
            }
        }

        int id = await SaveWithDetailsAsync(fatura, detaylar);

        try 
        {
            if (updateStok)
            {
                var allStoklar = await _firebaseService.GetAllAsync<StokKart>("Stoklar");
                foreach (var d in detaylar)
                {
                    var stok = allStoklar.FirstOrDefault(s => s.Id == d.StokId);
                    if (stok != null)
                    {
                        if (isSatis) stok.Miktar -= (double)d.Miktar;
                        else stok.Miktar += (double)d.Miktar;

                        if (updateStokPrices)
                        {
                             if (!isSatis) stok.AlisFiyati = d.BirimFiyat;
                             else stok.SatisFiyati = d.BirimFiyat;
                        }

                        await _firebaseService.SaveAsync("Stoklar", stok, stok.Id);

                        var sh = new StokHareket
                        {
                            Id = new Random().Next(1000000, 9999999),
                            StokId = stok.Id,
                            StokAdi = stok.StokAdi,
                            StokKodu = stok.StokKodu,
                            Tarih = fatura.Tarih,
                            IslemTuru = isSatis ? "ÇIKIŞ" : "GİRİŞ",
                            Miktar = (decimal)d.Miktar,
                            Fiyat = d.BirimFiyat,
                            EvrakNo = fatura.FaturaNo,
                            FaturaId = id,
                            Aciklama = $"Fatura No: {fatura.FaturaNo} ({(isSatis ? "Satış" : "Alış")})",
                            Giren = !isSatis ? (decimal)d.Miktar : 0,
                            Cikan = isSatis ? (decimal)d.Miktar : 0
                        };
                        await _firebaseService.SaveAsync("StokHareketler", sh, sh.Id);
                    }
                }
            }

            if (updateCari)
            {
                var allCariler = await _firebaseService.GetAllAsync<CariKart>("Cariler");
                var cariToUpdate = allCariler.FirstOrDefault(c => c.Id == fatura.CariId);
                if (cariToUpdate != null)
                {
                    if (isSatis) cariToUpdate.Borc += fatura.GenelToplam;
                    else cariToUpdate.Alacak += fatura.GenelToplam;
                    await _firebaseService.SaveAsync("Cariler", cariToUpdate, cariToUpdate.Id);
                    
                    var ch = new CariHareket
                    {
                        Id = new Random().Next(1000000, 9999999),
                        CariId = fatura.CariId,
                        CariUnvan = fatura.CariUnvan,
                        Tarih = fatura.Tarih,
                        IslemTuru = isSatis ? "Satış Faturası" : "Alış Faturası",
                        Borc = isSatis ? fatura.GenelToplam : 0,
                        Alacak = !isSatis ? fatura.GenelToplam : 0,
                        Vade = fatura.VadeTarihi,
                        EvrakNo = fatura.FaturaNo,
                        FaturaId = id,
                        Aciklama = $"Fatura No: {fatura.FaturaNo}"
                    };
                    await _firebaseService.SaveAsync("CariHareketler", ch, ch.Id);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FirebaseFaturaRepository] ERROR in Updates: {ex.Message}");
        }

        return id;
    }

    public override async Task<int> DeleteAsync(int id)
    {
        var fatura = await GetByIdAsync(id);
        if (fatura == null) return 0;
        return await DeleteAsync(fatura);
    }

    public override async Task<int> DeleteAsync(Fatura entity)
    {
        if (entity == null) return 0;

        try 
        {
            var detaylar = await GetDetaylarAsync(entity.Id);
            bool isSatis = (entity.Tur?.Equals("Satış", StringComparison.OrdinalIgnoreCase) == true || 
                            entity.Tur?.Equals("Satis", StringComparison.OrdinalIgnoreCase) == true);

            var allStoklar = await _firebaseService.GetAllAsync<StokKart>("Stoklar");
            foreach (var d in detaylar)
            {
                var stok = allStoklar.FirstOrDefault(s => s.Id == d.StokId);
                if (stok != null)
                {
                    if (isSatis) stok.Miktar += (double)d.Miktar;
                    else stok.Miktar -= (double)d.Miktar;
                    await _firebaseService.SaveAsync("Stoklar", stok, stok.Id);
                }
            }

            var allCariler = await _firebaseService.GetAllAsync<CariKart>("Cariler");
            var cariToRevert = allCariler.FirstOrDefault(c => c.Id == entity.CariId);
            if (cariToRevert != null)
            {
                if (isSatis) cariToRevert.Borc -= entity.GenelToplam;
                else cariToRevert.Alacak -= entity.GenelToplam;
                await _firebaseService.SaveAsync("Cariler", cariToRevert, cariToRevert.Id);
            }

            var allCariH = await _firebaseService.GetAllAsync<CariHareket>("CariHareketler");
            var chToDel = allCariH.Where(h => h.FaturaId == entity.Id || (h.EvrakNo == entity.FaturaNo && h.CariId == entity.CariId)).ToList();
            foreach (var ch in chToDel) await _firebaseService.DeleteAsync("CariHareketler", ch.Id);

            var allStokH = await _firebaseService.GetAllAsync<StokHareket>("StokHareketler");
            var shToDel = allStokH.Where(h => h.FaturaId == entity.Id || h.EvrakNo == entity.FaturaNo).ToList();
            foreach (var sh in shToDel) await _firebaseService.DeleteAsync("StokHareketler", sh.Id);

            await _firebaseService.DeleteAsync("FaturaDetaylar", entity.Id);
            return await base.DeleteAsync(entity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatura Delete Error: {ex.Message}");
            return 0;
        }
    }
}
