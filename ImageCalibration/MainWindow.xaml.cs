using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using Path = System.IO.Path;
using System.Security;
using System.IO.Packaging;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Threading;
using System.Globalization;
using IniParser;
using IniParser.Model;
using IniParser.Parser;
using ImageCalibration.Calibrations;

namespace ImageCalibration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string inputFolder;
        List<string> inputFiles = new List<string>();
        List<Calibration> calibrations = new List<Calibration>();

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            InitializeComponent();

            lookForCalibrationFiles();
        }

        private void lookForCalibrationFiles()
        {
            const string traditionalCalib = "Tradicional";
            const string australisCalib = "Australis";

            var calibPath = System.AppDomain.CurrentDomain.BaseDirectory + "calibs\\";
            var calibFiles = Directory.EnumerateFiles(calibPath, "*.ini", SearchOption.TopDirectoryOnly).ToList();

            foreach (var calib in calibFiles)
            {
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(calib);
                var name = Path.GetFileName(calib).Replace(".ini", "");

                if (data.Sections.ContainsSection(traditionalCalib))
                {
                    double xppa;
                    double.TryParse(data[traditionalCalib]["Xppa"], out xppa);

                    double yppa;
                    double.TryParse(data[traditionalCalib]["Yppa"], out yppa);

                    double k1;
                    double.TryParse(data[traditionalCalib]["K1"], out k1);

                    double k2;
                    double.TryParse(data[traditionalCalib]["K2"], out k2);

                    double k3;
                    double.TryParse(data[traditionalCalib]["K3"], out k3);

                    double p1;
                    double.TryParse(data[traditionalCalib]["P1"], out p1);

                    double p2;
                    double.TryParse(data[traditionalCalib]["P2"], out p2);

                    double f;
                    double.TryParse(data[traditionalCalib]["F"], out f);

                    double ps;
                    double.TryParse(data[traditionalCalib]["Ps"], out ps);

                    double psy;
                    double.TryParse(data[traditionalCalib]["Psy"], out psy);

                    var calibration = new TraditionalCalibration(name, xppa, yppa, k1, k2, k3, p1, p2, f, ps, psy);
                    calibrations.Add(calibration);
                }

                if (data.Sections.ContainsSection(australisCalib))
                {
                    double xppa;
                    double.TryParse(data[australisCalib]["Xppa"], out xppa);

                    double yppa;
                    double.TryParse(data[australisCalib]["Yppa"], out yppa);

                    double k1;
                    double.TryParse(data[australisCalib]["K1"], out k1);

                    double k2;
                    double.TryParse(data[australisCalib]["K2"], out k2);

                    double k3;
                    double.TryParse(data[australisCalib]["K3"], out k3);

                    double p1;
                    double.TryParse(data[australisCalib]["P1"], out p1);

                    double p2;
                    double.TryParse(data[australisCalib]["P2"], out p2);

                    double f;
                    double.TryParse(data[australisCalib]["F"], out f);

                    double ps;
                    double.TryParse(data[australisCalib]["Ps"], out ps);

                    double psx;
                    double.TryParse(data[australisCalib]["Psx"], out psx);

                    double psy;
                    double.TryParse(data[australisCalib]["Psy"], out psy);

                    double b1;
                    double.TryParse(data[australisCalib]["B1"], out b1);

                    double b2;
                    double.TryParse(data[australisCalib]["B2"], out b2);

                    var calibration = new AustralisCalibration(name, xppa, yppa, k1, k2, k3, p1, p2, f, ps, psx, psy, b1, b2);
                    calibrations.Add(calibration);
                }
            }

            cmbCalibrations.ItemsSource = calibrations;
            cmbCalibrations.DisplayMemberPath = "Name";
        }

        private void btnChooseInputFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                verifyInputDirectory(dialog.SelectedPath);
            }
        }

        private void btnChooseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                verifyOutputDirectory(dialog.SelectedPath);
            }
        }

        private void txtInputFolder_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtInputFolder.Text == "")
            {
                return;
            }
            verifyInputDirectory(txtInputFolder.Text);
        }

        private void verifyInputDirectory(string path)
        {
            if (checkIfDirectoryExists(path))
            {
                txtOutputFolder.IsEnabled = true;
                btnChooseOutputFolder.IsEnabled = true;
                txtInputFolder.Text = path;
                inputFolder = path;

                inputFiles.Clear();
                inputFiles = enumerateImagesInDirectory(path);

                txtStatusBar.Text = inputFiles.Count.ToString() + " arquivo(s) encontrado(s)!";
            }
            else
            {
                txtStatusBar.Text = "Diretório inválido!";
            }
        }

        private List<string> enumerateImagesInDirectory(string path)
        {
            var tifFiles = Directory.EnumerateFiles(path, "*.tif", SearchOption.TopDirectoryOnly).ToList();
            var jpgFiles = Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly).ToList();

            List<string> files = new List<string>();

            if (tifFiles.Count > 0)
            {
                files.AddRange(tifFiles);
            }
            if (jpgFiles.Count > 0)
            {
                files.AddRange(jpgFiles);
            }
            files.Sort();

            return files;
        }

        private bool checkIfDirectoryExists(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                return true;
            }
            MessageBox.Show("Diretório inválido!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return false;
        }

        private void txtOutputFolder_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtOutputFolder.Text == "")
            {
                return;
            }
            verifyOutputDirectory(txtOutputFolder.Text);
        }

        private void verifyOutputDirectory(string path)
        {
            if (checkIfDirectoryExists(path))
            {
                cmbCalibrations.IsEnabled = true;
                //tabCalibrations.IsEnabled = true;
            }
            else
            {
                cmbCalibrations.IsEnabled = false;
                //tabCalibrations.IsEnabled = false;
            }
        }

        private void btnSobre_Click(object sender, RoutedEventArgs e)
        {
            // TO-DO
            txtStatusBar.Text = tabCalibrations.SelectedIndex.ToString();
        }

        private void cmbCalibrations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var calibration = calibrations[cmbCalibrations.SelectedIndex];
            tabCalibrations.SelectedIndex = (int)calibration.CalibrationType;

            if (calibration.CalibrationType == CalibrationTypeEnum.TRADITIONAL)
            {
                var calib = (TraditionalCalibration)calibration;
                
                txtTraditionalXppa.Text = calib.Xppa.ToString();
                txtTraditionalYppa.Text = calib.Yppa.ToString();
                txtTraditionalK1.Text = calib.K1.ToString();
                txtTraditionalK2.Text = calib.K2.ToString();
                txtTraditionalK3.Text = calib.K3.ToString();
                txtTraditionalP1.Text = calib.P1.ToString();
                txtTraditionalP2.Text = calib.P2.ToString();
                txtTraditionalF.Text = calib.F.ToString();
                txtTraditionalPs.Text = calib.Ps.ToString();
                txtTraditionalPsy.Text = calib.Psy.ToString();
            }

            if (calibration.CalibrationType == CalibrationTypeEnum.AUSTRALIS)
            {
                var calib = (AustralisCalibration)calibration;

                txtAustralisXppa.Text = calib.Xppa.ToString();
                txtAustralisYppa.Text = calib.Yppa.ToString();
                txtAustralisK1.Text = calib.K1.ToString();
                txtAustralisK2.Text = calib.K2.ToString();
                txtAustralisK3.Text = calib.K3.ToString();
                txtAustralisP1.Text = calib.P1.ToString();
                txtAustralisP2.Text = calib.P2.ToString();
                txtAustralisF.Text = calib.F.ToString();
                txtAustralisPs.Text = calib.Ps.ToString();
                txtAustralisPsx.Text = calib.Psx.ToString();
                txtAustralisPsy.Text = calib.Psy.ToString();
                txtAustralisB1.Text = calib.B1.ToString();
                txtAustralisB2.Text = calib.B2.ToString();
            }
        }
    }
}
