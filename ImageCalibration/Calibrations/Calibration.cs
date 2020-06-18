using System.Drawing;
using System.Drawing.Imaging;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using System.IO;
using System.Threading.Tasks;
using ImageCalibration.Enums;
using ImageCalibration.Helpers;
using ImageCalibration.Models;
using System.Windows.Forms;
using System;

namespace ImageCalibration.Calibrations
{
    public abstract class Calibration : ICalibration
    {
        public string Name { get; }

        public CalibrationTypeEnum CalibrationType { get; }

        public Calibration(string name, CalibrationTypeEnum calibType)
        {
            Name = name;
            CalibrationType = calibType;
        }

        public abstract void CalculateCorrectedCoordinates(int xFinalImage, int yFinalImage, int widthFinalImage, int heightFinalImage, out double columnCorrected, out double lineCorrected);

        public void StartProcessing(string inputFile, string outputFolderPath, ProcessingConfiguration processingConfiguration)
        {
            // Prepara caminho para salvar imagem
            string filename = Path.GetFileName(inputFile);
            string outputFilePath = outputFolderPath + "\\" + filename;

            // Abre imagem
            Bitmap inputBitmap = new Bitmap(inputFile);

            // Processa imagem (coordenadas + cores)
            Bitmap processedBitmap = processImage(inputBitmap);

            // Setar resolução da imagem de destino para ser a mesma da origem
            processedBitmap.SetResolution(inputBitmap.HorizontalResolution, inputBitmap.VerticalResolution);

            inputBitmap.Dispose();

            // Rodar imagem (se necessário)
            rotateImage(processedBitmap, processingConfiguration.RotateFinalImage);

            // Cortar imagem (se necessário)
            if (processingConfiguration.ShouldCropImage)
            {
                processedBitmap = cropImage(processedBitmap, processingConfiguration.MaxCroppedWidth, processingConfiguration.MaxCroppedHeight);
            }

            // Gerar minis
            if (processingConfiguration.ShouldGenerateMinis)
            {
                string outputMiniFolderPath = outputFolderPath + "\\minis";
                string outputMiniFilePath = outputMiniFolderPath + "\\" + filename;
                Directory.CreateDirectory(outputMiniFolderPath);
                generateMinis(processedBitmap, outputMiniFilePath, processingConfiguration);
            }

            // Salvar imagem final
            saveImage(outputFilePath, processedBitmap, processingConfiguration.SaveFormat);
            processedBitmap.Dispose();
        }

        private static void saveImage(string outputFilePath, Bitmap image, SaveFormatEnum saveFormat)
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
                Parallel.For(0, newBitmapData.Width, x =>
                {
                    byte* dataNew = ptrFirstPixelNew + y * newBitmapData.Stride + x * bytesPerPixelNew;
                    // Variável "dataNew" é um ponteiro para o primeiro byte dos dados
                    // Exemplo para 3 bytes/pixel: data[0] = blue / data[1] = green / data[2] = red

                    // Calcula coordenadas corretas
                    double columnCorrected, rowCorrected;
                    CalculateCorrectedCoordinates(x, y, newBitmapData.Width, newBitmapData.Height, out columnCorrected, out rowCorrected);

                    // Calcula  cores corretas
                    byte[] newColors = new byte[bytesPerPixelOriginal];
                    newColors = getColorsFromBiliearInterpolation(rowCorrected, columnCorrected, newBitmapData.Width, newBitmapData.Height, ptrFirstPixelOriginal, originalBitmapData.Stride, bytesPerPixelOriginal);

                    // Salva cores obtidas na coordenada da nova imagem
                    for (int i = 0; i < bytesPerPixelNew; i++)
                    {
                        dataNew[i] = newColors[i];
                    }
                });
            });

            bitmap.UnlockBits(originalBitmapData);
            newBitmap.UnlockBits(newBitmapData);

            return newBitmap;
        }

        private static unsafe byte[] getColorsFromBiliearInterpolation(double y, double x, int maxWidth, int maxHeight, byte* ptrToFirstPixel, int stride, byte bytesPerPixel)
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

        private static void rotateImage(Bitmap image, RotateFinalImageEnum rotate)
        {
            switch (rotate)
            {
                case RotateFinalImageEnum.NO:
                    return;
                case RotateFinalImageEnum.R90CCW:
                    image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                    return;
                case RotateFinalImageEnum.R90CW:
                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                    return;
                case RotateFinalImageEnum.R180:
                    image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                    return;
                default:
                    return;
            }
        }

        private static Bitmap cropImage(Bitmap image, int newMaxWidth, int newMaxHeight)
        {
            var deltaWidth = image.Width - newMaxWidth;
            var deltaHeight = image.Height - newMaxHeight;

            // Se valor da imagem nova for maior do que a imagem original, então não corta nada e devolve a imagem original
            if ((deltaWidth < 0) || (deltaHeight < 0))
            {
                return image;
            }

            var topLeftX = (int)Math.Round((double)(deltaWidth / 2));
            var topLeftY = (int)Math.Round((double)(deltaHeight / 2));

            var topLeftCorner = new Point(topLeftX, topLeftY);
            var size = new Size(newMaxWidth, newMaxHeight);

            Rectangle cropRectangle = new Rectangle(topLeftCorner, size);

            Bitmap cropped = image.Clone(cropRectangle, image.PixelFormat);

            return cropped;
        }

        private static void generateMinis(Bitmap image, string outputMinifilePath, ProcessingConfiguration config)
        {
            int miniWidth = image.Width / config.MinisFactor;
            int miniHeight = image.Height / config.MinisFactor;

            Bitmap mini = new Bitmap(image, miniWidth, miniHeight);

            if (config.ShouldDrawBorder)
            {
                mini = drawBorder(mini, config.BorderThickness);
            }

            saveImage(outputMinifilePath, mini, SaveFormatEnum.JPG90);

            mini.Dispose();
        }

        private unsafe static Bitmap drawBorder(Bitmap image, int thickness)
        {
            BitmapData imageData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);

            byte bytesPerPixel = (byte)(Bitmap.GetPixelFormatSize(imageData.PixelFormat) / 8);

            // Ponteiro para o início do primeiro pixel da imagem
            byte* ptrFirstPixel = (byte*)imageData.Scan0.ToPointer();

            // Borda superior
            Parallel.For(0, thickness, y =>
            {
                Parallel.For(0, imageData.Width, x =>
                {
                    byte* data = ptrFirstPixel + y * imageData.Stride + x * bytesPerPixel;

                    // Pinta de preto
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        data[i] = (byte)0;
                    }
                });
            });

            // Borda esquerda
            Parallel.For(0, imageData.Height, y =>
            {
                Parallel.For(0, thickness, x =>
                {
                    byte* data = ptrFirstPixel + y * imageData.Stride + x * bytesPerPixel;

                    // Pinta de preto
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        data[i] = (byte)0;
                    }
                });
            });

            // Borda inferior
            Parallel.For(imageData.Height - thickness, imageData.Height, y =>
            {
                Parallel.For(0, imageData.Width, x =>
                {
                    byte* data = ptrFirstPixel + y * imageData.Stride + x * bytesPerPixel;

                    // Pinta de preto
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        data[i] = (byte)0;
                    }
                });
            });

            // Borda direita
            Parallel.For(0, imageData.Height, y =>
            {
                Parallel.For(imageData.Width - thickness, imageData.Width, x =>
                {
                    byte* data = ptrFirstPixel + y * imageData.Stride + x * bytesPerPixel;

                    // Pinta de preto
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        data[i] = (byte)0;
                    }
                });
            });

            image.UnlockBits(imageData);

            return image;
        }
    }
}
