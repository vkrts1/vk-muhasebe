using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class StokGrupItem : ObservableObject
{
    public StokKart Stok { get; }
    [ObservableProperty] private bool _isSelected;

    public StokGrupItem(StokKart stok) => Stok = stok;
}

public partial class StokGrupDuzenleViewModel : ViewModelBase
{
    protected readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<StokGrupItem> _items = new();
    [ObservableProperty] private ObservableCollection<string> _gruplar = new();
    [ObservableProperty] private string _hedefGrup = "";
    [ObservableProperty] private string _statusMessage = "Düzenlenecek ürünleri seçin.";

    public StokGrupDuzenleViewModel(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        _ = LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var stoklar = await _uow.Stoklar.GetAllAsync();
            Items = new ObservableCollection<StokGrupItem>(stoklar.OrderBy(s => s.StokAdi).Select(s => new StokGrupItem(s)));
            Gruplar = new ObservableCollection<string>(stoklar.Select(x => x.Grup ?? "Genel").Distinct().OrderBy(x => x));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Veri yükleme hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
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

        IsLoading = true;
        try
        {
            foreach (var item in selected)
            {
                item.Stok.Grup = HedefGrup;
                await _uow.Stoklar.SaveAsync(item.Stok);
            }

            StatusMessage = $"{selected.Count} adet ürünün grubu {HedefGrup} olarak güncellendi.";
            SuccessMessage = StatusMessage;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Güncelleme hatası: {ex.Message}";
            StatusMessage = "Hata oluştu.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
