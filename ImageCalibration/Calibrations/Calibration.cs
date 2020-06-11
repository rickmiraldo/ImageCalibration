﻿using ImageCalibration.Enums;
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

            Bitmap processedBitmap = processImage(inputBitmap);

            processedBitmap.SetResolution(72f, 72f);
            //processedBitmap.SetResolution(inputBitmap.HorizontalResolution, inputBitmap.VerticalResolution);

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
                    SaveHelper.SaveTiff(outputFilePath, image, saveFormat);
                    break;
                case SaveFormatEnum.TIFFLZW:
                    outputFilePath = Path.ChangeExtension(outputFilePath, "tif");
                    SaveHelper.SaveTiff(outputFilePath, image, saveFormat);
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

        private unsafe Bitmap processImage(Bitmap bitmap)
        {
            Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height, bitmap.PixelFormat);

            BitmapData originalBitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            BitmapData newBitmapData = newBitmap.LockBits(new Rectangle(0, 0, newBitmap.Width, newBitmap.Height), ImageLockMode.ReadWrite, newBitmap.PixelFormat);

            byte bytesPerPixelOriginal = (byte)(Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8);
            byte bytesPerPixelNew = (byte)(Bitmap.GetPixelFormatSize(newBitmap.PixelFormat) / 8);

            // Ponteiro para o início do primeiro pixel da imagem
            byte* ptrFirstPixelOriginal = (byte*)originalBitmapData.Scan0.ToPointer();
            byte* ptrFirstPixelNew = (byte*)newBitmapData.Scan0.ToPointer();

            Parallel.For(0, newBitmapData.Height, y =>
            {
                //for (int x = 0; x < newBitmapData.Width; x++)
                Parallel.For(0, newBitmapData.Width, x =>
                {
                    byte* dataNew = ptrFirstPixelNew + y * newBitmapData.Stride + x * bytesPerPixelNew;
                    // Variável "data" é um ponteiro para o primeiro byte dos dados
                    // Exemplo para 3 bytes/pixel: data[0] = blue / data[1] = green / data[2] = red

                    double columnCorrected, rowCorrected;
                    CalculateCorrectedCoordinates(x, y, newBitmapData.Width, newBitmapData.Height, out columnCorrected, out rowCorrected);

                    byte[] newColors = new byte[bytesPerPixelOriginal];
                    newColors = getColorsFromBiliearInterpolation(rowCorrected, columnCorrected, newBitmapData.Width, newBitmapData.Height, ptrFirstPixelOriginal, originalBitmapData.Stride, bytesPerPixelOriginal);

                    for (int i = 0; i < bytesPerPixelNew; i++)
                    {
                        dataNew[i] = newColors[i];
                    }
                }
                );
            });

            bitmap.UnlockBits(originalBitmapData);
            newBitmap.UnlockBits(newBitmapData);

            return newBitmap;
        }

        public abstract void CalculateCorrectedCoordinates(int xFinalImage, int yFinalImage, int widthFinalImage, int heightFinalImage, out double xMeasured, out double yMeasured);

        private unsafe byte[] getColorsFromBiliearInterpolation(double y, double x, int maxWidth, int maxHeight, byte* ptrToFirstPixel, int stride, byte bytesPerPixel)
        {
            // Coordenadas dos 4 pontos próximos
            var p0x = (int)x;
            var p0y = (int)y;

            var p1x = p0x + 1;
            var p1y = p0y;

            var p2x = p0x;
            var p2y = p0y + 1;

            var p3x = p1x;
            var p3y = p2y;

            var deltaX = x - (int)x;
            var deltaY = y - (int)y;

            // Ler as cores dos 4 pontos
            // Se estiver fora dos limites, atribuir valor 0 e delta 1
            byte[] p0Colors = new byte[bytesPerPixel];
            for (int i = 0; i < bytesPerPixel; i++)
            {
                if ((p0x < 0) || (p0x >= maxWidth))
                {
                    p0Colors[i] = 0;
                    deltaX = 1;
                }
                else if ((p0y < 0) || (p0y >= maxHeight))
                {
                    p0Colors[i] = 0;
                    deltaY = 1;
                }
                else
                {
                    byte* p0 = ptrToFirstPixel + p0y * stride + p0x * bytesPerPixel;
                    p0Colors[i] = p0[i];
                }
            }

            byte[] p1Colors = new byte[bytesPerPixel];
            for (int i = 0; i < bytesPerPixel; i++)
            {
                if ((p1x < 0) || (p1x >= maxWidth))
                {
                    p1Colors[i] = 0;
                    deltaX = 1;
                }
                else if ((p1y < 0) || (p1y >= maxHeight))
                {
                    p1Colors[i] = 0;
                    deltaY = 1;
                }
                else
                {
                    byte* p1 = ptrToFirstPixel + p1y * stride + p1x * bytesPerPixel;
                    p1Colors[i] = p1[i];
                }
            }

            byte[] p2Colors = new byte[bytesPerPixel];
            for (int i = 0; i < bytesPerPixel; i++)
            {
                if ((p2x < 0) || (p2x >= maxWidth))
                {
                    p2Colors[i] = 0;
                    deltaX = 1;
                }
                else if ((p2y < 0) || (p2y >= maxHeight))
                {
                    p2Colors[i] = 0;
                    deltaY = 1;
                }
                else
                {
                    byte* p2 = ptrToFirstPixel + p2y * stride + p2x * bytesPerPixel;
                    p2Colors[i] = p2[i];
                }
            }

            byte[] p3Colors = new byte[bytesPerPixel];
            for (int i = 0; i < bytesPerPixel; i++)
            {
                if ((p3x < 0) || (p3x >= maxWidth))
                {
                    p3Colors[i] = 0;
                    deltaX = 1;
                }
                else if ((p3y < 0) || (p3y >= maxHeight))
                {
                    p3Colors[i] = 0;
                    deltaY = 1;
                }
                else
                {
                    byte* p3 = ptrToFirstPixel + p3y * stride + p3x * bytesPerPixel;
                    p3Colors[i] = p3[i];
                }
            }

            // Calcular o peso de cada ponto
            byte[] calculatedColors = new byte[bytesPerPixel];

            for (int i = 0; i < bytesPerPixel; i++)
            {
                calculatedColors[i] = (byte)((1 - deltaX) * (1 - deltaY) * p0Colors[i] +
                                             deltaX * (1 - deltaY) * p1Colors[i] +
                                             (1 - deltaX) * deltaY * p2Colors[i] +
                                             deltaX * deltaY * p3Colors[i]);
            }

            return calculatedColors;
        }
    }
}
