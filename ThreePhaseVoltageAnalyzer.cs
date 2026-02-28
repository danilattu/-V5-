using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Wisp.Comtrade;
using Wisp.Comtrade.Models;

namespace VoltageAnalyzer
{
    /// <summary>
    /// Class for analyzing three-phase voltages from COMTRADE format files.
    /// </summary>
    public class ThreePhaseVoltageAnalyzer
    {
        // Constant parameters
        private const double TWO_PI = 2 * Math.PI;

        // Input parameters
        private RecordReader _comtradeData;
        private ConfigurationHandler configuration;

        private int _phaseAIndex;
        private int _phaseBIndex;
        private int _phaseCIndex;
        private double _nominalFrequency;

        public List<DateTime> TimeStampsOsc { get; private set; }
        public List<double> PhaseA { get; private set; }
        public List<double> PhaseB { get; private set; }
        public List<double> PhaseC { get; private set; }

        public ThreePhaseVoltageAnalyzer(string comtradeFilePath)
        {
            // Loading configuration from COMTRADE file
            configuration = new ConfigurationHandler(comtradeFilePath);

            // Loading data from the COMTRADE file
            _comtradeData = new RecordReader(comtradeFilePath);

            // Automatic determination of phase voltage indices
            FindPhaseVoltageIndices();

            // Getting the sampling frequency from the file (fallback to 1 Hz)
            _nominalFrequency = configuration.Frequency > 0 ? configuration.Frequency : 1.0;

            // Initialize lists for storing oscilloscopes
            InitializeOscLists();

            // Reading voltages with oscilloscopes
            ReadAllVoltages();
        }

        /// <summary>
        /// Clearing previous data
        /// </summary>
        private void ClearResults()
        {
            if (TimeStampsOsc == null) TimeStampsOsc = new List<DateTime>();
            else TimeStampsOsc.Clear();

            if (PhaseA == null) PhaseA = new List<double>();
            else PhaseA.Clear();

            if (PhaseB == null) PhaseB = new List<double>();
            else PhaseB.Clear();

            if (PhaseC == null) PhaseC = new List<double>();
            else PhaseC.Clear();
        }

        public void ReadAllVoltages()
        {
            ClearResults();

            // Получаем списки значений (предполагается, что методы возвращают перечисляемые значения)
            PhaseA = _comtradeData.GetAnalogPrimaryChannel(_phaseAIndex).ToList();
            PhaseB = _comtradeData.GetAnalogPrimaryChannel(_phaseBIndex).ToList();
            PhaseC = _comtradeData.GetAnalogPrimaryChannel(_phaseCIndex).ToList();

            // Вычисляем временные метки. Используем _nominalFrequency как частоту дискретизации (если это не так, замените на корректное значение из API)
            double sampleIntervalSeconds = _nominalFrequency > 0 ? 1.0 / _nominalFrequency : 1.0;

            DateTime start = configuration.StartTime;

            int count = Math.Max(PhaseA.Count, Math.Max(PhaseB.Count, PhaseC.Count));
            for (int i = 0; i < count; i++)
            {
                DateTime timestamp = start.AddSeconds(i * sampleIntervalSeconds);
                TimeStampsOsc.Add(timestamp);
            }
        }

        /// <summary>
        /// Initialization of lists for storing voltage oscillograms
        /// </summary>
        private void InitializeOscLists()
        {
            TimeStampsOsc = new List<DateTime>();
            PhaseA = new List<double>();
            PhaseB = new List<double>();
            PhaseC = new List<double>();
        }

        private void FindPhaseVoltageIndices()
        {
            // List of possible phase voltage channels
            List<PhaseVoltageCandidate> phaseVoltageCandidates = new List<PhaseVoltageCandidate>();

            // Regular expressions for finding phase designations
            var phaseARegex = new Regex(@"(?:UA|VA|U[_\s]*A|V[_\s]*A|Ua|Va|Phase[_\s]*A)", RegexOptions.IgnoreCase);
            var phaseBRegex = new Regex(@"(?:UB|VB|U[_\s]*B|V[_\s]*B|Ub|Vb|Phase[_\s]*B)", RegexOptions.IgnoreCase);
            var phaseCRegex = new Regex(@"(?:UC|VC|U[_\s]*C|V[_\s]*C|Uc|Vc|Phase[_\s]*C)", RegexOptions.IgnoreCase);

            // Loop through all analog channels
            for (int i = 0; i < configuration.AnalogChannelsCount; i++)
            {
                var channel = configuration.AnalogChannelInformationList[i];

                // Check units of measurement (must be volts for voltage)
                bool isVoltage = !string.IsNullOrEmpty(channel.Units) &&
                                 (channel.Units.ToLower().Contains("v") || channel.Units.ToLower().Contains("volt"));

                if (!isVoltage)
                    continue;

                // Check the channel name for phase compliance
                string channelName = channel.Name ?? string.Empty;
                PhaseType detectedPhase = PhaseType.Unknown;

                if (phaseARegex.IsMatch(channelName))
                {
                    detectedPhase = PhaseType.PhaseA;
                }
                else if (phaseBRegex.IsMatch(channelName))
                {
                    detectedPhase = PhaseType.PhaseB;
                }
                else if (phaseCRegex.IsMatch(channelName))
                {
                    detectedPhase = PhaseType.PhaseC;
                }

                // If the phase is recognized, add the candidate
                if (detectedPhase != PhaseType.Unknown)
                {
                    phaseVoltageCandidates.Add(new PhaseVoltageCandidate
                    {
                        ChannelIndex = i,
                        PhaseType = detectedPhase,
                        ChannelName = channelName
                    });
                }
            }

            // If we haven't found any phases by name, we'll try to determine them by order and units of measurement
            if (phaseVoltageCandidates.Count == 0)
            {
                // Find all channels with units of measurement in volts
                var voltageChannels = new List<int>();
                for (int i = 0; i < configuration.AnalogChannelsCount; i++)
                {
                    var channel = configuration.AnalogChannelInformationList[i];
                    bool isVoltage = !string.IsNullOrEmpty(channel.Units) &&
                                     (channel.Units.ToLower().Contains("v") || channel.Units.ToLower().Contains("volt"));
                    if (isVoltage) voltageChannels.Add(i);
                }

                // We are looking for sequential triplets of voltage channels
                if (voltageChannels.Count >= 3)
                {
                    phaseVoltageCandidates.Add(new PhaseVoltageCandidate
                    {
                        ChannelIndex = voltageChannels[0],
                        PhaseType = PhaseType.PhaseA,
                        ChannelName = configuration.AnalogChannelInformationList[voltageChannels[0]].Name
                    });
                    phaseVoltageCandidates.Add(new PhaseVoltageCandidate
                    {
                        ChannelIndex = voltageChannels[1],
                        PhaseType = PhaseType.PhaseB,
                        ChannelName = configuration.AnalogChannelInformationList[voltageChannels[1]].Name
                    });
                    phaseVoltageCandidates.Add(new PhaseVoltageCandidate
                    {
                        ChannelIndex = voltageChannels[2],
                        PhaseType = PhaseType.PhaseC,
                        ChannelName = configuration.AnalogChannelInformationList[voltageChannels[2]].Name
                    });
                }
            }

            // Check if we have found all three phases
            var phaseA = phaseVoltageCandidates.FirstOrDefault(c => c.PhaseType == PhaseType.PhaseA);
            var phaseB = phaseVoltageCandidates.FirstOrDefault(c => c.PhaseType == PhaseType.PhaseB);
            var phaseC = phaseVoltageCandidates.FirstOrDefault(c => c.PhaseType == PhaseType.PhaseC);

            if (phaseA != null && phaseB != null && phaseC != null)
            {
                _phaseAIndex = phaseA.ChannelIndex;
                _phaseBIndex = phaseB.ChannelIndex;
                _phaseCIndex = phaseC.ChannelIndex;
            }
            else
            {
                throw new Exception("Failed to automatically determine the indices of all three phase voltages. Please specify the phase indices explicitly.");
            }
        }

        /// <summary>
        /// Возвращает информацию о найденных фазных каналах (для отображения в UI)
        /// </summary>
        public string GetPhaseIndicesInfo()
        {
            return $"Phase indices: A={_phaseAIndex}, B={_phaseBIndex}, C={_phaseCIndex}";
        }
    }

    class PhaseVoltageCandidate
    {
        public int ChannelIndex { get; set; }
        public PhaseType PhaseType { get; set; }
        public string ChannelName { get; set; }
    }

    enum PhaseType
    {
        Unknown,
        PhaseA,
        PhaseB,
        PhaseC
    }
}

