using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record ShortcutItem(string Key, string Description, string Category);

public partial class KisayolTusuViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ShortcutItem> _shortcuts = new();

    public KisayolTusuViewModel()
    {
        InitializeShortcuts();
    }

    private void InitializeShortcuts()
    {
        Shortcuts = new ObservableCollection<ShortcutItem>
        {
            new("F1", "Genel Yardım ve Destek", "Genel"),
            new("F2", "Yeni Kayıt Ekleme", "İşlemler"),
            new("F3", "Arama / Filtreleme Odaklan", "Gezinme"),
            new("F5", "Listeyi Yenile", "İşlemler"),
            new("F10", "Kaydet ve Kapat", "İşlemler"),
            new("CTRL + S", "Düzenlemeleri Kaydet", "İşlemler"),
            new("CTRL + P", "Yazdır / PDF Oluştur", "Raporlama"),
            new("CTRL + F", "Hızlı Arama", "Gezinme"),
            new("ESC", "Pencereyi veya Menüyü Kapat", "Genel"),
            new("ALT + F4", "Uygulamadan Güvenli Çıkış", "Genel"),
            new("ENTER", "Seçili Kaydı Onayla / Düzenle", "İşlemler"),
            new("TAB", "Alanlar Arası Geçiş", "Gezinme")
        };
    }
}
