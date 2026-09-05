using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class CekDurumToColorConverter : IValueConverter
    {
        public static readonly CekDurumToColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string durum)
            {
                return durum switch
                {
                    "Tahsil" or "Tahsil Edildi" or "Tamamlandı" => SolidColorBrush.Parse("#10B981"),
                    "Karsiliksiz" or "Karşılıksız" or "Karşılıksız Çıktı" => SolidColorBrush.Parse("#EF4444"),
                    "Portföyde" or "Portfoyde" or "Bankaya Geçti" => SolidColorBrush.Parse("#2F6FED"),
                    "Tedarikçiye Verildi" or "Ciro Edildi" or "Tedarikçiye Yönlendirildi" => SolidColorBrush.Parse("#F59E0B"),
                    "İptal" or "Iptal" => SolidColorBrush.Parse("#6B7280"),
                    _ => SolidColorBrush.Parse("#4B5563")
                };
            }
            return SolidColorBrush.Parse("#4B5563");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
