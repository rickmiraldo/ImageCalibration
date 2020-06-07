﻿using System;
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
using System.Drawing;
using System.Diagnostics;

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
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US"); // Força o programa a usar ponto (.) como separador decimal no lugar de vírgula (,)
            InitializeComponent();

            lookForCalibrationFiles();
        }

        private void lookForCalibrationFiles()
        {
            // Definir os tipos de calibração
            const string traditionalCalib = "Tradicional";
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
            MessageBox.Show("Diretório inválido!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);

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

        private void cmbCalibrations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ao mudar a seleção do combobox de calibração, mudar para a aba correspondente, popular os campos corretos com os valores e apagar os campos das outras abas
            try
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

                    txtAustralisXppa.Text = "";
                    txtAustralisYppa.Text = "";
                    txtAustralisK1.Text = "";
                    txtAustralisK2.Text = "";
                    txtAustralisK3.Text = "";
                    txtAustralisP1.Text = "";
                    txtAustralisP2.Text = "";
                    txtAustralisF.Text = "";
                    txtAustralisPs.Text = "";
                    txtAustralisPsx.Text = "";
                    txtAustralisPsy.Text = "";
                    txtAustralisB1.Text = "";
                    txtAustralisB2.Text = "";
                }

                else if (calibration.CalibrationType == CalibrationTypeEnum.AUSTRALIS)
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

                    txtTraditionalXppa.Text = "";
                    txtTraditionalYppa.Text = "";
                    txtTraditionalK1.Text = "";
                    txtTraditionalK2.Text = "";
                    txtTraditionalK3.Text = "";
                    txtTraditionalP1.Text = "";
                    txtTraditionalP2.Text = "";
                    txtTraditionalF.Text = "";
                    txtTraditionalPs.Text = "";
                    txtTraditionalPsy.Text = "";
                }

                calibToUse = calibration;

                btnStart.IsEnabled = true;

                return;
            }
            catch (Exception)
            {
                txtTraditionalXppa.Text = "";
                txtTraditionalYppa.Text = "";
                txtTraditionalK1.Text = "";
                txtTraditionalK2.Text = "";
                txtTraditionalK3.Text = "";
                txtTraditionalP1.Text = "";
                txtTraditionalP2.Text = "";
                txtTraditionalF.Text = "";
                txtTraditionalPs.Text = "";
                txtTraditionalPsy.Text = "";

                txtAustralisXppa.Text = "";
                txtAustralisYppa.Text = "";
                txtAustralisK1.Text = "";
                txtAustralisK2.Text = "";
                txtAustralisK3.Text = "";
                txtAustralisP1.Text = "";
                txtAustralisP2.Text = "";
                txtAustralisF.Text = "";
                txtAustralisPs.Text = "";
                txtAustralisPsx.Text = "";
                txtAustralisPsy.Text = "";
                txtAustralisB1.Text = "";
                txtAustralisB2.Text = "";

                btnStart.IsEnabled = false;

                return;
            }
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

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            int totalFiles = inputFiles.Count;

            Stopwatch sw = new Stopwatch();

            sw.Start();
            for (int i = 0; i < totalFiles; i++)
            {
                await Task.Run(() => calibToUse.StartProcessingAsync(inputFiles[i], outputFolderPath));
                txtStatusBar.Text = "Processando " + (i + 1) + " de " + totalFiles + " arquivos...";
            }
            txtStatusBar.Text = "Processadas " + totalFiles + " imagens!";
            sw.Stop();

            MessageBox.Show("Tempo: " + (sw.ElapsedMilliseconds/1000).ToString() + " segundos");
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRun_TestRunClick(object sender, RoutedEventArgs e)
        {
            TraditionalCalibration c = new TraditionalCalibration("Test", -0.1685, -0.0419, -0.00002373, 0.000000009935, 0.000000000001, 0.000001724, 0.000001249, 55.1247, 0.0052, -0.0052);
            string file1 = "C:\\Users\\rickm\\Desktop\\teste\\real\\TIF_8.tif";
            string file2 = "C:\\Users\\rickm\\Desktop\\teste\\real\\JPG.jpg";
            List<string> files = new List<string>();
            files.Add(file1);
            files.Add(file2);
            string output = "C:\\Users\\rickm\\Desktop\\teste\\real\\out";

            calibToUse = c;
            inputFiles = files;
            outputFolderPath = output;

            btnStart_Click(sender, e);
        }
    }
}
