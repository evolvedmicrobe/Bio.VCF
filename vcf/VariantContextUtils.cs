using System;
using System.Collections.Generic;
using System.Linq;

namespace Bio.VCF
{
	public class VariantContextUtils
	{
		private static ISet<string> MISSING_KEYS_WARNED_ABOUT = new HashSet<string> ();
		private const bool ASSUME_MISSING_FIELDS_ARE_STRINGS = false;

		static VariantContextUtils ()
		{
		}

		/// <summary>
		/// Update the attributes of the attributes map given the VariantContext to reflect the
		/// proper chromosome-based VCF tags
		/// </summary>
		/// <param name="vc">          the VariantContext </param>
		/// <param name="attributes">  the attributes map to populate; must not be null; may contain old values </param>
		/// <param name="removeStaleValues"> should we remove stale values from the mapping? </param>
		/// <returns> the attributes map provided as input, returned for programming convenience </returns>
		public static IDictionary<string, object> CalculateChromosomeCounts (VariantContext vc, IDictionary<string, object> attributes, bool removeStaleValues)
		{
			return CalculateChromosomeCounts (vc, attributes, removeStaleValues, new HashSet<string> ());
		}

		/// <summary>
		/// Update the attributes of the attributes map given the VariantContext to reflect the
		/// proper chromosome-based VCF tags
		/// </summary>
		/// <param name="vc">          the VariantContext </param>
		/// <param name="attributes">  the attributes map to populate; must not be null; may contain old values </param>
		/// <param name="removeStaleValues"> should we remove stale values from the mapping? </param>
		/// <param name="founderIds"> - Set of founders Ids to take into account. AF and FC will be calculated over the founders.
		///                  If empty or null, counts are generated for all samples as unrelated individuals </param>
		/// <returns> the attributes map provided as input, returned for programming convenience </returns>
		public static IDictionary<string, object> CalculateChromosomeCounts (VariantContext vc, IDictionary<string, object> attributes, bool removeStaleValues, ISet<string> founderIds)
		{
			int AN = vc.CalledChrCount;

			// if everyone is a no-call, remove the old attributes if requested
			if (AN == 0 && removeStaleValues) {
				if (attributes.ContainsKey (VCFConstants.ALLELE_COUNT_KEY)) {
					attributes.Remove (VCFConstants.ALLELE_COUNT_KEY);
				}
				if (attributes.ContainsKey (VCFConstants.ALLELE_FREQUENCY_KEY)) {
					attributes.Remove (VCFConstants.ALLELE_FREQUENCY_KEY);
				}
				if (attributes.ContainsKey (VCFConstants.ALLELE_NUMBER_KEY)) {
					attributes.Remove (VCFConstants.ALLELE_NUMBER_KEY);
				}
				return attributes;
			}

			if (vc.HasGenotypes) {
				attributes [VCFConstants.ALLELE_NUMBER_KEY] = AN;

				// if there are alternate alleles, record the relevant tags
				if (vc.AlternateAlleles.Count > 0) {
					List<double> alleleFreqs = new List<double> ();
					List<int> alleleCounts = new List<int> ();
					List<int> foundersAlleleCounts = new List<int> ();
					double totalFoundersChromosomes = (double)vc.GetCalledChrCount (founderIds);
					int foundersAltChromosomes;
					foreach (Allele allele in vc.AlternateAlleles) {
						foundersAltChromosomes = vc.GetCalledChrCount (allele, founderIds);
						alleleCounts.Add (vc.GetCalledChrCount (allele));
						foundersAlleleCounts.Add (foundersAltChromosomes);
						if (AN == 0) {
							alleleFreqs.Add (0.0);
						} else {
							double freq = (double)foundersAltChromosomes / totalFoundersChromosomes;
							alleleFreqs.Add (freq);
						}
					}
					if (alleleCounts.Count == 1) {
						attributes [VCFConstants.ALLELE_COUNT_KEY] = alleleCounts [0];
					} else {
						attributes [VCFConstants.ALLELE_COUNT_KEY] = alleleCounts;
					}
					if (alleleFreqs.Count == 1) {
						attributes [VCFConstants.ALLELE_FREQUENCY_KEY] = alleleFreqs [0];
					} else {
						attributes [VCFConstants.ALLELE_FREQUENCY_KEY] = alleleFreqs;
					}
				} else {
					// if there's no alt AC and AF shouldn't be present
					attributes.Remove (VCFConstants.ALLELE_COUNT_KEY);
					attributes.Remove (VCFConstants.ALLELE_FREQUENCY_KEY);
				}
			}
			return attributes;
		}

		/// <summary>
		/// Update the attributes of the attributes map in the VariantContextBuilder to reflect the proper
		/// chromosome-based VCF tags based on the current VC produced by builder.make()
		/// </summary>
		/// <param name="builder">     the VariantContextBuilder we are updating </param>
		/// <param name="removeStaleValues"> should we remove stale values from the mapping? </param>
		public static void CalculateChromosomeCounts (VariantContextBuilder builder, bool removeStaleValues)
		{
			VariantContext vc = builder.make ();
			builder.Attributes = CalculateChromosomeCounts (vc, new Dictionary<string, object> (vc.Attributes), removeStaleValues, new HashSet<string> ());
		}

		/// <summary>
		/// Update the attributes of the attributes map in the VariantContextBuilder to reflect the proper
		/// chromosome-based VCF tags based on the current VC produced by builder.make()
		/// </summary>
		/// <param name="builder">     the VariantContextBuilder we are updating </param>
		/// <param name="founderIds"> - Set of founders to take into account. AF and FC will be calculated over the founders only.
		///                   If empty or null, counts are generated for all samples as unrelated individuals </param>
		/// <param name="removeStaleValues"> should we remove stale values from the mapping? </param>
		public static void CalculateChromosomeCounts (VariantContextBuilder builder, bool removeStaleValues, ISet<string> founderIds)
		{
			VariantContext vc = builder.make ();
			builder.Attributes = CalculateChromosomeCounts (vc, new Dictionary<string, object> (vc.Attributes), removeStaleValues, founderIds);
		}

		public static VCFCompoundHeaderLine GetMetaDataForField (VCFHeader header, string field)
		{
			VCFCompoundHeaderLine metaData = header.getFormatHeaderLine (field);
			if (metaData == null) {
				metaData = header.getInfoHeaderLine (field);
			}
			if (metaData == null) {
				throw new VCFParsingError ("Fully decoding VariantContext requires header line for all fields, but none was found for " + field);
			}
			return metaData;
		}

		/// <summary>
		/// Returns a newly allocated VC that is the same as VC, but without genotypes </summary>
		/// <param name="vc">  variant context </param>
		/// <returns>  new VC without genotypes </returns>
		public static VariantContext SitesOnlyVariantContext (VariantContext vc)
		{
			return (new VariantContextBuilder (vc)).noGenotypes ().make ();
		}

		/// <summary>
		/// Returns a newly allocated list of VC, where each VC is the same as the input VCs, but without genotypes </summary>
		/// <param name="vcs">  collection of VCs </param>
		/// <returns> new VCs without genotypes </returns>
		public static ICollection<VariantContext> SitesOnlyVariantContexts (ICollection<VariantContext> vcs)
		{
			IList<VariantContext> r = new List<VariantContext> ();
			foreach (VariantContext vc in vcs) {
				r.Add (SitesOnlyVariantContext (vc));
			}
			return r;
		}

		/// <summary>
		/// Gets size of the variant (end - start +1).
		/// </summary>
		/// <returns>The size.</returns>
		/// <param name="vc">Vc.</param>
		public static int GetSize (VariantContext vc)
		{
			return vc.End - vc.Start + 1;
		}

		public static ISet<string> GenotypeNames (ICollection<Genotype> genotypes)
		{
			return new HashSet<string> (genotypes.Select (x => x.SampleName));
		}

		/// <summary>
		/// Compute the end position for this VariantContext from the alleles themselves
		/// 
		/// In the trivial case this is a single BP event and end = start (open intervals)
		/// In general the end is start + ref length - 1, handling the case where ref length == 0
		/// However, if alleles contains a symbolic allele then we use endForSymbolicAllele in all cases
		/// </summary>
		/// <param name="alleles"> the list of alleles to consider.  The reference allele must be the first one </param>
		/// <param name="start"> the known start position of this event </param>
		/// <param name="endForSymbolicAlleles"> the end position to use if any of the alleles is symbolic.  Can be -1
		///                              if no is expected but will throw an error if one is found </param>
		/// <returns> this builder </returns>
		public static int ComputeEndFromAlleles (IList<Allele> alleles, int start, int endForSymbolicAlleles)
		{
			Allele reference = alleles [0];
			if (reference.NonReference) {
				throw new Exception ("computeEndFromAlleles requires first allele to be reference");
			}
			if (VariantContext.HasSymbolicAlleles (alleles)) {
				if (endForSymbolicAlleles == -1) {
					throw new Exception ("computeEndFromAlleles found a symbolic allele but endForSymbolicAlleles was provided");
				}
				return endForSymbolicAlleles;
			} else {
				return start + Math.Max (reference.Length - 1, 0);
			}
		}
	}
}