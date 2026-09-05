using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Globalization;
using System.IO;

namespace ErmayMuhasebe.Avalonia.Converters
{
    public class BitmapAssetValueConverter : IValueConverter
    {
        public static BitmapAssetValueConverter Instance = new BitmapAssetValueConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is byte[] bytes)
            {
                if (bytes.Length == 0) return null;
                try
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        return new Bitmap(ms);
                    }
                }
                catch { return null; }
            }

            if (value is string path && !string.IsNullOrWhiteSpace(path))
            {
                if (File.Exists(path))
                {
                    try
                    {
                        return new Bitmap(path);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
