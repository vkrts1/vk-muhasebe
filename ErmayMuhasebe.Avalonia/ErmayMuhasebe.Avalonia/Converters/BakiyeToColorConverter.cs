using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class BakiyeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isPositive = false;
        bool isNegative = false;

        if (value is decimal d) { isPositive = d > 0; isNegative = d < 0; }
        else if (value is bool b) { isPositive = b; isNegative = !b; }
        
        var color = isPositive ? Brushes.LimeGreen : (isNegative ? Brushes.Tomato : Brushes.White);
        
        if (parameter?.ToString() == "bg")
        {
            var solid = color as ISolidColorBrush;
            return new SolidColorBrush(solid!.Color, 0.15);
        }
        
        return color;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
