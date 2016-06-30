﻿// Copyright 2012, 2013, 2014 Derek J. Bailey
// Modified work Copyright 2016 Stefan Solntsev
// 
// This file (ThermoRawFile.cs) is part of CSMSL.
// 
// CSMSL is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// CSMSL is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
// License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with CSMSL. If not, see <http://www.gnu.org/licenses/>.

using MassSpectrometry;
using MSFileReaderLib;
using Spectra;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IO.Thermo
{
    public class ThermoRawFile : MsDataFile<ThermoSpectrum>
    {
        internal enum RawLabelDataColumn
        {
            MZ = 0,
            Intensity = 1,
            Resolution = 2,
            NoiseBaseline = 3,
            NoiseLevel = 4,
            Charge = 5
        }

        private enum ThermoMzAnalyzer
        {
            None = -1,
            ITMS = 0,
            TQMS = 1,
            SQMS = 2,
            TOFMS = 3,
            FTMS = 4,
            Sector = 5
        }

        public enum Smoothing
        {
            None = 0,
            Boxcar = 1,
            Gauusian = 2
        }

        public enum IntensityCutoffType
        {
            None = 0,
            Absolute = 1,
            Relative = 2
        };

        private IXRawfile5 _rawConnection;

        public ThermoRawFile(string filePath)
            : base(filePath, true, MsDataFileType.ThermoRawFile)
        {
        }

        public static bool AlwaysGetUnlabeledData = false;

        public override void Open()
        {
            if (_rawConnection != null)
                return;
            if (!File.Exists(FilePath) && !Directory.Exists(FilePath))
            {
                throw new IOException(string.Format("The MS data file {0} does not currently exist", FilePath));
            }

            _rawConnection = (IXRawfile5)new MSFileReader_XRawfile();
            _rawConnection.Open(FilePath);
            _rawConnection.SetCurrentController(0, 1); // first 0 is for mass spectrometer
        }


        protected override int GetFirstSpectrumNumber()
        {
            int spectrumNumber = 0;
            _rawConnection.GetFirstSpectrumNumber(ref spectrumNumber);
            return spectrumNumber;
        }

        protected override int GetLastSpectrumNumber()
        {
            int spectrumNumber = 0;
            _rawConnection.GetLastSpectrumNumber(ref spectrumNumber);
            return spectrumNumber;
        }

        private double GetRetentionTime(int spectrumNumber)
        {
            double retentionTime = 0;
            _rawConnection.RTFromScanNum(spectrumNumber, ref retentionTime);
            return retentionTime;
        }

        private int GetMsnOrder(int spectrumNumber)
        {
            int msnOrder = 0;
            _rawConnection.GetMSOrderForScanNum(spectrumNumber, ref msnOrder);
            return msnOrder;
        }

        public override int GetParentSpectrumNumber(int spectrumNumber)
        {
            return Convert.ToInt32(Regex.Match(GetPrecursorID(spectrumNumber), @"\d+$").Value);
        }

        public string GetSofwareVersion()
        {
            string softwareVersion = null;
            _rawConnection.GetInstSoftwareVersion(ref softwareVersion);
            return softwareVersion;
        }

        public double[,] GetChro(string scanFilter, MzRange range, double startTime, double endTime, Smoothing smoothing = Smoothing.None, int smoothingPoints = 3)
        {
            object chro = null;
            object flags = null;
            int size = 0;
            string mzrange = range.Minimum.ToString("F4") + "-" + range.Maximum.ToString("F4");
            _rawConnection.GetChroData(0, 0, 0, scanFilter, mzrange, string.Empty, 0.0, startTime, endTime, (int)smoothing, smoothingPoints, ref chro, ref flags, ref size);
            return (double[,])chro;
        }

        private object GetExtraValue(int spectrumNumber, string filter)
        {
            object value = null;
            _rawConnection.GetTrailerExtraValueForScanNum(spectrumNumber, filter, ref value);
            return value;
        }

        private string GetScanFilter(int spectrumNumber)
        {
            string filter = null;
            _rawConnection.GetFilterForScanNum(spectrumNumber, ref filter);
            return filter;
        }


        private string GetSpectrumID(int spectrumNumber)
        {
            int pnControllerType = 0;
            int pnControllerNumber = 0;
            _rawConnection.GetCurrentController(ref pnControllerType, ref pnControllerNumber);
            return "controllerType=" + pnControllerType + " controllerNumber=" + pnControllerNumber + " scan=" + spectrumNumber;
        }


        private static readonly Regex PolarityRegex = new Regex(@"\+ ", RegexOptions.Compiled);

        private Polarity GetPolarity(int spectrumNumber)
        {
            string filter = GetScanFilter(spectrumNumber);
            return PolarityRegex.IsMatch(filter) ? Polarity.Positive : Polarity.Negative;
        }

        protected ThermoSpectrum GetSpectrumFromRawFile(int spectrumNumber, bool profileIfAvailable = false)
        {
            try
            {
                return new ThermoSpectrum(GetLabeledData(spectrumNumber));
            }
            catch (ArgumentNullException)
            {
                return new ThermoSpectrum(GetUnlabeledData(spectrumNumber, true));
            }
        }

        public ThermoSpectrum GetLabeledSpectrum(int spectrumNumber)
        {
            var labelData = GetLabeledData(spectrumNumber);
            return new ThermoSpectrum(labelData);
        }

        private double[,] GetUnlabeledData(int spectrumNumber, bool useCentroid)
        {
            object massList = null;
            object peakFlags = null;
            int arraySize = -1;
            double centroidPeakWidth = 0.001;
            _rawConnection.GetMassListFromScanNum(ref spectrumNumber, null, 0, 0, 0, Convert.ToInt32(useCentroid), ref centroidPeakWidth, ref massList, ref peakFlags, ref arraySize);
            return (double[,])massList;
        }

        private double[,] GetLabeledData(int spectrumNumber)
        {
            object labels = null;
            object flags = null;
            _rawConnection.GetLabelData(ref labels, ref flags, ref spectrumNumber);
            double[,] data = labels as double[,];
            if (data == null || data.Length == 0)
                throw new ArgumentNullException("For spectrum number " + spectrumNumber + " the data is null!");
            return data;
        }

        private MZAnalyzerType GetMzAnalyzer(int spectrumNumber)
        {
            int mzanalyzer = 0;
            _rawConnection.GetMassAnalyzerTypeForScanNum(spectrumNumber, ref mzanalyzer);

            switch ((ThermoMzAnalyzer)mzanalyzer)
            {
                case ThermoMzAnalyzer.FTMS:
                    return MZAnalyzerType.Orbitrap;
                case ThermoMzAnalyzer.ITMS:
                    return MZAnalyzerType.IonTrap2D;
                case ThermoMzAnalyzer.Sector:
                    return MZAnalyzerType.Sector;
                case ThermoMzAnalyzer.TOFMS:
                    return MZAnalyzerType.TOF;
                default:
                    return MZAnalyzerType.Unknown;
            }
        }

        private double GetPrecursorMonoisotopicMz(int spectrumNumber)
        {
            return RefinePrecusorMz(spectrumNumber, GetMonoisotopicMZ(_rawConnection, spectrumNumber));
        }

        private static double GetMonoisotopicMZ(IXRawfile2 raw, int scanNumber)
        {
            object labels_obj = null;
            object values_obj = null;
            int array_size = -1;
            raw.GetTrailerExtraForScanNum(scanNumber, ref labels_obj, ref values_obj, ref array_size);
            string[] labels = (string[])labels_obj;
            string[] values = (string[])values_obj;
            for (int i = labels.GetLowerBound(0); i <= labels.GetUpperBound(0); i++)
            {
                if (labels[i].StartsWith("Monoisotopic M/Z"))
                {
                    double monoisotopic_mz = double.Parse(values[i], CultureInfo.InvariantCulture);
                    if (monoisotopic_mz > 0.0)
                    {
                        return monoisotopic_mz;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return -1;
        }


        private double RefinePrecusorMz(int spectrumNumber, double searchMZ)
        {
            int parentScanNumber = GetParentSpectrumNumber(spectrumNumber);
            var ms1Scan = GetScan(parentScanNumber).MassSpectrum;
            MzPeak peak = ms1Scan.GetClosestPeak(DoubleRange.FromDa(searchMZ, 50));
            if (peak != null)
                return peak.MZ;
            return double.NaN;
        }

        private double GetIsolationWidth(int spectrumNumber)
        {
            object width = GetExtraValue(spectrumNumber, "MS2 Isolation Width:");
            return Convert.ToDouble(width);
        }

        public double GetElapsedScanTime(int spectrumNumber)
        {
            object elapsedScanTime = GetExtraValue(spectrumNumber, "Elapsed Scan Time (sec):");
            return Convert.ToDouble(elapsedScanTime);
        }

        public double GetTIC(int spectrumNumber)
        {
            int numberOfPackets = -1;
            double startTime = double.NaN;
            double lowMass = double.NaN;
            double highMass = double.NaN;
            double totalIonCurrent = double.NaN;
            double basePeakMass = double.NaN;
            double basePeakIntensity = double.NaN;
            int numberOfChannels = -1;
            int uniformTime = -1;
            double frequency = double.NaN;
            _rawConnection.GetScanHeaderInfoForScanNum(spectrumNumber, ref numberOfPackets, ref startTime, ref lowMass,
                ref highMass,
                ref totalIonCurrent, ref basePeakMass, ref basePeakIntensity,
                ref numberOfChannels, ref uniformTime, ref frequency);

            return totalIonCurrent;
        }

        private DissociationType GetDissociationType(int spectrumNumber, int msnOrder = 2)
        {
            int type = 0;
            _rawConnection.GetActivationTypeForScanNum(spectrumNumber, msnOrder, ref type);
            return (DissociationType)type;
        }

        private MzRange GetMzRange(int spectrumNumber)
        {
            int numberOfPackets = -1;
            double startTime = double.NaN;
            double lowMass = double.NaN;
            double highMass = double.NaN;
            double totalIonCurrent = double.NaN;
            double basePeakMass = double.NaN;
            double basePeakIntensity = double.NaN;
            int numberOfChannels = -1;
            int uniformTime = -1;
            double frequency = double.NaN;
            _rawConnection.GetScanHeaderInfoForScanNum(spectrumNumber, ref numberOfPackets, ref startTime, ref lowMass,
                ref highMass,
                ref totalIonCurrent, ref basePeakMass, ref basePeakIntensity,
                ref numberOfChannels, ref uniformTime, ref frequency);

            return new MzRange(lowMass, highMass);
        }

        private int GetPrecusorCharge(int spectrumNumber, int msnOrder = 2)
        {
            short charge = Convert.ToInt16(GetExtraValue(spectrumNumber, "Charge State:"));
            return charge * (int)GetPolarity(spectrumNumber);
        }

        public override int GetSpectrumNumber(double retentionTime)
        {
            int spectrumNumber = 0;
            _rawConnection.ScanNumFromRT(retentionTime, ref spectrumNumber);
            return spectrumNumber;
        }

        private double GetInjectionTime(int spectrumNumber)
        {
            object time = GetExtraValue(spectrumNumber, "Ion Injection Time (ms):");
            return Convert.ToDouble(time);
        }

        public string GetInstrumentName()
        {
            string name = null;
            _rawConnection.GetInstName(ref name);
            return name;
        }

        public string GetInstrumentModel()
        {
            string model = null;
            _rawConnection.GetInstModel(ref model);
            return model;
        }

        private static Regex _etdReactTimeRegex = new Regex(@"@etd(\d+).(\d+)(\d+)", RegexOptions.Compiled);

        public double GetETDReactionTime(int spectrumNumber)
        {
            string scanheader = GetScanFilter(spectrumNumber);
            Match m = _etdReactTimeRegex.Match(scanheader);
            if (m.Success)
            {
                string etdTime = m.ToString();
                string Time = etdTime.Remove(0, 4);
                double reactTime = double.Parse(Time);
                return reactTime;
            }
            return double.NaN;
        }

        public Chromatogram GetTICChroma()
        {
            int nChroType1 = 1; //1=TIC 0=MassRange
            int nChroOperator = 0;
            int nChroType2 = 0;
            string bstrFilter = null;
            string bstrMassRanges1 = null;
            string bstrMassRanges2 = null;
            double dDelay = 0.0;
            double dStartTime = 0.0;
            double dEndTime = 0.0;
            int nSmoothingType = 1; //0=None 1=Boxcar 2=Gaussian
            int nSmoothingValue = 7;
            object pvarChroData = null;
            object pvarPeakFlags = null;
            int pnArraySize = 0;

            //(int nChroType1, int nChroOperator, int nChroType2, string bstrFilter, string bstrMassRanges1, string bstrMassRanges2, double dDelay, ref double pdStartTime, 
            //ref double pdEndTime, int nSmoothingType, int nSmoothingValue, ref object pvarChroData, ref object pvarPeakFlags, ref int pnArraySize);
            _rawConnection.GetChroData(nChroType1, nChroOperator, nChroType2, bstrFilter, bstrMassRanges1, bstrMassRanges2, dDelay, dStartTime, dEndTime, nSmoothingType, nSmoothingValue, ref pvarChroData, ref pvarPeakFlags, ref pnArraySize);

            double[,] pvarArray = (double[,])pvarChroData;

            return new Chromatogram(pvarArray);
        }

        private readonly static Regex _msxRegex = new Regex(@"([\d.]+)@", RegexOptions.Compiled);

        public List<double> GetMSXPrecursors(int spectrumNumber)
        {
            string scanheader = GetScanFilter(spectrumNumber);

            int msxnumber = -1;
            _rawConnection.GetMSXMultiplexValueFromScanNum(spectrumNumber, ref msxnumber);

            var matches = _msxRegex.Matches(scanheader);

            return (from Match match in matches select double.Parse(match.Groups[1].Value)).ToList();
        }

        private bool GetIsCentroid(int spectrumNumber)
        {
            int isCentroid = -1;
            _rawConnection.IsCentroidScanForScanNum(spectrumNumber, ref isCentroid);
            return isCentroid > 0;
        }

        private string GetPrecursorID(int spectrumNumber)
        {
            return GetSpectrumID(GetPrecursor(spectrumNumber));
        }

        private double GetPrecursorIsolationIntensity(int scanNumber)
        {
            double mz = -1;
            _rawConnection.GetPrecursorMassForScanNum(scanNumber, 2, ref mz);
            return GetScan(GetPrecursor(scanNumber)).MassSpectrum.GetClosestPeak(mz).Intensity;
        }



        private int GetPrecursor(int spectrumNumber)
        {
            int ms_order = -1;
            while (spectrumNumber >= 1)
            {
                _rawConnection.GetMSOrderForScanNum(spectrumNumber, ref ms_order);
                if (ms_order == 1)
                    return spectrumNumber;
                spectrumNumber--;
            }
            return spectrumNumber;
        }

        protected override MsDataScan<ThermoSpectrum> GetMsDataScanFromFile(int spectrumNumber)
        {
            var precursorID = GetPrecursorID(spectrumNumber);

            int numberOfPackets = -1;
            double startTime = double.NaN;
            double lowMass = double.NaN;
            double highMass = double.NaN;
            double totalIonCurrent = double.NaN;
            double basePeakMass = double.NaN;
            double basePeakIntensity = double.NaN;
            int numberOfChannels = -1;
            int uniformTime = -1;
            double frequency = double.NaN;
            _rawConnection.GetScanHeaderInfoForScanNum(spectrumNumber, ref numberOfPackets, ref startTime, ref lowMass,
                ref highMass, ref totalIonCurrent, ref basePeakMass, ref basePeakIntensity,
                ref numberOfChannels, ref uniformTime, ref frequency);
            MzRange mzRange = new MzRange(lowMass, highMass);

            if (precursorID.Equals(GetSpectrumID(spectrumNumber)))
                return new MsDataScan<ThermoSpectrum>(spectrumNumber, GetSpectrumFromRawFile(spectrumNumber), GetSpectrumID(spectrumNumber), GetMsnOrder(spectrumNumber), GetIsCentroid(spectrumNumber), GetPolarity(spectrumNumber), GetRetentionTime(spectrumNumber), mzRange, GetScanFilter(spectrumNumber));
            else
                return new MsDataScan<ThermoSpectrum>(spectrumNumber, GetSpectrumFromRawFile(spectrumNumber), GetSpectrumID(spectrumNumber), GetMsnOrder(spectrumNumber), GetIsCentroid(spectrumNumber), GetPolarity(spectrumNumber), GetRetentionTime(spectrumNumber), mzRange, GetScanFilter(spectrumNumber), precursorID, GetPrecursorMonoisotopicMz(spectrumNumber), GetPrecusorCharge(spectrumNumber), GetPrecursorIsolationIntensity(spectrumNumber), GetIsolationMZ(spectrumNumber), GetIsolationWidth(spectrumNumber), GetDissociationType(spectrumNumber), GetParentSpectrumNumber(spectrumNumber));
        }

        private double GetIsolationMZ(int spectrumNumber)
        {
            return GetMonoisotopicMZ(_rawConnection, spectrumNumber);
        }
    }
}