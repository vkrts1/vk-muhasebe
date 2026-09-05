using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class BakiyeToStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal bakiye)
        {
            if (bakiye < 0) return "Cari Alacaklı";
            if (bakiye > 0) return "Cari Borçlu";
            return "Bakiyesi Yok";
        }
        return "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
