using IO.MzML;
using IO.Thermo;
using NUnit.Framework;
using System;

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
            Assert.AreEqual(false, a.IsOpen);
            a.Open();
            Assert.AreEqual(true, a.IsOpen);
            Assert.AreEqual(1, a.FirstSpectrumNumber);
            Assert.AreEqual(3316, a.LastSpectrumNumber);
            Assert.AreEqual(3316, a.LastSpectrumNumber);

            var scan = a.GetScan(53);
            Assert.AreEqual(1.2623333333333333, scan.RetentionTime);
            Assert.AreEqual(1, scan.MsnOrder);
            Assert.AreEqual("controllerType=0 controllerNumber=1 scan=53", scan.id);
            Assert.AreEqual("+ c ESI Full ms [400.00-2000.00]", scan.ScanFilter);


            var spectrum = a.GetSpectrum(53);

            var peak = spectrum.GetBasePeak();
            Assert.AreEqual(75501, peak.Intensity);

            MzmlMethods.CreateAndWriteMyIndexedMZmlwithCalibratedSpectra(a);
        }
    }
}