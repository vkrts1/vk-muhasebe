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

public record BorcHatirlatmaItem(string CariUnvan, decimal Tutar, DateTime Vade, int GecikmeGunu, string Durum);

public partial class BorcHatirlaticiViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<BorcHatirlatmaItem> _hatirlatmalar = new();
    [ObservableProperty] private int _toplamGecikenCount;

    public BorcHatirlaticiViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var cariler = await _uow.Cariler.GetAllAsync();
        var list = new List<BorcHatirlatmaItem>();
        
        foreach (var c in cariler.Where(x => x.Borc > x.Alacak))
        {
            // Simüle edilmiş vade: Bugünün 10 gün öncesi
            var vade = DateTime.Now.AddDays(-10);
            int gecikme = (DateTime.Now - vade).Days;
            
            list.Add(new BorcHatirlatmaItem(c.Unvan ?? "Adsız Cari", c.Borc - c.Alacak, vade, gecikme, "Ödeme Bekliyor"));
        }

        Hatirlatmalar = new ObservableCollection<BorcHatirlatmaItem>(list.OrderByDescending(x => x.GecikmeGunu));
        ToplamGecikenCount = list.Count;
    }

    [RelayCommand]
    public void SmsGonder(BorcHatirlatmaItem item)
    {
        // SMS Servis Entegrasyonu Simülasyonu
        System.Diagnostics.Debug.WriteLine($"SMS Gönderildi: {item.CariUnvan} - Tutar: {item.Tutar}");
    }

    [RelayCommand]
    public void EmailGonder(BorcHatirlatmaItem item)
    {
        // Email Servis Entegrasyonu Simülasyonu
        System.Diagnostics.Debug.WriteLine($"Email Gönderildi: {item.CariUnvan}");
    }
}
