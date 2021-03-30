using System;
using ImageCalibration.Enums;

namespace ImageCalibration.Calibrations
{
    public class SimActivePositiveCalibration : Calibration
    {
        public double Xppa { get; }

        public double Yppa { get; }

        public double K0 { get; }

        public double K1 { get; }

        public double K2 { get; }

        public double K3 { get; }

        public double K4 { get; }

        public double K5 { get; }

        public double K6 { get; }

        public double K7 { get; }

        public double P1 { get; }

        public double P2 { get; }

        public double P3 { get; }

        public double P4 { get; }

        public double Psx { get; }

        public double Psy { get; }

        public SimActivePositiveCalibration(string name, double xppa, double yppa, double k0, double k1, double k2, double k3, double k4,
            double k5, double k6, double k7, double p1, double p2, double p3, double p4, double f, double psx,
            double psy) : base(name, CalibrationTypeEnum.USGS)
        {
            Xppa = xppa;
            Yppa = yppa;
            K0 = k0;
            K1 = k1;
            K2 = k2;
            K3 = k3;
            K4 = k4;
            K5 = k5;
            K6 = k6;
            K7 = k7;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
            Psx = psx;
            Psy = psy;
        }

        public override void CalculateCorrectedCoordinates(int xFinalImage, int yFinalImage, int widthFinalImage, int heightFinalImage, out double columnCorrected, out double lineCorrected)
        {
            var xi = xFinalImage - (widthFinalImage / 2);
            var yi = yFinalImage - (heightFinalImage / 2);
            var xOtico = (xi * Psx) - Xppa;
            var yOtico = (yi * Psy) - Yppa;

            // Correção de distorção radial
            var r2 = (xOtico * xOtico) + (yOtico * yOtico);
            var r4 = r2 * r2;
            var r6 = r4 * r2;
            var r8 = r6 * r2;
            var r10 = r8 * r2;
            var r12 = r10 * r2;
            var r14 = r12 * r2;
            var r = Math.Pow(r2, 0.5);

            var dRad = K0 + (K1 * r2) + (K2 * r4) + (K3 * r6) + (K4 * r8) + (K5 * r10) + (K6 * r12) + (K7 * r14);
            var deltaX = dRad * xOtico;
            var deltaY = dRad * yOtico;

            // Correção Decentering - Tangencial
            var fatorDecentering = 1 + P3 * r2 + P4 * r4;

            var dxt = fatorDecentering * (P1 * (r2 + 2 * deltaX * deltaX) + 2 * P2 * deltaX * deltaY);
            var dyt = fatorDecentering * (P2 * (r2 + 2 * deltaY * deltaY) + 2 * P1 * deltaX * deltaY);

            var xFotogrametrico = xOtico - (deltaX + dxt);
            var yFotogrametrico = yOtico - (deltaY + dyt);

            var x = xFotogrametrico + Xppa;
            var y = yFotogrametrico + Yppa;

            columnCorrected = (((x + Xppa) / Psx) + (widthFinalImage / 2));
            lineCorrected = (((y + Yppa) / Psy) + (heightFinalImage / 2));

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
