using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bio.VCF;

namespace TestVCF
{
    class Program
    {
        static string fname = @"C:\Users\Nigel\SkyDrive\Bio.VCF\carolinensis-bwa-all-merged-trimmedreads-correctheader-freebayes.vcf";
        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            VCFParser vcp = new VCFParser(fname);
            var i=vcp.Select(x=>x.NoCallCount).Count();
            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());
        }
    }
}
