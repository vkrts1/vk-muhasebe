using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class OverdueToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isOverdue = value is bool b && b;
        return isOverdue 
            ? new SolidColorBrush(Color.Parse("#FF4D4D")) 
            : new SolidColorBrush(Color.Parse("#94A3B8"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
