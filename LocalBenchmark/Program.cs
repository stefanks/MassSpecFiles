using IO.Thermo;
using System;

namespace LocalBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = new ThermoRawFile(@"C:\Users\stepa\Data\MouseForShaker\04-29-13_B6_Frac9_9p5uL.raw");
            file.Open();

            Console.WriteLine(file.ToString());
            Console.WriteLine(file.monoisotopicPrecursorSelectionEnabled);

            file = new ThermoRawFile(@"C:\Users\stepa\Data\jurkat\Original\120426_Jurkat_highLC_Frac5.raw");
            file.Open();

            Console.WriteLine(file.ToString());
            Console.WriteLine(file.monoisotopicPrecursorSelectionEnabled);
            Console.Read();
        }
    }
}
