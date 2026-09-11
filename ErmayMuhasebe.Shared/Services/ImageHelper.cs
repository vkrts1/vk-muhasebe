using System;
using System.IO;
using SkiaSharp;

namespace ErmayMuhasebe.Services
{
    public static class ImageHelper
    {
        /// <summary>
        /// Logo görselini en fazla maxWidth x maxHeight (varsayılan 800x800) olacak şekilde orantılı küçültür
        /// ve PNG formatında sıkıştırarak byte dizisi döner. 10MB'lık bir dosya 50-150KB'a iner.
        /// </summary>
        public static byte[] OptimizeLogo(byte[] rawBytes, int maxWidth = 800, int maxHeight = 800)
        {
            if (rawBytes == null || rawBytes.Length == 0)
                return Array.Empty<byte>();

            try
            {
                using var ms = new MemoryStream(rawBytes);
                using var bitmap = SKBitmap.Decode(ms);
                if (bitmap == null) return rawBytes;

                int targetWidth = bitmap.Width;
                int targetHeight = bitmap.Height;

                if (bitmap.Width > maxWidth || bitmap.Height > maxHeight)
                {
                    float ratio = Math.Min((float)maxWidth / bitmap.Width, (float)maxHeight / bitmap.Height);
                    targetWidth = Math.Max(1, (int)(bitmap.Width * ratio));
                    targetHeight = Math.Max(1, (int)(bitmap.Height * ratio));
                }
                else if (rawBytes.Length < 256 * 1024)
                {
                    // Zaten makul boyutlarda ve 256KB altındaysa olduğu gibi kullan
                    return rawBytes;
                }

                var info = new SKImageInfo(targetWidth, targetHeight, bitmap.ColorType, bitmap.AlphaType);
                using var resizedBitmap = bitmap.Resize(info, SKFilterQuality.Medium);
                if (resizedBitmap == null) return rawBytes;

                using var image = SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 90);
                if (data != null && data.Size > 0)
                {
                    return data.ToArray();
                }

                return rawBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageHelper] OptimizeLogo hatası: {ex.Message}");
                return rawBytes;
            }
        }
    }
}
