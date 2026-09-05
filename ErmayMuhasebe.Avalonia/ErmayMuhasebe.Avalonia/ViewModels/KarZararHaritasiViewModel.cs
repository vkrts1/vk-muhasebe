using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using System;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class KarZararHaritasiViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;

    [ObservableProperty] private ObservableCollection<CityProfitStat> _stats = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private CityProfitStat? _selectedCity;
    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _totalCost;
    [ObservableProperty] private decimal _totalProfit;
    [ObservableProperty] private decimal _maxProfit = 1;

    public KarZararHaritasiViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadStatsAsync();
    }

    public async Task LoadStatsAsync()
    {
        IsLoading = true;
        try
        {
            var data = await _uow.GetCityProfitStatsAsync();
            
            // If no data, add some mock data for the user to "try" as requested
            if (!data.Any())
            {
                data = new List<CityProfitStat>
                {
                    new CityProfitStat { Sehir = "ISTANBUL", SatisToplam = 150000, AlisToplam = 100000 },
                    new CityProfitStat { Sehir = "ANKARA", SatisToplam = 120000, AlisToplam = 80000 },
                    new CityProfitStat { Sehir = "IZMIR", SatisToplam = 90000, AlisToplam = 60000 },
                    new CityProfitStat { Sehir = "BURSA", SatisToplam = 70000, AlisToplam = 45000 },
                    new CityProfitStat { Sehir = "ADANA", SatisToplam = 50000, AlisToplam = 35000 }
                };
            }

            Stats = new ObservableCollection<CityProfitStat>(data.OrderByDescending(x => x.Profit));
            
            TotalSales = data.Sum(x => x.SatisToplam);
            TotalCost = data.Sum(x => x.AlisToplam);
            TotalProfit = data.Sum(x => x.Profit);
            
            if (data.Any()) MaxProfit = data.Max(x => x.Profit);
            if (MaxProfit <= 0) MaxProfit = 1;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Heatmap Hatası: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public int GetIntensity(decimal profit)
    {
        if (profit <= 0) return 0;
        return (int)((profit / MaxProfit) * 100);
    }
}
