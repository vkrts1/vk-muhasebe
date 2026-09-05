using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class CariTurToTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string tur)
        {
            if (tur == "Alici" || tur == "Alıcı" || tur == "Müşteri") return "MÜŞTERİ";
            if (tur == "Satici" || tur == "Satıcı" || tur == "Tedarikçi") return "TEDARİKÇİ";
        }
        return "CARİ";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class CariTurToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string tur)
        {
            if (tur == "Alici" || tur == "Alıcı" || tur == "Müşteri") return new SolidColorBrush(Color.Parse("#60A5FA")); // Blue
            if (tur == "Satici" || tur == "Satıcı" || tur == "Tedarikçi") return new SolidColorBrush(Color.Parse("#FFA500")); // Orange
        }
        return new SolidColorBrush(Color.Parse("#888888")); // Grey
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
public class IslemTuruToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string islem)
        {
            var upper = islem.ToUpper(new CultureInfo("tr-TR"));
            if (upper.Contains("SATIŞ") || upper.Contains("SATIS")) return new SolidColorBrush(Color.Parse("#2F6FED")); // Blue
            if (upper.Contains("ALIŞ") || upper.Contains("ALIS")) return new SolidColorBrush(Color.Parse("#F59E0B")); // Orange
            if (upper.Contains("TAHSİLAT") || upper.Contains("TAHSILAT")) return new SolidColorBrush(Color.Parse("#10B981")); // Green
            if (upper.Contains("ÖDEME") || upper.Contains("ODEME")) return new SolidColorBrush(Color.Parse("#EF4444")); // Red
            if (upper.Contains("ÇEK") || upper.Contains("CEK") || upper.Contains("SENET")) return new SolidColorBrush(Color.Parse("#8B5CF6")); // Purple
            if (upper.Contains("KREDİ") || upper.Contains("KREDI")) return new SolidColorBrush(Color.Parse("#1E3A8A")); // Navy Blue
            
            if (upper.Contains("ALACAK")) return new SolidColorBrush(Color.Parse("#10B981")); 
            if (upper.Contains("BORÇ") || upper.Contains("BORC")) return new SolidColorBrush(Color.Parse("#EF4444")); 
            if (upper.Contains("AÇILIŞ") || upper.Contains("ACILIS")) return new SolidColorBrush(Color.Parse("#6366f1")); 
        }
        return new SolidColorBrush(Color.Parse("#6B7280")); 
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
