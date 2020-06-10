using ImageCalibration.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageCalibration.Calibrations
{
    public class AustralisCalibration : Calibration
    {
        public double Xppa { get; set; }

        public double Yppa { get; set; }

        public double K0 { get; set; }

        public double K1 { get; set; }

        public double K2 { get; set; }

        public double K3 { get; set; }

        public double P1 { get; set; }

        public double P2 { get; set; }

        public double F { get; set; }

        public double Psx { get; set; }

        public double Psy { get; set; }

        public double B1 { get; set; }

        public double B2 { get; set; }

        public AustralisCalibration(string name, double xppa, double yppa, double k0, double k1, double k2, double k3,
            double p1, double p2, double f, double psx, double psy, double b1, double b2)
        {
            Name = name;
            CalibrationType = CalibrationTypeEnum.AUSTRALIS;
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

        public override void CalculateCorrectedCoordinates(int xFinal, int yFinal, int widthFinal, int heightFinal, out double xMeasured, out double yMeasured)
        {
            var xipc = xFinal - (widthFinal / 2);
            var yipc = yFinal - (heightFinal / 2);

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
            var dxr = (K1 * r2) + (K2 * r4) + (K3 * r6);
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

            var xf = xic - deltaX;
            var yf = yic - deltaY;
            var xi = xf + Xppa;
            var yi = yf + Yppa;
            var xip = xi / Psx;
            var yip = yi / Psy;

            xMeasured = (xip + (widthFinal / 2));
            yMeasured = (yip + (heightFinal / 2));

            // Verificar se o pixel mensurado não está fora do tamanho máximo
            if (xMeasured < 0)
            {
                xMeasured = 0;
            }
            else if (xMeasured > widthFinal)
            {
                xMeasured = widthFinal - 1;
            }

            if (yMeasured < 0)
            {
                yMeasured = 0;
            }
            else if (yMeasured > heightFinal)
            {
                yMeasured = heightFinal - 1;
            }
        }
    }
}
