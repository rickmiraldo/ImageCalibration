using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCalibration.Calibrations
{
    public abstract class Calibration : ICalibration
    {
        public string Name { get; set; }

        public CalibrationTypeEnum CalibrationType { get; set; }
    }
}
