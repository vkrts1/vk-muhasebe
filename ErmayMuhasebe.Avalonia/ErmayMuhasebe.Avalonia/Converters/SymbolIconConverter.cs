using Avalonia.Data.Converters;
using FluentIcons.Common;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class SymbolIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string iconName)
        {
            if (Enum.TryParse<Symbol>(iconName, true, out var symbol))
            {
                return symbol;
            }
        }
        return Symbol.Question;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
