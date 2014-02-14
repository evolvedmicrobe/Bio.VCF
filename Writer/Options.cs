using System;

namespace Bio.VCF
{


    [Flags]
	public enum VCFWriterOptions
	{
        NONE=0,
		DO_NOT_WRITE_GENOTYPES=1,
		ALLOW_MISSING_FIELDS_IN_HEADER=2
	}

}