using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TextBox = System.Windows.Controls.TextBox;
using System.Windows.Input;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Windows.Threading;
using ImageCalibration.Calibrations;
using ImageCalibration.Enums;
using ImageCalibration.Helpers;
using ImageCalibration.Models;

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
        Stopwatch swTotalTime = new Stopwatch();
        Stopwatch swUniqueTime = new Stopwatch();

        public MainWindow()
        {
            // Força o programa a usar ponto (.) como separador decimal no lugar de vírgula (,)
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            // Start-up
            InitializeComponent();

            // Procura pelas calibrações assim que iniciar o programa. Por padrão em uma subpasta "calibs" localizada na mesma pasta do executável
            string initialCalibPath = System.AppDomain.CurrentDomain.BaseDirectory + "calibs\\";
            lookForCalibrationFiles(initialCalibPath);
        }

        private void lookForCalibrationFiles(string calibPath)
        {
            calibrations.Clear();
            List<string> calibFiles;

            try
            {
                calibFiles = Directory.EnumerateFiles(calibPath, "*.ini", SearchOption.TopDirectoryOnly).ToList();
            }
            catch (Exception)
            {
                // Se não encontrou nada, avisar e parar
                cmbCalibrations.ItemsSource = new[] { "Nenhuma calibração encontrada" }.ToList();
                cmbCalibrations.DisplayMemberPath = "";
                cmbCalibrations.SelectedIndex = 0;
                calibToUse = null;
                return;
            }

            // Se encontrou, ler cada arquivo de calibração e criar um objeto correspondente
            foreach (var calib in calibFiles)
            {
                var configIni = IniHelper.ReadIni(calib);

                if (configIni == null)
                {
                    continue;
                }

                var calibName = Path.GetFileName(calib).Replace(".ini", "");

                try
                {
                    switch (configIni.CalibrationType)
                    {
                        case CalibrationTypeEnum.USGS:
                            var usgsCalib = new UsgsCalibration(calibName,
                                configIni.Parameters["Xppa"],
                                configIni.Parameters["Yppa"],
                                configIni.Parameters["K0"],
                                configIni.Parameters["K1"],
                                configIni.Parameters["K2"],
                                configIni.Parameters["K3"],
                                configIni.Parameters["K4"],
                                configIni.Parameters["P1"],
                                configIni.Parameters["P2"],
                                configIni.Parameters["P3"],
                                configIni.Parameters["P4"],
                                configIni.Parameters["F"],
                                configIni.Parameters["Psx"],
                                configIni.Parameters["Psy"]);
                            calibrations.Add(usgsCalib);
                            break;
                        case CalibrationTypeEnum.AUSTRALIS:
                            var australisCalib = new AustralisCalibration(calibName,
                                configIni.Parameters["Xppa"],
                                configIni.Parameters["Yppa"],
                                configIni.Parameters["K0"],
                                configIni.Parameters["K1"],
                                configIni.Parameters["K2"],
                                configIni.Parameters["K3"],
                                configIni.Parameters["P1"],
                                configIni.Parameters["P2"],
                                configIni.Parameters["F"],
                                configIni.Parameters["Psx"],
                                configIni.Parameters["Psy"],
                                configIni.Parameters["B1"],
                                configIni.Parameters["B2"]);
                            calibrations.Add(australisCalib);
                            break;
                        default:
                            continue;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            if (calibrations.Count == 0)
            {
                // Se não há calibrações, avisar
                cmbCalibrations.ItemsSource = new[] { "Nenhuma calibração encontrada" }.ToList();
                cmbCalibrations.SelectedIndex = 0;
                cmbCalibrations.DisplayMemberPath = "";
                calibToUse = null;
                return;
            }
            else
            {
                // Caso contrário, popular combobox com a lista de calibração recém-criada, e mostrar o nome de cada uma delas
                cmbCalibrations.ItemsSource = calibrations;
                cmbCalibrations.DisplayMemberPath = "Name";
            }
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
                txtInputFolder.Text = dialog.SelectedPath;
                checkInputDirectory(dialog.SelectedPath);
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
                txtOutputFolder.Text = dialog.SelectedPath;
                checkOutputDirectory(dialog.SelectedPath);
            }
        }

        private bool checkInputDirectory(string path)
        {
            // Verificação checa se o diretório de entrada existe e chama outra função para contar e listar as imagens que existem
            if (Directory.Exists(path))
            {
                inputFiles.Clear();
                inputFiles = enumerateImagesInDirectory(path);
                txtStatusBar.Text = inputFiles.Count.ToString() + " arquivo(s) encontrado(s)";
                return true;
            }
            else
            {
                showWarning("Diretório de entrada inválido!");
                return false;
            }
        }

        private bool checkOutputDirectory(string path)
        {
            // Verificação apenas checa se o diretório de saíde existe e habilita os próximos controles
            if (Directory.Exists(path))
            {
                outputFolderPath = path;
                return true;
            }
            else
            {
                showWarning("Diretório de saída inválido!");
                return false;
            }
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

        private void btnChooseCalibrationFolder_Click(object sender, RoutedEventArgs e)
        {
            // Verificação se o diretório de calibração existe e procura novamente por calibrações
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                if (Directory.Exists(dialog.SelectedPath))
                {
                    lookForCalibrationFiles(dialog.SelectedPath);
                }
            }
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

        private void validateIfPositiveInteger(object sender, TextCompositionEventArgs e)
        {
            // Valida se o que foi digitado no campo de texto é um número inteiro positivo
            TextBox textBox = sender as TextBox;
            var newText = textBox.Text + e.Text;

            var regex = new Regex(@"^[0-9]*?$");

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

                return;
            }
            catch (Exception)
            {
                clearUsgsCalibrationTab();
                clearAustralisCalibrationTab();

                return;
            }
        }

        private void cmbGenerateMinis_DropDownClosed(object sender, EventArgs e)
        {
            var shouldGenerateMinis = (ComboBoxItem)cmbGenerateMinis.SelectedItem;
            switch (shouldGenerateMinis.Content)
            {
                case "Sim":
                    txtMinisFactor.IsEnabled = true;
                    txtMinisFactor.Text = "10";
                    cmbMinisBorder_DropDownClosed(sender, e);
                    break;
                case "Não":
                    txtMinisFactor.IsEnabled = false;
                    txtMinisFactor.Text = "";
                    txtBorderThickness.IsEnabled = false;
                    txtBorderThickness.Text = "";
                    break;
                default:
                    break;
            }
        }

        private void cmbMinisBorder_DropDownClosed(object sender, EventArgs e)
        {
            var shouldDrawBorder = (ComboBoxItem)cmbMinisBorder.SelectedItem;
            switch (shouldDrawBorder.Content)
            {
                case "Sim":
                    txtBorderThickness.IsEnabled = true;
                    txtBorderThickness.Text = "2";
                    break;
                case "Não":
                    txtBorderThickness.IsEnabled = false;
                    txtBorderThickness.Text = "";
                    break;
                default:
                    break;
            }
        }

        private void cmbCropImage_DropDownClosed(object sender, EventArgs e)
        {
            var shouldCrop = (ComboBoxItem)cmbCropImage.SelectedItem;
            switch (shouldCrop.Content)
            {
                case "Não":
                    txtCropHeight.IsEnabled = false;
                    txtCropHeight.Text = "";
                    txtCropWidth.IsEnabled = false;
                    txtCropWidth.Text = "";
                    break;
                case "Sim":
                    txtCropHeight.IsEnabled = true;
                    txtCropWidth.IsEnabled = true;
                    break;
                default:
                    break;
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

        private void btnSobre_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Image Calibration - Beta 4");

            MessageBox.Show(sb.ToString(), "Sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSair_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            // Verificações de todos os campos antes de iniciar o processamento
            if (!verifyAllInputs())
            {
                return;
            }

            // Ler parâmetros de processamento
            var processingConfiguration = readProcessingConfiguration();
            if (processingConfiguration == null)
            {
                return;
            }

            // Bloquear inputs e limpar variávis
            blockInputs();
            txtAverageTime.Text = "00.0";
            txtPreviousTime.Text = "00.0";
            pgrProgressBar.Value = 0;
            txtPercentageComplete.Text = "0%";

            int totalFiles = inputFiles.Count;

            // Inicia o Dispatcher Timer (para gerar eventos de atualização de UI dos cronômetros)
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);
            dispatcherTimer.Tick += mainTimer_Tick;
            dispatcherTimer.Start();

            List<float> processingTimes = new List<float>();

            // Inicia o timer principal
            swTotalTime.Restart();

            // Iniciar processamento
            for (int i = 0; i < totalFiles; i++)
            {
                txtStatusBar.Text = "Processando " + (i + 1) + " de " + totalFiles + " imagens...";
                txtCurrentFile.Text = Path.GetFileName(inputFiles[i]);

                swUniqueTime.Start();
                await Task.Run(() => calibToUse.StartProcessing(inputFiles[i], outputFolderPath, processingConfiguration));
                swUniqueTime.Stop();

                var currentFileTime = swUniqueTime.ElapsedMilliseconds / 1000f;
                txtPreviousTime.Text = currentFileTime.ToString("00.0");

                processingTimes.Add(currentFileTime);
                txtAverageTime.Text = processingTimes.Average().ToString("00.0");

                var percentageComplete = (100 * (i + 1)) / totalFiles;
                pgrProgressBar.Value = percentageComplete;
                txtPercentageComplete.Text = percentageComplete.ToString() + "%";

                swUniqueTime.Reset();
            }
            // Fim do processamento

            // Parar timers
            dispatcherTimer.Stop();
            swTotalTime.Stop();

            // Atualizar status bar
            txtStatusBar.Text = "Processadas " + totalFiles + " imagens";
            txtCurrentFile.Text = "";

            // Desbloquear inputs
            unblockInputs();
        }

        private ProcessingConfiguration readProcessingConfiguration()
        {
            // Função que lê as configurações de processamento

            // Ler formato para salvar imagem
            SaveFormatEnum saveFormat;
            var saveFormatSelected = (ComboBoxItem)cmbSaveFormat.SelectedItem;
            switch (saveFormatSelected.Content)
            {
                case "TIFF":
                    saveFormat = SaveFormatEnum.TIFF;
                    break;
                case "TIFF LZW":
                    saveFormat = SaveFormatEnum.TIFFLZW;
                    break;
                case "JPG 90%":
                    saveFormat = SaveFormatEnum.JPG90;
                    break;
                case "JPG 100%":
                    saveFormat = SaveFormatEnum.JPG100;
                    break;
                default:
                    return null;
            }

            // Lê se deve gerar as minis
            bool shouldGenerateMinis;
            var shouldGenerate = (ComboBoxItem)cmbGenerateMinis.SelectedItem;
            switch (shouldGenerate.Content)
            {
                case "Sim":
                    shouldGenerateMinis = true;
                    break;
                case "Não":
                    shouldGenerateMinis = false;
                    break;
                default:
                    return null;
            }

            // Lê o valor do fator das minis
            int minisFactor = txtMinisFactor.Text == "" ? 0 : int.Parse(txtMinisFactor.Text);

            // Lê se deve pintar a borda de preto
            bool shouldDrawBorder;
            var shouldDraw = (ComboBoxItem)cmbMinisBorder.SelectedItem;
            switch (shouldDraw.Content)
            {
                case "Sim":
                    shouldDrawBorder = true;
                    break;
                case "Não":
                    shouldDrawBorder = false;
                    break;
                default:
                    return null;
            }

            // Lê o tamanho da borda
            int borderThickness = txtBorderThickness.Text == "" ? 0 : int.Parse(txtBorderThickness.Text);

            // Ler se a imagem será rotacionada
            RotateFinalImageEnum rotateFinalImage;
            var rotateSelected = (ComboBoxItem)cmbRotateImage.SelectedItem;
            switch (rotateSelected.Content)
            {
                case "Não":
                    rotateFinalImage = RotateFinalImageEnum.NO;
                    break;
                case "90° CCW":
                    rotateFinalImage = RotateFinalImageEnum.R90CCW;
                    break;
                case "90° CW":
                    rotateFinalImage = RotateFinalImageEnum.R90CW;
                    break;
                case "180°":
                    rotateFinalImage = RotateFinalImageEnum.R180;
                    break;
                default:
                    return null;
            }

            // Ler se a imagem deverá ser cortada
            bool shouldCropImage;
            var shouldCrop = (ComboBoxItem)cmbCropImage.SelectedItem;
            switch (shouldCrop.Content)
            {
                case "Não":
                    shouldCropImage = false;
                    break;
                case "Sim":
                    shouldCropImage = true;
                    break;
                default:
                    return null;
            }

            // Ler os valores de corte
            int height = txtCropHeight.Text == "" ? 0 : int.Parse(txtCropHeight.Text);
            int width = txtCropWidth.Text == "" ? 0 : int.Parse(txtCropWidth.Text);

            // Salvar os valores lidos no objeto de configuração que será usado durante o processamento
            var processingConfiguration = new ProcessingConfiguration(saveFormat, shouldGenerateMinis, minisFactor, shouldDrawBorder, borderThickness,
                rotateFinalImage, shouldCropImage, height, width);

            return processingConfiguration;
        }

        private bool verifyAllInputs()
        {
            // Verificar diretórios
            if (!checkInputDirectory(txtInputFolder.Text) || !checkOutputDirectory(txtOutputFolder.Text))
            {
                return false; ;
            }

            // Verificar se fator da mini é válido
            var shouldGenerateMinis = (ComboBoxItem)cmbGenerateMinis.SelectedItem;
            if (((string)shouldGenerateMinis.Content == "Sim") && (txtMinisFactor.Text == ""))
            {
                showWarning("Fator de escala das minis inválido!");
                return false;
            }

            // Verificar se valor das bordas pretas é válido
            var shouldDrawBorder = (ComboBoxItem)cmbMinisBorder.SelectedItem;
            if ((cmbMinisBorder.IsEnabled) && ((string)shouldDrawBorder.Content == "Sim") && (txtBorderThickness.Text == ""))
            {
                showWarning("Tamanho da borda da mini inválido!");
                return false;
            }

            // Verificar se calibração foi selecionada
            if (calibToUse == null)
            {
                showWarning("Nenhuma calibração selecionada!");
                return false;
            }

            // Verificar se valores de corte da imagem são válidos
            var shouldCrop = (ComboBoxItem)cmbCropImage.SelectedItem;
            if (((string)shouldCrop.Content == "Sim") && ((txtCropHeight.Text == "") || (txtCropWidth.Text == "")))
            {
                showWarning("Tamanho de corte da imagem inválido!");
                return false;
            }

            return true;
        }

        private void blockInputs()
        {
            txtInputFolder.IsEnabled = false;
            btnChooseInputFolder.IsEnabled = false;
            txtOutputFolder.IsEnabled = false;
            btnChooseOutputFolder.IsEnabled = false;
            btnChooseCalibrationFolder.IsEnabled = false;
            cmbSaveFormat.IsEnabled = false;
            cmbGenerateMinis.IsEnabled = false;
            txtMinisFactor.IsEnabled = false;
            cmbMinisBorder.IsEnabled = false;
            txtBorderThickness.IsEnabled = false;
            cmbCalibrations.IsEnabled = false;
            cmbRotateImage.IsEnabled = false;
            cmbCropImage.IsEnabled = false;
            txtCropHeight.IsEnabled = false;
            txtCropWidth.IsEnabled = false;
            btnStart.IsEnabled = false;
        }

        private void unblockInputs()
        {
            txtInputFolder.IsEnabled = true;
            btnChooseInputFolder.IsEnabled = true;
            txtOutputFolder.IsEnabled = true;
            btnChooseOutputFolder.IsEnabled = true;
            cmbSaveFormat.IsEnabled = true;
            cmbGenerateMinis.IsEnabled = true;
            cmbCalibrations.IsEnabled = true;
            btnChooseCalibrationFolder.IsEnabled = true;
            cmbRotateImage.IsEnabled = true;
            cmbCropImage.IsEnabled = true;
            btnStart.IsEnabled = true;
            if ((string)(((ComboBoxItem)cmbCropImage.SelectedItem).Content) == "Sim")
            {
                txtCropHeight.IsEnabled = true;
                txtCropWidth.IsEnabled = true;
            }
            if ((string)(((ComboBoxItem)cmbGenerateMinis.SelectedItem).Content) == "Sim")
            {
                txtMinisFactor.IsEnabled = true;
                cmbMinisBorder.IsEnabled = true;
                if ((string)(((ComboBoxItem)cmbMinisBorder.SelectedItem).Content) == "Sim")
                {
                    txtBorderThickness.IsEnabled = true;
                }
            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            // Update do timer principal
            float seconds = swTotalTime.ElapsedMilliseconds / 1000f;
            int minutes;
            int hours;
            string totalTime;
            if (seconds >= 60)
            {
                minutes = (int)seconds / 60;

                if (minutes >= 60)
                {
                    hours = (int)minutes / 60;
                    minutes = minutes - (hours * 60);
                    seconds = seconds - (hours * 60 * 60) - (minutes * 60);
                    totalTime = hours + ":" + checkLeadingZero(minutes) + ":" + seconds.ToString("00.0");
                }
                else
                {
                    seconds = seconds - (minutes * 60);
                    totalTime = "0:" + checkLeadingZero(minutes) + ":" + seconds.ToString("00.0");
                }
            }
            else
            {
                totalTime = "0:00:" + seconds.ToString("00.0");
            }
            txtTotalTime.Text = totalTime;

            // Update do timer individual
            seconds = swUniqueTime.ElapsedMilliseconds / 1000f;
            txtCurrentTime.Text = seconds.ToString("00.0");
        }

        private string checkLeadingZero(int number)
        {
            if (number < 10)
            {
                return "0" + number;
            }
            return number.ToString();
        }

        private void showWarning(string message)
        {
            MessageBox.Show(message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
