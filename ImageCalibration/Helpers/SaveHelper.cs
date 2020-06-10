using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Encoder = System.Drawing.Imaging.Encoder;

namespace ImageCalibration.Helpers
{
    public class SaveHelper
    {
        public static void SaveJpeg(string path, Bitmap image, long quality = 90L)
        {
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(Encoder.Quality, quality))
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
                encoderParameters.Param[0] = encoderParameter;
                image.Save(path, codecInfo, encoderParameters);
            }
        }

        public static void SaveTiff(string path, Bitmap image)
        {
            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(Encoder.Compression, (long)EncoderValue.CompressionNone))
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Tiff.Guid);
                encoderParameters.Param[0] = encoderParameter;
                image.Save(path, codecInfo, encoderParameters);
            }
        }
    }
}
