using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class GecikmeFaiziViewModel : ViewModelBase
{
    private readonly DovizService _dovizService;
    private decimal _tcmbPolicyRate = 50;

    [ObservableProperty] private decimal _anaPara = 50000;
    [ObservableProperty] private decimal _faizOrani = 48; // % Yıllık
    [ObservableProperty] private int _gecikmeGunu = 30;
    
    [ObservableProperty] private decimal _faizTutari;
    [ObservableProperty] private decimal _toplamTutar;

    [ObservableProperty] private int _seciliFaizIndex = 1; // Default to Ticari
    [ObservableProperty] private string _tcmbRateText = "TCMB Politika (Yükleniyor...)";

    public GecikmeFaiziViewModel(DovizService dovizService)
    {
        _dovizService = dovizService;
        _ = LoadTcmbRateAsync();
        Hesapla();
    }

    private async Task LoadTcmbRateAsync()
    {
        try
        {
            _tcmbPolicyRate = await _dovizService.GetTcmbPolicyRateAsync();
            TcmbRateText = $"TCMB Politika (Güncel: %{_tcmbPolicyRate:0})";
            if (SeciliFaizIndex == 2)
            {
                FaizOrani = _tcmbPolicyRate;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load TCMB rate in VM: {ex.Message}");
            TcmbRateText = "TCMB Politika (Güncel: %50)";
        }
    }

    partial void OnAnaParaChanged(decimal value) => Hesapla();
    partial void OnGecikmeGunuChanged(int value) => Hesapla();

    partial void OnSeciliFaizIndexChanged(int value)
    {
        if (value == 0) // Yasal
        {
            FaizOrani = 24;
        }
        else if (value == 1) // Ticari
        {
            FaizOrani = 48;
        }
        else if (value == 2) // TCMB Politika
        {
            FaizOrani = _tcmbPolicyRate;
        }
        // If 3 (Özel), do not change the rate value automatically
        Hesapla();
    }

    partial void OnFaizOraniChanged(decimal value)
    {
        if (value == 24)
        {
            if (SeciliFaizIndex != 0) SeciliFaizIndex = 0;
        }
        else if (value == 48)
        {
            if (SeciliFaizIndex != 1) SeciliFaizIndex = 1;
        }
        else if (value == _tcmbPolicyRate)
        {
            if (SeciliFaizIndex != 2) SeciliFaizIndex = 2;
        }
        else
        {
            if (SeciliFaizIndex != 3) SeciliFaizIndex = 3;
        }
        Hesapla();
    }

    [RelayCommand]
    public void Hesapla()
    {
        // Günlük faiz hesabı: (Ana Para * Faiz / 100) * Gecikme Günü / 365
        FaizTutari = AnaPara * (FaizOrani / 100) * GecikmeGunu / 365;
        ToplamTutar = AnaPara + FaizTutari;
    }
}
