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

        public override void CalculateCorrectedCoordinates(int xFinalImage, int yFinalImage, int widthFinalImage, int heightFinalImage, out double columnCorrected, out double lineCorrected)
        {
            var xi = xFinalImage - (widthFinalImage / 2);
            var yi = yFinalImage - (heightFinalImage / 2);
            var xOtico = (xi * Psx) - Xppa;
            var yOtico = (yi * Psy) - Yppa;

            // Correção de distorção radial
            // IVAN: r = radius distance from the principal center to the pixel in signed normalized pixels 
            var r2 = (xOtico * xOtico) + (yOtico * yOtico);
            var r4 = r2 * r2;
            var r6 = r4 * r2;
            var r = Math.Pow(r2, 0.5);

            // IVAN: dxr = delta of radial distotion function of the radial
            // IVAN: signed distance
            var dxr = K0 + (K1 * r2) + (K2 * r4) + (K3 * r6);
            var deltaX = dxr * xOtico;
            var deltaY = dxr * yOtico;

            // Correção aplicada nas duas direções
            // IVAN: xd1 and yd1 are the radial distortion free components 
            var xdr = xOtico + deltaX;
            var ydr = yOtico + deltaY;

            // Correção de distorção tangencial
            // IVAN: dxt and dyt are the delta tangential corretions
            // IVAN: xd and yd are the tangential distortion free components
            var dxt = P1 * (3 * Math.Pow(xOtico, 2) + Math.Pow(yOtico, 2)) + (2 * P2 * xOtico * yOtico);
            var dyt = P2 * (3 * Math.Pow(yOtico, 2) + Math.Pow(xOtico, 2)) + (2 * P1 * xOtico * yOtico);

            var xFotogrametrico = xdr + dxt;
            var yFotogrametrico = ydr + dyt;

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
