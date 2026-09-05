using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class VadeIncomingToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isIncoming = value is bool b && b;
        string mode = parameter?.ToString() ?? "fg";

        if (mode == "bg")
        {
            return isIncoming 
                ? new SolidColorBrush(Color.Parse("#1F00FFA3")) // Alpha green
                : new SolidColorBrush(Color.Parse("#1FFF4D4D")); // Alpha red
        }

        // Default: fg
        return isIncoming 
            ? new SolidColorBrush(Color.Parse("#00FFA3")) // Neon Emerald
            : new SolidColorBrush(Color.Parse("#FF4D4D")); // Hot Red
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
