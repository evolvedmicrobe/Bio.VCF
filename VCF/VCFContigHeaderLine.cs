using System.Collections.Generic;

namespace Bio.VCF
{


	/// <summary>
	/// A special class representing a contig VCF header line.  Nows the true contig order and sorts on that
	/// 
	/// @author mdepristo
	/// </summary>
	public class VCFContigHeaderLine : VCFSimpleHeaderLine
	{
		internal readonly int? contigIndex;


		/// <summary>
		/// create a VCF contig header line
		/// </summary>
		/// <param name="line">      the header line </param>
		/// <param name="version">   the vcf header version </param>
		/// <param name="key">            the key for this header line </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public VCFContigHeaderLine(final String line, final VCFHeaderVersion version, final String key, int contigIndex)
		public VCFContigHeaderLine(string line, VCFHeaderVersion version, string key, int contigIndex) : base(line, version, key, null)
		{
			this.contigIndex = contigIndex;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public VCFContigHeaderLine(final java.util.Map<String, String> mapping, int contigIndex)
		public VCFContigHeaderLine(IDictionary<string, string> mapping, int contigIndex) : base(VCFHeader.CONTIG_KEY, mapping, null)
		{
			this.contigIndex = contigIndex;
		}

		public virtual int? ContigIndex
		{
			get
			{
				return contigIndex;
			}
		}

		/// <summary>
		/// IT IS CRITIAL THAT THIS BE OVERRIDDEN SO WE SORT THE CONTIGS IN THE CORRECT ORDER
		/// </summary>
		/// <param name="other">
		/// @return </param>
		public override int CompareTo(object other)
		{
			if (other is VCFContigHeaderLine)
			{
				return contigIndex.Value.CompareTo(((VCFContigHeaderLine) other).contigIndex);
			}
			else
			{
				return base.CompareTo(other);
			}
		}
	}
}