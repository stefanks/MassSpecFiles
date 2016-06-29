using Chemistry;
using IO.MzML;
using MassSpectrometry;
using mzIdentML;
using NUnit.Framework;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Test
{
    [TestFixture]
    public sealed class TestMzML
    {
        [OneTimeSetUp]
        public void setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;

            UsefulProteomicsDatabases.Loaders.LoadElements(@"elements.dat");
        }


        [Test]
        public void LoadMzmlTest()
        {
            Mzml a = new Mzml(@"tiny.pwiz.1.1.mzML");
            Assert.AreEqual(false, a.IsIndexedMzML);

            a.Open();

            Assert.AreEqual(true, a.IsIndexedMzML);

            a.GetSpectrum(1);

        }


        [Test]
        public void WriteMzmlTest()
        {
            var peptide = new Peptide("KQEEQMETEQQNKDEGK");

            DefaultMzSpectrum MS1 = createSpectrum(peptide.GetChemicalFormula(), 300, 2000, 1);
            foreach (var ok in MS1)
                Console.WriteLine(ok);
            DefaultMzSpectrum MS2 = createMS2spectrum(peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true), 100, 1500);


            MsDataScan<DefaultMzSpectrum>[] Scans = new MsDataScan<DefaultMzSpectrum>[2];
            Console.WriteLine("Creating first scan");
            Scans[0] = new MsDataScan<DefaultMzSpectrum>(1, MS1.newSpectrumApplyFunctionToX(b => b + 0.00001 * b + 0.00001), "spectrum 1", 1, false, Polarity.Positive, 1.0, new MzRange(300, 2000), "first spectrum");

            Console.WriteLine("Creating second scan");
            Scans[1] = new MsDataScan<DefaultMzSpectrum>(2, MS2.newSpectrumApplyFunctionToX(b => b + 0.00001 * b + 0.00002), "spectrum 2", 2, false, Polarity.Positive, 2.0, new MzRange(100, 1500), "second spectrum", "first spectrum", 800, 2, double.NaN);
            Console.WriteLine("Creating DefaultMsDataFile");
            FakeMsDataFile myMsDataFile = new FakeMsDataFile("myFile.mzML");
            Console.WriteLine("Created! Now adding scans");
            myMsDataFile.Add(Scans);
            Console.WriteLine("Added scans");

            MzmlMethods.CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(myMsDataFile);
        }

        private DefaultMzSpectrum createMS2spectrum(IEnumerable<Fragment> fragments, int v1, int v2)
        {
            List<double> allMasses = new List<double>();
            List<double> allIntensities = new List<double>();
            foreach (ChemicalFormulaFragment f in fragments)
            {
                foreach (var p in createSpectrum(f.ThisChemicalFormula, v1, v2, 2))
                {
                    allMasses.Add(p.MZ);
                    allIntensities.Add(p.Intensity);
                }
            }
            var allMassesArray = allMasses.ToArray();
            var allIntensitiessArray = allIntensities.ToArray();

            Array.Sort(allMassesArray, allIntensitiessArray);
            return new DefaultMzSpectrum(allMassesArray, allIntensitiessArray, false);
        }

        private DefaultMzSpectrum createSpectrum(ChemicalFormula f, double lowerBound, double upperBound, int minCharge)
        {

            IsotopicDistribution isodist = new IsotopicDistribution(f, 0.1);
            DefaultMzSpectrum massSpectrum1 = new DefaultMzSpectrum(isodist.Masses.ToArray(), isodist.Intensities.ToArray(), false);
            massSpectrum1 = massSpectrum1.newSpectrumFilterByNumberOfMostIntense(5);

            var chargeToLookAt = minCharge;
            var correctedSpectrum = massSpectrum1.newSpectrumApplyFunctionToX(s => s.ToMassToChargeRatio(chargeToLookAt));

            List<double> allMasses = new List<double>();
            List<double> allIntensitiess = new List<double>();

            while (correctedSpectrum.FirstX > lowerBound)
            {
                foreach (var thisPeak in correctedSpectrum)
                {
                    if (thisPeak.MZ > lowerBound && thisPeak.MZ < upperBound)
                    {
                        allMasses.Add(thisPeak.MZ);
                        allIntensitiess.Add(thisPeak.Intensity);
                    }
                }
                chargeToLookAt += 1;
                correctedSpectrum = massSpectrum1.newSpectrumApplyFunctionToX(s => s.ToMassToChargeRatio(chargeToLookAt));
            }

            var allMassesArray = allMasses.ToArray();
            var allIntensitiessArray = allIntensitiess.ToArray();

            Array.Sort(allMassesArray, allIntensitiessArray);

            return new DefaultMzSpectrum(allMassesArray, allIntensitiessArray, false);
        }




        [Test]
        public void WriteMzidTest()
        {
            XmlSerializer _indexedSerializer = new XmlSerializer(typeof(MzIdentMLType));
            var _mzid = new MzIdentMLType();
            _mzid.DataCollection = new DataCollectionType();
            _mzid.DataCollection.AnalysisData = new AnalysisDataType();
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList = new SpectrumIdentificationListType[1];
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0] = new SpectrumIdentificationListType();
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult = new SpectrumIdentificationResultType[1];
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0] = new SpectrumIdentificationResultType();
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].spectrumID = "spectrum 2";
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem = new SpectrumIdentificationItemType[1];
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0] = new SpectrumIdentificationItemType();
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].experimentalMassToCharge = 1039.97880968;
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].calculatedMassToCharge = 1039.9684;
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].calculatedMassToChargeSpecified = true;
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].chargeState = 2;
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].cvParam = new mzIdentML.CVParamType[1];
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].cvParam[0] = new mzIdentML.CVParamType();
            _mzid.DataCollection.AnalysisData.SpectrumIdentificationList[0].SpectrumIdentificationResult[0].SpectrumIdentificationItem[0].cvParam[0].value = 100.ToString();

            _mzid.SequenceCollection = new SequenceCollectionType();
            _mzid.SequenceCollection.PeptideEvidence = new PeptideEvidenceType[1];
            _mzid.SequenceCollection.PeptideEvidence[0] = new PeptideEvidenceType();
            _mzid.SequenceCollection.PeptideEvidence[0].isDecoy = false;
            _mzid.SequenceCollection.Peptide = new PeptideType[1];
            _mzid.SequenceCollection.Peptide[0] = new PeptideType();
            _mzid.SequenceCollection.Peptide[0].PeptideSequence = "KQEEQMETEQQNKDEGK";

            TextWriter writer = new StreamWriter("myIdentifications.mzid");
            _indexedSerializer.Serialize(writer, _mzid);
            writer.Close();
        }
    }
}