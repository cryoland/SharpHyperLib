using System;
using HyperLib;

namespace MultispectralTest002
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = "world_dem.hdr";
            HeaderInfo file = HyperRead.LoadHeaderFile(filename);

            Console.WriteLine(file.Interleave);
            Console.ReadKey();
        }
    }
}