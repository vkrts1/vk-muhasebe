using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class OptimalFiyatViewModel : ViewModelBase
{
    [ObservableProperty] private decimal _alisFiyati = 100;
    [ObservableProperty] private decimal _hedefKarOrani = 25; // %
    [ObservableProperty] private decimal _kdvOrani = 20; // %
    [ObservableProperty] private decimal _ekMaliyet = 0;
    
    [ObservableProperty] private decimal _onerilenSatisFiyati;
    [ObservableProperty] private decimal _netKar;
    [ObservableProperty] private decimal _kdvTutari;

    public OptimalFiyatViewModel()
    {
        Hesapla();
    }

    partial void OnAlisFiyatiChanged(decimal value) => Hesapla();
    partial void OnHedefKarOraniChanged(decimal value) => Hesapla();
    partial void OnKdvOraniChanged(decimal value) => Hesapla();
    partial void OnEkMaliyetChanged(decimal value) => Hesapla();

    [RelayCommand]
    public void Hesapla()
    {
        decimal toplamMaliyet = AlisFiyati + EkMaliyet;
        decimal karTutari = toplamMaliyet * (HedefKarOrani / 100);
        decimal satisKdvHaric = toplamMaliyet + karTutari;
        
        KdvTutari = satisKdvHaric * (KdvOrani / 100);
        OnerilenSatisFiyati = satisKdvHaric + KdvTutari;
        NetKar = karTutari;
    }
}
