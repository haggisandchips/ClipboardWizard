using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ClipboardWizard.Service
{
    /// <summary>
    /// The one place that knows snippet images are stored as PNG bytes, so the encode/decode
    /// choice only has to be made once.
    /// </summary>
    public static class ImageCodec
    {
        public static byte[] EncodePng(Image image)
        {
            using MemoryStream stream = new();
            image.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        public static BitmapImage DecodeToBitmapImage(byte[] pngData)
        {
            if (pngData == null || pngData.Length == 0)
            {
                return null;
            }

            using MemoryStream stream = new(pngData);
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }
}
