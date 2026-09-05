using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class BarkodTasarimViewModel : ViewModelBase
{
    protected readonly IUnitOfWork _uow;
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
            var list = await _uow.Stoklar.GetAllAsync();
            Stoklar = new ObservableCollection<StokKart>(list.OrderBy(x => x.StokAdi));
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
    public void Yazdir()
    {
        // Platform specific print logic will be handled in the view or shared service
        SuccessMessage = "Yazdırma işlemi başlatıldı.";
    }
}
