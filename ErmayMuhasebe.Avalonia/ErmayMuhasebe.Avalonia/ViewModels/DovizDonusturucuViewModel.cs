using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using ErmayMuhasebe.Repositories;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record DovizVarlik(string DovizKodu, decimal Miktar, decimal Kur, decimal TryKarsiligi);

public partial class DovizDonusturucuViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<DovizVarlik> _varliklar = new();
    [ObservableProperty] private decimal _toplamTry;
    [ObservableProperty] private decimal _hedefDovizKuru = 1;
    [ObservableProperty] private string _hedefDovizKodu = "TRY";
    
    [ObservableProperty] private decimal _donusturulmusTutar;

    public DovizDonusturucuViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var cariler = await _uow.Cariler.GetAllAsync();
        // Basit simülasyon: Carilerin borç/alacak farklarını dövizli varsayalım (veya kasa banka)
        // Gerçekte Kasa/Banka bakiyeleri döviz bazlı çekilir.
        
        var list = new List<DovizVarlik>
        {
            new("USD", 1250.50m, 35.20m, 1250.50m * 35.20m),
            new("EUR", 800.00m, 38.15m, 800.00m * 38.15m),
            new("TRY", 45000.00m, 1.00m, 45000.00m)
        };

        Varliklar = new ObservableCollection<DovizVarlik>(list);
        ToplamTry = list.Sum(x => x.TryKarsiligi);
        HesaplaDonusum();
    }

    partial void OnHedefDovizKuruChanged(decimal value) => HesaplaDonusum();

    private void HesaplaDonusum()
    {
        if (HedefDovizKuru > 0)
            DonusturulmusTutar = ToplamTry / HedefDovizKuru;
    }
}
