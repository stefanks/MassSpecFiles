using Chemistry;
using IO.MzML;
using MassSpectrometry;
using NUnit.Framework;
using Proteomics;
using Spectra;
using System;
using System.Collections.Generic;

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
            Assert.AreEqual(false, a.IsOpen);

            a.Open();

            Assert.AreEqual(true, a.IsIndexedMzML);
            Assert.AreEqual(true, a.IsOpen);

            a.GetSpectrum(1);

        }


        [Test]
        public void WriteMzmlTest()
        {
            var peptide = new Peptide("KQEEQMETEQQNKDEGK");

            DefaultMzSpectrum MS1 = createSpectrum(peptide.GetChemicalFormula(), 300, 2000,1);
            DefaultMzSpectrum MS2 = createMS2scan(peptide.Fragment(FragmentTypes.b | FragmentTypes.y, true), 100, 1500);


            MsDataScan<DefaultMzSpectrum>[] Scans = new MsDataScan<DefaultMzSpectrum>[2];
            Console.WriteLine("Creating first scan");
            Scans[0] = new MsDataScan<DefaultMzSpectrum>(1, MS1, "first spectrum", 1, false, Polarity.Positive, 1.0, new DoubleRange(300, 2000), "first spectrum");
            
            Console.WriteLine("Creating second scan");
            Scans[1] = new MsDataScan<DefaultMzSpectrum>(2, MS2, "second spectrum", 2, false, Polarity.Positive, 2.0, new DoubleRange(100, 1500), "second spectrum", "first spectrum", 800, 2, double.NaN);
            Console.WriteLine("Creating DefaultMsDataFile");
            DefaultMsDataFile myMsDataFile = new DefaultMsDataFile("myFile.mzML");
            Console.WriteLine("Created! Now adding scans");
            myMsDataFile.Add(Scans);
            Console.WriteLine("Added scans");

            MzmlMethods.CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(myMsDataFile);
        }

        private DefaultMzSpectrum createMS2scan(IEnumerable<Fragment> fragments, int v1, int v2)
        {
            List<double> allMasses = new List<double>();
            List<double> allIntensities = new List<double>();
            foreach (ChemicalFormulaFragment f in fragments)
            {
                foreach (var p in createSpectrum(f.thisChemicalFormula, v1, v2,2))
                {
                    allMasses.Add(p.MZ);
                    allIntensities.Add(p.Intensity); 
                }
            }
            var allMassesArray = allMasses.ToArray();
            var allIntensitiessArray = allIntensities.ToArray();

            Array.Sort(allMassesArray, allIntensitiessArray);
            return new DefaultMzSpectrum(allMassesArray, allIntensitiessArray);
        }

        private DefaultMzSpectrum createSpectrum(ChemicalFormula f, double lowerBound, double upperBound, int minCharge)
        {
            IsotopicDistribution isodist = new IsotopicDistribution(0.1);

            double[] masses;
            double[] intensities;
            isodist.CalculateDistribuition(f, out masses, out intensities);
            DefaultMzSpectrum massSpectrum1 = new DefaultMzSpectrum(masses, intensities);
            massSpectrum1 = massSpectrum1.FilterByNumberOfMostIntense(5);

            var chargeToLookAt = minCharge;
            var correctedSpectrum = massSpectrum1.CorrectMasses(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);

            List<double> allMasses = new List<double>();
            List<double> allIntensitiess = new List<double>();

            while (correctedSpectrum.FirstMZ > lowerBound)
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
                correctedSpectrum = massSpectrum1.CorrectMasses(s => (s + chargeToLookAt * Constants.Proton) / chargeToLookAt);
            }

            var allMassesArray = allMasses.ToArray();
            var allIntensitiessArray = allIntensitiess.ToArray();

            Array.Sort(allMassesArray, allIntensitiessArray);

            return new DefaultMzSpectrum(allMassesArray, allIntensitiessArray);
        }
    }
}