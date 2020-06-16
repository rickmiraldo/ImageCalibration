using System;
using System.Collections.Generic;
using System.IO;
using ImageCalibration.Enums;

namespace ImageCalibration.Helpers
{
    public class IniData
    {
        public CalibrationTypeEnum CalibrationType { get; }

        public Dictionary<string, double> Parameters { get; }

        public IniData(CalibrationTypeEnum calibType, Dictionary<string, double> parameters)
        {
            CalibrationType = calibType;
            Parameters = parameters;
        }
    }

    public static class IniHelper
    {
        public static IniData ReadIni(string file)
        {
            const string USGS_CALIB_HEADER = "USGS";
            const string AUSTRALIS_CALIB_HEADER = "Australis";

            string[] lines = File.ReadAllLines(file);

            CalibrationTypeEnum calibType;
            Dictionary<string, double> parameters = new Dictionary<string, double>();

            try
            {
                string calibHeader = lines[0].Substring(1, lines[0].IndexOf(']') - 1);
                
                switch (calibHeader)
                {
                    case USGS_CALIB_HEADER:
                        calibType = CalibrationTypeEnum.USGS;
                        break;
                    case AUSTRALIS_CALIB_HEADER:
                        calibType = CalibrationTypeEnum.AUSTRALIS;
                        break;
                    default:
                        return null;
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    var lineSplit = lines[i].Split('=');
                    var value = double.Parse(lineSplit[1]);
                    parameters.Add(lineSplit[0], value);
                }
            }
            catch (Exception)
            {
                return null;
            }

            var iniData = new IniData(calibType, parameters);

            return iniData;
        }
    }
}
