using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public static readonly StatusToColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string durum)
            {
                return durum switch
                {
                    "Onaylandı" => Brushes.LimeGreen,
                    "Siparişleşti" or "Faturalandı" or "Faturalandırıldı" => Brushes.SkyBlue,
                    "Bekliyor" => Brushes.Orange,
                    "İptal" => Brushes.Crimson,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
