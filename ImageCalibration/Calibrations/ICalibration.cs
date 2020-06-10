using ImageCalibration.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCalibration.Calibrations
{
    interface ICalibration
    {
        void StartProcessing(string inputFile, string outputFolderPath, ProcessingConfiguration processingConfiguration);

        void CalculateCorrectedCoordinates(int xFinal, int yFinal, int widthFinal, int heightFinal, out double xMeasured, out double yMeasured);
    }
}
