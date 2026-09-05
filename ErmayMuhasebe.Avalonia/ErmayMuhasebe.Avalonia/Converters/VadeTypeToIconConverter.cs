using Avalonia.Data.Converters;
using FluentIcons.Common;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters;

public class VadeTypeToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string type = value?.ToString() ?? "";
        return type switch
        {
            "Fatura" => Symbol.Receipt,
            "Cek" => Symbol.Money,
            "Senet" => Symbol.Document,
            _ => Symbol.Question
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
