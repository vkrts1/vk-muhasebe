using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class FaturaTurToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string tur)
        {
            if (tur.Equals("Satış", StringComparison.OrdinalIgnoreCase) || tur.Equals("Satis", StringComparison.OrdinalIgnoreCase) || tur.Equals("In", StringComparison.OrdinalIgnoreCase)) 
                return SolidColorBrush.Parse("#3b82f6"); // Blue
            
            if (tur.Equals("Alış", StringComparison.OrdinalIgnoreCase) || tur.Equals("Alis", StringComparison.OrdinalIgnoreCase) || tur.Equals("Out", StringComparison.OrdinalIgnoreCase)) 
                return SolidColorBrush.Parse("#f97316"); // Orange
        }
        return SolidColorBrush.Parse("#6b7280"); // Grey
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
