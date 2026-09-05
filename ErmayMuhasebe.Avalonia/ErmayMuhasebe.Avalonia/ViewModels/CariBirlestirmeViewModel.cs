using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class CariBirlestirmeViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    
    [ObservableProperty] private ObservableCollection<CariKart> _cariler = new();
    [ObservableProperty] private CariKart? _kaynakCari; // Silinecek olan
    [ObservableProperty] private CariKart? _hedefCari; // Kalacak olan
    [ObservableProperty] private string _statusMessage = "Birleştirilecek cari kartları seçin.";

    public CariBirlestirmeViewModel(IUnitOfWork uow)
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
        try
        {
            StatusMessage = "Veriler yükleniyor...";
            IsLoading = true;
            var list = await _uow.Cariler.GetAllAsync();
            Cariler = new ObservableCollection<CariKart>(list.OrderBy(c => c.Unvan));
            StatusMessage = $"Veriler yüklendi: {Cariler.Count} adet cari bulundu. Birleştirmek istediğiniz carileri seçin.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Veri yükleme hatası: {ex.Message}";
            StatusMessage = $"Yükleme hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task BirlestirAsync()
    {
        if (KaynakCari == null || HedefCari == null || KaynakCari.Id == HedefCari.Id)
        {
            ErrorMessage = "Hata: İki farklı cari hesap seçilmelidir.";
            return;
        }

        try
        {
            StatusMessage = "Cari hareketler ve bakiyeler aktarılıyor...";
            IsLoading = true;
            
            await _uow.Cariler.MergeCariAsync(KaynakCari.Id, HedefCari.Id);
            
            SuccessMessage = $"'{KaynakCari.Unvan}' başarıyla '{HedefCari.Unvan}' ile birleştirildi.";
            KaynakCari = null;
            HedefCari = null;
            await LoadDataAsync();
            StatusMessage = "İşlem başarıyla tamamlandı.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Birleştirme hatası: {ex.Message}";
            StatusMessage = "Hata oluştu.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
