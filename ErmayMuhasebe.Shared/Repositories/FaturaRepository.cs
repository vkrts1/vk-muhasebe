using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using SQLite;

namespace ErmayMuhasebe.Repositories;

/// <summary>
/// Fatura Repository
/// Fatura işlemlerini yönetir
/// </summary>
public class FaturaRepository : BaseRepository<Fatura>, IFaturaRepository
{
    public FaturaRepository(DatabaseService dbService) : base(dbService)
    {
    }

    public override async Task<List<Fatura>> GetAllAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fatura>()
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.Tarih)
            .ToListAsync();
    }

    public override async Task<Fatura?> GetByIdAsync(int id)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fatura>()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
    }

    public override async Task<int> SaveAsync(Fatura entity)
    {
        var db = await GetConnectionAsync();
        
        if (entity.Id != 0)
        {
            var existing = await db.Table<Fatura>().FirstOrDefaultAsync(f => f.Id == entity.Id);
            if (existing != null && existing.Version != entity.Version)
            {
                throw new System.Exception("Çakışma Tespit Edildi! Bu fatura başka bir yerde güncellenmiş veya kilitlenmiş. Lütfen sayfayı yenileyip tekrar deneyin.");
            }
            entity.Version++;
            entity.UpdatedAt = DateTime.Now;
            await db.UpdateAsync(entity);
        }
        else
        {
            entity.Version = 1;
            entity.UpdatedAt = DateTime.Now;
            await db.InsertAsync(entity);
        }

        // Bulut senkronizasyonu
        await _syncService.SyncFaturaAsync(entity);
        
        return entity.Id;
    }

    public override async Task<int> DeleteAsync(Fatura entity)
    {
        var db = await GetConnectionAsync();
        
        // 1. Get Details to reverse stock
        var detaylar = await db.Table<FaturaDetay>().Where(d => d.FaturaId == entity.Id).ToListAsync();
        
        // Fallback: If no FaturaDetay records, infer from StokHareket
        if (!detaylar.Any() && !string.IsNullOrWhiteSpace(entity.FaturaNo))
        {
            var movements = await db.Table<StokHareket>()
                .Where(s => s.FaturaId == entity.Id || s.EvrakNo == entity.FaturaNo)
                .ToListAsync();
            foreach (var m in movements)
            {
                detaylar.Add(new FaturaDetay { FaturaId = entity.Id, StokId = m.StokId, Miktar = (double)m.Miktar });
            }
        }

        // 2. Reverse Stock Balances
        bool isSatis = (entity.Tur ?? "").Equals("Satış", System.StringComparison.OrdinalIgnoreCase) || 
                       (entity.Tur ?? "").Equals("Satis", System.StringComparison.OrdinalIgnoreCase);

        foreach (var d in detaylar)
        {
            var stok = await db.Table<StokKart>().FirstOrDefaultAsync(s => s.Id == d.StokId);
            if (stok != null)
            {
                if (isSatis) stok.Miktar += d.Miktar; // Sale reversed = Add back
                else stok.Miktar -= d.Miktar; // Buy reversed = Remove
                await db.UpdateAsync(stok);
                await _syncService.SyncStokAsync(stok);
            }
        }

        // 3. Reverse Cari Balance
        var cari = await db.Table<CariKart>().FirstOrDefaultAsync(c => c.Id == entity.CariId);
        if (cari != null)
        {
            if (isSatis) cari.Borc -= entity.GenelToplam;
            else cari.Alacak -= entity.GenelToplam;
            await db.UpdateAsync(cari);
            await _syncService.SyncCariAsync(cari);
        }

        // 4. Delete Details & Movements
        string fNo = entity.FaturaNo?.Trim() ?? "";
        string kplNo = !string.IsNullOrEmpty(fNo) ? $"KPL-{fNo}" : "";

        await db.ExecuteAsync("DELETE FROM FaturaDetay WHERE FaturaId = ?", entity.Id);
        await db.ExecuteAsync("DELETE FROM StokHareket WHERE FaturaId = ? OR (EvrakNo IS NOT NULL AND EvrakNo != '' AND EvrakNo = ?)", entity.Id, fNo);
        await db.ExecuteAsync("DELETE FROM CariHareket WHERE FaturaId = ? OR (CariId = ? AND EvrakNo IS NOT NULL AND EvrakNo != '' AND (EvrakNo = ? OR EvrakNo = ?))", entity.Id, entity.CariId, fNo, kplNo);

        // Sync Deletions to Cloud
        await _syncService.DeleteStokHareketByFaturaIdAsync(entity.Id, entity.FaturaNo);
        await _syncService.DeleteCariHareketByFaturaIdAsync(entity.Id, entity.FaturaNo);
        await _syncService.DeleteFaturaDetaylarAsync(entity.Id);

        entity.IsDeleted = true;
        await db.UpdateAsync(entity);
        await _syncService.SyncFaturaAsync(entity);

        // 5. Clean up linked financial records (Kasa / Banka)
        if (!string.IsNullOrEmpty(fNo))
        {
            var mkasa = await db.Table<KasaHareket>()
                .Where(k => k.EvrakNo == fNo || k.EvrakNo == kplNo || (k.Aciklama != null && k.Aciklama.Contains(fNo)))
                .ToListAsync();
            foreach(var k in mkasa)
            {
                var kasa = await db.Table<BankaKart>().FirstOrDefaultAsync(b => b.Id == k.KasaId);
                if (kasa != null)
                {
                    kasa.GuncelBakiye -= (k.Giren - k.Cikan);
                    await db.UpdateAsync(kasa);
                }
                await db.DeleteAsync(k);
            }
            
            var mbanka = await db.Table<BankaHareket>()
                .Where(b => b.EvrakNo == fNo || b.EvrakNo == kplNo || (b.Aciklama != null && b.Aciklama.Contains(fNo)))
                .ToListAsync();
            foreach(var b in mbanka)
            {
                var banka = await db.Table<BankaKart>().FirstOrDefaultAsync(bk => bk.Id == b.BankaId);
                if (banka != null)
                {
                    banka.GuncelBakiye -= (b.Giren - b.Cikan);
                    await db.UpdateAsync(banka);
                }
                await db.DeleteAsync(b);
            }
        }

        // 6. Recalculate Stock Costs
        await db.RunInTransactionAsync(tran => 
        {
            foreach(var d in detaylar)
            {
                _recalculateStockCostInternal(tran, d.StokId);
            }
        });

        return 1;
    }

    public override async Task<int> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return 0;
        return await DeleteAsync(entity);
    }

    public override async Task<List<Fatura>> GetDeletedAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fatura>()
            .Where(f => f.IsDeleted)
            .ToListAsync();
    }

    public override async Task RestoreAsync(Fatura entity)
    {
        entity.IsDeleted = false;
        await SaveAsync(entity);
    }

    // Özel metodlar
    public async Task<Fatura?> GetByNoAsync(string faturaNo)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fatura>()
            .FirstOrDefaultAsync(f => f.FaturaNo == faturaNo && !f.IsDeleted);
    }

    public async Task<List<Fatura>> GetByCariIdAsync(int cariId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fatura>()
            .Where(f => f.CariId == cariId && !f.IsDeleted)
            .OrderByDescending(f => f.Tarih)
            .ToListAsync();
    }

    public async Task<List<Fatura>> GetByTurAsync(string tur)
    {
        var db = await GetConnectionAsync();
        return await db.Table<Fatura>()
            .Where(f => f.Tur == tur && !f.IsDeleted)
            .OrderByDescending(f => f.Tarih)
            .ToListAsync();
    }

    public async Task<List<FaturaDetay>> GetDetaylarAsync(int faturaId)
    {
        var db = await GetConnectionAsync();
        return await db.Table<FaturaDetay>()
            .Where(d => d.FaturaId == faturaId)
            .ToListAsync();
    }

    public async Task<List<FaturaDetay>> GetAllDetaylarAsync()
    {
        var db = await GetConnectionAsync();
        return await db.Table<FaturaDetay>().ToListAsync();
    }

    public async Task<int> SaveWithDetailsAsync(Fatura fatura, List<FaturaDetay> detaylar)
    {
        var db = await GetConnectionAsync();
        
        await db.RunInTransactionAsync(tran =>
        {
            // Fatura kaydet
            if (fatura.Id != 0)
            {
                tran.Update(fatura);
            }
            else
            {
                var maxId = tran.ExecuteScalar<int>("SELECT COALESCE(MAX(Id), 0) FROM Fatura");
                fatura.Id = maxId + 1;
                tran.Insert(fatura);
            }

            // Eski detayları sil
            tran.Execute("DELETE FROM FaturaDetay WHERE FaturaId = ?", fatura.Id);

            // Yeni detayları ekle
            foreach (var detay in detaylar)
            {
                detay.FaturaId = fatura.Id;
                tran.Insert(detay);
            }
        });

        // Bulut senkronizasyonu
        await _syncService.SyncFaturaAsync(fatura);
        await _syncService.SyncFaturaDetaylarAsync(fatura.Id, detaylar);
        
        return fatura.Id;
    }

    public async Task<int> SaveWithDetailsAndTransactionAsync(Fatura fatura, List<FaturaDetay> detaylar, bool isSatis = true, bool updateCari = true, bool updateStok = true, bool updateStokPrices = false)
    {
        // This is a simplified alias to SaveWithTransactionAsync
        var db = await GetConnectionAsync();
        var cari = await db.Table<CariKart>().FirstOrDefaultAsync(c => c.Id == fatura.CariId);
        if (cari == null) return 0;

        await SaveWithTransactionAsync(fatura, detaylar, cari, updateCari, updateStok, updateStokPrices);
        return 1;
    }

    public async Task<decimal> GetSumAsync(DateTime? start = null, DateTime? end = null, string? tur = null)
    {
        var db = await GetConnectionAsync();
        
        string query = "SELECT SUM(GenelToplam) FROM Fatura WHERE NOT IsDeleted";
        var args = new List<object>();

        if (start != null) 
        {
            query += " AND Tarih >= ?";
            args.Add(start.Value);
        }
        if (end != null) 
        {
            query += " AND Tarih <= ?";
            args.Add(end.Value);
        }
        if (!string.IsNullOrEmpty(tur))
        {
            if (tur.Equals("Satış", StringComparison.OrdinalIgnoreCase) || tur.Equals("Satis", StringComparison.OrdinalIgnoreCase))
            {
               query += " AND (Tur = 'Satış' OR Tur = 'Satis')";
            }
            else if (tur.Equals("Alış", StringComparison.OrdinalIgnoreCase) || tur.Equals("Alis", StringComparison.OrdinalIgnoreCase))
            {
               query += " AND (Tur = 'Alış' OR Tur = 'Alis')";
            }
            else 
            {
                query += " AND Tur = ?";
                args.Add(tur);
            }
        }

        try 
        {
            return await db.ExecuteScalarAsync<decimal>(query, args.ToArray());
        }
        catch 
        {
            return 0;
        }
    }

    public async Task<int> SaveWithTransactionAsync(Fatura fatura, List<FaturaDetay> detaylar, CariKart cari, bool updateCari = true, bool updateStok = true, bool updateStokPrices = false)
    {
        var db = await GetConnectionAsync();
        var newStokHarekets = new List<StokHareket>();
        CariHareket? newCariHareket = null;

        await db.RunInTransactionAsync(tran => 
        {
            if (fatura.Id != 0) 
            {
                var oldDetails = tran.Query<FaturaDetay>("SELECT * FROM FaturaDetay WHERE FaturaId = ?", fatura.Id);
                var oldFatura = tran.Find<Fatura>(fatura.Id);
                if (oldFatura != null)
                {
                    bool oldIsSatis = (oldFatura.Tur ?? "").Equals("Satış", System.StringComparison.OrdinalIgnoreCase) || 
                                      (oldFatura.Tur ?? "").Equals("Satis", System.StringComparison.OrdinalIgnoreCase);

                    foreach (var od in oldDetails)
                    {
                        var stk = tran.Find<StokKart>(od.StokId);
                        if (stk != null)
                        {
                            if (oldIsSatis) stk.Miktar += od.Miktar;
                            else stk.Miktar -= od.Miktar;
                            tran.Update(stk);
                        }
                    }
                    var cr = tran.Find<CariKart>(oldFatura.CariId);
                    if (cr != null)
                    {
                        if (oldIsSatis) cr.Borc -= oldFatura.GenelToplam;
                        else cr.Alacak -= oldFatura.GenelToplam;
                        tran.Update(cr);
                    }
                }

                tran.Update(fatura);
                tran.Execute("DELETE FROM FaturaDetay WHERE FaturaId = ?", fatura.Id);
                tran.Execute("DELETE FROM StokHareket WHERE FaturaId = ?", fatura.Id);
                tran.Execute("DELETE FROM CariHareket WHERE FaturaId = ?", fatura.Id);
                // Also clean up linked financial movements if updating
                tran.Execute("DELETE FROM KasaHareket WHERE EvrakNo = ? AND CariId = ?", fatura.FaturaNo, fatura.CariId);
                tran.Execute("DELETE FROM BankaHareket WHERE EvrakNo = ? AND CariId = ?", fatura.FaturaNo, fatura.CariId);
            }
            else 
            {
                var maxId = tran.ExecuteScalar<int>("SELECT COALESCE(MAX(Id), 0) FROM Fatura");
                fatura.Id = maxId + 1;
                tran.Insert(fatura);
            }
            
            bool currentIsSatis = (fatura.Tur ?? "").Equals("Satış", System.StringComparison.OrdinalIgnoreCase) || 
                                  (fatura.Tur ?? "").Equals("Satis", System.StringComparison.OrdinalIgnoreCase);

            // Automated Payment Movement
            if (fatura.OdemeSekli == "Nakit" && fatura.KasaId.HasValue && fatura.KasaId > 0)
            {
                var kasaHareket = new KasaHareket
                {
                    KasaId = fatura.KasaId.Value,
                    Tarih = fatura.Tarih,
                    EvrakNo = fatura.FaturaNo,
                    CariId = fatura.CariId,
                    CariUnvan = fatura.CariUnvan,
                    IslemTuru = currentIsSatis ? "Tahsilat (Fatura)" : "Ödeme (Fatura)",
                    Aciklama = $"Fatura No: {fatura.FaturaNo} Peşin Ödeme",
                    Giren = currentIsSatis ? fatura.GenelToplam : 0,
                    Cikan = !currentIsSatis ? fatura.GenelToplam : 0,
                    TenantId = fatura.TenantId
                };
                tran.Insert(kasaHareket);
            }
            else if (fatura.OdemeSekli == "Kredi Kartı" && fatura.BankaId.HasValue && fatura.BankaId > 0)
            {
                var bankaHareket = new BankaHareket
                {
                    BankaId = fatura.BankaId.Value,
                    Tarih = fatura.Tarih,
                    EvrakNo = fatura.FaturaNo,
                    CariId = fatura.CariId,
                    CariUnvan = fatura.CariUnvan,
                    IslemTuru = currentIsSatis ? "Tahsilat (Fatura)" : "Ödeme (Fatura)",
                    Aciklama = $"Fatura No: {fatura.FaturaNo} Kredi Kartı",
                    Giren = currentIsSatis ? fatura.GenelToplam : 0,
                    Cikan = !currentIsSatis ? fatura.GenelToplam : 0,
                    Tutar = fatura.GenelToplam,
                    TenantId = fatura.TenantId
                };
                tran.Insert(bankaHareket);
            }

            foreach(var d in detaylar)
            {
                d.FaturaId = fatura.Id;
                tran.Insert(d);

                var stokHareket = new StokHareket
                {
                    StokId = d.StokId,
                    StokKodu = d.StokKodu ?? "",
                    StokAdi = d.StokAdi ?? "",
                    Tarih = fatura.Tarih,
                    IslemTuru = currentIsSatis ? "Satış Faturası" : "Alış Faturası",
                    EvrakNo = fatura.FaturaNo,
                    FaturaId = fatura.Id,
                    Miktar = (decimal)d.Miktar,
                    Fiyat = d.BirimFiyat, // CRITICAL FIX: Transfer price to movement
                    Giren = currentIsSatis ? 0 : (decimal)d.Miktar,
                    Cikan = currentIsSatis ? (decimal)d.Miktar : 0,
                    Aciklama = $"Fatura No: {fatura.FaturaNo}",
                    TenantId = fatura.TenantId // Assuming TenantId exists on Fatura
                };
                tran.Insert(stokHareket);
                newStokHarekets.Add(stokHareket);

                if (updateStok)
                {
                    var stok = tran.Find<StokKart>(d.StokId);
                    if(stok != null)
                    {
                        if(currentIsSatis) stok.Miktar -= d.Miktar;
                        else stok.Miktar += d.Miktar;

                        if (updateStokPrices)
                        {
                            if (!currentIsSatis) stok.AlisFiyati = d.BirimFiyat; 
                            else stok.SatisFiyati = d.BirimFiyat;
                        }
                        tran.Update(stok);
                    }
                }
            }

            if (updateCari)
            {
                var cr = tran.Find<CariKart>(fatura.CariId);
                if (cr != null)
                {
                    if (currentIsSatis) cr.Borc += fatura.GenelToplam;
                    else cr.Alacak += fatura.GenelToplam;
                    tran.Update(cr);

                    var cariHareket = new CariHareket
                    {
                        CariId = fatura.CariId,
                        Tarih = fatura.Tarih,
                        IslemTuru = currentIsSatis ? "Satış Faturası" : "Alış Faturası",
                        Borc = currentIsSatis ? fatura.GenelToplam : 0,
                        Alacak = !currentIsSatis ? fatura.GenelToplam : 0,
                        Aciklama = $"Fatura No: {fatura.FaturaNo}",
                        EvrakNo = fatura.FaturaNo,
                        FaturaId = fatura.Id,
                        Vade = fatura.VadeTarihi
                    };
                    tran.Insert(cariHareket);
                    newCariHareket = cariHareket;
                }
            }

            // RECALCULATE STOCK COSTS AFTER ALL MOVEMENTS SAVED
            foreach(var d in detaylar)
            {
                _recalculateStockCostInternal(tran, d.StokId);
            }
        });

        await _dbService.RecalculateCariBalanceAsync(fatura.CariId);
        await _syncService.SyncFaturaAsync(fatura);
        await _syncService.SyncFaturaDetaylarAsync(fatura.Id, detaylar);

        if (newCariHareket != null)
        {
            await _syncService.SyncCariHareketAsync(newCariHareket);
        }

        foreach (var sh in newStokHarekets)
        {
            await _syncService.SyncStokHareketAsync(sh);
        }

        if (updateStok)
        {
            var updatedStokIds = detaylar.Select(d => d.StokId).Distinct().ToList();
            var dbConn = await GetConnectionAsync();
            foreach (var sId in updatedStokIds)
            {
                var stk = await dbConn.Table<StokKart>().FirstOrDefaultAsync(s => s.Id == sId);
                if (stk != null)
                {
                    await _syncService.SyncStokAsync(stk);
                }
            }
        }

        return fatura.Id;
    }

    private void _recalculateStockCostInternal(SQLiteConnection tran, int stokId)
    {
        var stok = tran.Table<StokKart>().FirstOrDefault(s => s.Id == stokId);
        if (stok == null) return;
        
        var movements = tran.Table<StokHareket>()
            .Where(h => h.StokId == stokId)
            .OrderBy(h => h.Tarih)
            .ThenBy(h => h.Id)
            .ToList();
        
        decimal currentQuantity = 0;
        decimal currentTotalValue = 0;
        decimal averagePrice = 0;
        
        decimal totalSoldQuantity = 0;
        decimal totalSalesRevenue = 0;
        decimal averageSalesPrice = 0;

        foreach (var m in movements)
        {
            // Primary check: numeric flags Giren/Cikan
            bool isGiris = m.Giren > 0;
            bool isCikis = m.Cikan > 0;

            // Secondary check: Fallback to string matching if numeric fields are zero/empty
            if (!isGiris && !isCikis)
            {
                string tur = (m.IslemTuru ?? "").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                isGiris = (tur.Contains("GİRİŞ") || tur.Contains("ALIS") || tur.Contains("ALIŞ") || tur.Contains("ACILIS") || tur.Contains("AÇILIŞ") || tur.Contains("GİREN"));
                isCikis = (tur.Contains("ÇIKIŞ") || tur.Contains("CIKIS") || tur.Contains("SATIS") || tur.Contains("SATIŞ") || tur.Contains("ÇIKAN"));
                
                // Extra safety for Turkish characters with OrdinalIgnoreCase
                if (!isGiris && !isCikis)
                {
                    string t = m.IslemTuru ?? "";
                    isGiris = t.Contains("Giriş", StringComparison.OrdinalIgnoreCase) || t.Contains("Alış", StringComparison.OrdinalIgnoreCase) || t.Contains("Açılış", StringComparison.OrdinalIgnoreCase);
                    isCikis = t.Contains("Çıkış", StringComparison.OrdinalIgnoreCase) || t.Contains("Satış", StringComparison.OrdinalIgnoreCase);
                }
            }

            if (isGiris) 
            {
                decimal qty = m.Miktar > 0 ? m.Miktar : (m.Giren > 0 ? m.Giren : 0);
                decimal price = m.Fiyat;

                if (qty > 0)
                {
                    currentTotalValue += (qty * price);
                    currentQuantity += qty;
                    
                    if (currentQuantity > 0)
                        averagePrice = currentTotalValue / currentQuantity;
                    else
                    {
                         averagePrice = price; 
                         currentTotalValue = 0;
                    }
                }
            }
            else if (isCikis)
            {
                decimal qty = m.Miktar > 0 ? m.Miktar : (m.Cikan > 0 ? m.Cikan : 0);
                
                if (qty > 0)
                {
                    currentTotalValue -= (qty * averagePrice);
                    currentQuantity -= qty;

                    decimal salePrice = m.Fiyat;
                    totalSalesRevenue += (qty * salePrice);
                    totalSoldQuantity += qty;
                    
                    if (totalSoldQuantity > 0)
                        averageSalesPrice = totalSalesRevenue / totalSoldQuantity;
                }
            }
        }

        var lastPurchase = movements.Where(x => {
             var t = (x.IslemTuru ?? "").ToUpperInvariant();
             return t.Contains("GİRİŞ") || t.Contains("ALIS") || t.Contains("ALIŞ") || t.Contains("ACILIS") || t.Contains("AÇILIŞ");
        }).LastOrDefault();

        var lastSale = movements.Where(x => {
             var t = (x.IslemTuru ?? "").ToUpperInvariant();
             return t.Contains("ÇIKIŞ") || t.Contains("CIKIS") || t.Contains("SATIS") || t.Contains("SATIŞ");
        }).LastOrDefault();

        stok.OrtalamaAlisFiyati = averagePrice;
        stok.OrtalamaSatisFiyati = averageSalesPrice;
        if (lastPurchase != null) stok.AlisFiyati = lastPurchase.Fiyat;
        if (lastSale != null) stok.SatisFiyati = lastSale.Fiyat;

        tran.Update(stok);
    }

    private async Task<int> SoftDeleteAsync(Fatura entity)
    {
        entity.IsDeleted = true;
        return await SaveAsync(entity);
    }
}

