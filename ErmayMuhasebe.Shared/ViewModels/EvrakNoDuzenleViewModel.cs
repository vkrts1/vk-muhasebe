using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ErmayMuhasebe.Shared.ViewModels;

public partial class SeriNoItem : ObservableObject
{
    [ObservableProperty] private string _evrakTipi;
    [ObservableProperty] private string _seri;
    [ObservableProperty] private int _siradakiNo;

    public SeriNoItem(string tip, string seri, int no)
    {
        EvrakTipi = tip;
        Seri = seri;
        SiradakiNo = no;
    }
}

public abstract partial class EvrakNoDuzenleViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<SeriNoItem> _seriler = new();
    [ObservableProperty] private string _statusMessage = "Evrak serilerini buradan yönetebilirsiniz.";

    public EvrakNoDuzenleViewModel()
    {
        Seriler = new ObservableCollection<SeriNoItem>
        {
            new("Satış Faturası", "FAT", 1024),
            new("Tahsilat Makbuzu", "THS", 450),
            new("Ödeme Makbuzu", "ODM", 215),
            new("Teklif Formu", "TKF", 88)
        };
    }

    [RelayCommand]
    public virtual void Kaydet()
    {
        StatusMessage = "Evrak seri ve numara ayarları başarıyla güncellendi.";
    }
}
