using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class UrunBirlestirmeViewModel : ViewModelBase
{
    private readonly IUnitOfWork _uow;
    [ObservableProperty] private ObservableCollection<StokKart> _stoklar = new();
    [ObservableProperty] private StokKart? _kaynakStok; // Silinecek olan
    [ObservableProperty] private StokKart? _hedefStok; // Kalacak olan
    
    [ObservableProperty] private string _statusMessage = "Birleştirilecek ürünleri seçin.";

    public UrunBirlestirmeViewModel(IUnitOfWork uow)
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
            var list = await _uow.Stoklar.GetAllAsync();
            Stoklar = new ObservableCollection<StokKart>(list);
            StatusMessage = $"Veriler yüklendi: {Stoklar.Count} adet ürün bulundu. Birleştirmek istediğiniz ürünleri seçin.";
        }
        catch (System.Exception ex)
        {
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
        if (KaynakStok == null || HedefStok == null || KaynakStok.Id == HedefStok.Id)
        {
            StatusMessage = "Hata: İki farklı ürün seçilmelidir.";
            return;
        }

        try
        {
            StatusMessage = "Ürün hareketleri ve detayları aktarılıyor...";
            await _uow.Stoklar.MergeStokAsync(KaynakStok.Id, HedefStok.Id);
            
            StatusMessage = $"'{KaynakStok.StokAdi}' başarıyla '{HedefStok.StokAdi}' ile birleştirildi. Kaynak ürün silindi.";
            KaynakStok = null;
            HedefStok = null;
            await LoadDataAsync();
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Hata: {ex.Message}";
        }
    }
}
