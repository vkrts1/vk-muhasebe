using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class BakiyeToStatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal bakiye)
        {
            // Negative (Alacaklı/Payable) -> Red
            if (bakiye < 0) return new SolidColorBrush(Color.Parse("#e74c3c"));
            // Positive (Borçlu/Receivable) -> Green/Blue (Matching design)
            if (bakiye > 0) return new SolidColorBrush(Color.Parse("#2ecc71"));
        }
        return new SolidColorBrush(Color.Parse("#34495e")); // Neutral
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
