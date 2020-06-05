using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCalibration.Calibrations
{
    public class AustralisCalibration : Calibration
    {
        public double Xppa { get; set; }

        public double Yppa { get; set; }

        public double K1 { get; set; }

        public double K2 { get; set; }

        public double K3 { get; set; }

        public double P1 { get; set; }

        public double P2 { get; set; }

        public double F { get; set; }

        public double Ps { get; set; }

        public double Psx { get; set; }

        public double Psy { get; set; }

        public double B1 { get; set; }

        public double B2 { get; set; }

        public AustralisCalibration(string name, double xppa, double yppa, double k1, double k2, double k3, double p1, double p2, double f, double ps, double psx, double psy, double b1, double b2)
        {
            Name = name;
            CalibrationType = CalibrationTypeEnum.AUSTRALIS;
            Xppa = xppa;
            Yppa = yppa;
            K1 = k1;
            K2 = k2;
            K3 = k3;
            P1 = p1;
            P2 = p2;
            F = f;
            Ps = ps;
            Psx = psx;
            Psy = psy;
            B1 = b1;
            B2 = b2;
        }
    }
}
