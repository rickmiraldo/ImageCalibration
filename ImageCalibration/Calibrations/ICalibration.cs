using ImageCalibration.Enums;
using ImageCalibration.Models;
using System.Drawing;

namespace ImageCalibration.Calibrations
{
    interface ICalibration
    {
        void StartProcessing(string inputFile, string outputFolderPath, ProcessingConfiguration processingConfiguration);

        void CalculateCorrectedCoordinates(int xFinal, int yFinal, int widthFinal, int heightFinal, out double xMeasured, out double yMeasured);
    }
}
