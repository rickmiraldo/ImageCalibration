﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCalibration.Calibrations
{
    interface ICalibration
    {
        void StartProcessingAsync(string inputFile, string outputFolderPath);
    }
}
