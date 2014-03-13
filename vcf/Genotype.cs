using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace Bio.VCF
{


	/// <summary>
	/// This class encompasses all the basic information about a genotype.  It is immutable.
	/// TODO: Stench of java, remove all get/set non-sense later
	/// </summary>
	public abstract class Genotype : IComparable<Genotype>
	//Class appears to be implemented by Lazy and Fast genotype
    {
        #region STATIC MEMBERS
        /// <summary>
		/// A list of genotype field keys corresponding to values we
		/// manage inline in the Genotype object.  They must not appear in the
		/// extended attributes map
		/// </summary>
		public static readonly string[] PRIMARY_KEYS = new string[]{ VCFConstants.GENOTYPE_FILTER_KEY, VCFConstants.GENOTYPE_KEY, VCFConstants.GENOTYPE_QUALITY_KEY, VCFConstants.DEPTH_KEY, VCFConstants.GENOTYPE_ALLELE_DEPTHS, VCFConstants.GENOTYPE_PL_KEY};
        public const string PHASED_ALLELE_SEPARATOR = "|";
		public const string UNPHASED_ALLELE_SEPARATOR = "/";

        /// <summary>
        /// a utility method for generating sorted strings from a map key set. </summary>
        /// <param name="c"> the map </param>
        /// @param <T> the key type </param>
        /// @param <V> the value type </param>
        /// <returns> a sting, enclosed in {}, with comma seperated key value pairs in order of the keys </returns>
        /// TODO: This is a stupid thing to have as a method in C#
        protected internal static string sortedString(IDictionary<string, object> c)
        {
            var q = from kv in c orderby kv.Key select kv.Key+"="+kv.Value.ToString();
            return "{"+String.Join(", ", q)+"}";
        }
        /// <summary>
        /// Returns a display name for field name with value v if this isn't -1.  Otherwise returns "" </summary>
        /// <param name="name"> of the field ("AD") </param>
        /// <param name="v"> the value of the field, or -1 if missing </param>
        /// <returns> a non-null string for display if the field is not missing </returns>
        protected internal static string toStringIfExists(string name, int v)
        {
            return v == -1 ? "" : " " + name + " " + v;
        }
        /// <summary>
        /// Returns a display name for field name with String value v if this isn't null.  Otherwise returns "" </summary>
        /// <param name="name"> of the field ("FT") </param>
        /// <param name="v"> the value of the field, or null if missing </param>
        /// <returns> a non-null string for display if the field is not missing </returns>
        protected internal static string toStringIfExists(string name, string v)
        {
            return v == null ? "" : " " + name + " " + v;
        }
        /// <summary>
        /// Returns a display name for field name with values vs if this isn't null.  Otherwise returns "" </summary>
        /// <param name="name"> of the field ("AD") </param>
        /// <param name="vs"> the value of the field, or null if missing </param>
        /// <returns> a non-null string for display if the field is not missing </returns>
        protected internal static string toStringIfExists(string name, int[] vs)
        {
            if (vs == null)
            {
                return "";
            }
            else
            {
                StringBuilder b = new StringBuilder();
                b.Append(" ").Append(name).Append(" ");
                for (int i = 0; i < vs.Length; i++)
                {
                    if (i != 0)
                    {
                        b.Append(",");
                    }
                    b.Append(vs[i]);
                }
                return b.ToString();
            }
        }
        /// <summary>
        /// Does the attribute map have a mapping involving a forbidden key (i.e.,
        /// one that's managed inline by this Genotypes object?
        /// </summary>
        /// <param name="attributes"> the extended attributes key
        /// @return </param>
        protected internal static bool hasForbiddenKey(IDictionary<string, object> attributes)
        {
            foreach (String forbidden in PRIMARY_KEYS)
            {
                if (attributes.ContainsKey(forbidden))
                {
                    return true;
                }
            }
            return false;
        }
        protected internal static bool isForbiddenKey(string key)
        {
            return PRIMARY_KEYS.Contains(key);
        }

        #endregion

        private readonly string sampleName;
		private GenotypeType? type = null;
		private readonly string filters;

		
        protected internal Genotype(string sampleName, string filters)
		{
			this.sampleName = sampleName;
			this.filters = filters;
		}

		/// <returns> the alleles for this genotype.  Cannot be null.  May be empty </returns>
		public abstract IList<Allele> Alleles {get;}


        /// <summary>
        ///  The sequencing depth of this sample, or -1 if this value is missing
        /// </summary>
        public abstract int DP { get; }

        /// <summary>
        /// The count of reads, one for each allele in the surrounding Variant context,
        ///  matching the corresponding allele, or null if this value is missing.  MUST
        ///  NOT BE MODIFIED! 
        /// </summary>
        public abstract int[] AD { get; }
        
        /// <summary>
        /// Unsafe low-level accessor the PL field itself, may be null.
        /// </summary>
        /// <returns> a pointer to the underlying PL data.  MUST NOT BE MODIFIED! </returns>
        public abstract int[] PL { get; }


        /// <summary>
        /// Are the alleles phased w.r.t. the global phasing system?
        /// </summary>
        /// <returns> true if yes </returns>
        public abstract bool Phased { get; }

        /// <summary>
        /// Returns the extended attributes for this object </summary>
        /// <returns> is never null, but is often isEmpty() </returns>
        public abstract IDictionary<string, object> ExtendedAttributes { get; }

        /// <summary>
        /// Get the ith allele in this genotype
        /// </summary>
        /// <param name="i"> the ith allele, must be < the ploidy, starting with 0 </param>
        /// <returns> the allele at position i, which cannot be null </returns>
        public abstract Allele getAllele(int i);

		/// <summary>
		/// Returns how many times allele appears in this genotype object?
		/// </summary>
		/// <param name="allele"> </param>
		/// <returns> a value >= 0 indicating how many times the allele occurred in this sample's genotype </returns>
		public virtual int countAllele(Allele allele)
		{
            return Alleles.Count(x=>x.Equals(allele));
		}


		/// <summary>
		/// What is the ploidy of this sample?
		/// </summary>
		/// <returns> the ploidy of this genotype.  0 if the site is no-called. </returns>
		public virtual int Ploidy
		{
			get
			{
				return Alleles.Count;
			}
		}

		/// <summary>
		/// Returns the name associated with this sample.
		/// </summary>
		/// <returns> a non-null String </returns>
		public string SampleName
		{
			get
			{
				return sampleName;
			}
		}

		/// <summary>
		/// Returns a phred-scaled quality score, or -1 if none is available
		/// @return
		/// </summary>
		public abstract int GQ {get;}

		/// <summary>
		/// Does the PL field have a value? </summary>
		/// <returns> true if there's a PL field value </returns>
		public bool HasPL
		{
            get
            {
			return PL != null;
            }
		}

		/// <summary>
		/// Does the AD field have a value? </summary>
		/// <returns> true if there's a AD field value </returns>
		public bool HasAD
		{
            get {return AD!=null;}
		}

		/// <summary>
		/// Does the GQ field have a value? </summary>
		/// <returns> true if there's a GQ field value </returns>
		public bool HasGQ
		{
            get
            {
			return GQ != -1;
            }
		}

		/// <summary>
		/// Does the DP field have a value? </summary>
		/// <returns> true if there's a DP field value </returns>
		public bool HasDP
		{
            get{
                return DP != -1;
            }
			
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// The type of this genotype
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <returns> the high-level type of this sample's genotype </returns>
		public virtual GenotypeType Type
		{
			get
			{
				if (!type.HasValue)
				{
					type = determineType();
				}
				return type.Value;
			}
		}

		/// <summary>
		/// Internal code to determine the type of the genotype from the alleles vector </summary>
		/// <returns> the type </returns>
		protected internal virtual GenotypeType determineType() // we should never call if already calculated
		{
			// TODO -- this code is slow and could be optimized for the diploid case
			IList<Allele> alleles = Alleles;
			if (alleles.Count == 0)
			{
				return GenotypeType.UNAVAILABLE;
			}

			bool sawNoCall = false, sawMultipleAlleles = false;
			Allele observedAllele = null;

			foreach (Allele allele in alleles)
			{
				if (allele.NoCall)
				{
					sawNoCall = true;
				}
				else if (observedAllele == null)
				{
					observedAllele = allele;
				}
				else if (!allele.Equals(observedAllele))
				{
					sawMultipleAlleles = true;
				}
			}

			if (sawNoCall)
			{
				if (observedAllele == null)
				{
					return GenotypeType.NO_CALL;
				}
				return GenotypeType.MIXED;
			}

			if (observedAllele == null)
			{
				throw new Exception("BUG: there are no alleles present in this genotype but the alleles list is not null");
			}

			return sawMultipleAlleles ? GenotypeType.HET : observedAllele.Reference ? GenotypeType.HOM_REF : GenotypeType.HOM_VAR;
		}

		/// <returns> true if all observed alleles are the same (regardless of whether they are ref or alt); if any alleles are no-calls, this method will return false. </returns>
		public virtual bool Hom
		{
			get
			{
				return HomRef || HomVar;
			}
		}

		/// <returns> true if all observed alleles are ref; if any alleles are no-calls, this method will return false. </returns>
		public virtual bool HomRef
		{
			get
			{
				return Type == GenotypeType.HOM_REF;
			}
		}

		/// <returns> true if all observed alleles are alt; if any alleles are no-calls, this method will return false. </returns>
		public virtual bool HomVar
		{
			get
			{
				return Type == GenotypeType.HOM_VAR;
			}
		}

		/// <returns> true if we're het (observed alleles differ); if the ploidy is less than 2 or if any alleles are no-calls, this method will return false. </returns>
		public virtual bool Het
		{
			get
			{
				return Type == GenotypeType.HET;
			}
		}

		/// <returns> true if this genotype is not actually a genotype but a "no call" (e.g. './.' in VCF); if any alleles are not no-calls (even if some are), this method will return false. </returns>
		public virtual bool NoCall
		{
			get
			{
				return Type == GenotypeType.NO_CALL;
			}
		}

		/// <returns> true if this genotype is comprised of any alleles that are not no-calls (even if some are). </returns>
		public virtual bool Called
		{
			get
			{
				return Type != GenotypeType.NO_CALL && Type != GenotypeType.UNAVAILABLE;
			}
		}

		/// <returns> true if this genotype is comprised of both calls and no-calls. </returns>
		public virtual bool Mixed
		{
			get
			{
				return Type == GenotypeType.MIXED;
			}
		}

		/// <returns> true if the type of this genotype is set. </returns>
		public virtual bool Available
		{
			get
			{
				return Type != GenotypeType.UNAVAILABLE;
			}
		}

		// ------------------------------------------------------------------------------
		//
		// methods for getting genotype likelihoods for a genotype object, if present
		//
		// ------------------------------------------------------------------------------

		/// <returns> Returns true if this Genotype has PL field values </returns>
		public virtual bool hasLikelihoods()
		{
			return PL != null;
		}

		/// <summary>
		/// Convenience function that returns a string representation of the PL field of this
		/// genotype, or . if none is available.
		/// </summary>
		/// <returns> a non-null String representation for the PL of this sample </returns>
		public virtual string LikelihoodsString
		{
			get
			{
				return hasLikelihoods() ? Likelihoods.ToString() : VCFConstants.MISSING_VALUE_v4;
			}
		}

		/// <summary>
		/// Returns the GenotypesLikelihoods data associated with this Genotype, or null if missing </summary>
		/// <returns> null or a GenotypesLikelihood object for this sample's PL field </returns>
		public virtual GenotypeLikelihoods Likelihoods
		{
			get
			{
				return hasLikelihoods() ? GenotypeLikelihoods.fromPLs(PL) : null;
			}
		}

		/// <summary>
		/// Are all likelihoods for this sample non-informative?
		/// 
		/// Returns true if all PLs are 0 => 0,0,0 => true
		/// 0,0,0,0,0,0 => true
		/// 0,10,100 => false
		/// </summary>
		/// <returns> true if all samples PLs are equal and == 0 </returns>
		public virtual bool NonInformative
		{
			get
			{
				if (PL == null)
				{
					return true;
				}
				else
				{
					foreach (int pl in PL)
					{
						if (pl != 0)
						{
							return false;
						}
					}
    
					return true;
				}
			}
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Many different string representations
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Return a VCF-like string representation for the alleles of this genotype.
		/// 
		/// Does not append the reference * marker on the alleles.
		/// </summary>
		/// <returns> a string representing the genotypes, or null if the type is unavailable. </returns>
		public virtual string GenotypeString
		{
			get
			{
				return getGenotypeString(true);
			}
		}

		/// <summary>
		/// Return a VCF-like string representation for the alleles of this genotype.
		/// 
		/// If ignoreRefState is true, will not append the reference * marker on the alleles.
		/// </summary>
		/// <returns> a string representing the genotypes, or null if the type is unavailable. </returns>
		public virtual string getGenotypeString(bool ignoreRefState)
		{
			if (Ploidy == 0)
			{
				return "NA";
			}
			// Notes:
			// 1. Make sure to use the appropriate separator depending on whether the genotype is phased
			// 2. If ignoreRefState is true, then we want just the bases of the Alleles (ignoring the '*' indicating a ref Allele)
			// 3. So that everything is deterministic with regards to integration tests, we sort Alleles (when the genotype isn't phased, of course)
			return String.Join(Phased ? PHASED_ALLELE_SEPARATOR : UNPHASED_ALLELE_SEPARATOR, ignoreRefState ? AlleleStrings : 
                (Phased ? Alleles.Select(x=>x.ToString()) : ParsingUtils.SortList(Alleles).Select(x=>x.ToString())));
		}

		/// <summary>
		/// Utility that returns a list of allele strings corresponding to the alleles in this sample
		/// @return
		/// </summary>
		protected internal virtual IList<string> AlleleStrings
		{
			get
			{
				IList<string> al = new List<string>(Ploidy);
				foreach (Allele a in Alleles)
				{
					al.Add(a.BaseString);
				}
    
				return al;
			}
		}

		public virtual string ToString()
		{
			return string.Format("[{0} {1}{2}{3}{4}{5}{6}{7}]", SampleName, getGenotypeString(false), toStringIfExists(VCFConstants.GENOTYPE_QUALITY_KEY, GQ), toStringIfExists(VCFConstants.DEPTH_KEY, DP), toStringIfExists(VCFConstants.GENOTYPE_ALLELE_DEPTHS, AD), toStringIfExists(VCFConstants.GENOTYPE_PL_KEY, PL), toStringIfExists(VCFConstants.GENOTYPE_FILTER_KEY, Filters), sortedString(ExtendedAttributes));
		}

		public virtual string toBriefString()
		{
			return string.Format("{0}:Q{1:D}", getGenotypeString(false), GQ);
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Comparison operations
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// comparable genotypes -> compareTo on the sample names </summary>
		/// <param name="genotype">
		/// @return </param>
		public virtual int CompareTo(Genotype genotype)
		{
			return SampleName.CompareTo(genotype.SampleName);
		}

		public virtual bool sameGenotype(Genotype other)
		{
			return sameGenotype(other, true);
		}

		public virtual bool sameGenotype(Genotype other, bool ignorePhase)
		{
			if (Ploidy != other.Ploidy)
			{
				return false; // gotta have the same number of allele to be equal
			}

			// By default, compare the elements in the lists of alleles, element-by-element
			ICollection<Allele> thisAlleles = this.Alleles;
			ICollection<Allele> otherAlleles = other.Alleles;

			if (ignorePhase) // do not care about order, only identity of Alleles
			{
				thisAlleles = new SortedSet<Allele>(thisAlleles); //implemented Allele.compareTo()
				otherAlleles = new SortedSet<Allele>(otherAlleles);
			}

			return thisAlleles.Equals(otherAlleles);
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// get routines for extended attributes
		//
		// ---------------------------------------------------------------------------------------------------------



		/// <summary>
		/// Is key associated with a value (even a null one) in the extended attributes?
		/// 
		/// Note this will not return true for the inline attributes DP, GQ, AD, or PL
		/// </summary>
		/// <param name="key"> a non-null string key to check for an association </param>
		/// <returns> true if key has a value in the extendedAttributes </returns>
		public virtual bool HasExtendedAttribute(string key)
		{
			return ExtendedAttributes.ContainsKey(key);
		}

		/// <summary>
		/// Get the extended attribute value associated with key, if possible
		/// </summary>
		/// <param name="key"> a non-null string key to fetch a value for </param>
		/// <param name="defaultValue"> the value to return if key isn't in the extended attributes </param>
		/// <returns> a value (potentially) null associated with key, or defaultValue if no association exists </returns>
		public virtual object getExtendedAttribute(string key, object defaultValue)
		{
			return HasExtendedAttribute(key) ? ExtendedAttributes[key] : defaultValue;
		}

		/// <summary>
		/// Same as #getExtendedAttribute with a null default
		/// </summary>
		/// <param name="key">
		/// @return </param>
		public virtual object GetExtendedAttribute(string key)
		{
			return getExtendedAttribute(key, null);
		}

		/// <summary>
		/// Returns the filter string associated with this Genotype.
		/// </summary>
		/// <returns> If this result == null, then the genotype is considered PASSing filters
		///   If the result != null, then the genotype has failed filtering for the reason(s)
		///   specified in result.  To be reference compliant multiple filter field
		///   string values can be encoded with a ; separator. </returns>
		public string Filters
		{
			get
			{
				return filters;
			}
		}

		/// <summary>
		/// Is this genotype filtered or not?
		/// </summary>
		/// <returns> returns false if getFilters() == null </returns>
		public bool Filtered
		{
			get
			{
				return Filters != null;
			}
		}

		
		/// <summary>
		/// A totally generic getter, that allows you to specific keys that correspond
		/// to even inline values (GQ, for example).  Can be very expensive.  Additionally,
		/// all int[] are converted inline into List<Integer> for convenience.
		/// </summary>
		/// <param name="key">
		/// @return </param>
		public virtual object GetAnyAttribute(string key)
		{
			if (key.Equals(VCFConstants.GENOTYPE_KEY))
			{
				return Alleles;
			}
			else if (key.Equals(VCFConstants.GENOTYPE_QUALITY_KEY))
			{
				return GQ;
			}
			else if (key.Equals(VCFConstants.GENOTYPE_ALLELE_DEPTHS))
			{
				return AD;
			}
			else if (key.Equals(VCFConstants.GENOTYPE_PL_KEY))
			{
				return PL;
			}
			else if (key.Equals(VCFConstants.DEPTH_KEY))
			{
				return DP;
			}
			else
			{
				return GetExtendedAttribute(key);
			}
		}
		public virtual bool hasAnyAttribute(string key)
		{
			if (key==VCFConstants.GENOTYPE_KEY)
			{
				return Available;
			}
			else if (key==VCFConstants.GENOTYPE_QUALITY_KEY)
			{
				return HasGQ;
			}
			else if (key==VCFConstants.GENOTYPE_ALLELE_DEPTHS)
			{
				return HasAD;
			}
			else if (key==VCFConstants.GENOTYPE_PL_KEY)
			{
				return HasPL;
			}
			else if (key==VCFConstants.DEPTH_KEY)
			{
				return HasDP;
			}
			else
			{
				return HasExtendedAttribute(key);
			}
		}
		// TODO -- add getAttributesAsX interface here

		// ------------------------------------------------------------------------------
		//
		// private utilities
		//
		// ------------------------------------------------------------------------------


        #region OBSOLETE_MEMBERS
        [Obsolete]
        public virtual bool hasLog10PError()
        {
            return HasGQ;
        }
        [Obsolete]
        public virtual double Log10PError
        {
            get
            {
                return GQ / -10.0;
            }
        }
        [Obsolete]
        public virtual int PhredScaledQual
        {
            get
            {
                return GQ;
            }
        }
        [Obsolete]
        public virtual string getAttributeAsString(string key, string defaultValue)
        {
            object x = GetExtendedAttribute(key);
            if (x == null)
            {
                return defaultValue;
            }
            if (x is string)
            {
                return (string)x;
            }
            return Convert.ToString(x); // throws an exception if this isn't a string
        }
        [Obsolete]
        public virtual int getAttributeAsInt(string key, int defaultValue)
        {
            object x = GetExtendedAttribute(key);
            if (x == null || x == VCFConstants.MISSING_VALUE_v4)
            {
                return defaultValue;
            }
            if (x is int)
            {
                return (int)x;
            }
            return Convert.ToInt32((string)x); // throws an exception if this isn't a string
        }
        [Obsolete]
        public virtual double getAttributeAsDouble(string key, double defaultValue)
        {
            object x = GetExtendedAttribute(key);
            if (x == null)
            {
                return defaultValue;
            }
            if (x is double)
            {
                return (double)x;
            }
            return Convert.ToDouble((string)x); // throws an exception if this isn't a string
        }
        #endregion 

    }
}