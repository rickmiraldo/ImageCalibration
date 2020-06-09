using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Forms;
using System.IO;
using Path = System.IO.Path;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Threading;
using System.Globalization;
using IniParser;
using ImageCalibration.Calibrations;
using System.Diagnostics;
using TextBox = System.Windows.Controls.TextBox;
using System.Text.RegularExpressions;
using ImageCalibration.Models;
using ImageCalibration.Enums;

namespace ImageCalibration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string outputFolderPath;
        List<string> inputFiles = new List<string>();
        List<Calibration> calibrations = new List<Calibration>();
        Calibration calibToUse;

        public MainWindow()
        {
            // Força o programa a usar ponto (.) como separador decimal no lugar de vírgula (,)
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            // Start-up
            InitializeComponent();

            // Procura pelas calibrações assim que iniciar o programa
            lookForCalibrationFiles();
        }

        private void lookForCalibrationFiles()
        {
            // Definir os tipos de calibração
            const string usgsCalib = "USGS";
            const string australisCalib = "Australis";

            string calibPath;
            List<string> calibFiles;

            try
            {
                // Procura calibrações do tipo INI em uma subpasta "calibs" localizada na mesma pasta do executável
                calibPath = System.AppDomain.CurrentDomain.BaseDirectory + "calibs\\";
                calibFiles = Directory.EnumerateFiles(calibPath, "*.ini", SearchOption.TopDirectoryOnly).ToList();
            }
            catch (Exception)
            {
                // Se não encontrou nada, avisar e parar
                cmbCalibrations.ItemsSource = new[] { "Nenhuma calib encontrada" }.ToList();
                cmbCalibrations.SelectedIndex = 0;
                return;
            }

            // Se encontrou, ler cada arquivo de calibração e criar um objeto correspondente
            foreach (var calib in calibFiles)
            {
                var parser = new FileIniDataParser();
                var data = parser.ReadFile(calib);
                var name = Path.GetFileName(calib).Replace(".ini", "");

                if (data.Sections.ContainsSection(usgsCalib))
                {
                    double xppa;
                    double.TryParse(data[usgsCalib]["Xppa"], out xppa);

                    double yppa;
                    double.TryParse(data[usgsCalib]["Yppa"], out yppa);

                    double k0;
                    double.TryParse(data[usgsCalib]["K0"], out k0);

                    double k1;
                    double.TryParse(data[usgsCalib]["K1"], out k1);

                    double k2;
                    double.TryParse(data[usgsCalib]["K2"], out k2);

                    double k3;
                    double.TryParse(data[usgsCalib]["K3"], out k3);

                    double k4;
                    double.TryParse(data[usgsCalib]["K4"], out k4);

                    double p1;
                    double.TryParse(data[usgsCalib]["P1"], out p1);

                    double p2;
                    double.TryParse(data[usgsCalib]["P2"], out p2);

                    double p3;
                    double.TryParse(data[usgsCalib]["P3"], out p3);

                    double p4;
                    double.TryParse(data[usgsCalib]["P4"], out p4);

                    double f;
                    double.TryParse(data[usgsCalib]["F"], out f);

                    double psx;
                    double.TryParse(data[usgsCalib]["Psx"], out psx);

                    double psy;
                    double.TryParse(data[usgsCalib]["Psy"], out psy);

                    var calibration = new UsgsCalibration(name, xppa, yppa, k0, k1, k2, k3, k4, p1, p2, p3, p4, f, psx, psy);
                    calibrations.Add(calibration);
                }

                if (data.Sections.ContainsSection(australisCalib))
                {
                    double xppa;
                    double.TryParse(data[australisCalib]["Xppa"], out xppa);

                    double yppa;
                    double.TryParse(data[australisCalib]["Yppa"], out yppa);

                    double k0;
                    double.TryParse(data[australisCalib]["K0"], out k0);

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

                    double psx;
                    double.TryParse(data[australisCalib]["Psx"], out psx);

                    double psy;
                    double.TryParse(data[australisCalib]["Psy"], out psy);

                    double b1;
                    double.TryParse(data[australisCalib]["B1"], out b1);

                    double b2;
                    double.TryParse(data[australisCalib]["B2"], out b2);

                    var calibration = new AustralisCalibration(name, xppa, yppa, k0, k1, k2, k3, p1, p2, f, psx, psy, b1, b2);
                    calibrations.Add(calibration);
                }
            }

            // Popular combobox com a lista de calibração recém-criada, e mostrar o nome de cada uma delas
            cmbCalibrations.ItemsSource = calibrations;
            cmbCalibrations.DisplayMemberPath = "Name";
        }

        private void btnChooseInputFolder_Click(object sender, RoutedEventArgs e)
        {
            // Janela de escolher diretório de entrada
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                // Fazer verificações do diretório de entrada
                verifyInputDirectory(dialog.SelectedPath);
            }
        }

        private void btnChooseOutputFolder_Click(object sender, RoutedEventArgs e)
        {
            // Janela de escolher diretório de saída
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                // Fazer verificações do diretório de saída
                verifyOutputDirectory(dialog.SelectedPath);
            }
        }

        private void txtInputFolder_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Essa função é executada se alguém inserir um endereço manualmente no campo de entrada
            if (txtInputFolder.Text == "")
            {
                return;
            }

            // Fazer verificações do diretório de entrada
            verifyInputDirectory(txtInputFolder.Text);
        }

        private void txtOutputFolder_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Essa função é executada se alguém inserir um endereço manualmente no campo de saída
            if (txtOutputFolder.Text == "")
            {
                return;
            }

            // Fazer verificações do diretório de saída
            verifyOutputDirectory(txtOutputFolder.Text);
        }

        private void verifyInputDirectory(string path)
        {
            // Verificação checa se o diretório de entrada existe e chama outra função para contar e listar as imagens que existem
            if (checkIfDirectoryExists(path))
            {
                txtOutputFolder.IsEnabled = true;
                btnChooseOutputFolder.IsEnabled = true;
                txtInputFolder.Text = path;

                inputFiles.Clear();
                inputFiles = enumerateImagesInDirectory(path);

                txtStatusBar.Text = inputFiles.Count.ToString() + " arquivo(s) encontrado(s)!";
            }
            else
            {
                txtStatusBar.Text = "Diretório inválido!";
                txtOutputFolder.IsEnabled = false;
                btnChooseOutputFolder.IsEnabled = false;
            }
        }

        private void verifyOutputDirectory(string path)
        {
            // Verificação apenas checa se o diretório de saíde existe e habilita os próximos controles
            if (checkIfDirectoryExists(path))
            {
                outputFolderPath = path;
                cmbCalibrations.IsEnabled = true;
                txtOutputFolder.Text = path;
                //tabCalibrations.IsEnabled = true;
            }
            else
            {
                cmbCalibrations.IsEnabled = false;
                //tabCalibrations.IsEnabled = false;
            }
        }

        private bool checkIfDirectoryExists(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                return true;
            }
            showWarning("Diretório inválido!");

            return false;
        }

        private List<string> enumerateImagesInDirectory(string path)
        {
            // Atualmente somente verifica se existem arquivos TIF (inclui TIFF) e JPG
            // Não verifica subpastas
            List<string> tifFiles = Directory.EnumerateFiles(path, "*.tif", SearchOption.TopDirectoryOnly).ToList();
            List<string> jpgFiles = Directory.EnumerateFiles(path, "*.jpg", SearchOption.TopDirectoryOnly).ToList();

            List<string> files = new List<string>();

            files.AddRange(tifFiles);
            files.AddRange(jpgFiles);
            files.Sort();

            return files;
        }

        private void validateIfDecimalNumber(object sender, TextCompositionEventArgs e)
        {
            // Valida se o que foi digitado no campo de texto é um número decimal válido
            TextBox textBox = sender as TextBox;
            var newText = textBox.Text + e.Text;

            var regex = new Regex(@"^-{0,1}[0-9]*(?:\.[0-9]*)?$");

            if (regex.IsMatch(newText))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void cmbCalibrations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ao mudar a seleção do combobox de calibração, mudar para a aba correspondente,
            // popular os campos corretos com os valores e apagar os campos das outras abas
            try
            {
                var calibration = calibrations[cmbCalibrations.SelectedIndex];
                tabCalibrations.SelectedIndex = (int)calibration.CalibrationType;

                switch (calibration.CalibrationType)
                {
                    case CalibrationTypeEnum.USGS:
                        populateUsgsCalibrationTab((UsgsCalibration)calibration);
                        clearAustralisCalibrationTab();
                        break;
                    case CalibrationTypeEnum.AUSTRALIS:
                        populateAustralisCalibrationTab((AustralisCalibration)calibration);
                        clearUsgsCalibrationTab();
                        break;
                    default:
                        break;
                }

                calibToUse = calibration;

                btnStart.IsEnabled = true;
                txtScaleFactor.IsEnabled = true;

                return;
            }
            catch (Exception)
            {
                clearUsgsCalibrationTab();
                clearAustralisCalibrationTab();

                btnStart.IsEnabled = false;

                return;
            }
        }

        private void populateUsgsCalibrationTab(UsgsCalibration calib)
        {
            txtUsgsXppa.Text = calib.Xppa.ToString();
            txtUsgsYppa.Text = calib.Yppa.ToString();
            txtUsgsK0.Text = calib.K0.ToString();
            txtUsgsK1.Text = calib.K1.ToString();
            txtUsgsK2.Text = calib.K2.ToString();
            txtUsgsK3.Text = calib.K3.ToString();
            txtUsgsK4.Text = calib.K4.ToString();
            txtUsgsP1.Text = calib.P1.ToString();
            txtUsgsP2.Text = calib.P2.ToString();
            txtUsgsP3.Text = calib.P3.ToString();
            txtUsgsP4.Text = calib.P4.ToString();
            txtUsgsF.Text = calib.F.ToString();
            txtUsgsPsx.Text = calib.Psx.ToString();
            txtUsgsPsy.Text = calib.Psy.ToString();
        }

        private void populateAustralisCalibrationTab(AustralisCalibration calib)
        {
            txtAustralisXppa.Text = calib.Xppa.ToString();
            txtAustralisYppa.Text = calib.Yppa.ToString();
            txtAustralisK0.Text = calib.K0.ToString();
            txtAustralisK1.Text = calib.K1.ToString();
            txtAustralisK2.Text = calib.K2.ToString();
            txtAustralisK3.Text = calib.K3.ToString();
            txtAustralisP1.Text = calib.P1.ToString();
            txtAustralisP2.Text = calib.P2.ToString();
            txtAustralisF.Text = calib.F.ToString();
            txtAustralisPsx.Text = calib.Psx.ToString();
            txtAustralisPsy.Text = calib.Psy.ToString();
            txtAustralisB1.Text = calib.B1.ToString();
            txtAustralisB2.Text = calib.B2.ToString();
        }

        private void clearUsgsCalibrationTab()
        {
            txtUsgsXppa.Text = "";
            txtUsgsYppa.Text = "";
            txtUsgsK0.Text = "";
            txtUsgsK1.Text = "";
            txtUsgsK2.Text = "";
            txtUsgsK3.Text = "";
            txtUsgsK4.Text = "";
            txtUsgsP1.Text = "";
            txtUsgsP2.Text = "";
            txtUsgsP3.Text = "";
            txtUsgsP4.Text = "";
            txtUsgsF.Text = "";
            txtUsgsPsx.Text = "";
            txtUsgsPsy.Text = "";
        }

        private void clearAustralisCalibrationTab()
        {
            txtAustralisXppa.Text = "";
            txtAustralisYppa.Text = "";
            txtAustralisK0.Text = "";
            txtAustralisK1.Text = "";
            txtAustralisK2.Text = "";
            txtAustralisK3.Text = "";
            txtAustralisP1.Text = "";
            txtAustralisP2.Text = "";
            txtAustralisF.Text = "";
            txtAustralisPsx.Text = "";
            txtAustralisPsy.Text = "";
            txtAustralisB1.Text = "";
            txtAustralisB2.Text = "";
        }

        private void btnLicenca_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("INI Parser - The MIT License");
            sb.AppendLine();
            sb.AppendLine("Copyright (c) 2008 Ricardo Amores Hernández");
            sb.AppendLine();
            sb.AppendLine("Permission is hereby granted, free of charge, to any person obtaining a copy of " +
                "this software and associated documentation files(the \"Software\"), to deal in " +
                "the Software without restriction, including without limitation the rights to " +
                "use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies of " +
                "the Software, and to permit persons to whom the Software is furnished to do so, " +
                "subject to the following conditions:");
            sb.AppendLine();
            sb.AppendLine("The above copyright notice and this permission notice shall be included in all " +
                "copies or substantial portions of the Software.");
            sb.AppendLine();
            sb.AppendLine("THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
                "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS " +
                "FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR " +
                "COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER " +
                "IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN " +
                "CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.");

            MessageBox.Show(sb.ToString(), "Licenças", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSobre_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Image Calibration");
            sb.AppendLine();
            sb.AppendLine("Usando INI Parser por Ricardo Amores Hernández");
            sb.AppendLine("Github: https://github.com/rickyah/ini-parser");

            MessageBox.Show(sb.ToString(), "Sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var processingConfiguration = readProcessingConfiguration();
            if (processingConfiguration == null)
            {
                return;
            }

            int totalFiles = inputFiles.Count;

            btnStart.IsEnabled = false;
            txtInputFolder.IsEnabled = false;
            btnChooseInputFolder.IsEnabled = false;
            txtOutputFolder.IsEnabled = false;
            btnChooseOutputFolder.IsEnabled = false;
            cmbCalibrations.IsEnabled = false;

            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < totalFiles; i++)
            {
                txtStatusBar.Text = "Processando " + (i + 1) + " de " + totalFiles + " imagens...";
                await Task.Run(() => calibToUse.StartProcessing(inputFiles[i], outputFolderPath, processingConfiguration));
            }
            sw.Stop();

            TimeSpan elapsed = sw.Elapsed;
            string time = elapsed.TotalMinutes.ToString() + ":" + elapsed.TotalSeconds.ToString();

            float seconds = sw.ElapsedMilliseconds / 1000f;
            int minutes;
            float average = seconds / totalFiles;
            string totalTime;
            if (seconds >= 60)
            {
                minutes = (int)seconds / 60;
                seconds = seconds - (minutes * 60);
                totalTime = minutes + "min " + seconds.ToString("0") + "s";
            }
            else
            {
                totalTime = seconds.ToString("0.00") + "s";
            }
            txtStatusBar.Text = "Processadas " + totalFiles + " imagens em " + totalTime + ". Média de " +
                average.ToString("0.00") + "s por imagem.";

            btnStart.IsEnabled = true;
            txtInputFolder.IsEnabled = true;
            btnChooseInputFolder.IsEnabled = true;
            txtOutputFolder.IsEnabled = true;
            btnChooseOutputFolder.IsEnabled = true;
            cmbCalibrations.IsEnabled = true;
        }

        private ProcessingConfiguration readProcessingConfiguration()
        {
            float scaleFactor;

            try
            {
                scaleFactor = float.Parse(txtScaleFactor.Text);
            }
            catch (Exception)
            {
                showWarning("Fator de escala inválido!");
                return null;
            }

            var processingConfiguration = new ProcessingConfiguration
            {
                ImageScale = scaleFactor
            };

            return processingConfiguration;
        }

        private void showWarning(string message)
        {
            MessageBox.Show(message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
