using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class ObjectIsNullConverter : IValueConverter
    {
        public bool Inverted { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            return Inverted ? !isNull : isNull;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
