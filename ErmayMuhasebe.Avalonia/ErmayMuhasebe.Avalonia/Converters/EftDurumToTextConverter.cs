using Avalonia.Data.Converters;
using System;
using System.Globalization;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class EftDurumToTextConverter : IValueConverter
    {
        public static readonly EftDurumToTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is EftIslem islem)
            {
                if (islem.YonlendirilenCariId.HasValue || !string.IsNullOrEmpty(islem.YonlendirilenCariUnvan))
                {
                    return "Tedarikçiye Yönlendirildi";
                }

                if (islem.Durum == "İptal" || islem.Durum == "Iptal")
                {
                    return "İptal";
                }

                string tur = islem.IslemTuru ?? "Tahsilat";
                return $"{tur} Havale / EFT";
            }

            if (value is string durum)
            {
                return durum;
            }

            return "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
