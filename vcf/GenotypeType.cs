
namespace Bio.VCF
{

	/// <summary>
	/// Summary types for Genotype objects
	/// NOTE: These are used as list index values so must be 1..N 
	/// </summary>
	public enum GenotypeType
	{
		/// <summary>
		/// The sample is no-called (all alleles are NO_CALL </summary>
		NO_CALL=0,
		/// <summary>
		/// The sample is homozygous reference </summary>
		HOM_REF=1,
		/// <summary>
		/// The sample is heterozygous, with at least one ref and at least one one alt in any order </summary>
		HET=2,
		/// <summary>
		/// All alleles are non-reference </summary>
		HOM_VAR=3,
		/// <summary>
		/// There is no allele data availble for this sample (alleles.isEmpty) </summary>
		UNAVAILABLE=4,
		/// <summary>
		/// Some chromosomes are NO_CALL and others are called </summary>
		MIXED=5 // no-call and call in the same genotype

	}

}