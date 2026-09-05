using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class MaliyetHesaplamaViewModel : ErmayMuhasebe.Shared.ViewModels.ViewModelBase
{
    private double Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        value = value.Replace(",", ".");
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;
        return 0;
    }

    // Section 1: Toplam KG Hesaplama
    [ObservableProperty] private string _gr = "0";
    [ObservableProperty] private string _en = "160";
    [ObservableProperty] private string _sarim = "0";
    [ObservableProperty] private string _toplamKgSonuc = "0,00 KG";

    [RelayCommand]
    private void HesaplaKg()
    {
        double g = Parse(Gr);
        double e = Parse(En);
        double s = Parse(Sarim);

        double sonuc = (g * e * s) / 100000.0;
        ToplamKgSonuc = $"{sonuc:N2} KG";
        
        // Transfer to Section 2
        ToplamKg = sonuc.ToString("N2", CultureInfo.CurrentCulture);
    }

    partial void OnSarimChanged(string value)
    {
        // Transfer Sarim to Metrekare Section (Top Boy)
        TopBoy = value;
    }

    // Section 2: Top Fiyatı Hesaplama
    [ObservableProperty] private string _toplamKg = "0";
    [ObservableProperty] private string _kgFiyati = "0";
    [ObservableProperty] private string _topFiyatiSonuc = "0,00 $";

    [RelayCommand]
    private void HesaplaTopFiyati()
    {
        double kg = Parse(ToplamKg);
        double f = Parse(KgFiyati);

        double sonuc = kg * f;
        TopFiyatiSonuc = $"{sonuc:N2} $";
        
        // Transfer to Section 4
        TopFiyatiInput = sonuc.ToString("N2", CultureInfo.CurrentCulture);
    }

    // Section 3: Metrekare Hesaplama
    [ObservableProperty] private string _topBoy = "0";
    [ObservableProperty] private string _metrekareSonuc = "0,00 m²";

    [RelayCommand]
    private void HesaplaMetrekare()
    {
        double boy = Parse(TopBoy);
        // En is fixed at 160cm = 1.6m as per UI title
        double sonuc = boy * 1.6;
        MetrekareSonuc = $"{sonuc:N2} m²";
        
        // Transfer to Section 4
        ToplamMetrekare = sonuc.ToString("N2", CultureInfo.CurrentCulture);
    }

    // Section 4: Birim Metrekare Maliyeti Hesaplama
    [ObservableProperty] private string _toplamMetrekare = "0";
    [ObservableProperty] private string _topFiyatiInput = "0";
    [ObservableProperty] private string _birimMaliyetSonuc = "0,00 $";

    [RelayCommand]
    private void HesaplaBirimMaliyet()
    {
        double m2 = Parse(ToplamMetrekare);
        double f = Parse(TopFiyatiInput);

        if (m2 == 0) 
        {
            BirimMaliyetSonuc = "0,00 $";
            return;
        }

        double sonuc = f / m2;
        BirimMaliyetSonuc = $"{sonuc:N2} $";
    }
}
