using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record TeklifItem(string No, string Cari, decimal Tutar, DateTime Tarih, string Durum);

public partial class TeklifSiparisViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<TeklifItem> _bekleyenTeklifler = new();
    [ObservableProperty] private string _statusMessage = "İşlem bekleyen teklifler listeleniyor.";

    public TeklifSiparisViewModel()
    {
        LoadData();
    }

    private void LoadData()
    {
        var list = new List<TeklifItem>
        {
            new("TKF-001", "Global Lojistik A.Ş.", 15200, DateTime.Now.AddDays(-2), "Beklemede"),
            new("TKF-002", "Tekno Market", 4500, DateTime.Now.AddDays(-1), "Onaylandı"),
            new("TKF-003", "Mega İnşaat", 89000, DateTime.Now, "Taslak")
        };
        BekleyenTeklifler = new ObservableCollection<TeklifItem>(list);
    }

    [RelayCommand]
    public void FaturayaDonustur(TeklifItem item)
    {
        StatusMessage = $"{item.No} nolu teklif faturaya dönüştürme kuyruğuna alındı.";
    }
}
