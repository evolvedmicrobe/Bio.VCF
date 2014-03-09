
namespace Bio.VCF
{


	/// <summary>
	/// A class representing a key=value entry for genotype FORMAT fields in the VCF header
	/// </summary>
	public class VCFFormatHeaderLine : VCFCompoundHeaderLine
	{

		public VCFFormatHeaderLine(string name, int count, VCFHeaderLineType type, string description)
            : base(name, count, type, description, SupportedHeaderLineType.FORMAT)
		{
			if (type == VCFHeaderLineType.Flag)
			{
				throw new System.ArgumentException("Flag is an unsupported type for format fields");
			}
		}

		public VCFFormatHeaderLine(string name, VCFHeaderLineCount count, VCFHeaderLineType type, string description) 
            : base(name, count, type, description, SupportedHeaderLineType.FORMAT)
		{
		}

		public VCFFormatHeaderLine(string line, VCFHeaderVersion version)
            : base(line, version, SupportedHeaderLineType.FORMAT)
		{
		}

		// format fields do not allow flag values (that wouldn't make much sense, how would you encode this in the genotype).
		internal override bool allowFlagValues()
		{
			return false;
		}
	}
}