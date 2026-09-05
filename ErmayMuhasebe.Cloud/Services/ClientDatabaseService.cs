using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using MudBlazor;

using ErmayMuhasebe.Repositories.DataProviders;

namespace ErmayMuhasebe.Cloud.Services
{
    // Integrated Database Service: Bridges the Cloud UI with the centralized FirebaseService backend.
    public class ClientDatabaseService : DatabaseService, IDataProvider
    {
        private readonly FirebaseService _firebase;
        private readonly BrowserStorageService _browserStorage;
        private readonly IUnitOfWork _uow;

        // IDataProvider Repositories (Delegating to UoW)
        public ICariRepository Cariler => _uow.Cariler;
        public IStokRepository Stoklar => _uow.Stoklar;
        public IFaturaRepository Faturalar => _uow.Faturalar;
        public IBankaRepository Bankalar => _uow.Bankalar;
        public IKasaRepository Kasalar => _uow.Kasalar;
        public ISiparisRepository Siparisler => _uow.Siparisler;
        public ITeklifRepository Teklifler => _uow.Teklifler;
        public ICekRepository Cekler => _uow.Cekler;
        public ISenetRepository Senetler => _uow.Senetler;
        public IKrediKartiRepository KrediKartlari => _uow.KrediKartlari;
        public IEftRepository EftIslemleri => _uow.EftIslemleri;
        public IStokSayimRepository StokSayimlar => _uow.StokSayimlar;
        public IPortfoyRepository Portfolyo => _uow.Portfolyo;
        public IHedefRepository Hedefler => _uow.Hedefler;
        public IDovizRepository DovizKurlari => _uow.DovizKurlari;
        public IBelgeArsivRepository BelgeArsiv => _uow.BelgeArsiv;
        public INoteRepository Notes => _uow.Notes;
        public IMusteriTakipRepository MusteriTakip => _uow.MusteriTakip;

        public Task<int> SaveChangesAsync() => _uow.SaveChangesAsync();
        
        public override Task InitializeAsync() => Task.CompletedTask;
        public override Task InitializeAsync(string dbNameOrPath) => Task.CompletedTask;

        // In-Memory Cache for Performance
        private List<CariKart>? _cacheCariler;
        private List<StokKart>? _cacheStoklar;
        private List<Fatura>? _cacheFaturalar;
        private List<Siparis>? _cacheSiparisler;
        private List<BankaKart>? _cacheBankalar;
        
        public ClientDatabaseService(FirebaseService firebase, BrowserStorageService browserStorage, IUnitOfWork uow, IYearContext yearContext, Blazored.LocalStorage.ISyncLocalStorageService syncStorage) : base(yearContext)
        {
            _firebase = firebase;
            _browserStorage = browserStorage;
            _uow = uow;

            if (syncStorage != null && syncStorage.ContainKey("firebase_url"))
            {
                var url = syncStorage.GetItem<string>("firebase_url") ?? "";
                var token = syncStorage.GetItem<string>("firebase_token") ?? "";
                if (!string.IsNullOrEmpty(url))
                {
                    this.SyncService.SaveConfig(url, token);
                }
            }
        }

        private bool UseFirebase => _firebase.IsConfigured;

        // --- CARI METHODS ---
        public new async Task<List<CariKart>> GetCarilerAsync()
        {
            if (_cacheCariler != null) return _cacheCariler;
            _cacheCariler = await _uow.Cariler.GetAllAsync();
            return _cacheCariler;
        }

        private void ClearCariCache() => _cacheCariler = null;

        public new async Task<CariKart?> GetCariAsync(int id) 
        {
            var all = await GetCarilerAsync();
            return all.FirstOrDefault(c => c.Id == id);
        }

        public new async Task<int> SaveCariAsync(CariKart cari)
        {
            await _uow.Cariler.SaveAsync(cari);
            ClearCariCache(); // Invalidate
            return cari.Id;
        }

        public async Task<int> DeleteCariAsync(int id)
        {
             await _uow.Cariler.DeleteAsync(id);
             ClearCariCache(); // Invalidate
             return id;
        }
        
        public override void InvalidateAllCache()
        {
            _cacheCariler = null;
            _cacheStoklar = null;
            _cacheFaturalar = null;
            _cacheSiparisler = null;
            _cacheBankalar = null;
        }

        public new async Task<int> DeleteCariAsync(CariKart cari)
        {
             return await DeleteCariAsync(cari.Id);
        }

        public new async Task<List<CariHareket>> GetCariHareketlerAsync(int id)
        {
            var all = await _firebase.GetCariHareketlerAsync();
            return all.Where(h => h.CariId == id).OrderByDescending(h => h.Tarih).ToList();
        }

        public new async Task<List<CariDosya>> GetCariDosyalariAsync(int id)
        {
            await Task.Yield();
            // Placeholder for documents
            return new List<CariDosya> {
                new CariDosya { Ad = "Vergi Levhası.pdf", Tarih = DateTime.Now.AddMonths(-2), Boyut = "1.2 MB", Tur = "PDF" }
            };
        }

        public new async Task SaveCariHareketAsync(CariHareket h) 
        { 
            if (h.Id == 0)
            {
                var all = await _firebase.GetCariHareketlerAsync();
                h.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("CariHareketler", h, h.Id); 

            // Update Cari Balance
            var cari = await GetCariAsync(h.CariId);
            if (cari != null)
            {
                cari.Borc += h.Borc;
                cari.Alacak += h.Alacak;
                await SaveCariAsync(cari);
            }
        }

        public new async Task SaveStokHareketAsync(StokHareket h)
        {
            if (h.Id == 0)
            {
                var all = await _firebase.GetStokHareketlerAsync();
                h.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("StokHareketler", h, h.Id);
        }

        // --- STOK METHODS ---
        public new async Task<List<StokKart>> GetStoklarAsync()
        {
            if (_cacheStoklar != null) return _cacheStoklar;
            _cacheStoklar = await _uow.Stoklar.GetAllAsync();
            return _cacheStoklar;
        }

        private void ClearStokCache() => _cacheStoklar = null;

        public new async Task SaveStokAsync(StokKart stok)
        {
             await _uow.Stoklar.SaveAsync(stok);
             ClearStokCache(); // Invalidate
        }

        public new async Task<int> DeleteStokAsync(int id)
        {
            await _uow.Stoklar.DeleteAsync(id);
            ClearStokCache(); // Invalidate
            return 1;
        }

        public async Task<List<StokKart>> GetKritikStoklarAsync(int limit = 5)
        {
            var all = await GetStoklarAsync();
            return all.Where(s => s.Miktar <= s.KritikSeviye).OrderBy(s => s.Miktar).Take(limit).ToList();
        }

        public new async Task<StokKart?> GetStokAsync(int id) 
        {
            var all = await GetStoklarAsync();
            return all.FirstOrDefault(s => s.Id == id);
        }

        public async Task<List<StokHareket>> GetStokHareketlerAsync(int id)
        {
            var all = await _firebase.GetStokHareketlerAsync();
            return all.Where(h => h.StokId == id).OrderByDescending(h => h.Tarih).ToList();
        }

        public async Task<List<StokDosya>> GetStokDosyalariAsync(int id)
        {
            await Task.Yield();
            return new List<StokDosya> {
                new StokDosya { Ad = "Urun_Belgesi.pdf", Tarih = DateTime.Now.AddDays(-10), Boyut = "850 KB", Tur = "PDF" }
            };
        }

        // --- SIPARIS METHODS ---
        public new async Task<List<Siparis>> GetSiparislerAsync()
        {
            if (_cacheSiparisler != null) return _cacheSiparisler;
            _cacheSiparisler = await _uow.Siparisler.GetAllAsync();
            return _cacheSiparisler;
        }

        private void ClearSiparisCache() => _cacheSiparisler = null;

        public new async Task<Siparis?> GetSiparisAsync(int id)
        {
            var all = await GetSiparislerAsync();
            return all.FirstOrDefault(s => s.Id == id);
        }

        public new async Task<List<SiparisDetay>> GetSiparisDetaylarAsync(int id)
        {
            return await _firebase.GetSiparisDetaylarAsync(id);
        }

        public new async Task SaveSiparisWithDetailsAsync(Siparis s, List<SiparisDetay> details) 
        { 
            if (details == null || !details.Any()) throw new ValidationException("Sipariş kalemi olmadan kaydedilemez.");
            if (details.Any(d => d.Miktar <= 0)) throw new ValidationException("Sipariş miktarı 0 veya negatif olamaz.");
            if (details.Any(d => d.BirimFiyat < 0)) throw new ValidationException("Birim fiyat negatif olamaz.");

            if (s.Id == 0) 
            {
                var all = await GetSiparislerAsync();
                s.Id = all.Any() ? all.Max(x => x.Id) + 1 : new Random().Next(1000, 9999);
            }
            await _firebase.SaveSiparisAsync(s);
            await _firebase.SaveSiparisDetaylarAsync(s.Id, details);
            ClearSiparisCache(); // Invalidate
        }

        public new async Task<int> DeleteSiparisAsync(int id) 
        { 
            await _firebase.DeleteAsync("Siparisler", id); 
            ClearSiparisCache(); // Invalidate
            return id;
        }

        // --- TEKLIF METHODS ---
        public new async Task<List<Teklif>> GetTekliflerAsync()
        {
            return await _uow.Teklifler.GetAllAsync();
        }

        public new async Task<Teklif?> GetTeklifAsync(int id)
        {
            var all = await GetTekliflerAsync();
            return all.FirstOrDefault(x => x.Id == id);
        }

        public new async Task<List<TeklifDetay>> GetTeklifDetaylarAsync(int id)
        {
            return await _firebase.GetTeklifDetaylarAsync(id);
        }

        public new async Task SaveTeklifWithDetailsAsync(Teklif t, List<TeklifDetay> d) 
        { 
            if (d == null || !d.Any()) throw new ValidationException("Teklif kalemi olmadan kaydedilemez.");
            if (d.Any(x => x.Miktar <= 0)) throw new ValidationException("Teklif miktarı 0 veya negatif olamaz.");
            if (d.Any(x => x.BirimFiyat < 0)) throw new ValidationException("Birim fiyat negatif olamaz.");

            if (t.Id == 0) 
            {
                var all = await GetTekliflerAsync();
                t.Id = all.Any() ? all.Max(x => x.Id) + 1 : new Random().Next(1000, 9999);
            }
            await _firebase.SaveTeklifAsync(t); 
            await _firebase.SaveTeklifDetaylarAsync(t.Id, d);
        }

        public new async Task<int> DeleteTeklifAsync(int id)
        {
            await _firebase.DeleteAsync("Teklifler", id);
            return id;
        }

        // --- FATURA METHODS ---
        public new async Task<List<Fatura>> GetFaturalarAsync() 
        { 
            if (_cacheFaturalar != null) return _cacheFaturalar;
            _cacheFaturalar = await _uow.Faturalar.GetAllAsync();
            return _cacheFaturalar;
        }

        private void ClearFaturaCache() => _cacheFaturalar = null;

        public new async Task<Fatura?> GetFaturaAsync(int id) 
        { 
            var all = await GetFaturalarAsync(); 
            return all.FirstOrDefault(f => f.Id == id); 
        }

        public new async Task<int> DeleteFaturaAsync(int id)
        {
            await _uow.Faturalar.DeleteAsync(id);
            ClearFaturaCache(); // Invalidate
            return id;
        }

        public new async Task<List<FaturaDetay>> GetFaturaDetaylarAsync(int faturaId)
        {
            return await _uow.Faturalar.GetDetaylarAsync(faturaId);
        }
        
        public async Task SaveFaturaWithDetailsAsync(Fatura f, List<FaturaDetay> d, bool updateCari = true, bool updateStok = true, bool updateStokPrices = false) 
        { 
            if (d == null || !d.Any()) throw new ValidationException("Fatura kalemi olmadan kaydedilemez.");
            if (d.Any(x => x.Miktar <= 0)) throw new ValidationException("Fatura miktarı 0 veya negatif olamaz.");
            if (d.Any(x => x.BirimFiyat < 0)) throw new ValidationException("Birim fiyat negatif olamaz.");

            bool isSatis = (f.Tur?.Equals("Satış", StringComparison.OrdinalIgnoreCase) == true || 
                            f.Tur?.Equals("Satis", StringComparison.OrdinalIgnoreCase) == true);
            await _uow.Faturalar.SaveWithDetailsAndTransactionAsync(f, d, isSatis, updateCari, updateStok, updateStokPrices);
            
            // Invalidate Caches
            ClearFaturaCache();
            if (updateCari) ClearCariCache();
            if (updateStok) ClearStokCache();
        }

        // --- DOVIZ ---
        public override async Task<List<DovizKur>> GetDovizKurlariAsync() 
        {
            return await _uow.DovizKurlari.GetAllAsync();
        }

        public override async Task<DovizKur?> GetSonDovizKurAsync(string kod) 
        {
            var all = await GetDovizKurlariAsync();
            return all.Where(d => d.Kod == kod).OrderByDescending(d => d.Tarih).FirstOrDefault();
        }

        public override async Task<int> SaveDovizKurAsync(DovizKur d) 
        {
            var all = await GetDovizKurlariAsync();
            var existing = all.FirstOrDefault(x => x.Kod == d.Kod && x.Tarih.Date == d.Tarih.Date);
            
            if (existing != null)
            {
                d.Id = existing.Id;
            }
            else
            {
                d.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("DovizKurlari", d, d.Id);
            return 1;
        }

        public async Task<List<Fatura>> GetGecikmisAlacaklarAsync()
        {
            var all = await GetFaturalarAsync();
            return all.Where(f => (f.Tur == "Satış" || f.Tur == "Satis") && f.Odenen < f.GenelToplam && f.VadeTarihi < DateTime.Now).ToList();
        }

        // --- CASH / BANK METHODS ---
        public new async Task<List<BankaKart>> GetBankalarAsync() 
        {
            if (_cacheBankalar != null) return _cacheBankalar;
            _cacheBankalar = await _firebase.GetBankalarAsync();
            return _cacheBankalar;
        }

        private void ClearBankaCache() => _cacheBankalar = null;
        public new async Task SaveBankaAsync(BankaKart b) 
        { 
             if (b.Id == 0) {
                 var all = await GetBankalarAsync();
                 b.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
             }
             await _firebase.SaveAsync("Bankalar", b, b.Id); 
             ClearBankaCache(); // Invalidate
        }

        public async Task SaveCekAsync(Cek c)
        {
            if (c.Id == 0)
            {
                var all = await _firebase.GetAllAsync<Cek>("Cekler");
                c.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("Cekler", c, c.Id);
        }

        public new async Task SaveSenetAsync(Senet s)
        {
            if (s.Id == 0)
            {
                var all = await _firebase.GetAllAsync<Senet>("Senetler");
                s.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("Senetler", s, s.Id);
        }

        // --- GOREV METHODS ---
        public new async Task<List<Gorev>> GetGorevlerAsync() => await _firebase.GetAllAsync<Gorev>("Gorevler");
        public new async Task SaveGorevAsync(Gorev g) 
        { 
            if (g.Id == 0) {
                var all = await GetGorevlerAsync();
                g.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("Gorevler", g, g.Id); 
        }
        public new async Task<int> DeleteGorevAsync(int id) 
        { 
            await _firebase.DeleteAsync("Gorevler", id); 
            return 1;
        }

        // --- ANALYTICS & DASHBOARD ---
        public new async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var stats = new DashboardStats();
            var faturalar = await GetFaturalarAsync();
            var cariler = await GetCarilerAsync();
            var stoklar = await GetStoklarAsync();
            var bankalar = await GetBankalarAsync();

            stats.GunlukCiro = faturalar.Where(f => f.Tarih.Date == DateTime.Now.Date && (f.Tur == "Satış" || f.Tur == "Satis")).Sum(f => f.GenelToplam);
            stats.ToplamNakitVarligi = bankalar.Sum(b => b.GuncelBakiye);
            stats.ToplamAlacak = cariler.Sum(c => c.Borc - c.Alacak);
            stats.KritikStokSayisi = stoklar.Count(s => s.Miktar <= s.KritikSeviye);
            return stats;
        }

        public async Task<List<RecentTransactionItem>> GetRecentTransactionsAsync()
        {
            var faturalar = await GetFaturalarAsync();
            return faturalar.OrderByDescending(f => f.Tarih).Take(5)
                .Select(f => new RecentTransactionItem { 
                    Title = f.CariUnvan ?? "Pazar", 
                    Amount = f.GenelToplam, 
                    Date = f.Tarih, 
                    Type = (f.Tur == "Satış" || f.Tur == "Satis") ? "In" : "Out" 
                }).ToList();
        }

        public async Task<List<ChartSeries>> GetMonthlyProfitDataAsync()
        {
            var faturalar = await GetFaturalarAsync();
            var year = DateTime.Now.Year;
            var data = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                var sales = faturalar.Where(f => (f.Tur == "Satış" || f.Tur == "Satis") && f.Tarih.Year == year && f.Tarih.Month == i).Sum(f => f.GenelToplam);
                var costs = faturalar.Where(f => f.Tur == "Alış" && f.Tarih.Year == year && f.Tarih.Month == i).Sum(f => f.GenelToplam);
                data[i - 1] = (double)(sales - costs);
            }
            return new List<ChartSeries> { new ChartSeries { Name = "Net Kâr", Data = data } };
        }

        public async Task<double> GetTotalProfitYTDAsync()
        {
            var faturalar = await GetFaturalarAsync();
            var year = DateTime.Now.Year;
            var sales = faturalar.Where(f => (f.Tur == "Satış" || f.Tur == "Satis") && f.Tarih.Year == year).Sum(f => f.GenelToplam);
            var costs = faturalar.Where(f => f.Tur == "Alış" && f.Tarih.Year == year).Sum(f => f.GenelToplam);
            return (double)(sales - costs);
        }

        public async Task<double> GetProfitMarginYTDAsync()
        {
            var faturalar = await GetFaturalarAsync();
            var year = DateTime.Now.Year;
            var sales = faturalar.Where(f => (f.Tur == "Satış" || f.Tur == "Satis") && f.Tarih.Year == year).Sum(f => f.GenelToplam);
            var costs = faturalar.Where(f => f.Tur == "Alış" && f.Tarih.Year == year).Sum(f => f.GenelToplam);
            if (sales == 0) return 0;
            return (double)((sales - costs) / sales * 100);
        }

        public async Task<List<CariKart>> GetTopCustomersAsync()
        {
            var faturalar = await GetFaturalarAsync();
            var cariler = await GetCarilerAsync();
            var topIds = faturalar.Where(f => f.Tur == "Satış" || f.Tur == "Satis")
                .GroupBy(f => f.CariId)
                .OrderByDescending(g => g.Sum(f => f.GenelToplam))
                .Take(5)
                .Select(g => g.Key)
                .ToList();
            return cariler.Where(c => topIds.Contains(c.Id)).ToList();
        }

        public async Task<List<ProductProfitResult>> GetProductProfitabilityAsync()
        {
            var stoklar = await GetStoklarAsync();
            return stoklar.Select(s => new ProductProfitResult {
                StokAdi = s.StokAdi ?? "İsimsiz",
                Quantity = (int)s.Miktar,
                Revenue = (decimal)s.Miktar * s.SatisFiyati,
                Profit = (decimal)s.Miktar * (s.SatisFiyati - s.AlisFiyati),
                Margin = s.SatisFiyati > 0 ? (s.SatisFiyati - s.AlisFiyati) / s.SatisFiyati * 100 : 0
            }).OrderByDescending(x => x.Profit).Take(10).ToList();
        }

        // --- GLOBAL SEARCH ---
        public new async Task<List<ErmayMuhasebe.Models.GlobalSearchResult>> GlobalSearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new();
            var res = new List<ErmayMuhasebe.Models.GlobalSearchResult>();
            
            // Run fetches in parallel for performance
            var t1 = GetCarilerAsync();
            var t2 = GetStoklarAsync();
            var t3 = GetFaturalarAsync();
            var t4 = GetSiparislerAsync();

            await Task.WhenAll(t1, t2, t3, t4);

            var cariler = t1.Result;
            var stoklar = t2.Result;
            var faturalar = t3.Result;
            var siparisler = t4.Result;

            res.AddRange(cariler.Where(c => (c.Unvan ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(c => new ErmayMuhasebe.Models.GlobalSearchResult { Title = c.Unvan ?? "", Type = "Cari", Id = c.Id, Icon = Icons.Material.Filled.Person }));
            
            res.AddRange(stoklar.Where(s => (s.StokAdi ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) || (s.StokKodu ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(s => new ErmayMuhasebe.Models.GlobalSearchResult { Title = s.StokAdi ?? "", Type = "Stok", Id = s.Id, Icon = Icons.Material.Filled.Inventory2 }));
                
            res.AddRange(faturalar.Where(f => f.FaturaNo != null && f.FaturaNo.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(f => new ErmayMuhasebe.Models.GlobalSearchResult { Title = f.FaturaNo + " (Fatura)", Type = "Fatura", Id = f.Id, Icon = Icons.Material.Filled.Receipt }));

            res.AddRange(siparisler.Where(s => s.SiparisNo != null && s.SiparisNo.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(s => new ErmayMuhasebe.Models.GlobalSearchResult { Title = s.SiparisNo + " (Sipariş)", Type = "Siparis", Id = s.Id, Icon = Icons.Material.Filled.Assignment }));

            return res.Take(10).ToList();
        }

        public async Task<object> GetFullBackupAsync() 
        { 
            return new { 
                Caris = await GetCarilerAsync(), 
                Stocks = await GetStoklarAsync(), 
                Invoices = await GetFaturalarAsync(),
                Siparisler = await GetSiparislerAsync(),
                Teklifler = await GetTekliflerAsync()
            }; 
        }

        // --- CHART DATA ---
        public Task<List<ChartSeries>> GetLast7DaysSalesAsync() => Task.FromResult(new List<ChartSeries> { new ChartSeries { Name = "Satışlar", Data = new double[] { 10, 20, 15, 30, 25, 40, 35 } } });
        public Task<string[]> GetLast7DaysLabelsAsync() => Task.FromResult(new string[] { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" });
        
        public Task<List<ChartSeries>> GetMonthlyExpenseDataAsync() => Task.FromResult(new List<ChartSeries> { new ChartSeries { Name = "Giderler", Data = new double[] { 2000, 3000, 2500, 4000 } } });
        public Task<string[]> GetMonthlyExpenseLabelsAsync() => Task.FromResult(new[] { "Ocak", "Şubat", "Mart", "Nisan" });
        public Task<List<double>> GetExpenseDistributionDataAsync() => Task.FromResult(new List<double> { 30, 20, 15, 35 });
        public Task<string[]> GetExpenseDistributionLabelsAsync() => Task.FromResult(new[] { "Personel", "Kira", "Enerji", "Diğer" });

        public new async Task<int> ConvertTeklifToSiparisAsync(int id)
        {
            var t = await GetTeklifAsync(id);
            if (t == null) return 0;

            var details = await GetTeklifDetaylarAsync(id);
            var s = new Siparis
            {
                CariId = t.CariId,
                CariUnvan = t.CariUnvan,
                Tarih = DateTime.Now,
                Aciklama = t.Aciklama + " (Tekliften Dönüştürüldü)",
                GenelToplam = t.GenelToplam,
                Durum = "Bekliyor",
                OdemeBilgisi = t.OdemeBilgisi,
                BaglantiEvrakNo = t.TeklifNo
            };

            var sDetails = details.Select(d => new SiparisDetay
            {
                StokId = d.StokId,
                StokAdi = d.StokAdi,
                Miktar = d.Miktar,
                Birim = d.Birim,
                BirimFiyat = d.BirimFiyat,
                Tutar = d.Tutar,
                KdvOrani = d.KdvOrani,
                ParaBirimi = d.ParaBirimi
            }).ToList();

            await SaveSiparisWithDetailsAsync(s, sDetails);
            
            t.Durum = "Onaylandı";
            await _firebase.SaveTeklifAsync(t);
            
            return s.Id;
        }

        public new async Task<int> ConvertSiparisToFaturaAsync(int id)
        {
             var s = await GetSiparisAsync(id);
            if (s == null) return 0;

            var details = await GetSiparisDetaylarAsync(id);
            
            string pfx = "SAT";
            string fNo = $"{pfx}-{DateTime.Now:yyyyMMddHHmmss}";

            var f = new Fatura
            {
                CariId = s.CariId,
                CariUnvan = s.CariUnvan,
                Tarih = DateTime.Now,
                FaturaNo = fNo,
                Tur = "Satış",
                GenelToplam = s.GenelToplam,
                DovizTuru = details.FirstOrDefault()?.ParaBirimi,
                Aciklama = s.Aciklama + " (Siparişten Dönüştürüldü)"
            };

            decimal araToplam = 0;
            decimal toplamKdv = 0;

            var fDetails = details.Select(d => {
                decimal subtotal = (decimal)d.Miktar * d.BirimFiyat;
                decimal kdvAmt = subtotal * (decimal)d.KdvOrani / 100m;
                
                araToplam += subtotal;
                toplamKdv += kdvAmt;
                
                return new FaturaDetay
                {
                    StokId = d.StokId,
                    StokAdi = d.StokAdi,
                    StokKodu = "", 
                    Miktar = d.Miktar,
                    Birim = d.Birim,
                    BirimFiyat = d.BirimFiyat,
                    ToplamTutar = subtotal + kdvAmt,
                    KDVOrani = (int)d.KdvOrani,
                    Aciklama = d.Aciklama,
                    KDVTutari = kdvAmt
                };
            }).ToList();

            f.AraToplam = araToplam;
            f.ToplamKDV = toplamKdv;
            f.GenelToplam = araToplam + toplamKdv;

            await SaveFaturaWithDetailsAsync(f, fDetails, true, true);
            
            s.Durum = "Faturalandırıldı";
            await _firebase.SaveSiparisAsync(s);
            
            return f.Id;
        }
        public new async Task<FirmaProfili?> GetFirmaProfiliAsync()
        {
            var all = await _browserStorage.GetAllAsync<FirmaProfili>("FirmaProfili");
            var firma = all.FirstOrDefault();
            return firma ?? new FirmaProfili { FirmaAdi = "Ermay Muhasebe Cloud", Id = 1 };
        }
        
        public async Task<List<BelgeArsiv>> GetBelgelerAsync(string kategori)
        {
            var all = await _firebase.GetAllAsync<BelgeArsiv>("BelgeArsiv");
            return all.Where(b => b.Kategori == kategori && !b.IsDeleted).ToList();
        }

        public async Task SaveBelgeAsync(BelgeArsiv belge)
        {
            if (belge.Id == 0)
            {
                var all = await _firebase.GetAllAsync<BelgeArsiv>("BelgeArsiv");
                belge.Id = all.Any() ? all.Max(x => x.Id) + 1 : 1;
            }
            await _firebase.SaveAsync("BelgeArsiv", belge, belge.Id);
        }

        public async Task DeleteBelgeAsync(int id)
        {
            await _firebase.DeleteAsync("BelgeArsiv", id);
        }

        public new async Task<List<CityProfitStat>> GetCityProfitStatsAsync()
        {
            var faturalar = await GetFaturalarAsync();
            var cariler = await GetCarilerAsync();
            
            // Map cariler to cities
            var cityStats = new Dictionary<string, CityProfitStat>();
            
            foreach (var f in faturalar)
            {
                var cari = cariler.FirstOrDefault(c => c.Id == f.CariId);
                string sehir = (cari?.Il ?? "Bilinmiyor").ToUpper().Trim();
                if (string.IsNullOrEmpty(sehir)) sehir = "BİLİNMİYOR";

                if (!cityStats.ContainsKey(sehir))
                    cityStats[sehir] = new CityProfitStat { Sehir = sehir };

                if (f.Tur == "Satış" || f.Tur == "Satis")
                    cityStats[sehir].SatisToplam += f.GenelToplam;
                else
                    cityStats[sehir].AlisToplam += f.GenelToplam;
            }

            return cityStats.Values.ToList();
        }

        public async Task<object> GetAllDataForBackupAsync()
        {
            return new
            {
                Cariler = await GetCarilerAsync(),
                Stoklar = await GetStoklarAsync(),
                Faturalar = await GetFaturalarAsync(),
                Siparisler = await GetSiparislerAsync(),
                Teklifler = await GetTekliflerAsync(),
                Bankalar = await GetBankalarAsync(),
                Kasalar = await _firebase.GetKasaHareketlerAsync(),
                CariHareketler = await _firebase.GetCariHareketlerAsync(),
                StokHareketler = await _firebase.GetStokHareketlerAsync(),
                BankaHareketler = await _firebase.GetBankaHareketlerAsync(),
                Cekler = await _firebase.GetAllAsync<Cek>("Cekler"),
                Senetler = await _firebase.GetAllAsync<Senet>("Senetler"),
                Portfolyo = await _firebase.GetAllAsync<PortfoyKart>("Portfolyo"),
                AylikHedefler = await _uow.Hedefler.GetAllAylikHedeflerAsync(),
                HaftalikHedefler = await _uow.Hedefler.GetAllHaftalikHedeflerAsync(),
                YillikHedefler = await _uow.Hedefler.GetAllYillikHedeflerAsync(),
                DovizKurlari = await _uow.DovizKurlari.GetAllAsync(),
                FaturaDetaylar = await _uow.Faturalar.GetAllDetaylarAsync(),
                SiparisDetaylar = await _uow.Siparisler.GetAllDetaylarAsync(),
                TeklifDetaylar = await _uow.Teklifler.GetAllDetaylarAsync()
            };
        }

        public async Task RestoreBackupAsync(string jsonContent)
        {
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;
                await _browserStorage.ClearAllAsync(); // Clear existing browser data before restoration

                var restoreMap = new Dictionary<string, Type>
                {
                    { "Cariler", typeof(CariKart) },
                    { "Stoklar", typeof(StokKart) },
                    { "Faturalar", typeof(Fatura) },
                    { "Siparisler", typeof(Siparis) },
                    { "Teklifler", typeof(Teklif) },
                    { "Bankalar", typeof(BankaKart) },
                    { "Kasalar", typeof(KasaHareket) }, // Maps to KasaHareketler in Save
                    { "CariHareketler", typeof(CariHareket) },
                    { "StokHareketler", typeof(StokHareket) },
                    { "BankaHareketler", typeof(BankaHareket) },
                    { "Cekler", typeof(Cek) },
                    { "Senetler", typeof(Senet) },
                    { "Portfolyo", typeof(PortfoyKart) }
                };

                foreach (var kvp in restoreMap)
                {
                    string jsonKey = kvp.Key;
                    if (root.TryGetProperty(jsonKey, out var array) && array.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var listType = typeof(List<>).MakeGenericType(kvp.Value);
                        var deserializedList = System.Text.Json.JsonSerializer.Deserialize(array.GetRawText(), listType);
                        
                        string storageKey = jsonKey;
                        // Map internal naming anomalies to the actual table names used in Firebase/Browser DB
                        if (jsonKey == "Kasalar") storageKey = "KasaHareketler"; 

                        if (deserializedList != null)
                        {
                            var saveListMethod = typeof(BrowserStorageService).GetMethod("SaveListAsync")?.MakeGenericMethod(kvp.Value);
                            var task = (Task)saveListMethod?.Invoke(_browserStorage, new object[] { storageKey, deserializedList })!;
                            await task;
                        }
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Yedek geri yüklenemedi: {ex.Message}");
            }
        }

        public new async Task SaveFirmaProfiliAsync(FirmaProfili f)
        {
            f.Id = 1; // Always use ID 1 for single firma profile
            await _browserStorage.SaveAsync("FirmaProfili", f, f.Id);
        }

        // Kanban Methods
        public async Task<KanbanData?> GetKanbanDataAsync() => await _browserStorage.GetAsync<KanbanData>("kanban_data", 1);
        public async Task SaveKanbanDataAsync(KanbanData data) => await _browserStorage.SaveAsync("kanban_data", data, 1);
    }



    public class ProductProfitResult
    {
        public string StokAdi { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal Margin { get; set; }
    }
}
