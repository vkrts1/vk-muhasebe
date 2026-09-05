using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class TabMatchConverter : IValueConverter
    {
        public static readonly TabMatchConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string activeTab && parameter is string targetTab)
            {
                return string.Equals(activeTab, targetTab, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActiveTabBrushConverter : IValueConverter
    {
        public static readonly ActiveTabBrushConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string activeTab && parameter is string targetTab)
            {
                bool isMatch = string.Equals(activeTab, targetTab, StringComparison.OrdinalIgnoreCase);
                if (isMatch)
                {
                    // Aktif sekme: Seçili koyu panel rengi
                    return new SolidColorBrush(Color.Parse("#1E293B"));
                }
            }
            // Pasif sekme: Standart koyu buton rengi (Görüşmeler dahil tüm butonlar aynı)
            return new SolidColorBrush(Color.Parse("#111827"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
