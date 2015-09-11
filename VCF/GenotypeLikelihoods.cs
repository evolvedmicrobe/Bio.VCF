using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;



namespace Bio.VCF
{
	public class GenotypeLikelihoods
	{
		private const int NUM_LIKELIHOODS_CACHE_N_ALLELES = 5;
		private const int NUM_LIKELIHOODS_CACHE_PLOIDY = 10;
		// caching numAlleles up to 5 and ploidy up to 10
		private static readonly int[][] numLikelihoodCache = GeneralUtils.ReturnRectangularIntArray(NUM_LIKELIHOODS_CACHE_N_ALLELES,NUM_LIKELIHOODS_CACHE_PLOIDY);
		public static readonly int MAX_PL = short.MaxValue;

		//
		// There are two objects here because we are lazy in creating both representations
		// for this object: a vector of log10 Probs and the PL phred-scaled string.  Supports
		// having one set during initializating, and dynamic creation of the other, if needed
		//
		private double[] log10Likelihoods = null;
		private string likelihoodsAsString_PLs = null;


		/// <summary>
		/// initialize num likelihoods cache
		/// </summary>
		static GenotypeLikelihoods()
		{
			// must be done before PLIndexToAlleleIndex
			for (int numAlleles = 1; numAlleles < NUM_LIKELIHOODS_CACHE_N_ALLELES; numAlleles++)
			{
				for (int ploidy = 1; ploidy < NUM_LIKELIHOODS_CACHE_PLOIDY; ploidy++)
				{
					numLikelihoodCache[numAlleles][ploidy] = calcNumLikelihoods(numAlleles, ploidy);
				}
			}
		}

		/// <summary>
		/// The maximum number of alleles that we can represent as genotype likelihoods
		/// </summary>
		public const int MAX_ALT_ALLELES_THAT_CAN_BE_GENOTYPED = 50;

		/*
		* a cache of the PL index to the 2 alleles it represents over all possible numbers of alternate alleles
		*/
		private static readonly GenotypeLikelihoodsAllelePair[] PLIndexToAlleleIndex = calculatePLcache(MAX_ALT_ALLELES_THAT_CAN_BE_GENOTYPED);

		public static GenotypeLikelihoods fromPLField(string PLs)
		{
			return new GenotypeLikelihoods(PLs);
		}

		[Obsolete]
		public static GenotypeLikelihoods fromGLField(string GLs)
		{
			return new GenotypeLikelihoods(parseDeprecatedGLString(GLs));
		}

		public static GenotypeLikelihoods fromLog10Likelihoods(double[] log10Likelihoods)
		{
			return new GenotypeLikelihoods(log10Likelihoods);
		}

		public static GenotypeLikelihoods fromPLs(int[] pls)
		{
			return new GenotypeLikelihoods(PLsToGLs(pls));
		}

		//
		// You must use the factory methods now
		//
		private GenotypeLikelihoods(string asString)
		{
			likelihoodsAsString_PLs = asString;
		}

		private GenotypeLikelihoods(double[] asVector)
		{
			log10Likelihoods = asVector;
		}

		/// <summary>
		/// Returns the genotypes likelihoods in negative log10 vector format.  pr{AA} = x, this
		/// vector returns math.log10(x) for each of the genotypes.  Can return null if the
		/// genotype likelihoods are "missing".
		/// 
		/// @return
		/// </summary>
		public  double[] AsVector
		{
			get
			{
				// assumes one of the likelihoods vector or the string isn't null
				if (log10Likelihoods == null)
				{
					// make sure we create the GL string if it doesn't already exist
					log10Likelihoods = parsePLsIntoLikelihoods(likelihoodsAsString_PLs);
				}
    
				return log10Likelihoods;
			}
		}

		public  int[] AsPLs
		{
			get
			{
				double[] GLs = AsVector;
				return GLs == null ? null : GLsToPLs(GLs);
			}
		}

		public override string ToString()
		{
			return AsString;
		}

		public string AsString
		{
			get
			{
				if (likelihoodsAsString_PLs == null)
				{
					// todo -- should we accept null log10Likelihoods and set PLs as MISSING?
					if (log10Likelihoods == null)
					{
						throw new VCFParsingError("BUG: Attempted to get likelihoods as strings and neither the vector nor the string is set!");
					}
					likelihoodsAsString_PLs = convertLikelihoodsToPLString(log10Likelihoods);
				}
    
				return likelihoodsAsString_PLs;
			}
		}

		public override bool Equals(object aThat)
		{
			//check for self-comparison
			if (this == aThat)
			{
				return true;
			}

			if (!(aThat is GenotypeLikelihoods))
			{
				return false;
			}
			GenotypeLikelihoods that = (GenotypeLikelihoods)aThat;

			// now a proper field-by-field evaluation can be made.
			// GLs are considered equal if the corresponding PLs are equal
            int[] me=AsPLs;
            int[] other=that.AsPLs;
            return me.Length == other.Length && me.SequenceEqual(other);
		}

		//Return genotype likelihoods as an EnumMap with Genotypes as keys and likelihoods as values
		//Returns null in case of missing likelihoods
        //TODO: This seems to be making a strong diallelic site assumption, does not look safe
		public  Dictionary<GenotypeType, double> getAsMap(bool normalizeFromLog10)
		{
			//Make sure that the log10likelihoods are set
			double[] likelihoods = normalizeFromLog10 ? GeneralUtils.normalizeFromLog10(AsVector) : AsVector;
			if (likelihoods == null)
			{
				return null;
			}
			Dictionary<GenotypeType, double> likelihoodsMap = new Dictionary<GenotypeType, double>();
			likelihoodsMap[GenotypeType.HOM_REF]=likelihoods[(int)GenotypeType.HOM_REF-1];
			likelihoodsMap[GenotypeType.HET]=likelihoods[(int)GenotypeType.HET-1];
			likelihoodsMap[GenotypeType.HOM_VAR]= likelihoods[(int)GenotypeType.HOM_VAR-1];
			return likelihoodsMap;
		}

		private double getLog10GQ(IList<Allele> genotypeAlleles, IList<Allele> contextAlleles)
		{
			int allele1Index = contextAlleles.IndexOf(genotypeAlleles[0]);
			int allele2Index = contextAlleles.IndexOf(genotypeAlleles[1]);
			int plIndex = calculatePLindex(allele1Index,allele2Index);
			return getGQLog10FromLikelihoods(plIndex,AsVector);
		}

		public  double getLog10GQ(Genotype genotype, IList<Allele> vcAlleles)
		{
			return getLog10GQ(genotype.Alleles,vcAlleles);
		}

		public  double getLog10GQ(Genotype genotype, VariantContext context)
		{
			return getLog10GQ(genotype,context.Alleles);
		}

		public static double getGQLog10FromLikelihoods(int iOfChoosenGenotype, double[] likelihoods)
		{
			if (likelihoods == null)
			{
				return double.NegativeInfinity;
			}

			double qual = double.NegativeInfinity;
			for (int i = 0; i < likelihoods.Length; i++)
			{
				if (i == iOfChoosenGenotype)
				{
					continue;
				}
				if (likelihoods[i] >= qual)
				{
					qual = likelihoods[i];
				}
			}

			// qual contains now max(likelihoods[k]) for all k != bestGTguess
			qual = likelihoods[iOfChoosenGenotype] - qual;

			if (qual < 0)
			{
				// QUAL can be negative if the chosen genotype is not the most likely one individually.
				// In this case, we compute the actual genotype probability and QUAL is the likelihood of it not being the chosen one
				double[] normalized = GeneralUtils.normalizeFromLog10(likelihoods);
				double chosenGenotype = normalized[iOfChoosenGenotype];
				return Math.Log10(1.0 - chosenGenotype);
			}
			else
			{
				// invert the size, as this is the probability of making an error
				return -1 * qual;
			}
		}

		private static double[] parsePLsIntoLikelihoods(string likelihoodsAsString_PLs)
		{
			if (!likelihoodsAsString_PLs.Equals(VCFConstants.MISSING_VALUE_v4))
			{
				string[] strings = likelihoodsAsString_PLs.Split(',');
				double[] likelihoodsAsVector = new double[strings.Length];
				try
				{
					for (int i = 0; i < strings.Length; i++)
					{
						likelihoodsAsVector[i] = Convert.ToInt32(strings[i]) / -10.0;
					}
				}
				catch (System.FormatException e)
				{
					throw new VCFParsingError("The GL/PL tag contains non-integer values: " + likelihoodsAsString_PLs,e);
				}
				return likelihoodsAsVector;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Back-compatibility function to read old style GL formatted genotype likelihoods in VCF format </summary>
		/// <param name="GLString">
		/// @return </param>
		private static double[] parseDeprecatedGLString(string GLString)
		{
			if (!GLString.Equals(VCFConstants.MISSING_VALUE_v4))
			{
				string[] strings = GLString.Split(',');
				double[] likelihoodsAsVector = new double[strings.Length];
				for (int i = 0; i < strings.Length; i++)
				{
					likelihoodsAsVector[i] = Convert.ToDouble(strings[i]);
				}
				return likelihoodsAsVector;
			}

			return null;
		}

		private static string convertLikelihoodsToPLString(double[] GLs)
		{
			if (GLs == null)
			{
				return VCFConstants.MISSING_VALUE_v4;
			}

			StringBuilder s = new StringBuilder();
			bool first = true;
			foreach (int pl in GLsToPLs(GLs))
			{
				if (!first)
				{
					s.Append(",");
				}
				else
				{
					first = false;
				}

				s.Append(pl);
			}

			return s.ToString();
		}

		private static int[] GLsToPLs(double[] GLs)
		{
			int[] pls = new int[GLs.Length];
			double adjust = maxPL(GLs);
			for (int i = 0; i < GLs.Length; i++)
			{
				pls[i] = (int)Math.Round(Math.Min(-10 * (GLs[i] - adjust), MAX_PL));
			}
			return pls;
		}

		private static double maxPL(double[] GLs)
		{
			double adjust = double.NegativeInfinity;
			foreach (double l in GLs)
			{
				adjust = Math.Max(adjust, l);
			}
			return adjust;
		}

		private static double[] PLsToGLs(int[] pls)
		{
			double[] likelihoodsAsVector = new double[pls.Length];
			for (int i = 0; i < pls.Length; i++)
			{
				likelihoodsAsVector[i] = pls[i] / -10.0;
			}
			return likelihoodsAsVector;
		}

		// -------------------------------------------------------------------------------------
		//
		// Static conversion utilities, going from GL/PL index to allele index and vice versa.
		//
		// -------------------------------------------------------------------------------------

		/*
		* Class representing the 2 alleles (or rather their indexes into VariantContext.getAllele()) corresponding to a specific PL index.
		* Note that the reference allele is always index=0.
		*/
		public class GenotypeLikelihoodsAllelePair
		{
			public readonly int alleleIndex1, alleleIndex2;

			public GenotypeLikelihoodsAllelePair(int alleleIndex1, int alleleIndex2)
			{
				this.alleleIndex1 = alleleIndex1;
				this.alleleIndex2 = alleleIndex2;
			}
		}

		private static GenotypeLikelihoodsAllelePair[] calculatePLcache(int altAlleles)
		{
			int numLikelihoods = GenotypeLikelihoods.numLikelihoods(1 + altAlleles, 2);
			GenotypeLikelihoodsAllelePair[] cache = new GenotypeLikelihoodsAllelePair[numLikelihoods];

			// for all possible combinations of 2 alleles
			for (int allele1 = 0; allele1 <= altAlleles; allele1++)
			{
				for (int allele2 = allele1; allele2 <= altAlleles; allele2++)
				{
					cache[calculatePLindex(allele1, allele2)] = new GenotypeLikelihoodsAllelePair(allele1, allele2);
				}
			}

			// a bit of sanity checking
			for (int i = 0; i < cache.Length; i++)
			{
				if (cache[i] == null)
				{
					throw new Exception("BUG: cache entry " + i + " is unexpected null");
				}
			}

			return cache;
		}

		// -------------------------------------------------------------------------------------
		//
		// num likelihoods given number of alleles and ploidy
		//
		// -------------------------------------------------------------------------------------

		/// Actually does the computation in <seealso cref= #numLikelihoods
		/// </seealso>
		/// <param name="numAlleles"> </param>
		/// <param name="ploidy">
		/// @return </param>
		private static int calcNumLikelihoods(int numAlleles, int ploidy)
		{
			if (numAlleles == 1)
			{
				return 1;
			}
			else if (ploidy == 1)
			{
				return numAlleles;
			}
			else
			{
				int acc = 0;
				for (int k = 0; k <= ploidy; k++)
				{
					acc += calcNumLikelihoods(numAlleles - 1, ploidy - k);
				}
				return acc;
			}
		}

		/// <summary>
		/// Compute how many likelihood elements are associated with the given number of alleles
		/// Equivalent to asking in how many ways N non-negative integers can add up to P is S(N,P)
		/// where P = ploidy (number of chromosomes) and N = total # of alleles.
		/// Each chromosome can be in one single state (0,...,N-1) and there are P of them.
		/// Naive solution would be to store N*P likelihoods, but this is not necessary because we can't distinguish chromosome states, but rather
		/// only total number of alt allele counts in all chromosomes.
		/// 
		/// For example, S(3,2) = 6: For alleles A,B,C, on a diploid organism we have six possible genotypes:
		/// AA,AB,BB,AC,BC,CC.
		/// Another way of expressing is with vector (#of A alleles, # of B alleles, # of C alleles)
		/// which is then, for ordering above, (2,0,0), (1,1,0), (0,2,0), (1,1,0), (0,1,1), (0,0,2)
		/// In general, for P=2 (regular biallelic), then S(N,2) = N*(N+1)/2
		/// 
		/// Note this method caches the value for most common num Allele / ploidy combinations for efficiency
		/// 
		/// Recursive implementation:
		///   S(N,P) = sum_{k=0}^P S(N-1,P-k)
		///  because if we have N integers, we can condition 1 integer to be = k, and then N-1 integers have to sum to P-K
		/// With initial conditions
		///   S(N,1) = N  (only way to have N integers add up to 1 is all-zeros except one element with a one. There are N of these vectors)
		///   S(1,P) = 1 (only way to have 1 integer add to P is with that integer P itself).
		/// </summary>
		///   <param name="numAlleles">      Number of alleles (including ref) </param>
		///   <param name="ploidy">          Ploidy, or number of chromosomes in set </param>
		///   <returns>    Number of likelihood elements we need to hold. </returns>
		public static int numLikelihoods(int numAlleles, int ploidy)
		{
			if (numAlleles < NUM_LIKELIHOODS_CACHE_N_ALLELES && ploidy < NUM_LIKELIHOODS_CACHE_PLOIDY)
			{
				return numLikelihoodCache[numAlleles][ploidy];
			}
			else
			{
				// have to calculate on the fly
				return calcNumLikelihoods(numAlleles, ploidy);
			}
		}

		// As per the VCF spec: "the ordering of genotypes for the likelihoods is given by: F(j/k) = (k*(k+1)/2)+j.
		// In other words, for biallelic sites the ordering is: AA,AB,BB; for triallelic sites the ordering is: AA,AB,BB,AC,BC,CC, etc."
		// Assumes that allele1Index < allele2Index
		public static int calculatePLindex(int allele1Index, int allele2Index)
		{
			return (allele2Index * (allele2Index + 1) / 2) + allele1Index;
		}

		/// <summary>
		/// get the allele index pair for the given PL
		/// </summary>
		/// <param name="PLindex">   the PL index </param>
		/// <returns> the allele index pair </returns>
		public static GenotypeLikelihoodsAllelePair getAllelePair(int PLindex)
		{
			// make sure that we've cached enough data
			if (PLindex >= PLIndexToAlleleIndex.Length)
			{
				throw new Exception("Internal limitation: cannot genotype more than " + MAX_ALT_ALLELES_THAT_CAN_BE_GENOTYPED + " alleles");
			}

			return PLIndexToAlleleIndex[PLindex];
		}

		// An index conversion from the deprecated PL ordering to the new VCF-based ordering for up to 3 alternate alleles
		protected internal static readonly int[] PLindexConversion = new int[]{0, 1, 3, 6, 2, 4, 7, 5, 8, 9};

		/// <summary>
		/// get the PL indexes (AA, AB, BB) for the given allele pair; assumes allele1Index <= allele2Index.
		/// </summary>
		/// <param name="allele1Index">    the index in VariantContext.getAllele() of the first allele </param>
		/// <param name="allele2Index">    the index in VariantContext.getAllele() of the second allele </param>
		/// <returns> the PL indexes </returns>
		public static int[] getPLIndecesOfAlleles(int allele1Index, int allele2Index)
		{

			int[] indexes = new int[3];
			indexes[0] = calculatePLindex(allele1Index, allele1Index);
			indexes[1] = calculatePLindex(allele1Index, allele2Index);
			indexes[2] = calculatePLindex(allele2Index, allele2Index);
			return indexes;
		}
	}

}