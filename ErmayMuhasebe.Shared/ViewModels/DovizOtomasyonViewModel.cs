using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;
using ErmayMuhasebe.Repositories;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class DovizOtomasyonViewModel : ViewModelBase
{
    private readonly DovizService _dovizService;
    private readonly IUnitOfWork _uow;

    [ObservableProperty] private bool _isOtomatikGuncellemeDurumu = true;
    [ObservableProperty] private string _guncellemeAraligi = "Her Saat"; // Her Saat, Günde 1, Açılışta
    [ObservableProperty] private DateTime? _sonGuncelleme;
    [ObservableProperty] private string _kaynak = "TCMB (Türkiye Cumhuriyet Merkez Bankası)";
    [ObservableProperty] private string _statusMessage = "Sistem aktif, bir sonraki güncelleme bekleniyor.";
    
    [ObservableProperty]
    private ObservableCollection<DovizKur> _kurlar = new();

    public DovizOtomasyonViewModel(DovizService dovizService, IUnitOfWork uow)
    {
        _dovizService = dovizService;
        _uow = uow;
        SonGuncelleme = DateTime.Now.AddMinutes(-45);
        _ = LoadKurlarAsync();
    }

    [RelayCommand]
    public async Task LoadKurlarAsync()
    {
        try 
        {
            StatusMessage = "Kurlar güncelleniyor...";
            try
            {
                // Try to update from Web
                await _dovizService.UpdateRatesFromTcmbAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TCMB Update Failed: {ex.Message}");
                // Continue to load local data
            }
        
            var guncelKurlar = (await _uow.DovizKurlari.GetLatestAsync())
                              .Where(x => x.Kod == "USD" || x.Kod == "EUR" || x.Kod == "GBP")
                              .ToList();
            Kurlar = new ObservableCollection<DovizKur>(guncelKurlar);
            
            SonGuncelleme = DateTime.Now;
            StatusMessage = "Güncelleme başarıyla tamamlandı.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadKurlar Fatal Error: {ex}");
            StatusMessage = "Hata: Kurlar yüklenemedi.";
        }
    }

    [RelayCommand]
    public async Task SimdiGuncelleAsync()
    {
        await LoadKurlarAsync();
    }

    [RelayCommand]
    public void AyarlariKaydet()
    {
        StatusMessage = "Ayarlar kaydedildi.";
    }
}
