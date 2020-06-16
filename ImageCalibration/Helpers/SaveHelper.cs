using System.Drawing;
using System.Drawing.Imaging;
using Encoder = System.Drawing.Imaging.Encoder;
using System.Linq;
using ImageCalibration.Enums;

namespace ImageCalibration.Helpers
{
    public static class SaveHelper
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

        public static void SaveTiff(string path, Bitmap image, SaveFormatEnum saveFormat = SaveFormatEnum.TIFF)
        {
            long compression;

            switch (saveFormat)
            {
                case SaveFormatEnum.TIFF:
                    compression = (long)EncoderValue.CompressionNone;
                    break;
                case SaveFormatEnum.TIFFLZW:
                    compression = (long)EncoderValue.CompressionLZW;
                    break;
                default:
                    compression = (long)EncoderValue.CompressionNone;
                    break;
            }

            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            using (EncoderParameter encoderParameter = new EncoderParameter(Encoder.Compression, compression))
            {
                ImageCodecInfo codecInfo = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Tiff.Guid);
                encoderParameters.Param[0] = encoderParameter;
                image.Save(path, codecInfo, encoderParameters);
            }
        }
    }
}
