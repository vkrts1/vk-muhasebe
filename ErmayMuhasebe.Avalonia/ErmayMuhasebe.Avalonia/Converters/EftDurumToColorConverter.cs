using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class EftDurumToColorConverter : IValueConverter
    {
        public static readonly EftDurumToColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is EftIslem islem)
            {
                if (islem.YonlendirilenCariId.HasValue || !string.IsNullOrEmpty(islem.YonlendirilenCariUnvan))
                {
                    return SolidColorBrush.Parse("#F59E0B"); // Orange
                }

                if (islem.Durum == "İptal" || islem.Durum == "Iptal")
                {
                    return SolidColorBrush.Parse("#6B7280"); // Gray
                }

                string tur = islem.IslemTuru ?? "Tahsilat";
                if (tur == "Ödeme" || tur == "Borç Dekontu")
                {
                    return SolidColorBrush.Parse("#2F6FED"); // Blue (or Red, let's use Blue to fit the theme)
                }

                return SolidColorBrush.Parse("#10B981"); // Green for Tahsilat / Receipt
            }

            return SolidColorBrush.Parse("#4B5563");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
