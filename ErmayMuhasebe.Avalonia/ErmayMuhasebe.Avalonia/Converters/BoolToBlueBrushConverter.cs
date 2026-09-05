using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class BoolToBlueBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isMatched = false;
            if (parameter != null && value != null)
            {
                isMatched = value.ToString() == parameter.ToString();
            }
            else if (value is bool b)
            {
                isMatched = b;
            }

            if (isMatched)
            {
                return new SolidColorBrush(Color.Parse("#3b82f6"));
            }
            return new SolidColorBrush(Color.Parse("#11FFFFFF"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
