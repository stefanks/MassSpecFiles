using NUnit.Framework;
using IO.MzML;
using System;

namespace Test
{
    [TestFixture]
    public sealed class TestMzML
    {
        [OneTimeSetUp]
        public void setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
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
        }
    }
}