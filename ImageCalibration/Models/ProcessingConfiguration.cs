using ImageCalibration.Enums;

namespace ImageCalibration.Models
{
    public class ProcessingConfiguration
    {
        public SaveFormatEnum SaveFormat { get; }

        public bool ShouldGenerateMinis { get; }

        public int MinisFactor { get; }

        public RotateFinalImageEnum RotateFinalImage { get; }

        public bool ShouldCropImage { get; }

        public int MaxCroppedHeight { get; }

        public int MaxCroppedWidth { get; }

        public ProcessingConfiguration(SaveFormatEnum saveFormat, bool shouldGenerateMinis, int minisFactor, RotateFinalImageEnum rotateFinalImage,
            bool shouldCropImage, int maxHeight, int maxWidth)
        {
            SaveFormat = saveFormat;
            ShouldGenerateMinis = shouldGenerateMinis;
            MinisFactor = minisFactor;
            RotateFinalImage = rotateFinalImage;
            ShouldCropImage = shouldCropImage;
            MaxCroppedHeight = maxHeight;
            MaxCroppedWidth = maxWidth;
        }
    }
}
