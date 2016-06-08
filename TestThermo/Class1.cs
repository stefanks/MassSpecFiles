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
            Console.WriteLine(Environment.CurrentDirectory);
            ThermoRawFile a = new ThermoRawFile(@"Shew_246a_LCQa_15Oct04_Andro_0904-2_4-20.RAW");
            Assert.AreEqual(false, a.IsOpen);
            a.Open();            
            Assert.AreEqual(true, a.IsOpen);


            var spectrum = a.GetSpectrum(53);


            var peak = spectrum.GetBasePeak();
            Assert.AreEqual(75501, peak.Intensity);
        }
    }
}