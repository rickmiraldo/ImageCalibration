using ImageCalibration.Enums;
using ImageCalibration.Helpers;
using ImageCalibration.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Encoder = System.Drawing.Imaging.Encoder;

namespace ImageCalibration.Calibrations
{
    public abstract class Calibration : ICalibration
    {
        public string Name { get; set; }

        public CalibrationTypeEnum CalibrationType { get; set; }

        public async void StartProcessingAsync(string inputFile, string outputFolderPath, ProcessingConfiguration processingConfiguration)
        {
            await Task.Run(() =>
            {
                string filename = Path.GetFileName(inputFile);
                string outputFilePath = outputFolderPath + "\\" + filename;

                Bitmap inputBitmap = new Bitmap(inputFile);

                Bitmap processedBitmap = processUsingLockbitsAndUnsafeAndParallel(inputBitmap);
                saveImage(outputFilePath, processedBitmap);
            });
        }

        private void saveImage(string outputFilePath, Bitmap image, long quality = 90L)
        {
            string extension = Path.GetExtension(outputFilePath).ToLower();

            if (extension == ".jpg")
            {
                SaveHelper.SaveJpeg(outputFilePath, image, quality);
                return;
            }
            else if ((extension == ".tif") || (extension == ".tiff"))
            {
                image.Save(outputFilePath, ImageFormat.Tiff);
                return;
            }
        }

        private Bitmap processUsingLockbits(Bitmap inputBitmap)
        {
            BitmapData bitmapData = inputBitmap.LockBits(new Rectangle(0, 0, inputBitmap.Width, inputBitmap.Height), ImageLockMode.ReadWrite, inputBitmap.PixelFormat);

            int bytesPerPixel = Bitmap.GetPixelFormatSize(inputBitmap.PixelFormat) / 8;
            int byteCount = bitmapData.Stride * inputBitmap.Height; // Stride = Número de bytes necessários para armazenar uma linha da imagem
            byte[] pixels = new byte[byteCount]; // Variável pixels armazena a quantidade total de bytes da imagem
            IntPtr ptrFirstPixel = bitmapData.Scan0; // Ponteiro para o início do primeiro pixel da imagem

            // Copiar dados da memória para o array "pixels"
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
            int heightInPixels = bitmapData.Height;
            int widthInBytes = bitmapData.Width * bytesPerPixel;

            // Percorrer cada linha
            for (int y = 0; y < heightInPixels; y++)
            {
                int currentLine = y * bitmapData.Stride;

                // Percorrer cada pixel (sendo que cada pixel possui "bytesPerPixel" de largura)
                for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                {
                    // Guardar valores atuais da imagem
                    int oldBlue = pixels[currentLine + x];
                    int oldGreen = pixels[currentLine + x + 1];
                    int oldRed = pixels[currentLine + x + 2];

                    // Armazenar novos valores na imagem
                    pixels[currentLine + x] = (byte)oldGreen;
                    pixels[currentLine + x + 1] = (byte)oldRed;
                    pixels[currentLine + x + 2] = (byte)oldBlue;
                }
            }

            // Copiar dados modificados de volta para a memória
            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            inputBitmap.UnlockBits(bitmapData);

            return inputBitmap;
        }

        private Bitmap processUsingLockbitsAndUnsafe(Bitmap inputBitmap)
        {
            unsafe
            {
                BitmapData bitmapData = inputBitmap.LockBits(new Rectangle(0, 0, inputBitmap.Width, inputBitmap.Height), ImageLockMode.ReadWrite, inputBitmap.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(inputBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* prtFirstPixel = (byte*)bitmapData.Scan0; // Ponteiro para o início do primeiro pixel da imagem 

                // Percorrer cada linha
                for (int y = 0; y < heightInPixels; y++)
                {
                    byte* currentLine = prtFirstPixel + (y * bitmapData.Stride); // Stride = Número de bytes necessários para armazenar uma linha da imagem

                    // Percorrer cada pixel (sendo que cada pixel possui "bytesPerPixel" de largura)
                    for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                    {
                        // Guardar valores atuais da imagem
                        int oldBlue = currentLine[x];
                        int oldGreen = currentLine[x + 1];
                        int oldRed = currentLine[x + 2];

                        // Armazenar novos valores na imagem
                        currentLine[x] = (byte)oldGreen;
                        currentLine[x + 1] = (byte)oldRed;
                        currentLine[x + 2] = (byte)oldBlue;
                    }
                }

                // Copiar dados modificados de volta para a memória
                inputBitmap.UnlockBits(bitmapData);
            }

            return inputBitmap;
        }

        private Bitmap processUsingLockbitsAndUnsafeAndParallel(Bitmap inputBitmap)
        {
            unsafe
            {
                BitmapData bitmapData = inputBitmap.LockBits(new Rectangle(0, 0, inputBitmap.Width, inputBitmap.Height), ImageLockMode.ReadWrite, inputBitmap.PixelFormat);

                int bytesPerPixel = Bitmap.GetPixelFormatSize(inputBitmap.PixelFormat) / 8;
                int heightInPixels = bitmapData.Height;
                int widthInBytes = bitmapData.Width * bytesPerPixel;
                byte* prtFirstPixel = (byte*)bitmapData.Scan0; // Ponteiro para o início do primeiro pixel da imagem 

                // Percorrer cada linha
                Parallel.For(0, heightInPixels, y =>
                {
                    byte* currentLine = prtFirstPixel + (y * bitmapData.Stride); // Stride = Número de bytes necessários para armazenar uma linha da imagem

                    // Percorrer cada pixel (sendo que cada pixel possui "bytesPerPixel" de largura)
                    for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                    {
                        //CalculatePixel();
                        // Guardar valores atuais da imagem
                        int oldBlue = currentLine[x];
                        int oldGreen = currentLine[x + 1];
                        int oldRed = currentLine[x + 2];

                        // Armazenar novos valores na imagem
                        currentLine[x] = (byte)oldGreen;
                        currentLine[x + 1] = (byte)oldRed;
                        currentLine[x + 2] = (byte)oldBlue;
                    }
                });

                // Copiar dados modificados de volta para a memória
                inputBitmap.UnlockBits(bitmapData);
            }

            return inputBitmap;
        }

        protected abstract void CalculatePixel();
    }
}
