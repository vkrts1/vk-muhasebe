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

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class LimitItem : ObservableObject
{
    public CariKart Cari { get; }
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(DolulukOrani))]
    [NotifyPropertyChangedFor(nameof(Renk))]
    private decimal? _krediLimiti;
    [ObservableProperty] private decimal _riskLimiti;

    public decimal MevcutRisk => Cari.Borc - Cari.Alacak;
    public double DolulukOrani => (double)((KrediLimiti ?? 0) > 0 ? (MevcutRisk / (KrediLimiti ?? 1) * 100) : 0);
    public string Renk => DolulukOrani > 90 ? "#EF4444" : (DolulukOrani > 70 ? "#F59E0B" : "#10B981");

    public LimitItem(CariKart cari)
    {
        Cari = cari;
        KrediLimiti = cari.RiskLimiti > 0 ? cari.RiskLimiti : 0; 
        RiskLimiti = cari.RiskLimiti;
    }
}

public partial class MusteriLimitViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<LimitItem> _limitler = new();
    [ObservableProperty] private string _filterText = "";

    public MusteriLimitViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var cariler = await _uow.Cariler.GetAllAsync();
        var list = cariler.Select(c => new LimitItem(c))
                          .OrderByDescending(x => x.DolulukOrani)
                          .ToList();

        Limitler = new ObservableCollection<LimitItem>(list);
    }

    [RelayCommand]
    public async Task KaydetAsync(LimitItem item)
    {
        if (item == null || item.Cari == null) return;

        var cari = await _uow.Cariler.GetByIdAsync(item.Cari.Id);
        if (cari != null)
        {
            // Update Risk Limit
            // Note: In UI we are binding to 'KrediLimiti' property of LimitItem. 
            // In DB we are mapping this to 'RiskLimiti'.
            cari.RiskLimiti = item.KrediLimiti ?? 0; 
            
            await _uow.Cariler.SaveAsync(cari);
            
            // Local objeyi de güncelle ki UI başka yerlerde (cache vs) doğru görsün
            item.Cari.RiskLimiti = cari.RiskLimiti;
            item.RiskLimiti = cari.RiskLimiti;

            System.Diagnostics.Debug.WriteLine($"Risk Limiti Güncellendi: {cari.Unvan} -> {cari.RiskLimiti}");
        }
    }
}
