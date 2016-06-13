// Copyright 2012, 2013, 2014 Derek J. Bailey
// Modified work Copyright 2016 Stefan Solntsev
// 
// This file (Mzml.cs) is part of CSMSL.
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

using Ionic.Zlib;
using MassSpectrometry;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace IO.MzML
{
    public class Mzml : MsDataFile<DefaultMzSpectrum>
    {
        private static string _msnOrderAccession = "MS:1000511";
        private static string _precursorCharge = "MS:1000041";
        private static string _lowestObservedMZ = "MS:1000528";
        private static string _highestObservedMZ = "MS:1000527";
        private static string _precursorMass = "MS:1000744";
        private static string _isolationWindowLowerOffset = "MS:1000828";
        private static string _isolationWindowUpperOffset = "MS:1000829";
        private static string _retentionTime = "MS:1000016";
        private static string _ionInjectionTime = "MS:1000927";
        private static string _mzArray = "MS:1000514";
        private static string _intensityArray = "MS:1000515";
        private const string _CID = "MS:1001880";
        private const string _HCD = "MS:1000422";
        private const string _ETD = "MS:1000598";
        private const string _MPD = "MS:1000435";
        private const string _ECD = "MS:1000250";
        private const string _PQD = "MS:1000599";
        private const string _quadrupole = "MS:1000081";
        private const string _linearIonTrap = "MS:1000291";
        private const string _IonTrap2DAxialEject = "MS:1000078";
        private const string _IonTrap2DRadialEject = "MS:1000083";
        private const string _IonTrap3D = "MS:1000082";
        private const string _orbitrap = "MS:1000484";
        private const string _TOF = "MS:1000084";
        private const string _FTICR = "MS:1000079";
        private const string _magneticSector = "MS:1000080";
        private const string _nozlibCompress = "MS:1000576";
        private const string _zlibCompression = "MS:1000574";
        private const string _64bit = "MS:1000523";
        private const string _32bit = "MS:1000521";
        private const string _negativePolarity = "MS:1000129";
        private const string _positivePolarity = "MS:1000130";
        private const string _filterString = "MS:1000512";
        private const string _centroidSpectrum = "MS:1000127";
        private const string _profileSpectrum = "MS:1000128";
        private const string _peakIntensity = "MS:1000042";


        private static XmlSerializer _indexedSerializer = new XmlSerializer(typeof(indexedmzML));
        private static XmlSerializer _mzMLSerializer = new XmlSerializer(typeof(mzMLType));

        private indexedmzML _indexedmzMLConnection;
        private mzMLType _mzMLConnection;

        public Mzml(string filePath)
            : base(filePath, MsDataFileType.Mzml)
        {
        }

        public static void Write(string filePath, indexedmzML _indexedmzMLConnection)
        {
            TextWriter writer = new StreamWriter(filePath);
            _indexedSerializer.Serialize(writer, _indexedmzMLConnection);
            writer.Close();
        }



        public override void Open()
        {
            if (!IsOpen || _mzMLConnection == null)
            {
                Stream stream = new FileStream(FilePath, FileMode.Open);
                try
                {
                    _indexedmzMLConnection = _indexedSerializer.Deserialize(stream) as indexedmzML;
                    _mzMLConnection = _indexedmzMLConnection.mzML;
                }
                catch (Exception)
                {
                    try
                    {
                        _mzMLConnection = _mzMLSerializer.Deserialize(stream) as mzMLType;
                    }
                    catch (Exception)
                    {
                        throw new InvalidDataException("Unable to parse " + FilePath + " as a mzML file!");
                    }
                }


                IsOpen = true;
            }
        }

        public bool IsIndexedMzML
        {
            get { return _indexedmzMLConnection != null; }
        }

        public override DissociationType GetDissociationType(int spectrumNumber, int msnOrder = 2)
        {
            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList.precursor[0].activation.cvParam)
            {
                switch (cv.accession)
                {
                    case _CID:
                        return DissociationType.CI;
                    case _HCD:
                        return DissociationType.HCD;
                    case _ETD:
                        return DissociationType.ETD;
                    case _MPD:
                        return DissociationType.MPD;
                    case _PQD:
                        return DissociationType.PQD;
                    default:
                        return DissociationType.UnKnown;
                }
            }
            throw new ArgumentNullException("Could not find dissociation type for spectrum number " + spectrumNumber + 1);
        }

        public override int GetMsnOrder(int spectrumNumber)
        {
            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].cvParam)
            {
                if (cv.accession.Equals(_msnOrderAccession))
                {
                    return int.Parse(cv.value);
                }
            }
            throw new ArgumentNullException("Could not find MSn level for spectrum number " + spectrumNumber + 1);
        }

        public override int GetPrecusorCharge(int spectrumNumber, int msnOrder = 2)
        {
            // PRECURSOR ARE ONLY IN MS2 SPECTRA!!!
            spectrumNumber--;

            if (_mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList == null)
                throw new ArgumentNullException("Couldn't find precursor charge in spectrum number " + spectrumNumber + 1 + ", possibly an MS1 Spectrum!");

            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam)
            {
                if (cv.accession.Equals(_precursorCharge))
                {
                    return short.Parse(cv.value);
                }
            }
            throw new ArgumentNullException("Couldn't find precursor charge in spectrum number " + spectrumNumber + 1);
        }

        public override MzRange GetMzRange(int spectrumNumber)
        {
            spectrumNumber--;
            double high = double.NaN;
            double low = double.NaN;

            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].cvParam)
            {
                if (cv.accession.Equals(_lowestObservedMZ))
                {
                    low = double.Parse(cv.value);
                }
                if (cv.accession.Equals(_highestObservedMZ))
                {
                    high = double.Parse(cv.value);
                }
            }
            if (double.IsNaN(low) || double.IsNaN(high))
            {
                throw new ArgumentNullException("Could not find MZ range for " + spectrumNumber + 1);
            }
            return new MzRange(low, high);
        }


        public override double GetIsolationWidth(int spectrumNumber)
        {
            spectrumNumber--;
            double low = double.NaN;
            double high = double.NaN;

            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList.precursor[0].isolationWindow.cvParam)
            {
                if (cv.accession.Equals(_isolationWindowLowerOffset))
                {
                    low = double.Parse(cv.value);
                }
                if (cv.accession.Equals(_isolationWindowUpperOffset))
                {
                    high = double.Parse(cv.value);
                }
            }
            if (double.IsNaN(low) || double.IsNaN(high))
            {
                throw new ArgumentNullException("Could not determine isolation width for " + spectrumNumber + 1);
            }
            return high - low;
        }

        public override string GetScanFilter(int spectrumNumber)
        {
            spectrumNumber--;

            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].scanList.scan[0].cvParam)
            {
                if (cv.accession.Equals(_filterString))
                {
                    return cv.value;
                }
            }
            throw new ArgumentNullException("Could not get scan filter for " + spectrumNumber + 1);
        }

        private static readonly Regex MZAnalyzerTypeRegex = new Regex(@"^[a-zA-Z]*", RegexOptions.Compiled);

        public override MZAnalyzerType GetMzAnalyzer(int spectrumNumber)
        {

            string filter = GetScanFilter(spectrumNumber);

            string type = MZAnalyzerTypeRegex.Match(filter).Captures[0].Value;

            switch (type)
            {
                case "ITMS":
                    return MZAnalyzerType.IonTrap2D;
                case "TQMS":
                    throw new InvalidDataException("Not sure what TQMS is");
                case "SQMS":
                    throw new InvalidDataException("Not sure what SQMS is");
                case "TOFMS":
                    return MZAnalyzerType.TOF;
                case "FTMS":
                    return MZAnalyzerType.Orbitrap;
                case "Sector":
                    return MZAnalyzerType.Sector;
            }

            // Maybe in the beginning of the file, there is a single analyzer?
            // Gets the first analyzer used.        
            string analyzer = _mzMLConnection.instrumentConfigurationList.instrumentConfiguration[0].cvParam[0].accession;

            switch (analyzer)
            {
                case _quadrupole:
                    return MZAnalyzerType.Quadrupole;
                case _linearIonTrap:
                    return MZAnalyzerType.IonTrap2D;
                case _IonTrap3D:
                    return MZAnalyzerType.IonTrap3D;
                case _orbitrap:
                    return MZAnalyzerType.Orbitrap;
                case _TOF:
                    return MZAnalyzerType.TOF;
                case _FTICR:
                    return MZAnalyzerType.FTICR;
                case _magneticSector:
                    return MZAnalyzerType.Sector;
                default:
                    return MZAnalyzerType.Unknown;
            }
        }

        public override Polarity GetPolarity(int spectrumNumber)
        {

            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].cvParam)
            {
                if (cv.accession.Equals(_negativePolarity))
                {
                    return Polarity.Negative;
                }
                if (cv.accession.Equals(_positivePolarity))
                {
                    return Polarity.Positive;
                }
            }
            //return Polarity.Neutral;
            throw new ArgumentNullException("Could not find polarity for spectrum number " + spectrumNumber + 1);
        }

        public override double GetRetentionTime(int spectrumNumber)
        {
            spectrumNumber--;
            if (_mzMLConnection.run.spectrumList.spectrum[spectrumNumber].scanList.scan[0].cvParam == null)
            {
                return double.NaN;
            }
            double rt = -1;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].scanList.scan[0].cvParam)
            {
                if (cv.accession.Equals(_retentionTime))
                {
                    rt = double.Parse(cv.value);
                }

                if (cv.unitName == "second")
                    rt /= 60;
            }

            if (rt >= 0)
                return rt;

            throw new ArgumentNullException("Could not determine retention time for " + spectrumNumber + 1);
        }

        public override double GetInjectionTime(int spectrumNumber)
        {
            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].scanList.scan[0].cvParam)
            {
                if (cv.accession.Equals(_ionInjectionTime))
                {
                    return double.Parse(cv.value);
                }
            }
            throw new ArgumentNullException("Could not determine injection time for " + spectrumNumber + 1);
        }

        public override double GetResolution(int spectrumNumber)
        {
            return 0.0;
        }

        protected override int GetFirstSpectrumNumber()
        {
            return 1;
        }

        protected override int GetLastSpectrumNumber()
        {
            return Convert.ToInt32(_mzMLConnection.run.spectrumList.count);
        }

        public override int GetSpectrumNumber(double retentionTime)
        {
            // TODO need to convert this to a binary search of some sort. Or if the data is indexedMZML see if the indices work better.
            int totalSpectra = Convert.ToInt32(_mzMLConnection.run.spectrumList.count);
            for (int i = 0; i < totalSpectra; i++)
            {
                foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[i].scanList.scan[0].cvParam)
                {
                    if (cv.accession.Equals(_retentionTime))
                    {
                        if (double.Parse(cv.value) == retentionTime)
                        {
                            return i + 1;
                        }
                    }
                }
            }
            throw new ArgumentNullException("Could not determine spectrum number");
        }

        public static byte[] ConvertDoublestoBase64(double[] toConvert, bool zlibCompressed, bool is32bit)
        {

            var mem = new MemoryStream();
            for (int i = 0; i < toConvert.Length; i++)
            {
                byte[] ok = BitConverter.GetBytes(toConvert[i]);
                mem.Write(ok, 0, ok.Length);
            }
            mem.Position = 0;

            byte[] bytes = mem.ToArray();
            if (zlibCompressed)
            {

                bytes = ZlibStream.CompressBuffer(bytes);

                //using (var compressStream = new MemoryStream())
                //using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
                //{
                //    mem.CopyTo(compressor);
                //    compressor.Close();
                //    a = compressStream.ToArray();
                //}
                //var finalStream = new MemoryStream();
                //finalStream.Write(new byte[] { 0x78, 0x9c}, 0, 2);
                //finalStream.Write(a, 0, a.Length);
                //// Cannot get the same checksum, so not gonna fake it, and will just put zeros
                //finalStream.Write(BitConverter.GetBytes(ComputeChecksum(a,0,a.Length)), 0, 4);
                //return finalStream.ToArray();
            }

            return bytes;
        }



        /// <summary>
        /// Converts a 64-based encoded byte array into an double[]
        /// </summary>
        /// <param name="bytes">the 64-bit encoded byte array</param>
        /// <param name="zlibCompressed">Specifies if the byte array is zlib compressed</param>
        /// <returns>a decompressed, de-encoded double[]</returns>
        private static double[] ConvertBase64ToDoubles(byte[] bytes, bool zlibCompressed = false, bool is32bit = true)
        {

            // Add capability of compressed data
            if (zlibCompressed)
            {

                bytes = ZlibStream.UncompressBuffer(bytes);
            }

            int size = is32bit ? sizeof(float) : sizeof(double);

            int length = bytes.Length / size;
            double[] convertedArray = new double[length];

            for (int i = 0; i < length; i++)
            {
                if (is32bit)
                {
                    convertedArray[i] = BitConverter.ToSingle(bytes, i * size);
                }
                else
                {
                    convertedArray[i] = BitConverter.ToDouble(bytes, i * size);
                }
            }
            return convertedArray;
        }

        public override bool GetIsCentroid(int spectrumNumber)
        {
            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].cvParam)
            {
                if (cv.accession.Equals(_centroidSpectrum))
                {
                    return true;
                }
                if (cv.accession.Equals(_profileSpectrum))
                {
                    return false;
                }
            }
            throw new ArgumentNullException("Could not determine if spectrum " + spectrumNumber + 1 + " is centroid or profile");
        }

        public override string GetSpectrumID(int spectrumNumber)
        {
            spectrumNumber--;
            return _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].id;
        }

        public override string GetPrecursorID(int spectrumNumber)
        {
            spectrumNumber--;
            return _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList.precursor[0].spectrumRef;
        }

        public override double GetPrecursorIsolationIntensity(int spectrumNumber)
        {
            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam)
            {
                if (cv.accession.Equals(_peakIntensity))
                {
                    return Convert.ToDouble(cv.value);
                }
            }
            throw new ArgumentNullException("Could not determine precursor intensity of spectrum " + spectrumNumber + 1);
        }

        public override double GetPrecursorMonoisotopicMz(int spectrumNumber)
        {
            spectrumNumber--;
            foreach (CVParamType cv in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam)
            {
                if (cv.accession.Equals(_precursorMass))
                {
                    return double.Parse(cv.value);
                }
            }
            throw new ArgumentNullException("Could not determine precursor monoisotopic mass for " + spectrumNumber + 1);
        }

        public override double GetPrecursorIsolationMz(int spectrumNumber)
        {
            throw new NotImplementedException();
        }

        public override MzRange GetIsolationRange(int spectrumNumber)
        {
            throw new NotImplementedException();
        }

        protected override MsDataScan<DefaultMzSpectrum> GetMsDataScanFromFile(int spectrumNumber)
        {

            spectrumNumber--; // 0-based indexing

            double[] masses = null;
            double[] intensities = null;

            foreach (BinaryDataArrayType binaryData in _mzMLConnection.run.spectrumList.spectrum[spectrumNumber].binaryDataArrayList.binaryDataArray)
            {
                bool compressed = false;
                bool mzArray = false;
                bool intensityArray = false;
                bool is32bit = true;
                foreach (CVParamType cv in binaryData.cvParam)
                {
                    if (cv.accession.Equals(_zlibCompression))
                    {
                        compressed = true;
                    }
                    if (cv.accession.Equals(_64bit))
                    {
                        is32bit = false;
                    }
                    if (cv.accession.Equals(_32bit))
                    {
                        is32bit = true;
                    }
                    if (cv.accession.Equals(_mzArray))
                    {
                        mzArray = true;
                    }
                    if (cv.accession.Equals(_intensityArray))
                    {
                        intensityArray = true;
                    }
                }

                double[] data = ConvertBase64ToDoubles(binaryData.binary, compressed, is32bit);
                if (mzArray)
                {
                    masses = data;
                }

                if (intensityArray)
                {
                    intensities = data;
                }
            }

            if (masses == null || intensities == null)
            {
                throw new InvalidDataException("Unable to find spectral data for spectrum number " + spectrumNumber + 1);
            }

            var ok = new DefaultMzSpectrum(masses, intensities);
            return new MsDataScan<DefaultMzSpectrum>(spectrumNumber, ok, GetSpectrumID(spectrumNumber + 1), GetMsnOrder(spectrumNumber + 1), GetIsCentroid(spectrumNumber + 1), GetPolarity(spectrumNumber + 1), GetRetentionTime(spectrumNumber + 1), GetMzRange(spectrumNumber + 1), GetScanFilter(spectrumNumber + 1));
        }
    }

    public static class MzmlMethods
    {
        public static void CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile, List<IMzSpectrum<MzPeak>> calibratedSpectra, List<double> calibratedPrecursorMZs, string outputFile)
        {
            indexedmzML _indexedmzMLConnection = new indexedmzML();
            _indexedmzMLConnection.mzML = new mzMLType();
            _indexedmzMLConnection.mzML.version = "1";

            _indexedmzMLConnection.mzML.cvList = new CVListType();
            _indexedmzMLConnection.mzML.cvList.count = "1";
            _indexedmzMLConnection.mzML.cvList.cv = new CVType[1];
            _indexedmzMLConnection.mzML.cvList.cv[0] = new CVType();
            _indexedmzMLConnection.mzML.cvList.cv[0].URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo";
            _indexedmzMLConnection.mzML.cvList.cv[0].fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology";
            _indexedmzMLConnection.mzML.cvList.cv[0].id = "MS";

            _indexedmzMLConnection.mzML.fileDescription = new FileDescriptionType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent = new ParamGroupType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam = new CVParamType[2];
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0].accession = "MS:1000579"; // MS1 Data
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1].accession = "MS:1000580"; // MSn Data

            _indexedmzMLConnection.mzML.softwareList = new SoftwareListType();
            _indexedmzMLConnection.mzML.softwareList.count = "1";

            _indexedmzMLConnection.mzML.softwareList.software = new SoftwareType[1];
            // For a RAW file!!!
            // ToDo: read softwareList from mzML file
            //_indexedmzMLConnection.mzML.softwareList.software[1] = new SoftwareType();
            //_indexedmzMLConnection.mzML.softwareList.software[1].id = "ThermoSoftware";
            //_indexedmzMLConnection.mzML.softwareList.software[1].version = rawFile.GetSofwareVersion();
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam = new CVParamType[1];
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0] = new CVParamType();
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0].accession = "MS:1000693";

            _indexedmzMLConnection.mzML.softwareList.software[0] = new SoftwareType();
            _indexedmzMLConnection.mzML.softwareList.software[0].id = "StefanSoftware";
            _indexedmzMLConnection.mzML.softwareList.software[0].version = "1";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam = new CVParamType[1];
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].accession = "MS:1000799";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].value = "StefanSoftware";


            // Leaving empty. Can't figure out the configurations. 
            // ToDo: read instrumentConfigurationList from mzML file
            _indexedmzMLConnection.mzML.instrumentConfigurationList = new InstrumentConfigurationListType();

            _indexedmzMLConnection.mzML.dataProcessingList = new DataProcessingListType();
            // Only writing mine! Might have had some other data processing (but not if it is a raw file)
            // ToDo: read dataProcessingList from mzML file
            _indexedmzMLConnection.mzML.dataProcessingList.count = "1";
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing = new DataProcessingType[1];
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0] = new DataProcessingType();
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0].id = "StefanDataProcessing";


            _indexedmzMLConnection.mzML.run = new RunType();

            // ToDo: Finish the chromatogram writing!
            _indexedmzMLConnection.mzML.run.chromatogramList = new ChromatogramListType();
            _indexedmzMLConnection.mzML.run.chromatogramList.count = "1";
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram = new ChromatogramType[1];
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram[0] = new ChromatogramType();

            _indexedmzMLConnection.mzML.run.spectrumList = new SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = (myMsDataFile.LastSpectrumNumber).ToString();
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "StefanDataProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[myMsDataFile.LastSpectrumNumber];

            // Loop over all spectra
            for (int i = 0; i < calibratedSpectra.Count; i++)
            {
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i] = new SpectrumType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].defaultArrayLength = calibratedSpectra[i].Count;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].index = i.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].id = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).id;

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam = new CVParamType[7];

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0] = new CVParamType();

                if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).MsnOrder == 1)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000579";
                }
                else if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).MsnOrder == 2)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList = new PrecursorListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor = new PrecursorType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0] = new PrecursorType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].spectrumRef = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).PrecursorID;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList = new SelectedIonListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon = new ParamGroupType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0] = new ParamGroupType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam = new CVParamType[3];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].value = calibratedPrecursorMZs[i].ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].accession = "MS:1000744";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].name = "charge state";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).SelectedIonChargeState.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].accession = "MS:1000041";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].name = "peak intensity";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).SelectedIonIsolationIntensity.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].accession = "MS:1000042";



                }

                // OPTIONAL, but need for CSMSL reader. ms level
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].name = "ms level";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).MsnOrder.ToString();

                // Centroid?
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2] = new CVParamType();
                if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).isCentroid)
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000127";
                else
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000128";

                // Polarity
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3] = new CVParamType();
                if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).Polarity == Polarity.Negative)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "negative scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000129";
                }
                else if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).Polarity == Polarity.Positive)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "positive scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000130";
                }

                // Spectrum title
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].name = "spectrum title";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].accession = "MS:1000796";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).id;


                if (calibratedSpectra[i].Count > 0)
                {
                    // Lowest observed mz
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].name = "lowest observed m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].accession = "MS:1000528";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].value = calibratedSpectra[i].FirstMZ.ToString();


                    // Highest observed mz
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].name = "highest observed m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].accession = "MS:1000527";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].value = calibratedSpectra[i].LastMZ.ToString();

                }


                // Retention time
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList = new ScanListType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.count = "1";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan = new ScanType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0] = new ScanType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam = new CVParamType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].name = "scan start time";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].accession = "MS:1000016";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).RetentionTime.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitCvRef = "UO";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitAccession = "UO:0000031";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitName = "minute";

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList = new BinaryDataArrayListType();

                // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
                // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.count = 2.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray = new BinaryDataArrayType[2];

                // M/Z Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary = Mzml.ConvertDoublestoBase64(calibratedSpectra[i].GetMasses(), false, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary.Length / 3))).ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam = new CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000514";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "m/z array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].name = "64-bit float";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000574";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "zlib compression";

                // Intensity Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary = Mzml.ConvertDoublestoBase64(calibratedSpectra[i].GetIntensities(), false, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary.Length / 3))).ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam = new CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000515";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "intensity array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].name = "64-bit float";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000574";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "zlib compression";
            }

            Mzml.Write(outputFile, _indexedmzMLConnection);
        }

        public static void CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(IMsDataFile<IMzSpectrum<MzPeak>> myMsDataFile)
        {

            //Console.WriteLine("In CreateAndWriteMyIndexedMZmlwithCalibratedSpectra");

            indexedmzML _indexedmzMLConnection = new indexedmzML();
            _indexedmzMLConnection.mzML = new mzMLType();
            _indexedmzMLConnection.mzML.version = "1";

            _indexedmzMLConnection.mzML.cvList = new CVListType();
            _indexedmzMLConnection.mzML.cvList.count = "1";
            _indexedmzMLConnection.mzML.cvList.cv = new CVType[1];
            _indexedmzMLConnection.mzML.cvList.cv[0] = new CVType();
            _indexedmzMLConnection.mzML.cvList.cv[0].URI = @"https://raw.githubusercontent.com/HUPO-PSI/psi-ms-CV/master/psi-ms.obo";
            _indexedmzMLConnection.mzML.cvList.cv[0].fullName = "Proteomics Standards Initiative Mass Spectrometry Ontology";
            _indexedmzMLConnection.mzML.cvList.cv[0].id = "MS";

            _indexedmzMLConnection.mzML.fileDescription = new FileDescriptionType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent = new ParamGroupType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam = new CVParamType[2];
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[0].accession = "MS:1000579"; // MS1 Data
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1] = new CVParamType();
            _indexedmzMLConnection.mzML.fileDescription.fileContent.cvParam[1].accession = "MS:1000580"; // MSn Data

            _indexedmzMLConnection.mzML.softwareList = new SoftwareListType();
            _indexedmzMLConnection.mzML.softwareList.count = "1";

            //Console.WriteLine("oikay");

            _indexedmzMLConnection.mzML.softwareList.software = new SoftwareType[1];
            // For a RAW file!!!
            // ToDo: read softwareList from mzML file
            //_indexedmzMLConnection.mzML.softwareList.software[1] = new SoftwareType();
            //_indexedmzMLConnection.mzML.softwareList.software[1].id = "ThermoSoftware";
            //_indexedmzMLConnection.mzML.softwareList.software[1].version = rawFile.GetSofwareVersion();
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam = new CVParamType[1];
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0] = new CVParamType();
            //_indexedmzMLConnection.mzML.softwareList.software[1].cvParam[0].accession = "MS:1000693";

            _indexedmzMLConnection.mzML.softwareList.software[0] = new SoftwareType();
            _indexedmzMLConnection.mzML.softwareList.software[0].id = "StefanSoftware";
            _indexedmzMLConnection.mzML.softwareList.software[0].version = "1";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam = new CVParamType[1];
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0] = new CVParamType();
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].accession = "MS:1000799";
            _indexedmzMLConnection.mzML.softwareList.software[0].cvParam[0].value = "StefanSoftware";


            // Leaving empty. Can't figure out the configurations. 
            // ToDo: read instrumentConfigurationList from mzML file
            _indexedmzMLConnection.mzML.instrumentConfigurationList = new InstrumentConfigurationListType();

            _indexedmzMLConnection.mzML.dataProcessingList = new DataProcessingListType();
            // Only writing mine! Might have had some other data processing (but not if it is a raw file)
            // ToDo: read dataProcessingList from mzML file
            _indexedmzMLConnection.mzML.dataProcessingList.count = "1";
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing = new DataProcessingType[1];
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0] = new DataProcessingType();
            _indexedmzMLConnection.mzML.dataProcessingList.dataProcessing[0].id = "StefanDataProcessing";


            _indexedmzMLConnection.mzML.run = new RunType();

            // ToDo: Finish the chromatogram writing!
            _indexedmzMLConnection.mzML.run.chromatogramList = new ChromatogramListType();
            _indexedmzMLConnection.mzML.run.chromatogramList.count = "1";
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram = new ChromatogramType[1];
            _indexedmzMLConnection.mzML.run.chromatogramList.chromatogram[0] = new ChromatogramType();

            _indexedmzMLConnection.mzML.run.spectrumList = new SpectrumListType();
            _indexedmzMLConnection.mzML.run.spectrumList.count = (myMsDataFile.LastSpectrumNumber - myMsDataFile.FirstSpectrumNumber + 1).ToString();
            _indexedmzMLConnection.mzML.run.spectrumList.defaultDataProcessingRef = "StefanDataProcessing";
            _indexedmzMLConnection.mzML.run.spectrumList.spectrum = new SpectrumType[myMsDataFile.LastSpectrumNumber - myMsDataFile.FirstSpectrumNumber + 1];

            //Console.WriteLine("mymsdatafile.lastspectrumnumber - mymsdatafile.firstspectrumnumber + 1 = " + (myMsDataFile.LastSpectrumNumber - myMsDataFile.FirstSpectrumNumber + 1));
            // Loop over all spectra
            for (int i = 0; i < myMsDataFile.LastSpectrumNumber - myMsDataFile.FirstSpectrumNumber + 1; i++)
            {
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i] = new SpectrumType();

                //Console.WriteLine("i = " + i);

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].defaultArrayLength = myMsDataFile.GetSpectrum(i + myMsDataFile.FirstSpectrumNumber).Count;
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].index = i.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].id = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).id;

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam = new CVParamType[7];

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0] = new CVParamType();

                //Console.WriteLine(i + myMsDataFile.FirstSpectrumNumber);
                if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).MsnOrder == 1)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000579";
                }
                else if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).MsnOrder == 2)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[0].accession = "MS:1000580";

                    // So needs a precursor!
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList = new PrecursorListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor = new PrecursorType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0] = new PrecursorType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].spectrumRef = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).PrecursorID;
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList = new SelectedIonListType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.count = 1.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon = new ParamGroupType[1];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0] = new ParamGroupType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam = new CVParamType[3];
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].name = "selected ion m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).SelectedIonMonoisotopicMZ.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[0].accession = "MS:1000744";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].name = "charge state";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).SelectedIonChargeState.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[1].accession = "MS:1000041";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].name = "peak intensity";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).SelectedIonIsolationIntensity.ToString();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].precursorList.precursor[0].selectedIonList.selectedIon[0].cvParam[2].accession = "MS:1000042";



                }

                // OPTIONAL, but need for CSMSL reader. ms level
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].name = "ms level";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].accession = "MS:1000511";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[1].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).MsnOrder.ToString();

                // Centroid?
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2] = new CVParamType();
                if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).isCentroid)
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000127";
                else
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[2].accession = "MS:1000128";

                // Polarity
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3] = new CVParamType();
                if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).Polarity == Polarity.Negative)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "negative scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000129";
                }
                else if (myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).Polarity == Polarity.Positive)
                {
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].name = "positive scan";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[3].accession = "MS:1000130";
                }

                // Spectrum title
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].name = "spectrum title";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].accession = "MS:1000796";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[4].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).id;

                if (myMsDataFile.GetSpectrum(i + myMsDataFile.FirstSpectrumNumber).Count > 0)
                {
                    // Lowest observed mz
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].name = "lowest observed m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].accession = "MS:1000528";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[5].value = myMsDataFile.GetSpectrum(i + myMsDataFile.FirstSpectrumNumber).FirstMZ.ToString();


                    // Highest observed mz
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6] = new CVParamType();
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].name = "highest observed m/z";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].accession = "MS:1000527";
                    _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].cvParam[6].value = myMsDataFile.GetSpectrum(i + myMsDataFile.FirstSpectrumNumber).LastMZ.ToString();

                }

                // Retention time
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList = new ScanListType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.count = "1";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan = new ScanType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0] = new ScanType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam = new CVParamType[1];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].name = "scan start time";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].accession = "MS:1000016";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].value = myMsDataFile.GetScan(i + myMsDataFile.FirstSpectrumNumber).RetentionTime.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitCvRef = "UO";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitAccession = "UO:0000031";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].scanList.scan[0].cvParam[0].unitName = "minute";

                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList = new BinaryDataArrayListType();

                // ONLY WRITING M/Z AND INTENSITY DATA, NOT THE CHARGE! (but can add charge info later)
                // CHARGE (and other stuff) CAN BE IMPORTANT IN ML APPLICATIONS!!!!!
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.count = 2.ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray = new BinaryDataArrayType[2];

                // M/Z Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary = Mzml.ConvertDoublestoBase64(myMsDataFile.GetSpectrum(i + myMsDataFile.FirstSpectrumNumber).GetMasses(), false, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].binary.Length / 3))).ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam = new CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000514";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "m/z array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[1].name = "64-bit float";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0] = new CVParamType();
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].accession = "MS:1000574";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[0].cvParam[0].name = "zlib compression";

                // Intensity Data
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1] = new BinaryDataArrayType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary = Mzml.ConvertDoublestoBase64(myMsDataFile.GetSpectrum(i + myMsDataFile.FirstSpectrumNumber).GetIntensities(), false, false);
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].encodedLength = (4 * Math.Ceiling(((double)_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].binary.Length / 3))).ToString();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam = new CVParamType[2];
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000515";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "intensity array";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1] = new CVParamType();
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].accession = "MS:1000523";
                _indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[1].name = "64-bit float";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0] = new CVParamType();
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].accession = "MS:1000574";
                //_indexedmzMLConnection.mzML.run.spectrumList.spectrum[i].binaryDataArrayList.binaryDataArray[1].cvParam[0].name = "zlib compression";
            }

            Mzml.Write(Path.Combine(Path.GetDirectoryName(myMsDataFile.FilePath), Path.GetFileNameWithoutExtension(myMsDataFile.FilePath)) + ".mzML", _indexedmzMLConnection);
        }

    }

}

