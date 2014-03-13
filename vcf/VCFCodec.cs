using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bio.VCF.NewCollections;

namespace Bio.VCF
{
	/// <summary>
	/// A feature codec for the VCF 4 specification
	/// 
	/// <p>
	/// VCF is a text file format (most likely stored in a compressed manner). It contains meta-information lines, a
	/// header line, and then data lines each containing information about a position in the genome.
	/// </p>
	/// <p>One of the main uses of next-generation sequencing is to discover variation amongst large populations
	/// of related samples. Recently the format for storing next-generation read alignments has been
	/// standardised by the SAM/BAM file format specification. This has significantly improved the
	/// interoperability of next-generation tools for alignment, visualisation, and variant calling.
	/// We propose the Variant Call Format (VCF) as a standarised format for storing the most prevalent
	/// types of sequence variation, including SNPs, indels and larger structural variants, together
	/// with rich annotations. VCF is usually stored in a compressed manner and can be indexed for
	/// fast data retrieval of variants from a range of positions on the reference genome.
	/// The format was developed for the 1000 Genomes Project, and has also been adopted by other projects
	/// such as UK10K, dbSNP, or the NHLBI Exome Project. VCFtools is a software suite that implements
	/// various utilities for processing VCF files, including validation, merging and comparing,
	/// and also provides a general Perl and Python API.
	/// The VCF specification and VCFtools are available from http://vcftools.sourceforge.net.</p>
	/// 
	/// <p> </summary>
	/// See also: <seealso cref= <a href="http://vcftools.sourceforge.net/specs.html">VCF specification</a><br> </seealso>
	/// See also: <seealso cref= <a href="http://www.ncbi.nlm.nih.gov/pubmed/21653522">VCF spec. publication</a>
	/// </p>
	/// 
	/// <h2>File format example</h2>
	/// <pre>
	///     ##fileformat=VCFv4.0
	///     #CHROM  POS     ID      REF     ALT     QUAL    FILTER  INFO    FORMAT  NA12878
	///     chr1    109     .       A       T       0       PASS  AC=1    GT:AD:DP:GL:GQ  0/1:610,327:308:-316.30,-95.47,-803.03:99
	///     chr1    147     .       C       A       0       PASS  AC=1    GT:AD:DP:GL:GQ  0/1:294,49:118:-57.87,-34.96,-338.46:99
	/// </pre>
	/// 
	/// @author Mark DePristo
	/// @since 2010 </seealso>
	public class VCFCodec : AbstractVCFCodec
	{
		// Our aim is to read in the records and convert to VariantContext as quickly as possible, relying on VariantContext to do the validation of any contradictory (or malformed) record parameters.
		public const string VCF4_MAGIC_HEADER = "##fileformat=VCFv4";

		public VCFCodec ()
            : base ("VCFv4", VCFHeaderVersion.VCF4_0, VCFHeaderVersion.VCF4_1)
		{
		}

		/// <summary>
		/// Parse the filter string, first checking to see if we already have parsed it in a previous attempt
		/// </summary>
		/// <param name="filterString">the string to parse </param>
		/// <returns> a set of the filters applied or null if filters were not applied to the record (e.g. as per the missing value in a VCF) </returns>
		protected internal override void validateFilters (string filterString)
		{
            
			if (filterString.Equals (VCFConstants.PASSES_FILTERS_v3)) {
				generateException (VCFConstants.PASSES_FILTERS_v3 + " is an invalid filter name in vcf4", lineNo);
			}
			if (filterString.Length == 0) {
				generateException ("The VCF specification requires a valid filter status: filter was " + filterString, lineNo);
			}

		}

		protected override IList<string> parseFilters (string filterString)
		{
			// null for unfiltered
			if (filterString.Equals (VCFConstants.UNFILTERED))
				return null;

			if (filterString.Equals (VCFConstants.PASSES_FILTERS_v4))
				return new List<string> (0);
			validateFilters (filterString);
			// Get Filter String, note this method makes the entry if it doesn't already exist
			return filterHash [filterString].List;
		}

		public bool CanDecode (string potentialInput)
		{
			return CanDecodeFile (potentialInput, VCF4_MAGIC_HEADER);
		}
	}
}