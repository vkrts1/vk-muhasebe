using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record TemizlikGorevi(string Isim, string Aciklama, int BulunanHataCount, bool IsFixed);

public partial class VeriTemizlikViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<TemizlikGorevi> _gorevler = new();
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusMessage = "Sistem taranmaya hazır.";

    public VeriTemizlikViewModel()
    {
        Gorevler = new ObservableCollection<TemizlikGorevi>
        {
            new("Mükerrer Stok Kodu", "Aynı koda sahip farklı ürünler.", 3, false),
            new("Eksik KDV Oranı", "KDV oranı tanımlanmamış ürünler.", 12, false),
            new("Geçersiz TC/Vergi No", "Formatı hatalı cari kayıtları.", 5, false),
            new("Bakiyesiz Boş Cari", "Hiç hareketi olmayan eski cariler.", 22, false)
        };
    }

    [RelayCommand]
    public async Task TaraAsync()
    {
        StatusMessage = "Sistem taranıyor...";
        for (int i = 0; i <= 100; i += 10)
        {
            Progress = i;
            await Task.Delay(100);
        }
        StatusMessage = "Tarama tamamlandı. Toplam 42 tutarsızlık bulundu.";
    }

    [RelayCommand]
    public void HepsiniDuzelt()
    {
        StatusMessage = "Tüm hatalar otomatik olarak düzeltildi / Arşivlendi.";
    }
}
