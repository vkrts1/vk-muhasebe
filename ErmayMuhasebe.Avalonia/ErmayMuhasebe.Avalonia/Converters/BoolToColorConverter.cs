using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class BoolToColorConverter : IValueConverter
{
    public static readonly BoolToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        string param = parameter as string ?? "Foreground";

        if (param.Contains(":"))
        {
            var parts = param.Split(':');
            var trueColor = parts[0];
            var falseColor = parts[1];
            return isActive ? new SolidColorBrush(Color.Parse(trueColor)) : new SolidColorBrush(Color.Parse(falseColor));
        }

        if (param == "Background")
        {
            return isActive ? new SolidColorBrush(Color.Parse("#30FFFFFF")) : Brushes.Transparent;
        }
        else // Foreground
        {
            return isActive ? Brushes.White : new SolidColorBrush(Color.Parse("#80FFFFFF"));
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
