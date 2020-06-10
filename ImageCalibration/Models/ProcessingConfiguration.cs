using ImageCalibration.Enums;

namespace ImageCalibration.Models
{
    public class ProcessingConfiguration
    {
        public SaveFormatEnum SaveFormat { get; set; }

        public bool ShouldResize { get; set; }

        public float ImageScale { get; set; }

        public ProcessingConfiguration() { }
    }
}
