using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class CekDurumToTextConverter : IValueConverter
    {
        public static readonly CekDurumToTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string durum)
            {
                return durum switch
                {
                    "Tahsil" => "Tahsil Edildi",
                    "Karsiliksiz" => "Karşılıksız",
                    "Portfoyde" => "Portföyde",
                    _ => durum
                };
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
