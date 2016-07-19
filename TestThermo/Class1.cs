using IO.MzML;
using IO.Thermo;
using NUnit.Framework;
using Spectra;
using System;
using System.IO;

namespace TestThermo
{
    [TestFixture]
    public sealed class TestThermo
    {
        [OneTimeSetUp]
        public void setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        }


        [Test]
        public void LoadThermoTest()
        {
            ThermoRawFile a = new ThermoRawFile(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW");
            a.Open();
            a.Open();
            Assert.AreEqual(1, a.FirstSpectrumNumber);
            Assert.AreEqual(3316, a.LastSpectrumNumber);
            Assert.AreEqual(3316, a.LastSpectrumNumber);

            var scan = a.GetScan(53);
            Assert.AreEqual(1.2623333333333333, scan.RetentionTime);
            Assert.AreEqual(1, scan.MsnOrder);
            Assert.AreEqual("controllerType=0 controllerNumber=1 scan=53", scan.id);
            Assert.AreEqual("+ c ESI Full ms [400.00-2000.00]", scan.ScanFilter);


            var spectrum = a.GetScan(53).MassSpectrum;

            var peak = spectrum.PeakWithHighestY;
            Assert.AreEqual(75501, peak.Intensity);

            Assert.AreEqual(1, spectrum.newSpectrumFilterByY(7.5e4).Count);
            Assert.AreEqual(2, spectrum.newSpectrumExtract(new DoubleRange(923, 928)).Count);



            Assert.AreEqual("1.3", a.GetSofwareVersion());
            double ya;
            a.GetScan(948).TryGetSelectedIonGuessIsolationIntensity(out ya);
            Assert.AreEqual(4125760, ya);

            Assert.AreEqual("LCQ", a.GetInstrumentName());
            Assert.AreEqual("LCQ", a.GetInstrumentModel());


            MzmlMethods.CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(a);
        }


        [Test]
        public void ThermoLoadError()
        {
            ThermoRawFile a = new ThermoRawFile(@"aaa.RAW");
            Assert.Throws<IOException>(() => { a.Open(); });
        }
        [Test]
        public void LoadThermoTest2()
        {
            ThermoRawFile a = new ThermoRawFile(@"05-13-16_cali_MS_60K-res_MS.raw");
            a.Open();
            Assert.AreEqual(360, a.LastSpectrumNumber);
            var ok = a.GetScan(1).MassSpectrum.GetNoises();
            Assert.AreEqual(2401.57, ok[0], 0.01);
            ThermoSpectrum ok2 = a.GetScan(1).MassSpectrum.newSpectrumExtract(0, 500);
            Assert.GreaterOrEqual(1000, a.GetScan(1).MassSpectrum.newSpectrumExtract(0, 500).LastX);
            Assert.AreEqual(2, a.GetScan(1).MassSpectrum.newSpectrumFilterByY(5e6).Count);
            var ye = a.GetScan(1).MassSpectrum.CopyTo2DArray();
            Assert.AreEqual(1, ye[4, 1119]);
        }
        [Test]
        public void LoadThermoTest3()
        {
            ThermoRawFile a = new ThermoRawFile(@"05-13-16_cali_MS-MS_524_7-5K-res.raw");
            a.Open();
            Assert.AreEqual(1289, a.LastSpectrumNumber);
        }

    }
}