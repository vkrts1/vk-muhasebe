using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public record DurakItem(string Isim, string Adres, string Saat, bool ZiyaretEdildi);

public partial class RotaPlanlamaViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<DurakItem> _duraklar = new();
    [ObservableProperty] private string _ekipAdi = "Batı Dağıtım Ekibi";
    
    // Inputs
    [ObservableProperty] private string _yeniDurakIsim = "";
    [ObservableProperty] private string _yeniDurakAdres = "";
    [ObservableProperty] private string _yeniDurakSaat = "09:00";

    private string _filePath;

    public RotaPlanlamaViewModel()
    {
        _filePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "ErmayRoutes.json");
        LoadData();
    }

    private void LoadData()
    {
        if (System.IO.File.Exists(_filePath))
        {
            try
            {
                var json = System.IO.File.ReadAllText(_filePath);
                var items = System.Text.Json.JsonSerializer.Deserialize<List<DurakItem>>(json);
                if (items != null)
                {
                    Duraklar = new ObservableCollection<DurakItem>(items);
                    return;
                }
            }
            catch { }
        }

        // Default Data if no file
        Duraklar = new ObservableCollection<DurakItem>
        {
            new("Merkez Depo", "İkitelli Org. San.", "09:00", true),
            new("ABC Market", "Başakşehir 4. Etap", "10:30", false)
        };
    }

    private void SaveData()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Duraklar);
            System.IO.File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    [RelayCommand]
    public void Ekle()
    {
        if (string.IsNullOrWhiteSpace(YeniDurakIsim)) return;

        Duraklar.Add(new DurakItem(YeniDurakIsim, YeniDurakAdres, YeniDurakSaat, false));
        YeniDurakIsim = "";
        YeniDurakAdres = "";
        SaveData();
    }

    [RelayCommand]
    public void Sil(DurakItem item)
    {
        Duraklar.Remove(item);
        SaveData();
    }
}
