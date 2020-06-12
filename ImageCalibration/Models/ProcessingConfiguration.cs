using ImageCalibration.Enums;

namespace ImageCalibration.Models
{
    public class ProcessingConfiguration
    {
        public SaveFormatEnum SaveFormat { get; set; }

        public RotateFinalImageEnum RotateFinalImage { get; set; }

        public bool ShouldCropImage { get; set; }

        public int MaxCroppedHeight { get; set; }

        public int MaxCroppedWidth { get; set; }

        public ProcessingConfiguration() { }
    }
}
