using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using ErmayMuhasebe.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Shared.ViewModels;

public abstract partial class CariDetayViewModel : ViewModelBase
{
    protected readonly IUnitOfWork _uow;
    protected readonly IPdfService _pdfService;

    [ObservableProperty] private CariKart? _cari;
    [ObservableProperty] private ObservableCollection<CariHareket> _hareketler = new();
    
    // Summary properties
    [ObservableProperty] private decimal _borc;
    [ObservableProperty] private decimal _alacak;
    [ObservableProperty] private decimal _bakiye;
    [ObservableProperty] private decimal _gecikenBorcToplam;

    public CariDetayViewModel(IUnitOfWork uow, IPdfService pdfService)
    {
        _uow = uow;
        _pdfService = pdfService;
    }

    public virtual async Task InitializeAsync(int cariId)
    {
        Cari = await _uow.Cariler.GetByIdAsync(cariId);
        if (Cari != null)
        {
            Borc = Cari.Borc;
            Alacak = Cari.Alacak;
            Bakiye = Borc - Alacak;
            await LoadHareketlerAsync();
            await LoadExtendedDetailsAsync();
        }
    }

    [ObservableProperty] private ObservableCollection<CariFaturaDetayItem> _faturalar = new();
    [ObservableProperty] private ObservableCollection<CariEvrakItem> _bekleyenEvraklar = new();
    
    [ObservableProperty] private double _riskOrani;
    [ObservableProperty] private bool _isRiskLimitExceeded;
    [ObservableProperty] private string _riskLimitStatusText = "Risk Limiti Tanımlanmamış";
    [ObservableProperty] private string _performansSkoru = "-";
    [ObservableProperty] private string _performansMetni = "Veri Yok";

    public virtual async Task LoadExtendedDetailsAsync()
    {
        if (Cari == null) return;

        try
        {
            // 1. Faturalar
            var allFaturalar = await _uow.Faturalar.GetAllAsync();
            var cariFaturalar = allFaturalar.Where(f => !f.IsDeleted && f.CariId == Cari.Id)
                                            .OrderByDescending(f => f.Tarih)
                                            .ToList();

            var faturaItems = new List<CariFaturaDetayItem>();
            var overdueFaturalar = new List<Fatura>();

            foreach (var f in cariFaturalar)
            {
                var kalan = f.GenelToplam - f.Odenen;
                var item = new CariFaturaDetayItem
                {
                    Id = f.Id,
                    FaturaNo = f.FaturaNo,
                    Tarih = f.Tarih,
                    VadeTarihi = f.VadeTarihi,
                    GenelToplam = f.GenelToplam,
                    Odenen = f.Odenen,
                    Kalan = kalan,
                    IsIncoming = f.Tur == "Satis" || f.Tur == "Satış"
                };

                if (kalan <= 0)
                {
                    item.OdemeDurumu = "Ödendi";
                    item.KalanGunText = "Ödendi";
                    item.IsOverdue = false;
                    item.StatusColor = "Gray";
                }
                else
                {
                    item.OdemeDurumu = f.Odenen > 0 ? "Kısmi Ödendi" : "Ödenmedi";
                    int days = (f.VadeTarihi.Date - DateTime.Today).Days;

                    if (days < 0)
                    {
                        item.KalanGunText = $"{Math.Abs(days)} GÜN GECİKTİ";
                        item.IsOverdue = true;
                        item.StatusColor = "Red";
                        overdueFaturalar.Add(f);
                    }
                    else if (days == 0)
                    {
                        item.KalanGunText = "BUGÜN";
                        item.IsOverdue = false;
                        item.StatusColor = "Green";
                    }
                    else
                    {
                        item.KalanGunText = $"{days} Gün Kaldı";
                        item.IsOverdue = false;
                        item.StatusColor = "Green";
                    }
                }
                faturaItems.Add(item);
            }

            // 2. Çek / Senetler
            var evrakItems = new List<CariEvrakItem>();

            var allCekler = await _uow.Cekler.GetAllAsync();
            var cariCekler = allCekler.Where(c => c.CariId == Cari.Id && c.Durum != "Tahsil Edildi" && c.Durum != "Ödendi").ToList();
            foreach (var c in cariCekler)
            {
                var evrak = new CariEvrakItem
                {
                    Id = c.Id,
                    Type = "Çek",
                    No = c.CekNo,
                    VadeTarihi = c.VadeTarihi,
                    Tutar = c.Tutar,
                    Tur = c.CekTuru == "Verilen" ? "Verilen" : "Alınan",
                    Durum = c.Durum
                };

                int days = (c.VadeTarihi.Date - DateTime.Today).Days;
                if (days < 0)
                {
                    evrak.KalanGunText = $"{Math.Abs(days)} GÜN GECİKTİ";
                    evrak.IsOverdue = true;
                    evrak.StatusColor = "Red";
                }
                else if (days == 0)
                {
                    evrak.KalanGunText = "BUGÜN";
                    evrak.IsOverdue = false;
                    evrak.StatusColor = "Green";
                }
                else
                {
                    evrak.KalanGunText = $"{days} Gün Kaldı";
                    evrak.IsOverdue = false;
                    evrak.StatusColor = "Green";
                }
                evrakItems.Add(evrak);
            }

            var allSenetler = await _uow.Senetler.GetAllAsync();
            var cariSenetler = allSenetler.Where(s => s.CariId == Cari.Id && s.Durum != "Tahsil Edildi" && s.Durum != "Ödendi").ToList();
            foreach (var s in cariSenetler)
            {
                var evrak = new CariEvrakItem
                {
                    Id = s.Id,
                    Type = "Senet",
                    No = s.SenetNo,
                    VadeTarihi = s.VadeTarihi,
                    Tutar = s.Tutar,
                    Tur = s.SenetTuru == "Verilen" ? "Verilen" : "Alınan",
                    Durum = s.Durum
                };

                int days = (s.VadeTarihi.Date - DateTime.Today).Days;
                if (days < 0)
                {
                    evrak.KalanGunText = $"{Math.Abs(days)} GÜN GECİKTİ";
                    evrak.IsOverdue = true;
                    evrak.StatusColor = "Red";
                }
                else if (days == 0)
                {
                    evrak.KalanGunText = "BUGÜN";
                    evrak.IsOverdue = false;
                    evrak.StatusColor = "Green";
                }
                else
                {
                    evrak.KalanGunText = $"{days} Gün Kaldı";
                    evrak.IsOverdue = false;
                    evrak.StatusColor = "Green";
                }
                evrakItems.Add(evrak);
            }

            evrakItems = evrakItems.OrderBy(e => e.VadeTarihi).ToList();

            // 3. Risk Limiti Oranı Hesaplama
            double riskOrani = 0;
            bool isExceeded = false;
            string statusText = "Risk Limiti Tanımlanmamış";

            if (Cari.RiskLimiti > 0)
            {
                statusText = $"{Cari.RiskLimiti:N2} ₺";
                if (Bakiye > 0)
                {
                    riskOrani = (double)(Bakiye / Cari.RiskLimiti) * 100;
                    isExceeded = Bakiye > Cari.RiskLimiti;
                }
            }

            // 4. Ödeme Performans Skoru Hesaplama
            string score = "A+";
            string desc = "Zamanında Öder / Gecikme Yok";

            if (overdueFaturalar.Any())
            {
                double avgDelay = overdueFaturalar.Average(f => (DateTime.Today - f.VadeTarihi).Days);
                if (avgDelay <= 5)
                {
                    score = "A";
                    desc = $"Güvenilir (Ortalama {avgDelay:F0} gün gecikmeli)";
                }
                else if (avgDelay <= 15)
                {
                    score = "B";
                    desc = $"Standart (Ortalama {avgDelay:F0} gün gecikmeli)";
                }
                else if (avgDelay <= 30)
                {
                    score = "C";
                    desc = $"Riskli (Ortalama {avgDelay:F0} gün gecikmeli)";
                }
                else
                {
                    score = "F";
                    desc = $"Kritik (Ortalama {avgDelay:F0} gün gecikmeli)";
                }
            }

            await InvokeOnUIThreadAsync(() =>
            {
                Faturalar = new ObservableCollection<CariFaturaDetayItem>(faturaItems);
                BekleyenEvraklar = new ObservableCollection<CariEvrakItem>(evrakItems);
                RiskOrani = riskOrani;
                IsRiskLimitExceeded = isExceeded;
                RiskLimitStatusText = statusText;
                PerformansSkoru = score;
                PerformansMetni = desc;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading extended details: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task LoadHareketlerAsync()
    {
        if (Cari == null) return;
        try 
        {
            IsLoading = true;
            var list = await _uow.Cariler.GetHareketlerAsync(Cari.Id);
            
            // Calculate Running Balance
            var sorted = list.OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
            decimal balance = 0;
            foreach (var h in sorted)
            {
                balance += (h.Borc - h.Alacak);
                h.KalanBakiye = balance;
            }

            // Vade bilgisi boş olan hareketleri faturalardan eşleyerek güncelle ve DB'ye Kaydet
            bool synced = false;
            foreach (var h in sorted.Where(x => x.Vade == null && x.FaturaId.HasValue))
            {
                var f = await _uow.Faturalar.GetByIdAsync(h.FaturaId.GetValueOrDefault());
                if (f != null) 
                {
                    h.Vade = f.VadeTarihi;
                    await _uow.Cariler.SaveHareketAsync(h); // DB'ye kaydet ki kalıcı olsun
                    synced = true;
                }
            }
            if (synced) // Eğer DB güncellemesi yapıldıysa listeyi tazele
            {
                list = await _uow.Cariler.GetHareketlerAsync(Cari.Id);
                sorted = list.OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
            }

            await InvokeOnUIThreadAsync(() => 
            {
                Hareketler = new ObservableCollection<CariHareket>(sorted.OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id));
                
                // Borç ve Alacak toplamlarını hareketlerden tekrar hesaplayarak güncelliği garanti et
                Borc = sorted.Sum(x => x.Borc);
                Alacak = sorted.Sum(x => x.Alacak);
                Bakiye = Borc - Alacak;

                // Geciken hesaplama mantığı (FIFO):
                if (Bakiye > 0)
                {
                    // Cari bize borçlu (Alacağımız var)
                    // Gelecek borçları hesapla (Vadesi bugün veya sonrası olanlar)
                    var gelecekBorclar = sorted.Where(h => h.Borc > 0 && h.Vade.HasValue && h.Vade.Value.Date >= DateTime.Today).Sum(h => h.Borc);
                    // Net bakiye gelecek borçları aşıyorsa, aşan kısım vadesi geçmiş alacağımızdır
                    GecikenBorcToplam = Math.Max(0, Bakiye - gelecekBorclar);
                }
                else if (Bakiye < 0)
                {
                    // Biz cariye borçluyuz (Ödememiz gereken tutar)
                    var mutlakBakiye = Math.Abs(Bakiye);
                    // Gelecek ödemelerimizi hesapla (Vadesi bugün veya sonrası olan alacak hareketleri - faturalar vb.)
                    var gelecekAlacaklar = sorted.Where(h => h.Alacak > 0 && h.Vade.HasValue && h.Vade.Value.Date >= DateTime.Today).Sum(h => h.Alacak);
                    // Net borcumuz gelecek vadeli alımları aşıyorsa, aşan kısım vadesi geçmiş borcumuzdur
                    GecikenBorcToplam = Math.Max(0, mutlakBakiye - gelecekAlacaklar);
                }
                else
                {
                    GecikenBorcToplam = 0;
                }
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public virtual void GoBack() { }
}

public class CariFaturaDetayItem
{
    public int Id { get; set; }
    public string? FaturaNo { get; set; }
    public DateTime Tarih { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal GenelToplam { get; set; }
    public decimal Odenen { get; set; }
    public decimal Kalan { get; set; }
    public string OdemeDurumu { get; set; } = "Ödenmedi";
    public string KalanGunText { get; set; } = "";
    public bool IsOverdue { get; set; }
    public string StatusColor { get; set; } = "Gray";
    public bool IsIncoming { get; set; }
}

public class CariEvrakItem
{
    public int Id { get; set; }
    public string Type { get; set; } = "Çek"; // Çek, Senet
    public string? No { get; set; }
    public DateTime VadeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public string Tur { get; set; } = "Alınan"; // Alınan, Verilen
    public string? Durum { get; set; }
    public string KalanGunText { get; set; } = "";
    public bool IsOverdue { get; set; }
    public string StatusColor { get; set; } = "Gray";
}
