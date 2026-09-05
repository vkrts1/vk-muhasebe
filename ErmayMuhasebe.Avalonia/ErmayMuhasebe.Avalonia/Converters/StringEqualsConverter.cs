using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class StringEqualsConverter : IValueConverter
    {
        public bool Inverted { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Inverted ? true : false;
            
            bool result = value.ToString() == parameter.ToString();
            return Inverted ? !result : result;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked && parameter != null)
            {
                return parameter.ToString();
            }
            return global::Avalonia.Data.BindingOperations.DoNothing;
        }
    }
}
