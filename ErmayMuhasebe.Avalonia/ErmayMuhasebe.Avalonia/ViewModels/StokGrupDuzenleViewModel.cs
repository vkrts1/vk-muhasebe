using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class StokGrupItem : ObservableObject
{
    public StokKart Stok { get; }
    [ObservableProperty] private bool _isSelected;

    public StokGrupItem(StokKart stok) => Stok = stok;
}

public partial class StokGrupDuzenleViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<StokGrupItem> _items = new();
    [ObservableProperty] private ObservableCollection<string> _gruplar = new();
    [ObservableProperty] private string _hedefGrup = "";
    [ObservableProperty] private string _statusMessage = "Düzenlenecek ürünleri seçin.";

    public StokGrupDuzenleViewModel(IUnitOfWork uow)
    {
        _uow = uow;
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        var stoklar = await _uow.Stoklar.GetAllAsync();
        Items = new ObservableCollection<StokGrupItem>(stoklar.Select(s => new StokGrupItem(s)));
        var grupListesi = await _uow.Stoklar.GetGruplarAsync();
        Gruplar = new ObservableCollection<string>(grupListesi);
    }

    [RelayCommand]
    public async Task UygulaAsync()
    {
        var selected = Items.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0 || string.IsNullOrWhiteSpace(HedefGrup))
        {
            StatusMessage = "Hata: En az bir ürün ve bir hedef grup seçilmelidir.";
            return;
        }

        var trimmed = HedefGrup.Trim();
        await _uow.Stoklar.SaveGrupAsync(trimmed);

        foreach (var item in selected)
        {
            item.Stok.Grup = trimmed;
            await _uow.Stoklar.SaveAsync(item.Stok);
        }

        StatusMessage = $"{selected.Count} adet ürünün grubu {trimmed} olarak güncellendi.";
        await LoadDataAsync();
    }
}
