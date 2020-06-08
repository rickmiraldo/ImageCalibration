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

        protected override void CalculatePixel()
        {
            MessageBox.Show("iu");
        }
    }
}
