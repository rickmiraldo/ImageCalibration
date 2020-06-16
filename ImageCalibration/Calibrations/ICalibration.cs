using ImageCalibration.Models;

namespace ImageCalibration.Calibrations
{
    interface ICalibration
    {
        void StartProcessing(string inputFile, string outputFolderPath, ProcessingConfiguration processingConfiguration);

        void CalculateCorrectedCoordinates(int xFinalImage, int yFinalImage, int widthFinalImage, int heightFinalImage, out double columnCorrected, out double lineCorrected);
    }
}
