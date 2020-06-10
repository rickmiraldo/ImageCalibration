using ImageCalibration.Enums;
using ImageCalibration.Helpers;
using ImageCalibration.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Encoder = System.Drawing.Imaging.Encoder;
using Size = System.Drawing.Size;

namespace ImageCalibration.Calibrations
{
    public abstract class Calibration : ICalibration
    {
        public string Name { get; set; }

        public CalibrationTypeEnum CalibrationType { get; set; }

        public void StartProcessing(string inputFile, string outputFolderPath, ProcessingConfiguration processingConfiguration)
        {
            string filename = Path.GetFileName(inputFile);
            string outputFilePath = outputFolderPath + "\\" + filename;

            Bitmap inputBitmap = new Bitmap(inputFile);

            Bitmap processedBitmap = processImage(inputBitmap, processingConfiguration);
            saveImage(outputFilePath, processedBitmap, processingConfiguration.SaveFormat);

            inputBitmap.Dispose();
            processedBitmap.Dispose();
        }

        private void saveImage(string outputFilePath, Bitmap image, SaveFormatEnum saveFormat)
        {
            switch (saveFormat)
            {
                case SaveFormatEnum.TIFF:
                    outputFilePath = Path.ChangeExtension(outputFilePath, "tif");
                    SaveHelper.SaveTiff(outputFilePath, image);
                    break;
                case SaveFormatEnum.JPG90:
                    outputFilePath = Path.ChangeExtension(outputFilePath, "jpg");
                    SaveHelper.SaveJpeg(outputFilePath, image, 90L);
                    break;
                case SaveFormatEnum.JPG100:
                    outputFilePath = Path.ChangeExtension(outputFilePath, "jpg");
                    SaveHelper.SaveJpeg(outputFilePath, image, 100L);
                    break;
                default:
                    break;
            }
        }

        private unsafe Bitmap processImage(Bitmap bitmap, ProcessingConfiguration configuration)
        {
            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, bitmap.PixelFormat);

            BitmapData originalBitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            BitmapData newBitmapData = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.ReadWrite, newBitmap.PixelFormat);

            byte bitsPerPixelOriginal = (byte)Bitmap.GetPixelFormatSize(bitmap.PixelFormat);
            byte bitsPerPixelNew = (byte)Bitmap.GetPixelFormatSize(newBitmap.PixelFormat);

            // Ponteiro para o início do primeiro pixel da imagem
            byte* ptrFirstPixelOriginal = (byte*)originalBitmapData.Scan0.ToPointer();
            byte* ptrFirstPixelNew = (byte*)newBitmapData.Scan0.ToPointer();

            Parallel.For(0, newBitmapData.Height, y =>
            {
                for (int x = 0; x < newBitmapData.Width; x++)
                {
                    byte* dataNew = ptrFirstPixelNew + y * newBitmapData.Stride + x * bitsPerPixelNew / 8;
                    // Variável "data" é um ponteiro para o primeiro byte dos dados
                    //data[0] = blue;
                    //data[1] = green;
                    //data[2] = red;

                    double columnCorrected, rowCorrected;
                    CalculateCorrectedCoordinates(x, y, newBitmapData.Width, newBitmapData.Height, out columnCorrected, out rowCorrected);

                    byte* dataOriginal = ptrFirstPixelOriginal + (int)rowCorrected * originalBitmapData.Stride + (int)columnCorrected * bitsPerPixelOriginal / 8;

                    dataNew[0] = dataOriginal[0];
                    dataNew[1] = dataOriginal[1];
                    dataNew[2] = dataOriginal[2];


                    //dataNew[0] = dataOriginal[1];
                    //dataNew[1] = dataOriginal[2];
                    //dataNew[2] = dataOriginal[0];
                }
            });

            bitmap.UnlockBits(originalBitmapData);
            newBitmap.UnlockBits(newBitmapData);

            return newBitmap;
        }

        private Bitmap resizeImage(Bitmap bitmap, float scaleFactor)
        {
            int newWidth = (int)(bitmap.Width * scaleFactor);
            int newHeight = (int)(bitmap.Height * scaleFactor);

            Rectangle rectangle = new Rectangle(0, 0, newWidth, newHeight);
            Bitmap scaledBitmap = new Bitmap(newWidth, newHeight, bitmap.PixelFormat);

            scaledBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (var graphics = Graphics.FromImage(scaledBitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(bitmap, rectangle, 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return scaledBitmap;
        }

        public abstract void CalculateCorrectedCoordinates(int xFinal, int yFinal, int widthFinal, int heightFinal, out double xMeasured, out double yMeasured);
    }
}
