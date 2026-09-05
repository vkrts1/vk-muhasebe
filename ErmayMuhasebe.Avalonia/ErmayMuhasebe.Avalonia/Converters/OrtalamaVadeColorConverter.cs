using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class OrtalamaVadeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int days)
        {
            if (days < 0) return SolidColorBrush.Parse("#F87171"); // Red (Vadesi geçmiş)
            if (days <= 7) return SolidColorBrush.Parse("#FBBF24"); // Yellow (Vadesi yaklaşmış)
            return SolidColorBrush.Parse("#34D399"); // Green (Vadesi var)
        }
        return Brushes.LightGray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
