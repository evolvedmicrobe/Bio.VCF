using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bio.VCF
{
	/// <summary>
	/// A feature codec for the VCF3 specification, to read older VCF files.  VCF3 has been
	/// depreciated in favor of VCF4 (See VCF codec for the latest information)
	/// 
	/// <p>
	/// Reads historical VCF3 encoded files (1000 Genomes Pilot results, for example)
	/// </p>
	/// 
	/// <p> </summary>
	/// See also: <seealso cref= <a href="http://vcftools.sourceforge.net/specs.html">VCF specification</a><br> </seealso>
	/// See also: <seealso cref= <a href="http://www.ncbi.nlm.nih.gov/pubmed/21653522">VCF spec. publication</a>
	/// </p>
	/// 
	public class VCF3Codec : AbstractVCFCodec
	{
		public VCF3Codec () : base ("VCFv3", VCFHeaderVersion.VCF3_2, VCFHeaderVersion.VCF3_3)
		{
		}

		public const string VCF3_MAGIC_HEADER = "##fileformat=VCFv3";

		protected internal override void validateFilters (string filterString)
		{
			if (filterString.Length == 0) {
				generateException ("The VCF specification requires a valid filter status");
			}
		}

		public bool CanDecode (string potentialInput)
		{
			return CanDecodeFile (potentialInput, VCF3_MAGIC_HEADER);
		}

		/// <summary>
		/// parse the filter string, first checking to see if we already have parsed it in a previous attempt </summary>
		/// <param name="filterString"> the string to parse </param>
		/// <returns> a set of the filters applied </returns>
		protected override IList<String> parseFilters (String filterString)
		{
			// null for unfiltered
			if (filterString.Equals (VCFConstants.UNFILTERED))
				return null;
            
			if (filterString.Equals (VCFConstants.PASSES_FILTERS_v3))
				return new List<String> (0);

			validateFilters (filterString);

			return filterHash [filterString].List;
		}
	}
}