using System.Collections.Generic;

/*
* Copyright (c) 2012 The Broad Institute
* 
* Permission is hereby granted, free of charge, to any person
* obtaining a copy of this software and associated documentation
* files (the "Software"), to deal in the Software without
* restriction, including without limitation the rights to use,
* copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following
* conditions:
* 
* The above copyright notice and this permission notice shall be
* included in all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
* OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
* NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
* HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
* WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
* THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace Bio.VCF
{
	/// <summary>
	/// This class encompasses all the basic information about a genotype.  It is immutable.
	/// 
	/// A genotype has several key fields
	/// 
	/// -- a sample name, must be a non-null string
	/// 
	/// -- an ordered list of alleles, intrepreted as the genotype of the sample,
	///    each allele for each chromosome given in order.  If alleles = [a*, t]
	///    then the sample is a/t, with a (the reference from the *) the first
	///    chromosome and t on the second chromosome
	/// 
	/// -- a isPhased marker indicting where the alleles are phased with respect to some global
	///    coordinate system.  See VCF4.1 spec for a detailed discussion
	/// 
	/// -- Inline, optimized ints and int[] values for:
	///      -- GQ: the phred-scaled genotype quality, of -1 if it's missing
	/// 
	///      -- DP: the count of reads at this locus for this sample, of -1 if missing
	/// 
	///      -- AD: an array of counts of reads at this locus, one for each Allele at the site.
	///             that is, for each allele in the surrounding VariantContext.  Null if missing.
	/// 
	///      -- PL: phred-scaled genotype likelihoods in standard VCF4.1 order for
	///             all combinations of the alleles in the surrounding VariantContext, given
	///             the ploidy of the sample (from the alleles vector).  Null if missing.
	/// 
	/// -- A general map from String keys to -> Object values for all other attributes in
	///    this genotype.  Note that this map should not contain duplicate values for the
	///    standard bindings for GQ, DP, AD, and PL.  Genotype filters can be put into
	///    this genotype, but it isn't respected by the GATK in analyses
	/// 
	/// The only way to build a Genotype object is with a GenotypeBuilder, which permits values
	/// to be set in any order, which means that GenotypeBuilder may at some in the chain of
	/// sets pass through invalid states that are not permitted in a fully formed immutable
	/// Genotype.
	/// 
	/// Note this is a simplified, refactored Genotype object based on the original
	/// generic (and slow) implementation from the original VariantContext + Genotype
	/// codebase.
	/// 
	/// @author Mark DePristo
	/// @since 05/12
	/// </summary>
	public sealed class FastGenotype : Genotype
	{
		private readonly IList<Allele> alleles;
		private readonly bool isPhased;
		private readonly int GQ_Renamed;
		private readonly int DP_Renamed;
		private readonly int[] AD_Renamed;
		private readonly int[] PL_Renamed;
		private readonly Dictionary<string, object> extendedAttributes;

		/// <summary>
		/// The only way to make one of these, for use by GenotypeBuilder only
		/// </summary>
		/// <param name="sampleName"> </param>
		/// <param name="alleles"> </param>
		/// <param name="isPhased"> </param>
		/// <param name="GQ"> </param>
		/// <param name="DP"> </param>
		/// <param name="AD"> </param>
		/// <param name="PL"> </param>
		/// <param name="extendedAttributes"> </param>
		protected internal FastGenotype(string sampleName, IList<Allele> alleles, bool isPhased, int GQ, int DP, int[] AD, int[] PL, string filters, Dictionary<string, object> extendedAttributes) : base(sampleName, filters)
		{
			this.alleles = alleles;
			this.isPhased = isPhased;
			this.GQ_Renamed = GQ;
			this.DP_Renamed = DP;
			this.AD_Renamed = AD;
			this.PL_Renamed = PL;
			this.extendedAttributes = extendedAttributes;
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Implmenting the abstract methods
		//
		// ---------------------------------------------------------------------------------------------------------

		public override IList<Allele> Alleles
		{
			get
			{
				return alleles;
			}
		}

		public override Allele getAllele(int i)
		{
			return alleles[i];
		}

		public override bool Phased
		{
			get
			{
				return isPhased;
			}
		}

		public override int DP
		{
			get
			{
				return DP_Renamed;
			}
		}

		public override int[] AD
		{
			get
			{
				return AD_Renamed;
			}
		}

		public override int GQ
		{
			get
			{
				return GQ_Renamed;
			}
		}

		public override int[] PL
		{
			get
			{
				return PL_Renamed;
			}
		}

		// ---------------------------------------------------------------------------------------------------------
		// 
		// get routines for extended attributes
		//
		// ---------------------------------------------------------------------------------------------------------

		public override IDictionary<string, object> ExtendedAttributes
		{
			get
			{
				return extendedAttributes;
			}
		}

		/// <summary>
		/// Is values a valid AD or PL field </summary>
		/// <param name="values">
		/// @return </param>
		private static bool validADorPLField(int[] values)
		{
			if (values != null)
			{
				foreach (int v in values)
				{
					if (v < 0)
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}