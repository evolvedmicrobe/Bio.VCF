using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Bio.VCF
{
	public class VariantContextIterator 
    {
		private readonly VCFCodec vcfCodec = new VCFCodec();
        readonly VCFHeader vcfHeader;
        private System.IO.StreamReader reader;
        public VariantContextIterator(FileInfo vcfFile)
		{
            try
            {
                this.reader = new StreamReader(vcfFile.FullName);
            }
            catch (Exception thrown)
            {
                throw new VCFParsingError("Could not open file: " + vcfFile.FullName, thrown);
            }
            vcfHeader = vcfCodec.readHeader(reader);
		}
		public void Close()
		{
            if (reader != null)
            { reader.Close(); }
            reader = null;
		}
		public VCFHeader Header
		{
			get
			{
				return this.vcfHeader;
			}
		}
        public IEnumerable<VariantContext> Variants()
        {
            if (reader != null)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return vcfCodec.decode(line);
                }
            }
        }
	}

}