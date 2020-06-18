using ImageCalibration.Enums;

namespace ImageCalibration.Models
{
    public class ProcessingConfiguration
    {
        public SaveFormatEnum SaveFormat { get; }

        public bool ShouldGenerateMinis { get; }

        public int MinisFactor { get; }

        public bool ShouldDrawBorder { get; }

        public int BorderThickness { get; }

        public RotateFinalImageEnum RotateFinalImage { get; }

        public bool ShouldCropImage { get; }

        public int MaxCroppedHeight { get; }

        public int MaxCroppedWidth { get; }

        public ProcessingConfiguration(SaveFormatEnum saveFormat, bool shouldGenerateMinis, int minisFactor, bool shouldDrawBorder, int borderThickness,
            RotateFinalImageEnum rotateFinalImage, bool shouldCropImage, int maxHeight, int maxWidth)
        {
            SaveFormat = saveFormat;
            ShouldGenerateMinis = shouldGenerateMinis;
            MinisFactor = minisFactor;
            ShouldDrawBorder = shouldDrawBorder;
            BorderThickness = borderThickness;
            RotateFinalImage = rotateFinalImage;
            ShouldCropImage = shouldCropImage;
            MaxCroppedHeight = maxHeight;
            MaxCroppedWidth = maxWidth;
        }
    }
}
