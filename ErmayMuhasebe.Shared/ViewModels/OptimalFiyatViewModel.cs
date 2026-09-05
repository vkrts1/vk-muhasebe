using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace ErmayMuhasebe.Shared.ViewModels;

public abstract partial class OptimalFiyatViewModel : ViewModelBase
{
    private decimal _alisFiyati = 100;
    public decimal AlisFiyati
    {
        get => _alisFiyati;
        set
        {
            if (SetProperty(ref _alisFiyati, value))
                Hesapla();
        }
    }

    private decimal _hedefKarOrani = 25; // %
    public decimal HedefKarOrani
    {
        get => _hedefKarOrani;
        set
        {
            if (SetProperty(ref _hedefKarOrani, value))
                Hesapla();
        }
    }

    private decimal _kdvOrani = 20; // %
    public decimal KdvOrani
    {
        get => _kdvOrani;
        set
        {
            if (SetProperty(ref _kdvOrani, value))
                Hesapla();
        }
    }

    private decimal _ekMaliyet = 0;
    public decimal EkMaliyet
    {
        get => _ekMaliyet;
        set
        {
            if (SetProperty(ref _ekMaliyet, value))
                Hesapla();
        }
    }
    
    private decimal _onerilenSatisFiyati;
    public decimal OnerilenSatisFiyati
    {
        get => _onerilenSatisFiyati;
        set => SetProperty(ref _onerilenSatisFiyati, value);
    }

    private decimal _netKar;
    public decimal NetKar
    {
        get => _netKar;
        set => SetProperty(ref _netKar, value);
    }

    private decimal _kdvTutari;
    public decimal KdvTutari
    {
        get => _kdvTutari;
        set => SetProperty(ref _kdvTutari, value);
    }

    public OptimalFiyatViewModel()
    {
        Hesapla();
    }

    [RelayCommand]
    public virtual void Hesapla()
    {
        decimal toplamMaliyet = AlisFiyati + EkMaliyet;
        decimal karTutari = toplamMaliyet * (HedefKarOrani / 100);
        decimal satisKdvHaric = toplamMaliyet + karTutari;
        
        KdvTutari = satisKdvHaric * (KdvOrani / 100);
        OnerilenSatisFiyati = satisKdvHaric + KdvTutari;
        NetKar = karTutari;
    }
}
