using ClipboardWizard.Service;
using System.Drawing;

namespace ClipboardWizard.Tests.Service
{
    public class ImageCodecTests
    {
        [Fact]
        public void EncodePng_ThenDecodeToBitmapImage_RoundTripsDimensions()
        {
            using Bitmap bitmap = new(3, 2);

            byte[] encoded = ImageCodec.EncodePng(bitmap);
            System.Windows.Media.Imaging.BitmapImage decoded = ImageCodec.DecodeToBitmapImage(encoded);

            Assert.NotEmpty(encoded);
            Assert.Equal(3, decoded.PixelWidth);
            Assert.Equal(2, decoded.PixelHeight);
        }

        [Fact]
        public void DecodeToBitmapImage_ReturnsNull_ForEmptyData()
        {
            Assert.Null(ImageCodec.DecodeToBitmapImage([]));
            Assert.Null(ImageCodec.DecodeToBitmapImage(null));
        }
    }
}
