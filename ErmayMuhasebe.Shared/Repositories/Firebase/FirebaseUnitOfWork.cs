using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using System.Text.Json;

namespace ErmayMuhasebe.Repositories.Firebase;

/// <summary>
/// Unit of Work Implementation (Firebase)
/// Tüm Firebase repository'lerini tek bir yerden yönetir
/// </summary>
public class FirebaseUnitOfWork : IUnitOfWork
{
    protected readonly IFirebaseService _firebaseService;

    public Task InsertWithIdAsync<T>(T entity) where T : class
    {
        throw new NotImplementedException("FirebaseUnitOfWork does not support InsertWithIdAsync.");
    }

    public FirebaseUnitOfWork(IFirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
        Cariler = new FirebaseCariRepository(firebaseService);
        Stoklar = new FirebaseStokRepository(firebaseService);
        Faturalar = new FirebaseFaturaRepository(firebaseService);
        Bankalar = new FirebaseBankaRepository(firebaseService);
        Kasalar = new FirebaseKasaRepository(firebaseService);
        Siparisler = new FirebaseSiparisRepository(firebaseService);
        Teklifler = new FirebaseTeklifRepository(firebaseService);
        Cekler = new FirebaseCekRepository(firebaseService);
        Senetler = new FirebaseSenetRepository(firebaseService);
        KrediKartlari = new FirebaseKrediKartiRepository(firebaseService);
        EftIslemleri = new FirebaseEftRepository(firebaseService);
        StokSayimlar = new FirebaseStokSayimRepository(firebaseService);
        Portfolyo = new FirebasePortfoyRepository(firebaseService);
        Hedefler = new FirebaseHedefRepository(firebaseService);
        DovizKurlari = new FirebaseDovizRepository(firebaseService);
        BelgeArsiv = new FirebaseBelgeArsivRepository(firebaseService);
        Notes = new FirebaseNoteRepository(firebaseService);
        MusteriTakip = new FirebaseMusteriTakipRepository(firebaseService);
    }

    public ICariRepository Cariler { get; }
    public IStokRepository Stoklar { get; }
    public IFaturaRepository Faturalar { get; }
    public IBankaRepository Bankalar { get; }
    public IKasaRepository Kasalar { get; }
    public ISiparisRepository Siparisler { get; }
    public ITeklifRepository Teklifler { get; }
    public ICekRepository Cekler { get; }
    public ISenetRepository Senetler { get; }
    public IKrediKartiRepository KrediKartlari { get; }
    public IEftRepository EftIslemleri { get; }
    public IStokSayimRepository StokSayimlar { get; }
    public IPortfoyRepository Portfolyo { get; }
    public IHedefRepository Hedefler { get; }
    public IDovizRepository DovizKurlari { get; }
    public IBelgeArsivRepository BelgeArsiv { get; }
    public INoteRepository Notes { get; }
    public IMusteriTakipRepository MusteriTakip { get; }

    // Maintenance
    public Task<string> GetDatabasePathAsync() => Task.FromResult("Cloud");
    public Task<long> GetDatabaseSizeAsync() => Task.FromResult(0L); // Not applicable
    public Task PerformMaintenanceAsync() => Task.CompletedTask; // Not applicable

    // Analytics
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var stats = new DashboardStats();
        try
        {
            var faturalar = await Faturalar.GetAllAsync();
            var cariler = await Cariler.GetAllAsync();
            var bankalar = await Bankalar.GetAllAsync();
            var stoklar = await Stoklar.GetAllAsync();

            var today = DateTime.Now.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            stats.GunlukCiro = faturalar.Where(f => f.Tarih.Date == today && (f.Tur == "Satış" || f.Tur == "Satis") && !f.IsDeleted).Sum(f => f.GenelToplam);
            stats.AylikCiro = faturalar.Where(f => f.Tarih >= startOfMonth && (f.Tur == "Satış" || f.Tur == "Satis") && !f.IsDeleted).Sum(f => f.GenelToplam);
            
            // Tahsilat (Kasa ve Banka girişleri)
            var kasalar = await Kasalar.GetAllAsync();
            decimal kasaTahsilat = kasalar.Where(k => k.Tarih >= startOfMonth && k.IslemTuru == "Tahsilat").Sum(k => k.Giren);
            // Not: BankaHareketler repository'si gerekebilir, şimdilik basitleştirilmiş
            stats.ToplamTahsilat = kasaTahsilat; 

            stats.ToplamBorc = cariler.Where(c => !c.IsDeleted && c.Alacak > c.Borc).Sum(c => c.Alacak - c.Borc);
            stats.ToplamAlacak = cariler.Where(c => !c.IsDeleted && c.Borc > c.Alacak).Sum(c => c.Borc - c.Alacak);
            stats.ToplamNakitVarligi = bankalar.Sum(b => b.GuncelBakiye);
            stats.KritikStokSayisi = stoklar.Count(s => !s.IsDeleted && s.Miktar <= (s.MinSeviye > 0 ? s.MinSeviye : 5));

            // Karlılık
            decimal aylikAlis = faturalar.Where(f => f.Tarih >= startOfMonth && (f.Tur == "Alış" || f.Tur == "Alis") && !f.IsDeleted).Sum(f => f.GenelToplam);
            stats.Karlilik = stats.AylikCiro - aylikAlis;
            if (stats.AylikCiro > 0)
                stats.KarlilikOrani = (double)(Math.Round((stats.Karlilik / stats.AylikCiro) * 100, 2));
        }
        catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"Firebase stats error: {ex.Message}"); }
        return stats;
    }

    public async Task<List<RecentTransactionItem>> GetRecentTransactionsAsync()
    {
        var list = new List<RecentTransactionItem>();
        try
        {
            var activeCaris = (await Cariler.GetAllAsync()).Where(c => !c.IsDeleted).ToDictionary(c => c.Id);
            var faturas = await Faturalar.GetAllAsync();
            var kasas = await Kasalar.GetAllAsync();

            foreach(var f in faturas.Where(f => !f.IsDeleted && (f.CariId != 0 ? activeCaris.ContainsKey(f.CariId) : activeCaris.Any())).OrderByDescending(x => x.Tarih).Take(10))
            {
                list.Add(new RecentTransactionItem {
                    Title = f.CariUnvan ?? (activeCaris.TryGetValue(f.CariId, out var c) ? c.Unvan : "Bilinmeyen Cari"),
                    Description = f.Tur + " Faturası",
                    Amount = f.GenelToplam,
                    Date = f.Tarih,
                    Type = (f.Tur == "Satış" || f.Tur == "Satis") ? "In" : "Out",
                    TextColor = (f.Tur == "Satış" || f.Tur == "Satis") ? "#34D399" : "#F87171"
                });
            }

            foreach(var k in kasas.Where(k => (k.IslemTuru == "Tahsilat" || k.IslemTuru == "Ödeme") && (!k.CariId.HasValue || k.CariId == 0 || activeCaris.ContainsKey(k.CariId.Value))).OrderByDescending(x => x.Tarih).Take(10))
            {
                list.Add(new RecentTransactionItem {
                    Title = k.CariUnvan ?? k.Aciklama ?? "Kasa İşlemi",
                    Description = k.IslemTuru + " (Nakit)",
                    Amount = k.IslemTuru == "Tahsilat" ? k.Giren : k.Cikan,
                    Date = k.Tarih,
                    Type = k.IslemTuru == "Tahsilat" ? "In" : "Out",
                    TextColor = k.IslemTuru == "Tahsilat" ? "#34D399" : "#F87171"
                });
            }
        }
        catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"Firebase recent tx error: {ex.Message}"); }
        return list.OrderByDescending(x => x.Date).Take(10).ToList();
    }

    public async Task<List<CariAlertItem>> GetRiskyCarisAsync()
    {
        try
        {
            var cariler = await Cariler.GetAllAsync();
            return cariler.Where(c => !c.IsDeleted && (c.Borc - c.Alacak) > 0)
                          .OrderByDescending(c => c.Borc - c.Alacak)
                          .Take(10)
                          .Select(c => new CariAlertItem {
                              CariId = c.Id,
                              Unvan = c.Unvan ?? "",
                              Bakiye = c.Borc - c.Alacak,
                              OrtalamaVade = 0,
                              SonIslemTarihi = DateTime.Now
                          }).ToList();
        }
        catch { return new List<CariAlertItem>(); }
    }

    public async Task<List<CariAlertItem>> GetPayableCarisAsync()
    {
        try
        {
            var cariler = await Cariler.GetAllAsync();
            return cariler.Where(c => !c.IsDeleted && (c.Alacak - c.Borc) > 0)
                          .OrderByDescending(c => c.Alacak - c.Borc)
                          .Take(10)
                          .Select(c => new CariAlertItem {
                              CariId = c.Id,
                              Unvan = c.Unvan ?? "",
                              Bakiye = c.Alacak - c.Borc,
                              OrtalamaVade = 0,
                              SonIslemTarihi = DateTime.Now
                          }).ToList();
        }
        catch { return new List<CariAlertItem>(); }
    }

    public async Task<List<IncomeExpenseItem>> GetMonthlyIncomeExpenseAsync()
    {
        var result = new List<IncomeExpenseItem>();
        try
        {
            var faturas = await Faturalar.GetAllAsync();
            var monthCount = 6;
            var endDate = DateTime.Now;
            var startDate = endDate.AddMonths(-(monthCount - 1));

            for (int i = 0; i < monthCount; i++)
            {
                var d = startDate.AddMonths(i);
                var monthFaturas = faturas.Where(f => !f.IsDeleted && f.Tarih.Month == d.Month && f.Tarih.Year == d.Year).ToList();

                var inc = monthFaturas.Where(f => f.Tur == "Satış" || f.Tur == "Satis").Sum(f => f.GenelToplam);
                var exp = monthFaturas.Where(f => f.Tur == "Alış" || f.Tur == "Alis").Sum(f => f.GenelToplam);

                result.Add(new IncomeExpenseItem {
                    Month = d.ToString("MMM"),
                    Income = inc,
                    Expense = exp,
                    Year = d.Year,
                    MonthInt = d.Month
                });
            }
        }
        catch { }
        return result;
    }

    public async Task<List<FinanceTrendItem>> GetFinanceTrendAsync(string period)
    {
        var result = new List<FinanceTrendItem>();
        try
        {
            var today = DateTime.Now.Date;
            if (period.ToLower() == "daily")
            {
                var start = today.AddDays(-9);
                var kasas = await Kasalar.GetAllAsync();
                var all = kasas.Where(k => k.Tarih >= start).ToList();

                for (int i = 0; i < 10; i++)
                {
                    var date = start.AddDays(i);
                    var dData = all.Where(x => x.Tarih.Date == date.Date);

                    result.Add(new FinanceTrendItem {
                        Label = date.ToString("dd MMM"),
                        Date = date,
                        Income = dData.Where(x => x.IslemTuru == "Tahsilat").Sum(x => x.Giren),
                        Expense = dData.Where(x => x.IslemTuru == "Ödeme").Sum(x => x.Cikan),
                        Redirected = 0
                    });
                }
            }
            else if (period.ToLower() == "weekly")
            {
                var start = today.AddDays(-49);
                var kasas = await Kasalar.GetAllAsync();
                var all = kasas.Where(k => k.Tarih >= start).ToList();

                for (int i = 0; i < 8; i++)
                {
                    var date = start.AddDays(i * 7);
                    int weekNum = System.Globalization.ISOWeek.GetWeekOfYear(date);
                    var wData = all.Where(x => System.Globalization.ISOWeek.GetWeekOfYear(x.Tarih) == weekNum && x.Tarih.Year == date.Year);

                    result.Add(new FinanceTrendItem {
                        Label = $"{weekNum}. Hafta",
                        Date = date,
                        Income = wData.Where(x => x.IslemTuru == "Tahsilat").Sum(x => x.Giren),
                        Expense = wData.Where(x => x.IslemTuru == "Ödeme").Sum(x => x.Cikan),
                        Redirected = 0
                    });
                }
            }
            else if (period.ToLower() == "yearly")
            {
                var startYear = today.Year - 4;
                var kasas = await Kasalar.GetAllAsync();
                var all = kasas.Where(k => k.Tarih.Year >= startYear).ToList();

                for (int i = 0; i < 5; i++)
                {
                    int year = startYear + i;
                    var yData = all.Where(x => x.Tarih.Year == year);
                    result.Add(new FinanceTrendItem {
                        Label = year.ToString(),
                        Date = new DateTime(year, 1, 1),
                        Income = yData.Where(x => x.IslemTuru == "Tahsilat").Sum(x => x.Giren),
                        Expense = yData.Where(x => x.IslemTuru == "Ödeme").Sum(x => x.Cikan),
                        Redirected = 0
                    });
                }
            }
            else // Monthly
            {
                var trend = await GetMonthlyIncomeExpenseAsync();
                result = trend.Select(x => new FinanceTrendItem {
                    Label = x.Month,
                    Date = new DateTime(x.Year, x.MonthInt, 1),
                    Income = x.Income,
                    Expense = x.Expense,
                    Redirected = 0
                }).ToList();
            }
        }
        catch { }
        return result;
    }

    public Task<List<SatisHedefi>> GetSatisHedefleriAsync() => Hedefler.GetAllAylikHedeflerAsync();
    public Task<List<SatisHedefi>> GetSatisHedefleriAsync(int yil) => Hedefler.GetAylikHedeflerAsync(yil);
    public Task<List<YillikSatisHedefi>> GetYillikSatisHedefleriAsync() => Hedefler.GetAllYillikHedeflerAsync();
    public Task<List<HaftalikSatisHedefi>> GetHaftalikSatisHedefleriAsync() => Hedefler.GetAllHaftalikHedeflerAsync();
    public async Task<List<CityProfitStat>> GetCityProfitStatsAsync()
    {
        var faturalar = await Faturalar.GetAllAsync();
        var cariler = await Cariler.GetAllAsync();
        
        var cityStats = new Dictionary<string, CityProfitStat>();
        
        foreach (var f in faturalar)
        {
            var cari = System.Linq.Enumerable.FirstOrDefault(cariler, c => c.Id == f.CariId);
            string sehir = (cari?.Il ?? "Bilinmiyor").ToUpper().Trim();
            if (string.IsNullOrEmpty(sehir)) sehir = "BİLİNMİYOR";

            if (!cityStats.ContainsKey(sehir))
                cityStats[sehir] = new CityProfitStat { Sehir = sehir };

            if ((f.Tur ?? "").Equals("Satış", System.StringComparison.OrdinalIgnoreCase) || (f.Tur ?? "").Equals("Satis", System.StringComparison.OrdinalIgnoreCase))
                cityStats[sehir].SatisToplam += f.GenelToplam;
            else
                cityStats[sehir].AlisToplam += f.GenelToplam;
        }

        return System.Linq.Enumerable.ToList(cityStats.Values);
    }
    public async Task RecalculateSystemBalancesAsync()
    {
        // 1. Cari Bakiyelerini Yenile
        var cariler = await Cariler.GetAllAsync();
        foreach (var c in cariler)
        {
            await Cariler.RecalculateBalanceAsync(c.Id);
        }

        // 2. Banka ve Kasa Bakiyelerini Yenile
        var bankalar = await Bankalar.GetAllAsync();
        foreach (var b in bankalar)
        {
            await Bankalar.RecalculateBalanceAsync(b.Id);
        }
    }
    public async Task RestoreBackupAsync(string jsonData)
    {
        using JsonDocument doc = JsonDocument.Parse(jsonData);
        JsonElement root = doc.RootElement;

        async Task Restore<T>(string propName, IRepository<T> repo) where T : class
        {
             if (root.TryGetProperty(propName, out JsonElement json))
             {
                 var list = JsonSerializer.Deserialize<List<T>>(json.GetRawText());
                 if (list != null) foreach (var item in list) await repo.SaveAsync(item);
             }
        }

        await Restore<CariKart>("Cariler", Cariler);
        await Restore<StokKart>("Stoklar", Stoklar);
        await Restore<Fatura>("Faturalar", Faturalar);
        await Restore<BankaKart>("Bankalar", Bankalar);
        await Restore<KasaHareket>("Kasalar", Kasalar);
        await Restore<Siparis>("Siparisler", Siparisler);
        await Restore<Teklif>("Teklifler", Teklifler);
        await Restore<Cek>("Cekler", Cekler);
        await Restore<Senet>("Senetler", Senetler);
        await Restore<KrediKartiIslem>("KrediKartlari", KrediKartlari);
        await Restore<EftIslem>("EftIslemleri", EftIslemleri);
        await Restore<StokSayimFisi>("StokSayimlar", StokSayimlar);
        await Restore<PortfoyKart>("Portfolyo", Portfolyo);
        await Restore<DovizKur>("DovizKurlari", DovizKurlari);

        // Specialized: Hedefler (multiple types)
        if (root.TryGetProperty("AylikHedefler", out JsonElement ahJson))
        {
            var list = JsonSerializer.Deserialize<List<SatisHedefi>>(ahJson.GetRawText());
            if (list != null) foreach (var item in list) await Hedefler.SaveAylikHedefAsync(item);
        }
        if (root.TryGetProperty("HaftalikHedefler", out JsonElement hhJson))
        {
            var list = JsonSerializer.Deserialize<List<HaftalikSatisHedefi>>(hhJson.GetRawText());
            if (list != null) foreach (var item in list) await Hedefler.SaveHaftalikHedefAsync(item);
        }
        if (root.TryGetProperty("YillikHedefler", out JsonElement yhJson))
        {
            var list = JsonSerializer.Deserialize<List<YillikSatisHedefi>>(yhJson.GetRawText());
            if (list != null) foreach (var item in list) await Hedefler.SaveYillikHedefAsync(item);
        }

        // Movements
        if (root.TryGetProperty("CariHareketler", out JsonElement chJson))
        {
            var list = JsonSerializer.Deserialize<List<CariHareket>>(chJson.GetRawText());
            if (list != null) foreach (var item in list) await Cariler.SaveHareketAsync(item);
        }
        
        if (root.TryGetProperty("StokHareketler", out JsonElement shJson))
        {
            var list = JsonSerializer.Deserialize<List<StokHareket>>(shJson.GetRawText());
            if (list != null) foreach (var item in list) await Stoklar.SaveHareketAsync(item);
        }

        if (root.TryGetProperty("KasaHareketler", out JsonElement khJson))
        {
            var list = JsonSerializer.Deserialize<List<KasaHareket>>(khJson.GetRawText());
            if (list != null) foreach (var item in list) await Kasalar.SaveAsync(item);
        }

        if (root.TryGetProperty("BankaHareketler", out JsonElement bhJson))
        {
            var list = JsonSerializer.Deserialize<List<BankaHareket>>(bhJson.GetRawText());
            if (list != null) foreach (var item in list) await Bankalar.SaveHareketAsync(item);
        }

        // Details (Grouped for Firebase)
        if (root.TryGetProperty("FaturaDetaylar", out JsonElement fdJson))
        {
            var list = JsonSerializer.Deserialize<List<FaturaDetay>>(fdJson.GetRawText());
            if (list != null) 
            {
                foreach (var g in System.Linq.Enumerable.GroupBy(list, d => d.FaturaId)) 
                    await _firebaseService.SaveAsync("FaturaDetaylar", g.ToList(), g.Key);
            }
        }

        if (root.TryGetProperty("SiparisDetaylar", out JsonElement sdJson))
        {
            var list = JsonSerializer.Deserialize<List<SiparisDetay>>(sdJson.GetRawText());
            if (list != null) 
            {
                foreach (var g in System.Linq.Enumerable.GroupBy(list, d => d.SiparisId)) 
                    await _firebaseService.SaveAsync("SiparisDetaylar", g.ToList(), g.Key);
            }
        }

        if (root.TryGetProperty("TeklifDetaylar", out JsonElement tdJson))
        {
            var list = JsonSerializer.Deserialize<List<TeklifDetay>>(tdJson.GetRawText());
            if (list != null) 
            {
                foreach (var g in System.Linq.Enumerable.GroupBy(list, d => d.TeklifId)) 
                    await _firebaseService.SaveAsync("TeklifDetaylar", g.ToList(), g.Key);
            }
        }

        if (root.TryGetProperty("StokSayimDetaylar", out JsonElement ssdJson))
        {
            var list = JsonSerializer.Deserialize<List<StokSayimDetay>>(ssdJson.GetRawText());
            if (list != null) 
            {
                foreach (var g in System.Linq.Enumerable.GroupBy(list, d => d.FisId)) 
                    await _firebaseService.SaveAsync("StokSayimDetaylar", g.ToList(), g.Key);
            }
        }

        await RecalculateSystemBalancesAsync();
    }

    public void SetCloudConfig(string url, string secret) => _firebaseService.SetConfig(url, secret);
    public (string Url, string Secret) GetCloudConfig() => ("", "");
    public void EnableAutoSync(bool enable) { }
    public async Task SyncToCloudAsync() => await Task.CompletedTask;
    public async Task SyncFromCloudAsync() => await Task.CompletedTask;
    
    // IDataProvider implementation
    public virtual async Task InitializeAsync() => await Task.CompletedTask;
    public virtual async Task InitializeAsync(string dbNameOrPath) => await Task.CompletedTask;
    public virtual void InvalidateAllCache() { /* No local cache for Firebase yet */ }

    public async Task<int> SaveChangesAsync()
    {
        await Task.CompletedTask;
        return 1;
    }

    public async Task BeginTransactionAsync()
    {
        await Task.CompletedTask;
    }

    public async Task CommitTransactionAsync()
    {
        await Task.CompletedTask;
    }

    public async Task RollbackTransactionAsync()
    {
        await Task.CompletedTask;
    }

    public async Task<FirmaProfili> GetFirmaProfiliAsync()
    {
        var list = await _firebaseService.GetAllAsync<FirmaProfili>("FirmaProfili");
        return list.FirstOrDefault(x => x.Id == 1) ?? new FirmaProfili { Id = 1, FirmaAdi = "Ermay Muhasebe Cloud" };
    }

    public async Task SaveFirmaProfiliAsync(FirmaProfili f)
    {
        f.Id = 1;
        await _firebaseService.SaveAsync("FirmaProfili", f, 1);
    }

    public async Task ClearAllTablesAsync()
    {
        if (_firebaseService != null)
        {
            await _firebaseService.DeleteYearAsync(DateTime.Now.Year);
        }
    }
}
