using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class BarkodTasarimViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<StokKart> _stoklar = new();
    [ObservableProperty] private StokKart? _seciliStok;
    
    [ObservableProperty] private double _etiketGenislik = 40; // mm
    [ObservableProperty] private double _etiketYukseklik = 20; // mm
    
    [ObservableProperty] private bool _showFiyat = true;
    [ObservableProperty] private bool _showStokAdi = true;
    [ObservableProperty] private bool _showBarkodNo = true;

    public BarkodTasarimViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var list = await _uow.Stoklar.GetAllAsync();
        Stoklar = new ObservableCollection<StokKart>(list);
    }

    [RelayCommand]
    public void Yazdir()
    {
        // Yazdırma simülasyonu veya PDF üretimi tetiklenebilir
        System.Diagnostics.Debug.WriteLine("Barkod Yazdırılıyor...");
    }
}
