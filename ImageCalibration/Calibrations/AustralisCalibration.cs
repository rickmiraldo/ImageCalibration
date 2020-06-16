using System;
using ImageCalibration.Enums;

namespace ImageCalibration.Calibrations
{
    public class AustralisCalibration : Calibration
    {
        public double Xppa { get; }

        public double Yppa { get; }

        public double K0 { get; }

        public double K1 { get; }

        public double K2 { get; }

        public double K3 { get; }

        public double P1 { get; }

        public double P2 { get; }

        public double F { get; }

        public double Psx { get; }

        public double Psy { get; }

        public double B1 { get; }

        public double B2 { get; }

        public AustralisCalibration(string name, double xppa, double yppa, double k0, double k1, double k2, double k3,
            double p1, double p2, double f, double psx, double psy, double b1, double b2) : base(name, CalibrationTypeEnum.AUSTRALIS)
        {
            Xppa = xppa;
            Yppa = yppa;
            K0 = k0;
            K1 = k1;
            K2 = k2;
            K3 = k3;
            P1 = p1;
            P2 = p2;
            F = f;
            Psx = psx;
            Psy = psy;
            B1 = b1;
            B2 = b2;
        }

        public override void CalculateCorrectedCoordinates(int xFinalImage, int yFinalImage, int widthFinalImage, int heightFinalImage, out double columnCorrected, out double lineCorrected)
        {
            var xipc = xFinalImage - (widthFinalImage / 2);
            var yipc = yFinalImage - (heightFinalImage / 2);

            var xic = xipc * Psx;
            var yic = yipc * Psy;

            // Correção de distorção radial
            // IVAN: r = radius distance from the principal center to the pixel in signed normalized pixels 
            var r2 = (xic * xic) + (yic * yic);
            var r4 = r2 * r2;
            var r6 = r4 * r2;
            var r = Math.Pow(r2, 0.5);

            // IVAN: dxr = delta of radial distotion function of the radial
            // IVAN: signed distance
            var dxr = K0 + (K1 * r2) + (K2 * r4) + (K3 * r6);
            var deltaRadX = dxr * xic;
            var deltaRadY = dxr * yic;

            // Correção de distorção tangencial
            // IVAN: dxt and dyt are the delta tangential corretions
            // IVAN: xd and yd are the tangential distortion free components
            var dxt = P1 * (r2 + 2 * xic * xic) + 2 * P2 * xic * yic;
            var dyt = P2 * (r2 + 2 * yic * yic) + 2 * P1 * xic * yic;

            var dxort = (B1 * xic) + (B2 * yic);
            var deltaX = deltaRadX + dxt + dxort;
            var deltaY = deltaRadY + dyt;

            var xFotogrametrico = xic - deltaX;
            var yFotogrametrico = yic - deltaY;
            var xi = xFotogrametrico + Xppa;
            var yi = yFotogrametrico + Yppa;
            var xip = xi / Psx;
            var yip = yi / Psy;

            columnCorrected = (xip + (widthFinalImage / 2));
            lineCorrected = (yip + (heightFinalImage / 2));

            // Verificar se o pixel mensurado não está fora do tamanho máximo
            if (columnCorrected < 0)
            {
                columnCorrected = 0;
            }
            else if (columnCorrected > widthFinalImage)
            {
                columnCorrected = widthFinalImage - 1;
            }

            if (lineCorrected < 0)
            {
                lineCorrected = 0;
            }
            else if (lineCorrected > heightFinalImage)
            {
                lineCorrected = heightFinalImage - 1;
            }
        }
    }
}
