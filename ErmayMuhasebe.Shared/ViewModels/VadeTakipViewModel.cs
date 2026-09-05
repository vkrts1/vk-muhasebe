using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Shared.ViewModels;

public abstract partial class VadeTakipViewModel : ViewModelBase
{
    protected readonly IUnitOfWork _uow;

    [ObservableProperty] private ObservableCollection<VadeItem> _vadeler = new();
    [ObservableProperty] private decimal _toplamAlacak;
    [ObservableProperty] private decimal _toplamBorc;
    [ObservableProperty] private int _gecikmisAdet;

    public VadeTakipViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadVadelerAsync();
    }

    [RelayCommand]
    public async Task LoadVadelerAsync()
    {
        if (IsLoading) return;
        
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var items = new List<VadeItem>();

            // 1. Faturalar
            var faturalar = await _uow.Faturalar.GetAllAsync();
            foreach (var f in faturalar.Where(x => x.Kalan > 0))
            {
                items.Add(new VadeItem
                {
                    Type = "Fatura",
                    SourceId = f.Id,
                    CariId = f.CariId,
                    Tur = f.Tur == "Satis" || f.Tur == "Satış" ? "Alacak (Fatura)" : "Borç (Fatura)",
                    CariAdi = f.CariUnvan ?? "Bilinmeyen",
                    VadeTarihi = f.VadeTarihi,
                    Tutar = f.Kalan,
                    IsIncoming = f.Tur == "Satis" || f.Tur == "Satış"
                });
            }

            // 2. Çekler
            var cekler = await _uow.Cekler.GetAllAsync();
            foreach (var c in cekler.Where(x => x.Durum == "Portföyde" || x.Durum == "Portfoyde"))
            {
                items.Add(new VadeItem
                {
                    Type = "Cek",
                    SourceId = c.Id,
                    CariId = c.CariId ?? 0,
                    Tur = c.CekTuru == "Verilen" ? "Borç (Çek)" : "Alacak (Çek)",
                    CariAdi = c.CariUnvan ?? c.AsilBorclu ?? "Bilinmeyen",
                    VadeTarihi = c.VadeTarihi,
                    Tutar = c.Tutar,
                    IsIncoming = c.CekTuru != "Verilen"
                });
            }

            // 3. Senetler
            var senetler = await _uow.Senetler.GetAllAsync();
            foreach (var s in senetler.Where(x => x.Durum == "Portföyde" || x.Durum == "Portfoyde"))
            {
                items.Add(new VadeItem
                {
                    Type = "Senet",
                    SourceId = s.Id,
                    CariId = s.CariId ?? 0,
                    Tur = s.SenetTuru == "Verilen" ? "Borç (Senet)" : "Alacak (Senet)",
                    CariAdi = s.CariUnvan ?? s.AsilBorclu ?? "Bilinmeyen",
                    VadeTarihi = s.VadeTarihi,
                    Tutar = s.Tutar,
                    IsIncoming = s.SenetTuru != "Verilen"
                });
            }

            await InvokeOnUIThreadAsync(() => 
            {
                Vadeler = new ObservableCollection<VadeItem>(items.OrderBy(x => x.VadeTarihi));
                ToplamAlacak = Vadeler.Where(x => x.IsIncoming).Sum(x => x.Tutar);
                ToplamBorc = Vadeler.Where(x => !x.IsIncoming).Sum(x => x.Tutar);
                GecikmisAdet = Vadeler.Count(x => x.KalanGun < 0);
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Vade Takip yükleme hatası: {ex.Message}";
            Console.WriteLine($"VadeTakip Error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override void OnNavigatedTo()
    {
        _ = LoadVadelerAsync();
    }

    protected override Task InvokeOnUIThreadAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}

public class VadeItem
{
    public string Type { get; set; } = ""; // Fatura, Cek, Senet
    public int SourceId { get; set; }
    public int CariId { get; set; }
    public string Tur { get; set; } = "";
    public string CariAdi { get; set; } = "";
    public DateTime VadeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public bool IsIncoming { get; set; }
    public bool RenkliUyari => KalanGun < 0; 
    
    public int KalanGun => (VadeTarihi - DateTime.Today).Days;
    public string KalanGunText => KalanGun < 0 ? $"{Math.Abs(KalanGun)} GÜN GECİKTİ" : $"{KalanGun} Gün Kaldı";
}
