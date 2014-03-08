
namespace Bio.VCF
{


	/// <summary>
	/// @author ebanks
	///         <p/>
	///         Class VCFInfoHeaderLine
	///         <p/>
	///         A class representing a key=value entry for INFO fields in the VCF header
	/// </summary>
	public class VCFInfoHeaderLine : VCFCompoundHeaderLine
	{
		public VCFInfoHeaderLine(string name, int count, VCFHeaderLineType type, string description) : base(name, count, type, description, SupportedHeaderLineType.INFO)
		{
		}

		public VCFInfoHeaderLine(string name, VCFHeaderLineCount count, VCFHeaderLineType type, string description) : base(name, count, type, description, SupportedHeaderLineType.INFO)
		{
		}

		public VCFInfoHeaderLine(string line, VCFHeaderVersion version) : base(line, version, SupportedHeaderLineType.INFO)
		{
		}

		// info fields allow flag values
		internal override bool allowFlagValues()
		{
			return true;
		}
	}

}