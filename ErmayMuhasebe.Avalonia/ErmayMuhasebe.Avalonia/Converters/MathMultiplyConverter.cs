using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class MathMultiplyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double val && parameter is string paramStr && double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var multiplier))
        {
            return val * multiplier;
        }
        if (value is decimal dval && parameter is string dparamStr && double.TryParse(dparamStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dmultiplier))
        {
            return (double)dval * dmultiplier;
        }
        return value ?? 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
