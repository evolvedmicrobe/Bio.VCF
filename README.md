Bio.VCF
=======

A CSharp Parser for VCF Files.  Can iterate over VCF files and provides typed access to all of the relavent information as well as data validation.

Simple Usage Example:

'''
  string fname = @"MyFile.vcf";
  VCFParser vcp = new VCFParser(fname);
  var myData=vcp.Where(x=>x.NoCallCount <20 && x.Biallelic).ToList();
'''
         
    
