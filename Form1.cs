using ScottPlot;
using System;
using System.IO;
using System.Windows.Forms;
using VoltageAnalyzer;

namespace ПРИЛОЖЕНИЕ_V5_Задание_4_Модуль_3                              
{
    public partial class Form1 : Form
    {
        private ThreePhaseVoltageAnalyzer voltageAnalyzer;

        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "COMTRADE files (*.cfg)|*.cfg|All (*.*)|*.*";
                openFileDialog.Title = "Open COMTRADE File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        LoadComtradeFile(openFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening file: {ex.Message}", "Error",
                         MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadComtradeFile(string filePath)
        {
            // Убираем обращения к несуществующим контролам statusLabel / InfoToolStrip*
            // Обновляем заголовок формы и выводим краткую информацию через MessageBox
            this.Text = $"Loading: {Path.GetFileName(filePath)}";
            Application.DoEvents();

            try
            {
                voltageAnalyzer = new ThreePhaseVoltageAnalyzer(filePath);

                this.Text = $"Oscilloscope: {Path.GetFileName(filePath)}";

                // Метод реализован в ThreePhaseVoltageAnalyzer (GetPhaseIndicesInfo)
                string phaseInfo = voltageAnalyzer.GetPhaseIndicesInfo();

                // Проверяем наличие хотя бы одного временного штампа
                if (voltageAnalyzer.TimeStampsOsc != null && voltageAnalyzer.TimeStampsOsc.Count > 0)
                {
                    MessageBox.Show($"Start time: {voltageAnalyzer.TimeStampsOsc[0]} | {phaseInfo}", "File loaded",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"{phaseInfo}", "File loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                PlotVoltageData(); // вызываем параметрless версию
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing COMTRADE file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Сделал параметрless метод, т.к. он вызывается без аргументов
        private void PlotVoltageData()
        {
            if (voltageAnalyzer == null)
                return;

            OscPlot.Plot.Clear();

            int dataPoints = this.voltageAnalyzer.PhaseA?.Count ?? 0;
            if (dataPoints == 0)
            {
                OscPlot.Refresh();
                return;
            }

            double[] timeSeconds = new double[dataPoints];
            DateTime startTime = voltageAnalyzer.TimeStampsOsc[0];

            double[] phaseA = new double[dataPoints];
            double[] phaseB = new double[dataPoints];
            double[] phaseC = new double[dataPoints];

            for (int i = 0; i < dataPoints; i++)
            {
                timeSeconds[i] = (voltageAnalyzer.TimeStampsOsc[i] - startTime).TotalSeconds;

                phaseA[i] = voltageAnalyzer.PhaseA[i];
                phaseB[i] = voltageAnalyzer.PhaseB[i];
                phaseC[i] = voltageAnalyzer.PhaseC[i];
            }

            var phA = OscPlot.Plot.Add.SignalXY(timeSeconds, phaseA);
            phA.LegendText = "Ua";

            var phB = OscPlot.Plot.Add.SignalXY(timeSeconds, phaseB);
            phB.LegendText = "Ub";

            var phC = OscPlot.Plot.Add.SignalXY(timeSeconds, phaseC);
            phC.LegendText = "Uc";

            OscPlot.Plot.Axes.AutoScale();
            OscPlot.Plot.ShowLegend(ScottPlot.Alignment.UpperRight);
            OscPlot.Refresh();
        }

        private void файлToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newvar = new NewClass();
            newvar.Name = "";
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void оПрограммеToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show(
               "Автор: Даниил Латту. Дата: 28.02.2026",
               "О программе",
               MessageBoxButtons.OK,
               MessageBoxIcon.Information);
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
