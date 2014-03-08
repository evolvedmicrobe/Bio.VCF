using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Collections.ObjectModel;

namespace Bio.VCF
{
	/// <summary>
	/// A builder class for genotypes
	/// 
	/// Provides convenience setter methods for all of the Genotype field
	/// values.  Setter methods can be used in any order, allowing you to
	/// pass through states that wouldn't be allowed in the highly regulated
	/// immutable Genotype class.
	/// 
	/// All fields default to meaningful MISSING values.
	/// 
	/// Call make() to actually create the corresponding Genotype object from
	/// this builder.  Can be called multiple times to create independent copies,
	/// or with intervening sets to conveniently make similar Genotypes with
	/// slight modifications.
	/// 
	/// </summary>
	public sealed class GenotypeBuilder
    {
        #region STATIC STUFF
        private static readonly List<Allele> HAPLOID_NO_CALL = new List<Allele>(){Allele.NO_CALL};
		private static readonly List<Allele> DIPLOID_NO_CALL = new List<Allele>(){Allele.NO_CALL, Allele.NO_CALL};
        private static readonly Dictionary<string, object> NO_ATTRIBUTES = new Dictionary<string, object>();
        private static readonly ReadOnlyCollection<Allele> HAPLOID_NO_CALL_READONLY = HAPLOID_NO_CALL.AsReadOnly();
        private static readonly ReadOnlyCollection<Allele> DIPLOID_NO_CALL_READONLY = DIPLOID_NO_CALL.AsReadOnly();
        
        // -----------------------------------------------------------------
        //
        // Factory methods
        //
        // -----------------------------------------------------------------
        public static Genotype Create(string sampleName, List<Allele> alleles)
        {
            return (new GenotypeBuilder(sampleName, alleles)).Make();
        }
        public static Genotype Create(string sampleName, List<Allele> alleles, Dictionary<string, object> attributes)
        {
            var gb = new GenotypeBuilder(sampleName, alleles);
            gb.AddAttributes(attributes);
            return gb.Make();
        }
        protected internal static Genotype Create(string sampleName, List<Allele> alleles, double[] gls)
        {
            var gb = new GenotypeBuilder(sampleName, alleles);
            gb.setPL(gls);
            return gb.Make();
        }
        /// <summary>
        /// Create a new Genotype object for a sample that's missing from the VC (i.e., in
        /// the output header).  Defaults to a diploid no call genotype ./.
        /// </summary>
        /// <param name="sampleName"> the name of this sample </param>
        /// <returns> an initialized Genotype with sampleName that's a diploid ./. no call genotype </returns>
        public static Genotype CreateMissing(string sampleName, int ploidy)
        {
             ReadOnlyCollection<Allele> als;
            switch (ploidy)
            {
                case 1:
                    als= HAPLOID_NO_CALL_READONLY;
                    break;
                case 2:
                    als= DIPLOID_NO_CALL_READONLY;
                    break;
                default:
                    als = Enumerable.Range(0, ploidy).Select(x => Allele.NO_CALL).ToList().AsReadOnly();
                    break;
            }
            Dictionary<string, object> ea = NO_ATTRIBUTES;
            return new FastGenotype(sampleName, als, false,-1,-1,null,null,null, ea);
	    }
        #endregion

        private Dictionary<string, object> extendedAttributes = null;
		private string filters_Renamed;
        //TODO: What is this point of this?
		private int initialAttributeMapSize = 5;
      
		/// <summary>
		/// Create a empty builder.  Both a sampleName and alleles must be provided
		/// before trying to make a Genotype from this builder.
		/// </summary>
		public GenotypeBuilder(bool makeAlleleList=true)
		{
            GQ = -1;
            DP = -1;
            AD = null;
            PL = null;
            if (makeAlleleList)
            {
                Alleles = new List<Allele>();
            }
		}
		/// <summary>
		/// Create a builder using sampleName.  Alleles must be provided
		/// before trying to make a Genotype from this builder. </summary>
		/// <param name="sampleName"> </param>
		public GenotypeBuilder(string sampleName,bool makeAlleleList=true) :this(makeAlleleList)
		{
			SampleName=sampleName;
		}
		/// <summary>
		/// Make a builder using sampleName and alleles for starting values </summary>
		/// <param name="sampleName"> </param>
		/// <param name="alleles"> </param>
		public GenotypeBuilder(string sampleName, List<Allele> alleles):this(false)
		{
			SampleName=sampleName;
			Alleles=alleles;
		}
		/// <summary>
		/// Create a new builder starting with the values in Genotype g </summary>
		/// <param name="g"> </param>
		public GenotypeBuilder(Genotype g):this()
		{
			copy(g);
		}
		/// <summary>
        /// Copy all of the values for this builder from Genotype g 
		/// </summary>
		/// <param name="g"></param>
		/// <returns></returns>
        /// TODO: Why have a copy on a builder?  Either implement ICloneable or remove this old method.
        public GenotypeBuilder copy(Genotype g)
		{
			SampleName=g.SampleName;
			Alleles=g.Alleles.ToList();
			Phased=g.Phased;
			GQ=g.GQ;
			DP=g.DP;
			AD=g.AD;
			PL=g.PL;
			Filter=g.Filters;
			AddAttributes(g.ExtendedAttributes);
			return this;
		}
		/// <summary>
		/// Reset all of the builder attributes to their defaults.  After this
		/// function you must provide sampleName and alleles before trying to
		/// make more Genotypes.
		/// </summary>
		public void Reset(bool keepSampleName)
		{
			if (!keepSampleName)
			{
				SampleName = null;
			}
            pAlleles = null;//new List<Allele>(0);
			Phased = false;
			GQ = -1;
			DP = -1;
			AD = null;
			PL = null;
			filters_Renamed = null;
			extendedAttributes = null;
		}
		/// <summary>
		/// Create a new Genotype object using the values set in this builder.
		/// 
		/// After creation the values in this builder can be modified and more Genotypes
		/// created, althrough the contents of array values like PL should never be modified
		/// inline as they are not copied for efficiency reasons.
		/// </summary>
		/// <returns> a newly minted Genotype object with values provided from this builder </returns>
		public Genotype Make()
		{
			Dictionary<string, object> ea;
            if (extendedAttributes == null)
            { ea = NO_ATTRIBUTES; }
            else {ea= extendedAttributes; }
			return new FastGenotype(SampleName, pAlleles, Phased, GQ, DP, AD, PL, filters_Renamed, ea);
		}
		/// <summary>
		/// Set this genotype's name </summary>
		/// <param name="sampleName">
		/// @return </param>
		public string SampleName
		{
            get;set;
		}
        private List<Allele> pAlleles;
		/// <summary>
		/// Set this genotype's alleles </summary>
		/// <param name="alleles">
		/// @return </param>
		public List<Allele> Alleles
		{
            get
            {
                if (pAlleles == null)
                {
                    this.pAlleles = new List<Allele>();
                }    
                return this.pAlleles;
            }
            set
            {                        
                this.pAlleles = value;
            }           
		}
		/// <summary>
		/// Is this genotype phased? </summary>
		/// <param name="phased">
		/// @return </param>
		public bool Phased
		{get;set;}
		public int GQ
		{
            get;set;
		}
		/// <summary>
		/// This genotype has no GQ value
		/// @return
		/// </summary>
		public void noGQ()
		{
			GQ = -1;
		}
		/// <summary>
		/// This genotype has no AD value
		/// @return
		/// </summary>
		public void noAD()
		{
			AD = null;
	    }
		/// <summary>
		/// This genotype has no DP value
		/// @return
		/// </summary>
		public void noDP()
		{
			DP = -1;			
		}
		/// <summary>
		/// This genotype has no PL value
		/// @return
		/// </summary>
		public void noPL()
		{
			PL = null;			
		}
		/// <summary>
		/// This genotype has this DP value
		/// @return
		/// </summary>
		public int DP
		{
            get;set;
		}
		/// <summary>
		/// This genotype has this AD value
		/// @return
		/// </summary>
		public int[] AD
		{get;set;}
		/// <summary>
		/// This genotype has this PL value, as int[].  FAST
		/// </summary>
		public int[] PL
		{
            get;set;
		}
		/// <summary>
		/// This genotype has this PL value, converted from double[]. SLOW
		/// </summary>
		public void setPL(double[] GLs)
		{
			this.PL = GenotypeLikelihoods.fromLog10Likelihoods(GLs).AsPLs;		
		}
		/// <summary>
		/// This genotype has these attributes.
		/// 
		/// Cannot contain inline attributes (DP, AD, GQ, PL)
		/// </summary>
		public void AddAttributes(IDictionary<string, object> attributes)
		{
			foreach (KeyValuePair<string, object> pair in attributes)
			{
				AddAttribute(pair.Key, pair.Value);
			}
		}
		/// <summary>
		/// Tells this builder to remove all extended attributes
		/// </summary>
		public void noAttributes()
		{
			this.extendedAttributes = null;
		}
		/// <summary>
		/// This genotype has this attribute key / value pair.
		/// 
		/// Cannot contain inline attributes (DP, AD, GQ, PL)
		/// </summary>
		public void AddAttribute(string key, object value)
		{
			if (extendedAttributes == null)
			{
				extendedAttributes = new Dictionary<string, object>(initialAttributeMapSize);
			}
			extendedAttributes[key] = value;
		}
		/// <summary>
		/// Tells this builder to make a Genotype object that has had filters applied,
		/// which may be empty (passes) or have some value indicating the reasons
		/// why it's been filtered.
		/// </summary>
		/// <param name="filters"> non-null list of filters.  empty list => PASS </param>
		public void SetFilters(List<string> filters)
		{
			if (filters.Count == 0)
			{
                Filter = null;
			}
			else if (filters.Count == 1)
			{
                Filter = filters[0];
			}
			else
			{
                filters.Sort();
				Filter=String.Join(";", filters);
			}
		}
		/// <summary>
		/// varargs version of #filters </summary>
		/// <param name="filters">
		public void SetFilters(params string[] filters)
		{
			SetFilters(filters.ToList());
		}
		/// <summary>
		/// Most efficient version of setting filters -- just set the filters string to filters
		/// </summary>
		/// <param name="filter"> if filters == null or filters.equals("PASS") => genotype is PASS
		/// @return </param>
		public string Filter
		{
            set
            {
                this.filters_Renamed = VCFConstants.PASSES_FILTERS_v4.Equals(value) ? null : value;
            }

		}
		/// <summary>
		/// This genotype is unfiltered
		/// 
		/// @return
		/// </summary>
		public void SetUnFiltered()
		{
			Filter=null;
		}
		/// <summary>
		/// Tell's this builder that we have at most these number of attributes
		/// </summary>
		public void MaxAttributes(int i)
		{
			initialAttributeMapSize = i;
		}
	}
}