using ImageCalibration.Enums;
using ImageCalibration.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ImageCalibration.Calibrations
{
    public class UsgsCalibration : Calibration
    {
        public double Xppa { get; set; }

        public double Yppa { get; set; }

        public double K0 { get; set; }

        public double K1 { get; set; }

        public double K2 { get; set; }

        public double K3 { get; set; }

        public double K4 { get; set; }

        public double P1 { get; set; }

        public double P2 { get; set; }

        public double P3 { get; set; }

        public double P4 { get; set; }

        public double F { get; set; }

        public double Psx { get; set; }

        public double Psy { get; set; }

        public UsgsCalibration(string name, double xppa, double yppa, double k0, double k1, double k2, double k3, double k4,
            double p1, double p2, double p3, double p4, double f, double psx, double psy)
        {
            Name = name;
            CalibrationType = CalibrationTypeEnum.USGS;
            Xppa = xppa;
            Yppa = yppa;
            K0 = k0;
            K1 = k1;
            K2 = k2;
            K3 = k3;
            K4 = k4;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            P4 = p4;
            F = f;
            Psx = psx;
            Psy = psy;
        }

        public override void CalculateCorrectedCoordinates(int xFinal, int yFinal, int widthFinal, int heightFinal, out double xMeasured, out double yMeasured)
        {
            var xi = xFinal - (widthFinal / 2);
            var yi = yFinal - (heightFinal / 2);
            var x = (xi * Psx) - Xppa;
            var y = (yi * Psy) - Yppa;

            // Correção de distorção radial
            // IVAN: r = radius distance from the principal center to the pixel in signed normalized pixels 
            var r2 = (x * x) + (y * y);
            var r4 = r2 * r2;
            var r6 = r4 * r2;
            var r = Math.Pow(r2, 0.5);

            // IVAN: dxr = delta of radial distotion function of the radial
            var dxr = (K1 * r2) + (K2 * r4) + (K3 * r6);
            var deltaX = dxr * x;
            var deltaY = dxr * y;

            // Correção aplicada nas duas direções
            // IVAN: xd1 and yd1 are the radial distortion free components 
            var xdr = x + deltaX;
            var ydr = y + deltaY;

            // Correção de distorção tangencial
            // IVAN: dxt and dyt are the delta tangential corretions
            // IVAN: xd and yd are the tangential distortion free components
            var dxt = P1 * (3 * Math.Pow(x, 2) + Math.Pow(y, 2)) + (2 * P2 * x * y);
            var dyt = P2 * (3 * Math.Pow(y, 2) + Math.Pow(x, 2)) + (2 * P1 * x * y);

            var xf = xdr + dxt;
            var yf = ydr + dyt;

            xMeasured = xf + Xppa;
            yMeasured = yf + Yppa;
        }
    }
}
