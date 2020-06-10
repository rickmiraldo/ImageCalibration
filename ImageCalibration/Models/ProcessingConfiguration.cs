using ImageCalibration.Enums;

namespace ImageCalibration.Models
{
    public class ProcessingConfiguration
    {
        public SaveFormatEnum SaveFormat { get; set; }

        public bool ShouldScale { get; set; }

        public float ScaleFactor { get; set; }

        public ProcessingConfiguration() { }
    }
}
