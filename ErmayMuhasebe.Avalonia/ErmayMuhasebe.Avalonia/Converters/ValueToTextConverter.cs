using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class ValueToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is string paramStr && paramStr.Contains("|"))
            {
                var parts = paramStr.Split('|');
                bool isTrue = false;
                
                if (value is bool b) isTrue = b;
                else if (value != null) isTrue = true;

                return isTrue ? parts[0] : parts[1];
            }
            return value?.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
