using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bio.VCF.NewCollections;

namespace Bio.VCF
{
	
    /// <summary>
	/// Class VariantContext
	/// 
	/// == High-level overview ==
	/// 
	/// The VariantContext object is a single general class system for representing genetic variation data composed of:
	/// 
	/// * Allele: representing single genetic haplotypes (A, T, ATC, -)
	/// * Genotype: an assignment of alleles for each chromosome of a single named sample at a particular locus
	/// * VariantContext: an abstract class holding all segregating alleles at a locus as well as genotypes
	///    for multiple individuals containing alleles at that locus
	/// 
	/// The class system works by defining segregating alleles, creating a variant context representing the segregating
	/// information at a locus, and potentially creating and associating genotypes with individuals in the context.
	/// 
	/// All of the classes are highly validating -- call validate() if you modify them -- so you can rely on the
	/// self-consistency of the data once you have a VariantContext in hand.  The system has a rich set of assessor
	/// and manipulator routines, as well as more complex static support routines in VariantContextUtils.
	/// 
	/// The VariantContext (and Genotype) objects are attributed (supporting addition of arbitrary key/value pairs) and
	/// filtered (can represent a variation that is viewed as suspect).
	/// 
	/// VariantContexts are dynamically typed, so whether a VariantContext is a SNP, Indel, or NoVariant depends
	/// on the properties of the alleles in the context.  See the detailed documentation on the Type parameter below.
	/// 
	/// It's also easy to create subcontexts based on selected genotypes.
	/// 
	/// == Working with Variant Contexts ==
	/// By default, VariantContexts are immutable.  In order to access (in the rare circumstances where you need them)
	/// setter routines, you need to create MutableVariantContexts and MutableGenotypes.
	/// 
	/// === Some example data ===
	/// 
	/// Allele A, Aref, T, Tref;
	/// Allele del, delRef, ATC, ATCref;
	/// 
	/// A [ref] / T at 10
	/// GenomeLoc snpLoc = GenomeLocParser.createGenomeLoc("chr1", 10, 10);
	/// 
	/// - / ATC [ref] from 20-23
	/// GenomeLoc delLoc = GenomeLocParser.createGenomeLoc("chr1", 20, 22);
	/// 
	///  // - [ref] / ATC immediately after 20
	/// GenomeLoc insLoc = GenomeLocParser.createGenomeLoc("chr1", 20, 20);
	/// 
	/// === Alleles ===
	/// 
	/// See the documentation in the Allele class itself
	/// 
	/// What are they?
	/// 
	/// Alleles can be either reference or non-reference
	/// 
	/// Example alleles used here:
	/// 
	///   del = new Allele("-");
	///   A = new Allele("A");
	///   Aref = new Allele("A", true);
	///   T = new Allele("T");
	///   ATC = new Allele("ATC");
	/// 
	/// === Creating variant contexts ===
	/// 
	/// ==== By hand ====
	/// 
	/// Here's an example of a A/T polymorphism with the A being reference:
	/// 
	/// <pre>
	/// VariantContext vc = new VariantContext(name, snpLoc, Arrays.asList(Aref, T));
	/// </pre>
	/// 
	/// If you want to create a non-variant site, just put in a single reference allele
	/// 
	/// <pre>
	/// VariantContext vc = new VariantContext(name, snpLoc, Arrays.asList(Aref));
	/// </pre>
	/// 
	/// A deletion is just as easy:
	/// 
	/// <pre>
	/// VariantContext vc = new VariantContext(name, delLoc, Arrays.asList(ATCref, del));
	/// </pre>
	/// 
	/// The only 2 things that distinguishes between a insertion and deletion are the reference allele
	/// and the location of the variation.  An insertion has a Null reference allele and at least
	/// one non-reference Non-Null allele.  Additionally, the location of the insertion is immediately after
	/// a 1-bp GenomeLoc (at say 20).
	/// 
	/// <pre>
	/// VariantContext vc = new VariantContext("name", insLoc, Arrays.asList(delRef, ATC));
	/// </pre>
	/// 
	/// ==== Converting rods and other data structures to VCs ====
	/// 
	/// You can convert many common types into VariantContexts using the general function:
	/// 
	/// <pre>
	/// VariantContextAdaptors.convertToVariantContext(name, myObject)
	/// </pre>
	/// 
	/// dbSNP and VCFs, for example, can be passed in as myObject and a VariantContext corresponding to that
	/// object will be returned.  A null return type indicates that the type isn't yet supported.  This is the best
	/// and easiest way to create contexts using RODs.
	/// 
	/// 
	/// === Working with genotypes ===
	/// 
	/// <pre>
	/// List<Allele> alleles = Arrays.asList(Aref, T);
	/// Genotype g1 = new Genotype(Arrays.asList(Aref, Aref), "g1", 10);
	/// Genotype g2 = new Genotype(Arrays.asList(Aref, T), "g2", 10);
	/// Genotype g3 = new Genotype(Arrays.asList(T, T), "g3", 10);
	/// VariantContext vc = new VariantContext(snpLoc, alleles, Arrays.asList(g1, g2, g3));
	/// </pre>
	/// 
	/// At this point we have 3 genotypes in our context, g1-g3.
	/// 
	/// You can assess a good deal of information about the genotypes through the VariantContext:
	/// 
	/// <pre>
	/// vc.hasGenotypes()
	/// vc.isMonomorphicInSamples()
	/// vc.isPolymorphicInSamples()
	/// vc.getSamples().size()
	/// 
	/// vc.getGenotypes()
	/// vc.getGenotypes().get("g1")
	/// vc.hasGenotype("g1")
	/// 
	/// vc.getCalledChrCount()
	/// vc.getCalledChrCount(Aref)
	/// vc.getCalledChrCount(T)
	/// </pre>
	/// 
	/// === NO_CALL alleles ===
	/// 
	/// The system allows one to create Genotypes carrying special NO_CALL alleles that aren't present in the
	/// set of context alleles and that represent undetermined alleles in a genotype:
	/// 
	/// Genotype g4 = new Genotype(Arrays.asList(Allele.NO_CALL, Allele.NO_CALL), "NO_DATA_FOR_SAMPLE", 10);
	/// 
	/// 
	/// === subcontexts ===
	/// It's also very easy get subcontext based only the data in a subset of the genotypes:
	/// 
	/// <pre>
	/// VariantContext vc12 = vc.subContextFromGenotypes(Arrays.asList(g1,g2));
	/// VariantContext vc1 = vc.subContextFromGenotypes(Arrays.asList(g1));
	/// </pre>
	/// 
	/// <s3>
	///     Fully decoding.  Currently VariantContexts support some fields, particularly those
	///     stored as generic attributes, to be of any type.  For example, a field AB might
	///     be naturally a floating point number, 0.51, but when it's read into a VC its
	///     not decoded into the Java presentation but left as a string "0.51".  A fully
	///     decoded VariantContext is one where all values have been converted to their
	///     corresponding Java object types, based on the types declared in a VCFHeader.
	/// 
	///     The fullyDecode() takes a header object and creates a new fully decoded VariantContext
	///     where all fields are converted to their true java representation.  The VCBuilder
	///     can be told that all fields are fully decoded, in which case no work is done when
	///     asking for a fully decoded version of the VC.
	/// </s3>
	/// 
	/// NIGEL NOTE: This class was way too big, I divided it into 3 partial classes, VariantContextStatics.cs 
    /// contains the statics for this class, while VariantContextGetters.cs contains a lot of get methods that 
    /// do not seem useful in C# and may be removed in a future version
	/// </summary>
	public partial class VariantContext
    {
        
		/// <summary>
		/// The location of this VariantContext </summary>
		protected internal readonly string contig;
		protected internal readonly long start;
		protected internal readonly long stop;
		private readonly string ID_Renamed;
        public string ID
        {
            get
            {
                return ID_Renamed;
            }
        }
        private bool fullyDecoded = false;
        protected internal CommonInfo commonInfo = null;
        public CommonInfo CommonInfo
        {
            get
            {
                return commonInfo;
            }
        }

		/// <summary>
		/// The type (cached for performance reasons) of this context </summary>
		protected internal VariantType? type = null;

		/// <summary>
		/// A set of the alleles segregating in this context </summary>
		protected internal readonly IList<Allele> alleles;
        /// <summary>
        /// Gets the alleles.  This method should return all of the alleles present at the location,
        /// including the reference allele.  There are no constraints imposed on the ordering of alleles
        /// in the set. If the reference is not an allele in this context it will not be included.
        /// </summary>
        /// <returns> the set of alleles </returns>
        public IList<Allele> Alleles
        {
            get
            {
                return alleles;
            }
        }

		/// <summary>
		/// A mapping from sampleName -> genotype objects for all genotypes associated with this context </summary>
		protected internal GenotypesContext genotypes = null;

		/// <summary>
		/// Counts for each of the possible Genotype types in this context </summary>
		protected internal int[] genotypeCounts = null;

		// a fast cached access point to the ref / alt alleles for biallelic case
		private Allele REF = null;

		// set to the alt allele when biallelic, otherwise == null
		private Allele ALT = null;

		/* cached monomorphic value: null -> not yet computed, False, True */
		private bool? monomorphic = null;

		// ---------------------------------------------------------------------------------------------------------
		//
		// constructors: see VariantContextBuilder
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="other"> the VariantContext to copy </param>
		protected internal VariantContext(VariantContext other) : this(other.Source, other.ID, other.Chr, other.Start, other.End, other.Alleles, other.Genotypes, other.Log10PError, other.FiltersMaybeNull, other.Attributes, other.fullyDecoded, NO_VALIDATION)
		{
		}

		/// <summary>
		/// the actual constructor.  Private access only
		/// </summary>
		/// <param name="source">          source </param>
		/// <param name="contig">          the contig </param>
		/// <param name="start">           the start base (one based) </param>
		/// <param name="stop">            the stop reference base (one based) </param>
		/// <param name="alleles">         alleles </param>
		/// <param name="genotypes">       genotypes map </param>
		/// <param name="log10PError">  qual </param>
		/// <param name="filters">         filters: use null for unfiltered and empty set for passes filters </param>
		/// <param name="attributes">      attributes </param>
		/// <param name="validationToPerform">     set of validation steps to take </param>
		protected internal VariantContext(string source, string ID, string contig, long start, long stop, ICollection<Allele> alleles, GenotypesContext genotypes, double log10PError, ISet<string> filters, IDictionary<string, object> attributes, bool fullyDecoded, Validation validationToPerform)
		{
			if (contig == null)
			{
				throw new System.ArgumentException("Contig cannot be null");
			}
			this.contig = contig;
			this.start = start;
			this.stop = stop;

			// intern for efficiency.  equals calls will generate NPE if ID is inappropriately passed in as null
			if (ID == null || ID.Equals(""))
			{
				throw new System.ArgumentException("ID field cannot be the null or the empty string");
			}
			this.ID_Renamed = ID.Equals(VCFConstants.EMPTY_ID_FIELD) ? VCFConstants.EMPTY_ID_FIELD : ID;

			this.commonInfo = new CommonInfo(source, log10PError, filters, attributes);

			if (alleles == null)
			{
				throw new System.ArgumentException("Alleles cannot be null");
			}

			// we need to make this a LinkedHashSet in case the user prefers a given ordering of alleles
			this.alleles = makeAlleles(alleles);

			if (genotypes == null || genotypes == NO_GENOTYPES)
			{
				this.genotypes = NO_GENOTYPES;
			}
			else
			{
                genotypes.Mutable = false;
				this.genotypes = genotypes;
			}

			// cache the REF and ALT alleles
			int nAlleles = alleles.Count;
			foreach (Allele a in alleles)
			{
				if (a.Reference)
				{
					REF = a;
				} // only cache ALT when biallelic
				else if (nAlleles == 2)
				{
					ALT = a;
				}
			}

			this.fullyDecoded = fullyDecoded;
            validate(validationToPerform);
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Selectors
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// This method subsets down to a set of samples.
		/// 
		/// At the same time returns the alleles to just those in use by the samples,
		/// if rederiveAllelesFromGenotypes is true, otherwise the full set of alleles
		/// in this VC is returned as the set of alleles in the subContext, even if
		/// some of those alleles aren't in the samples
		/// 
		/// WARNING: BE CAREFUL WITH rederiveAllelesFromGenotypes UNLESS YOU KNOW WHAT YOU ARE DOING
		/// </summary>
		/// <param name="sampleNames">    the sample names </param>
		/// <param name="rederiveAllelesFromGenotypes"> if true, returns the alleles to just those in use by the samples, true should be default </param>
		/// <returns> new VariantContext subsetting to just the given samples </returns>
		public VariantContext subContextFromSamples(ISet<string> sampleNames, bool rederiveAllelesFromGenotypes)
		{
			if (sampleNames.SetEquals(SampleNames) && !rederiveAllelesFromGenotypes)
			{
				return this; // fast path when you don't have any work to do
			}
			else
			{
				VariantContextBuilder builder = new VariantContextBuilder(this);
				GenotypesContext newGenotypes = genotypes.subsetToSamples(sampleNames);
				if (rederiveAllelesFromGenotypes)
				{
					builder.SetAlleles(allelesOfGenotypes(newGenotypes));
				}
				else
				{
					builder.SetAlleles(alleles);
				}

                builder.SetGenotypes(newGenotypes);
                return builder.make();
			}
		}

		/// <seealso cref= #subContextFromSamples(java.util.Set, boolean) with rederiveAllelesFromGenotypes = true
		/// </seealso>
		/// <param name="sampleNames">
		/// @return </param>
		public  VariantContext subContextFromSamples(ISet<string> sampleNames)
		{
			return subContextFromSamples(sampleNames, true);
		}
		public  VariantContext subContextFromSample(string sampleName)
		{
            var singleton = new HashSet<string>();
            singleton.Add(sampleName);
			return subContextFromSamples(singleton);
		}
		/// <summary>
		/// helper routine for subcontext </summary>
		/// <param name="genotypes"> genotypes </param>
		/// <returns> allele set </returns>
		private ISet<Allele> allelesOfGenotypes(ICollection<Genotype> genotypes)
		{
			ISet<Allele> alleles = new HashSet<Allele>();
			bool addedref = false;
			foreach (Genotype g in genotypes)
			{
				foreach (Allele a in g.Alleles)
				{
					addedref = addedref || a.Reference;
					if (a.Called)
					{
						alleles.Add(a);
					}
				}
			}
			if (!addedref)
			{
				alleles.Add(Reference);
			}

			return alleles;
		}

        #region Type Operations

        /// <summary>
		/// see: http://www.ncbi.nlm.nih.gov/bookshelf/br.fcgi?book=handbook&part=ch5&rendertype=table&id=ch5.ch5_t3
		/// 
		/// Format:
		/// dbSNP variation class
		/// Rules for assigning allele classes
		/// Sample allele definition
		/// 
		/// Single Nucleotide Polymorphisms (SNPs)a
		///   Strictly defined as single base substitutions involving A, T, C, or G.
		///   A/T
		/// 
		/// Deletion/Insertion Polymorphisms (DIPs)
		///   Designated using the full sequence of the insertion as one allele, and either a fully
		///   defined string for the variant allele or a '-' character to specify the deleted allele.
		///   This class will be assigned to a variation if the variation alleles are of different lengths or
		///   if one of the alleles is deleted ('-').
		///   T/-/CCTA/G
		/// 
		/// No-variation
		///   Reports may be submitted for segments of sequence that are assayed and determined to be invariant
		///   in the sample.
		///   (NoVariation)
		/// 
		/// Mixed
		///   Mix of other classes
		/// 
		/// Also supports NO_VARIATION type, used to indicate that the site isn't polymorphic in the population
		/// 
		/// 
		/// Not currently supported:
		/// 
		/// Heterozygous sequence
		/// The term heterozygous is used to specify a region detected by certain methods that do not
		/// resolve the polymorphism into a specific sequence motif. In these cases, a unique flanking
		/// sequence must be provided to define a sequence context for the variation.
		/// (heterozygous)
		/// 
		/// Microsatellite or short tandem repeat (STR)
		/// Alleles are designated by providing the repeat motif and the copy number for each allele.
		/// Expansion of the allele repeat motif designated in dbSNP into full-length sequence will
		/// be only an approximation of the true genomic sequence because many microsatellite markers are
		/// not fully sequenced and are resolved as size variants only.
		/// (CAC)8/9/10/11
		/// 
		/// Named variant
		/// Applies to insertion/deletion polymorphisms of longer sequence features, such as retroposon
		/// dimorphism for Alu or line elements. These variations frequently include a deletion '-' indicator
		/// for the absent allele.
		/// (alu) / -
		/// 
		/// Multi-Nucleotide Polymorphism (MNP)
		///   Assigned to variations that are multi-base variations of a single, common length
		///   GGA/AGT
		/// </summary>
		public enum VariantType
		{
			NO_VARIATION,
			SNP,
			MNP, // a multi-nucleotide polymorphism
			INDEL,
			SYMBOLIC,
			MIXED,
            NOT_SET
		}

		/// <summary>
		/// Determines (if necessary) and returns the type of this variation by examining the alleles it contains.
		/// </summary>
		/// <returns> the type of this VariantContext
		///  </returns>
		public  VariantType Type
		{
			get
			{
				if (!type.HasValue)
				{
					determineType();
				}
    
				return type.Value;
			}
		}

		/// <summary>
		/// convenience method for SNPs
		/// </summary>
		/// <returns> true if this is a SNP, false otherwise </returns>
		public bool SNP
		{
			get
			{
				return Type == VariantType.SNP;
			}
		}
		/// <summary>
		/// convenience method for variants
		/// </summary>
		/// <returns> true if this is a variant allele, false if it's reference </returns>
		public  bool Variant
		{
			get
			{
				return Type != VariantType.NO_VARIATION;
			}
		}
		/// <summary>
		/// convenience method for point events
		/// </summary>
		/// <returns> true if this is a SNP or ref site, false if it's an indel or mixed event </returns>
		public  bool PointEvent
		{
			get
			{
				return SNP || !Variant;
			}
		}
		/// <summary>
		/// convenience method for indels
		/// </summary>
		/// <returns> true if this is an indel, false otherwise </returns>
		public  bool Indel
		{
			get
			{
				return Type == VariantType.INDEL;
			}
		}
		/// <returns> true if the alleles indicate a simple insertion (i.e., the reference allele is Null) </returns>
		public bool SimpleInsertion
		{
			get
			{
				// can't just call !isSimpleDeletion() because of complex indels
				return Type == VariantType.INDEL && Biallelic && Reference.Length == 1;
			}
		}
		/// <returns> true if the alleles indicate a simple deletion (i.e., a single alt allele that is Null) </returns>
		public bool SimpleDeletion
		{
			get
			{
				// can't just call !isSimpleInsertion() because of complex indels
				return Type == VariantType.INDEL && Biallelic && getAlternateAllele(0).Length == 1;
			}
		}
		/// <returns> true if the alleles indicate neither a simple deletion nor a simple insertion </returns>
		public bool ComplexIndel
		{
			get
			{
				return Indel && !SimpleDeletion && !SimpleInsertion;
			}
		}
		public bool Symbolic
		{get
			{return Type == VariantType.SYMBOLIC;}
		}
		public bool StructuralIndel
		{
			get
			{
				if (Type == VariantType.INDEL)
				{
					IList<int> sizes = IndelLengths;
					if (sizes != null)
					{
                        return sizes.Any(s => s > MAX_ALLELE_SIZE_FOR_NON_SV);
					}
				}
				return false;
			}
		}
		/// 
		/// <returns> true if the variant is symbolic or a large indel </returns>
		public bool SymbolicOrSV
		{
			get
			{
				return Symbolic || StructuralIndel;
			}
		}
		public bool MNP
		{
			get
			{
				return Type == VariantType.MNP;
			}
		}
		/// <summary>
		/// convenience method for indels
		/// </summary>
		/// <returns> true if this is an mixed variation, false otherwise </returns>
		public  bool Mixed
		{
			get
			{
				return Type == VariantType.MIXED;
			}
		}
        #endregion
        
        
        // ---------------------------------------------------------------------------------------------------------
		//
		// Generic accessors
		//
		// ---------------------------------------------------------------------------------------------------------

		public bool hasID()
		{
			return ID != VCFConstants.EMPTY_ID_FIELD;
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// get routines to access context info fields
		//
		// ---------------------------------------------------------------------------------------------------------
		public  string Source
		{
			get
			{
				return commonInfo.Name;
			}
		}
		public  ISet<string> FiltersMaybeNull
		{
			get
			{
				return commonInfo.FiltersMaybeNull;
			}
		}
		public  ISet<string> Filters
		{
			get
			{
				return commonInfo.Filters;
			}
		}
		public  bool Filtered
		{
			get
			{
				return commonInfo.Filtered;
			}
		}
		public  bool NotFiltered
		{
			get
			{
				return commonInfo.NotFiltered;
			}
		}
		public  bool filtersWereApplied()
		{
			return commonInfo.filtersWereApplied();
		}
		public  bool hasLog10PError()
		{
			return commonInfo.hasLog10PError();
		}
		public  double Log10PError
		{
			get
			{
				return commonInfo.Log10PError;
			}
		}
		public  double PhredScaledQual
		{
			get
			{
				return commonInfo.PhredScaledQual;
			}
		}
        //TODO: Consider making string/string type
		public  IDictionary<string, object> Attributes
		{
			get
			{
				return commonInfo.Attributes;
			}
		}
		public bool hasAttribute(string key)
		{
			return commonInfo.hasAttribute(key);
		}
		public  object getAttribute(string key)
		{
			return commonInfo.getAttribute(key);
		}

		public  object getAttribute(string key, object defaultValue)
		{
			return commonInfo.getAttribute(key, defaultValue);
		}

		public  string getAttributeAsString(string key, string defaultValue)
		{
			return commonInfo.getAttributeAsString(key, defaultValue);
		}
		public  int getAttributeAsInt(string key, int defaultValue)
		{
			return commonInfo.getAttributeAsInt(key, defaultValue);
		}
		public  double getAttributeAsDouble(string key, double defaultValue)
		{
			return commonInfo.getAttributeAsDouble(key, defaultValue);
		}
		public  bool getAttributeAsBoolean(string key, bool defaultValue)
		{
			return commonInfo.getAttributeAsBoolean(key, defaultValue);
		}

		
		// ---------------------------------------------------------------------------------------------------------
		//
		// Working with alleles
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <returns> the reference allele for this context </returns>
		public  Allele Reference
		{
			get
			{
				Allele refe = REF;
				if (refe == null)
				{
					throw new VCFParsingError("BUG: no reference allele found at " + this);
				}
				return refe;
			}
		}
		/// <returns> true if the context is strictly bi-allelic </returns>
		public  bool Biallelic
		{
			get
			{
				return NAlleles == 2;
			}
		}

		/// <returns> The number of segregating alleles in this context </returns>
		public  int NAlleles
		{
			get
			{
				return alleles.Count;
			}
		}

		/// <summary>
		/// Returns the maximum ploidy of all samples in this VC, or default if there are no genotypes
		/// 
		/// This function is caching, so it's only expensive on the first call
		/// </summary>
		/// <param name="defaultPloidy"> the default ploidy, if all samples are no-called </param>
		/// <returns> default, or the max ploidy </returns>
		public  int getMaxPloidy(int defaultPloidy)
		{
			return genotypes.getMaxPloidy(defaultPloidy);
		}

		/// <returns> The allele sharing the same bases as this String.  A convenience method; better to use byte[] </returns>
		public  Allele getAllele(string allele)
		{
			return getAllele(VCFUtils.StringToSBytes(allele));
		}

		/// <returns> The allele sharing the same bases as this byte[], or null if no such allele is present. </returns>
		public  Allele getAllele(sbyte[] allele)
		{
			return Allele.getMatchingAllele(Alleles, allele);
		}

		/// <returns> True if this context contains Allele allele, or false otherwise </returns>
		public  bool hasAllele(Allele allele)
		{
			return hasAllele(allele, false, true);
		}
		public  bool hasAllele(Allele allele, bool ignoreRefState)
		{
			return hasAllele(allele, ignoreRefState, true);
		}

		public  bool hasAlternateAllele(Allele allele)
		{
			return hasAllele(allele, false, false);
		}

		public  bool hasAlternateAllele(Allele allele, bool ignoreRefState)
		{
			return hasAllele(allele, ignoreRefState, false);
		}

		private bool hasAllele(Allele allele, bool ignoreRefState, bool considerRefAllele)
		{
			if ((considerRefAllele && allele == REF) || allele == ALT) // optimization for cached cases
			{
				return true;
			}

			IList<Allele> allelesToConsider = considerRefAllele ? Alleles : AlternateAlleles;
			foreach (Allele a in allelesToConsider)
			{
				if (a.Equals(allele, ignoreRefState))
				{
					return true;
				}
			}

			return false;
		}

		
		/// <summary>
		/// Gets the alternate alleles.  This method should return all the alleles present at the location,
		/// NOT including the reference allele.  There are no constraints imposed on the ordering of alleles
		/// in the set.
		/// </summary>
		/// <returns> the set of alternate alleles </returns>
        //TODO: Verify first allele is at start, also this originally returned a sublist, changin it to just a copy of the list
		public  IList<Allele> AlternateAlleles
		{
			get
			{
                return Enumerable.Range(1,(alleles.Count-1)).Select(i=>alleles[i]).ToList();
				//Original java version
                //return alleles.subList(1, alleles.Count);
			}
		}

		/// <summary>
		/// Gets the sizes of the alternate alleles if they are insertion/deletion events, and returns a list of their sizes
		/// </summary>
		/// <returns> a list of indel lengths ( null if not of type indel or mixed ) </returns>
		public  IList<int> IndelLengths
		{
			get
			{
				if (Type != VariantType.INDEL && Type != VariantType.MIXED)
				{
					return null;
				}
    
				IList<int> lengths = new List<int>();
				foreach (Allele a in AlternateAlleles)
				{
					lengths.Add(a.Length - Reference.Length);
				}
    
				return lengths;
			}
		}

		/// <param name="i"> -- the ith allele (from 0 to n - 2 for a context with n alleles including a reference allele) </param>
		/// <returns> the ith non-reference allele in this context </returns>
		/// <exception cref="IllegalArgumentException"> if i is invalid </exception>
		public  Allele getAlternateAllele(int i)
		{
			return alleles[i + 1];
		}

		/// <param name="other">  VariantContext whose alleles to compare against </param>
		/// <returns> true if this VariantContext has the same alleles (both ref and alts) as other,
		///         regardless of ordering. Otherwise returns false. </returns>
		public  bool hasSameAllelesAs(VariantContext other)
		{
			return hasSameAlternateAllelesAs(other) && other.Reference.Equals(Reference, false);
		}

		/// <param name="other">  VariantContext whose alternate alleles to compare against </param>
		/// <returns> true if this VariantContext has the same alternate alleles as other,
		///         regardless of ordering. Otherwise returns false. </returns>
		public  bool hasSameAlternateAllelesAs(VariantContext other)
		{
			IList<Allele> thisAlternateAlleles = AlternateAlleles;
			IList<Allele> otherAlternateAlleles = other.AlternateAlleles;

			if (thisAlternateAlleles.Count != otherAlternateAlleles.Count)
			{
				return false;
			}

			foreach (Allele allele in thisAlternateAlleles)
			{
				if (!otherAlternateAlleles.Contains(allele))
				{
					return false;
				}
			}

			return true;
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Working with genotypes
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <returns> the number of samples in the context </returns>
		public  int NSamples
		{
			get
			{
				return genotypes.Count;
			}
		}

		/// <returns> true if the context has associated genotypes </returns>
		public  bool hasGenotypes()
		{
			return genotypes.Count > 0;
		}

		public  bool hasGenotypes(ICollection<string> sampleNames)
		{
			return genotypes.ContainsSamples(sampleNames);
		}

		/// <returns> set of all Genotypes associated with this context </returns>
		public  GenotypesContext Genotypes
		{
			get
			{
				return genotypes;
			}
		}


		/// <summary>
		/// Returns a map from sampleName -> Genotype for the genotype associated with sampleName.  Returns a map
		/// for consistency with the multi-get function.
		/// </summary>
		/// <param name="sampleName">   the sample name </param>
		/// <returns> mapping from sample name to genotype </returns>
		/// <exception cref="IllegalArgumentException"> if sampleName isn't bound to a genotype </exception>
		public  GenotypesContext getGenotypes(string sampleName)
		{
			return getGenotypes(new List<string>(){sampleName});
		}

		/// <summary>
		/// Returns a map from sampleName -> Genotype for each sampleName in sampleNames.  Returns a map
		/// for consistency with the multi-get function.
		/// 
		/// For testing convenience only
		/// </summary>
		/// <param name="sampleNames"> a unique list of sample names </param>
		/// <returns> subsetting genotypes context </returns>
		/// <exception cref="IllegalArgumentException"> if sampleName isn't bound to a genotype </exception>
		protected internal  GenotypesContext getGenotypes(ICollection<string> sampleNames)
		{
			return Genotypes.subsetToSamples(new HashSet<string>(sampleNames));
		}

		public  GenotypesContext getGenotypes(ISet<string> sampleNames)
		{
			return Genotypes.subsetToSamples(sampleNames);
		}


		/// <returns> the set of all sample names in this context, not ordered </returns>
		public  ISet<string> SampleNames
		{
			get
			{
				return Genotypes.SampleNames;
			}
		}

		public  IList<string> SampleNamesOrderedByName
		{
			get
			{
				return Genotypes.SampleNamesOrderedByName;
			}
		}

		/// <param name="sample">  the sample name
		/// </param>
		/// <returns> the Genotype associated with the given sample in this context or null if the sample is not in this context </returns>
		public  Genotype getGenotype(string sample)
		{
			return Genotypes[sample];
		}

		public  bool hasGenotype(string sample)
		{
			return Genotypes.ContainsSample(sample);
		}

		public  Genotype getGenotype(int ith)
		{
			return genotypes[ith];
		}


		/// <summary>
		/// Returns the number of chromosomes carrying any allele in the genotypes (i.e., excluding NO_CALLS)
		/// </summary>
		/// <returns> chromosome count </returns>
		public  int CalledChrCount
		{
			get
			{
				ISet<string> noSamples = new HashSet<string>();
				return getCalledChrCount(noSamples);
			}
		}

		/// <summary>
		/// Returns the number of chromosomes carrying any allele in the genotypes (i.e., excluding NO_CALLS)
		/// </summary>
		/// <param name="sampleIds"> IDs of samples to take into account. If empty then all samples are included. </param>
		/// <returns> chromosome count </returns>
		public  int getCalledChrCount(ISet<string> sampleIds)
		{
			int n = 0;
			GenotypesContext genotypes = sampleIds.Count==0? Genotypes : getGenotypes(sampleIds);

			foreach (Genotype g in genotypes)
			{
				foreach (Allele a in g.Alleles)
				{
					n += a.NoCall ? 0 : 1;
				}
			}
			return n;
		}

		/// <summary>
		/// Returns the number of chromosomes carrying allele A in the genotypes
		/// </summary>
		/// <param name="a"> allele </param>
		/// <returns> chromosome count </returns>
		public  int getCalledChrCount(Allele a)
		{
			return getCalledChrCount(a,new HashSet<string>());
		}

		/// <summary>
		/// Returns the number of chromosomes carrying allele A in the genotypes
		/// </summary>
		/// <param name="a"> allele </param>
		/// <param name="sampleIds"> - IDs of samples to take into account. If empty then all samples are included. </param>
		/// <returns> chromosome count </returns>
		public  int getCalledChrCount(Allele a, ISet<string> sampleIds)
		{
			int n = 0;
			GenotypesContext genotypes = sampleIds.Count==0 ? Genotypes : getGenotypes(sampleIds);

			foreach (Genotype g in genotypes)
			{
				n += g.countAllele(a);
			}

			return n;
		}

		/// <summary>
		/// Genotype-specific functions -- are the genotypes monomorphic w.r.t. to the alleles segregating at this
		/// site?  That is, is the number of alternate alleles among all fo the genotype == 0?
		/// </summary>
		/// <returns> true if it's monomorphic </returns>
		public  bool MonomorphicInSamples
		{
			get
			{
				if (!monomorphic.HasValue)
				{
					monomorphic = !Variant || (hasGenotypes() && getCalledChrCount(Reference) == CalledChrCount);
				}
				return monomorphic.Value;
			}
		}

		/// <summary>
		/// Genotype-specific functions -- are the genotypes polymorphic w.r.t. to the alleles segregating at this
		/// site?  That is, is the number of alternate alleles among all fo the genotype > 0?
		/// </summary>
		/// <returns> true if it's polymorphic </returns>
		public  bool PolymorphicInSamples
		{
			get
			{
				return !MonomorphicInSamples;
			}
		}

		private void calculateGenotypeCounts()
		{
			if (genotypeCounts == null)
			{
                genotypeCounts = new int[Enum.GetValues(typeof(GenotypeType)).Length];
                foreach (Genotype g in Genotypes)
				{genotypeCounts[(int)g.Type]++;}
			}
		}

		/// <summary>
		/// Genotype-specific functions -- how many no-calls are there in the genotypes?
		/// </summary>
		/// <returns> number of no calls </returns>
		public  int NoCallCount
		{
			get
			{
				calculateGenotypeCounts();
				return genotypeCounts[(int)GenotypeType.NO_CALL];
			}
		}

		/// <summary>
		/// Genotype-specific functions -- how many hom ref calls are there in the genotypes?
		/// </summary>
		/// <returns> number of hom ref calls </returns>
		public  int HomRefCount
		{
			get
			{
				calculateGenotypeCounts();
				return genotypeCounts[(int)GenotypeType.HOM_REF];
			}
		}

		/// <summary>
		/// Genotype-specific functions -- how many het calls are there in the genotypes?
		/// </summary>
		/// <returns> number of het calls </returns>
		public  int HetCount
		{
			get
			{
				calculateGenotypeCounts();
				return genotypeCounts[(int)GenotypeType.HET];
			}
		}

		/// <summary>
		/// Genotype-specific functions -- how many hom var calls are there in the genotypes?
		/// </summary>
		/// <returns> number of hom var calls </returns>
		public  int HomVarCount
		{
			get
			{
				calculateGenotypeCounts();
				return genotypeCounts[(int)GenotypeType.HOM_VAR];
			}
		}

		/// <summary>
		/// Genotype-specific functions -- how many mixed calls are there in the genotypes?
		/// </summary>
		/// <returns> number of mixed calls </returns>
		public  int MixedCount
		{
			get
			{
				calculateGenotypeCounts();
				return genotypeCounts[(int)GenotypeType.MIXED];
			}
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// validation: extra-strict validation routines for paranoid users
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Run all extra-strict validation tests on a Variant Context object
		/// </summary>
		/// <param name="reportedReference">   the reported reference allele </param>
		/// <param name="observedReference">   the actual reference allele </param>
		/// <param name="rsIDs">               the true dbSNP IDs </param>
		public  void extraStrictValidation(Allele reportedReference, Allele observedReference, ISet<string> rsIDs)
		{
			// validate the reference
			validateReferenceBases(reportedReference, observedReference);

			// validate the RS IDs
			validateRSIDs(rsIDs);

			// validate the altenate alleles
			validateAlternateAlleles();

			// validate the AN and AC fields
			validateChromosomeCounts();

			// TODO: implement me
			//checkReferenceTrack();
		}

		public  void validateReferenceBases(Allele reportedReference, Allele observedReference)
		{
			if (reportedReference != null && !reportedReference.basesMatch(observedReference))
			{
				throw new Exception(string.Format("the REF allele is incorrect for the record at position {0}:{1:D}, fasta says {2} vs. VCF says {3}", Chr, Start, observedReference.BaseString, reportedReference.BaseString));
			}
		}

		public  void validateRSIDs(ISet<string> rsIDs)
		{
			if (rsIDs != null && hasID())
			{
				foreach (string id in ID.Split(VCFConstants.ID_FIELD_SEPARATOR_AS_ARRAY, StringSplitOptions.RemoveEmptyEntries))
				{
					if (id.StartsWith("rs") && !rsIDs.Contains(id))
					{
						throw new Exception(string.Format("the rsID {0} for the record at position {1}:{2:D} is not in dbSNP", id, Chr, Start));
					}
				}
			}
		}

		public  void validateAlternateAlleles()
		{
			if (!hasGenotypes())
			{
				return;
			}
			IList<Allele> reportedAlleles = Alleles;
			ISet<Allele> observedAlleles = new HashSet<Allele>();
			observedAlleles.Add(Reference);
			foreach (Genotype g in Genotypes)
			{
				if (g.Called)
				{
                    foreach(Allele a in g.Alleles)                    
                    {observedAlleles.Add(a);}
				}
			}
			if (observedAlleles.Contains(Allele.NO_CALL))
			{
				observedAlleles.Remove(Allele.NO_CALL);
			}

			if (reportedAlleles.Count != observedAlleles.Count)
			{
				throw new Exception(string.Format("one or more of the ALT allele(s) for the record at position {0}:{1:D} are not observed at all in the sample genotypes", Chr, Start));
			}

			int originalSize = reportedAlleles.Count;
			// take the intersection and see if things change
			observedAlleles.IntersectWith(reportedAlleles);
			if (observedAlleles.Count != originalSize)
			{
				throw new Exception(string.Format("one or more of the ALT allele(s) for the record at position {0}:{1:D} are not observed at all in the sample genotypes", Chr, Start));
			}
		}

		public  void validateChromosomeCounts()
		{
			if (!hasGenotypes())
			{
				return;
			}

			// AN
			if (hasAttribute(VCFConstants.ALLELE_NUMBER_KEY))
			{
				int reportedAN = Convert.ToInt32(getAttribute(VCFConstants.ALLELE_NUMBER_KEY).ToString());
				int observedAN = CalledChrCount;
				if (reportedAN != observedAN)
				{
					throw new Exception(string.Format("the Allele Number (AN) tag is incorrect for the record at position {0}:{1:D}, {2:D} vs. {3:D}", Chr, Start, reportedAN, observedAN));
				}
			}

			// AC
			if (hasAttribute(VCFConstants.ALLELE_COUNT_KEY))
			{
				List<int?> observedACs = new List<int?>();

				// if there are alternate alleles, record the relevant tags
				if (AlternateAlleles.Count > 0)
				{
					foreach (Allele allele in AlternateAlleles)
					{
						observedACs.Add(getCalledChrCount(allele));
					}
				}
				else // otherwise, set them to 0
				{
					observedACs.Add(0);
				}

				if (getAttribute(VCFConstants.ALLELE_COUNT_KEY) is IList)
				{
					observedACs.Sort();
					IList reportedACs = (IList)getAttribute(VCFConstants.ALLELE_COUNT_KEY);
                    //reportedACs.Sort();
					if (observedACs.Count != reportedACs.Count)
					{
						throw new Exception(string.Format("the Allele Count (AC) tag doesn't have the correct number of values for the record at position {0}:{1:D}, {2:D} vs. {3:D}", Chr, Start, reportedACs.Count, observedACs.Count));
					}
					for (int i = 0; i < observedACs.Count; i++)
					{
						if (Convert.ToInt32(reportedACs[i].ToString()) != observedACs[i])
						{
							throw new Exception(string.Format("the Allele Count (AC) tag is incorrect for the record at position {0}:{1:D}, {2} vs. {3:D}", Chr, Start, reportedACs[i], observedACs[i]));
						}
					}
				}
				else
				{
					if (observedACs.Count != 1)
					{
						throw new Exception(string.Format("the Allele Count (AC) tag doesn't have enough values for the record at position {0}:{1:D}", Chr, Start));
					}
					int reportedAC = Convert.ToInt32(getAttribute(VCFConstants.ALLELE_COUNT_KEY).ToString());
					if (reportedAC != observedACs[0])
					{
						throw new Exception(string.Format("the Allele Count (AC) tag is incorrect for the record at position {0}:{1:D}, {2:D} vs. {3:D}", Chr, Start, reportedAC, observedACs[0]));
					}
				}
			}
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// validation: the normal validation routines are called automatically upon creation of the VC
		//
		// ---------------------------------------------------------------------------------------------------------
        private void validate(Validation validationToPerform)
		{
			validateStop();
            if(validationToPerform.HasFlag(Validation.ALLELES))
            {validateAlleles();}
            if(validationToPerform.HasFlag(Validation.GENOTYPES))
            {validateGenotypes();}
		}

		/// <summary>
		/// Check that getEnd() == END from the info field, if it's present
		/// </summary>
		private void validateStop()
		{
			if (hasAttribute(VCFConstants.END_KEY))
			{
                int end = getAttributeAsInt(VCFConstants.END_KEY, -1);
				Debug.Assert(end != -1);
				if (end != End)
				{
                    string message = "Badly formed variant context at location " + Chr + ":" + Start + "; getEnd() was " + End + " but this VariantContext contains an END key with value " + end;
					if (GeneralUtils.DEBUG_MODE_ENABLED && WARN_ABOUT_BAD_END)
					{
						Console.Error.WriteLine(message);
					}
					else
					{
						throw new Exception(message);
					}
				}
			}
			else
			{
				long length = (stop - start) + 1;
				if (!hasSymbolicAlleles() && length != Reference.Length)
				{
					throw new Exception("BUG: GenomeLoc " + contig + ":" + start + "-" + stop + " has a size == " + length + " but the variation reference allele has length " + Reference.Length + " this = " + this);
				}
			}
		}

		private void validateAlleles()
		{

			bool alreadySeenRef = false;

			foreach (Allele allele in alleles)
			{
				// make sure there's only one reference allele
				if (allele.Reference)
				{
					if (alreadySeenRef)
					{
						throw new System.ArgumentException("BUG: Received two reference tagged alleles in VariantContext " + alleles + " this=" + this);
					}
					alreadySeenRef = true;
				}

				if (allele.NoCall)
				{
					throw new System.ArgumentException("BUG: Cannot add a no call allele to a variant context " + alleles + " this=" + this);
				}
			}

			// make sure there's one reference allele
			if (!alreadySeenRef)
			{
				throw new System.ArgumentException("No reference allele found in VariantContext");
			}
		}

		private void validateGenotypes()
		{
			if (this.genotypes == null)
			{
				throw new Exception("Genotypes is null");
			}

			foreach (Genotype g in this.genotypes)
			{
				if (g.Available)
				{
					foreach (Allele gAllele in g.Alleles)
					{
						if (!hasAllele(gAllele) && gAllele.Called)
						{
							throw new Exception("Allele in genotype " + gAllele + " not in the variant context " + alleles);
						}
					}
				}
			}
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// utility routines
		//
		// ---------------------------------------------------------------------------------------------------------

		private void determineType()
		{
			if (type == null)
			{
				switch (NAlleles)
				{
					case 0:
						throw new Exception("Unexpected error: requested type of VariantContext with no alleles!" + this);
					case 1:
						// note that this doesn't require a reference allele.  You can be monomorphic independent of having a
						// reference allele
						type = VariantType.NO_VARIATION;
						break;
					default:
						determinePolymorphicType();
					break;
				}
			}
		}

		private void determinePolymorphicType()
		{
			type = null;

			// do a pairwise comparison of all alleles against the reference allele
			foreach (Allele allele in alleles)
			{
				if (allele == REF)
				{
					continue;
				}

				// find the type of this allele relative to the reference
				VariantType biallelicType = typeOfBiallelicVariant(REF, allele);

				// for the first alternate allele, set the type to be that one
				if (type == null)
				{
					type = biallelicType;
				}
				// if the type of this allele is different from that of a previous one, assign it the MIXED type and quit
				else if (biallelicType != type)
				{
					type = VariantType.MIXED;
					return;
				}
			}
		}

		private static VariantType typeOfBiallelicVariant(Allele reference, Allele allele)
		{
			if (reference.Symbolic)
			{
				throw new Exception("Unexpected error: encountered a record with a symbolic reference allele");
			}

			if (allele.Symbolic)
			{
				return VariantType.SYMBOLIC;
			}

			if (reference.Length == allele.Length)
			{
				if (allele.Length == 1)
				{
					return VariantType.SNP;
				}
				else
				{
					return VariantType.MNP;
				}
			}

			// Important note: previously we were checking that one allele is the prefix of the other.  However, that's not an
			// appropriate check as can be seen from the following example:
			// REF = CTTA and ALT = C,CT,CA
			// This should be assigned the INDEL type but was being marked as a MIXED type because of the prefix check.
			// In truth, it should be absolutely impossible to return a MIXED type from this method because it simply
			// performs a pairwise comparison of a single alternate allele against the reference allele (whereas the MIXED type
			// is reserved for cases of multiple alternate alleles of different types).  Therefore, if we've reached this point
			// in the code (so we're not a SNP, MNP, or symbolic allele), we absolutely must be an INDEL.

			return VariantType.INDEL;

			// old incorrect logic:
			// if (oneIsPrefixOfOther(ref, allele))
			//     return Type.INDEL;
			// else
			//     return Type.MIXED;
		}

		public override string ToString()
		{
			return string.Format("[VC {0} @ {1} Q{2} of type={3} alleles={4} attr={5} GT={6}", Source, contig + ":" + (start - stop == 0 ? start.ToString() : start.ToString() + "-" + stop.ToString()), hasLog10PError() ? string.Format("{0:F2}", PhredScaledQual) : ".", this.type, ParsingUtils.sortList(this.Alleles), ParsingUtils.sortedString(this.Attributes), this.Genotypes);
		}

		public  string toStringWithoutGenotypes()
		{
			return string.Format("[VC {0} @ {1} Q{2} of type={3} alleles={4} attr={5}", 
                Source, contig + ":" + (start - stop == 0 ? start.ToString() : start.ToString() + "-" + stop.ToString()), hasLog10PError() ? string.Format("{0:F2}", PhredScaledQual) : ".", 
                this.type, ParsingUtils.sortList(this.Alleles), ParsingUtils.sortedString(this.Attributes));
		}

		// protected basic manipulation routines
		private static IList<Allele> makeAlleles(ICollection<Allele> alleles)
		{
			IList<Allele> alleleList = new List<Allele>(alleles.Count);

			bool sawRef = false;
			foreach (Allele a in alleles)
			{
				foreach (Allele b in alleleList)
				{
					if (a.Equals(b, true))
					{
						throw new System.ArgumentException("Duplicate allele added to VariantContext: " + a);
					}
				}

				// deal with the case where the first allele isn't the reference
				if (a.Reference)
				{
					if (sawRef)
					{
						throw new System.ArgumentException("Alleles for a VariantContext must contain at most one reference allele: " + alleles);
					}
					alleleList.Insert(0, a);
					sawRef = true;
				}
				else
				{
					alleleList.Add(a);
				}
			}

			if (alleleList.Count == 0)
			{
				throw new System.ArgumentException("Cannot create a VariantContext with an empty allele list");
			}

			if (alleleList[0].NonReference)
			{
				throw new System.ArgumentException("Alleles for a VariantContext must contain at least one reference allele: " + alleles);
			}

			return alleleList;
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Fully decode
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Return a VC equivalent to this one but where all fields are fully decoded
		/// 
		/// See VariantContext document about fully decoded
		/// </summary>
		/// <param name="header"> containing types about all fields in this VC </param>
		/// <returns> a fully decoded version of this VC </returns>
		public  VariantContext fullyDecode(VCFHeader header, bool lenientDecoding)
		{
			if (FullyDecoded)
			{
				return this;
			}
			else
			{
				// TODO -- warning this is potentially very expensive as it creates copies over and over
				VariantContextBuilder builder = new VariantContextBuilder(this);
				fullyDecodeInfo(builder, header, lenientDecoding);
				fullyDecodeGenotypes(builder, header);
				builder.FullyDecoded=true;
				return builder.make();
			}
		}

		/// <summary>
		/// See VariantContext document about fully decoded </summary>
		/// <returns> true if this is a fully decoded VC </returns>
		public  bool FullyDecoded
		{
			get
			{
				return fullyDecoded;
			}
		}

		private void fullyDecodeInfo(VariantContextBuilder builder, VCFHeader header, bool lenientDecoding)
		{
			builder.Attributes=fullyDecodeAttributes(Attributes, header, lenientDecoding);
		}
		private IDictionary<string, object> fullyDecodeAttributes(IDictionary<string, object> attributes, VCFHeader header, bool lenientDecoding)
		{
			IDictionary<string, object> newAttributes = new Dictionary<string, object>(10);

			foreach (KeyValuePair<string, object> attr in attributes)
			{
				string field = attr.Key;

				if (field.Equals(VCFConstants.GENOTYPE_FILTER_KEY))
				{
					continue; // gross, FT is part of the extended attributes
				}

				VCFCompoundHeaderLine format = VariantContextUtils.getMetaDataForField(header, field);
				object decoded = decodeValue(field, attr.Value, format);

				if (decoded != null && !lenientDecoding && format.CountType != VCFHeaderLineCount.UNBOUNDED && format.Type != VCFHeaderLineType.Flag) // we expect exactly the right number of elements
				{
					int obsSize = decoded is IList ? ((IList) decoded).Count : 1;
					int expSize = format.getCount(this);
					if (obsSize != expSize)
					{
						throw new VCFParsingError("Discordant field size detected for field " + field + " at " + Chr + ":" + Start + ".  Field had " + obsSize + " values " + "but the header says this should have " + expSize + " values based on header record " + format);
					}
				}
				newAttributes[field] = decoded;
			}

			return newAttributes;
		}

		private object decodeValue(string field, object value, VCFCompoundHeaderLine format)
		{
			if (value is string)
			{
				if (field.Equals(VCFConstants.GENOTYPE_PL_KEY))
				{
					return GenotypeLikelihoods.fromPLField((string)value);
				}

				string str = (string)value;
				if (str.IndexOf(',') != -1)
				{
					string[] splits = str.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
					IList<object> values = new List<object>(splits.Length);
					for (int i = 0; i < splits.Length; i++)
					{
						values.Add(decodeOne(field, splits[i], format));
					}
					return values;
				}
				else
				{
					return decodeOne(field, str, format);
				}
			}
			else if (value is IList && (((IList) value)[0]) is string)
			{
				IList<string> asList = (IList<string>)value;
				IList<object> values = new List<object>(asList.Count);
				foreach (String s in asList)
				{
					values.Add(decodeOne(field, s, format));
				}
				return values;
			}
			else
			{
				return value;
			}

			// allowMissingValuesComparedToHeader
		}

		private object decodeOne(string field, string str, VCFCompoundHeaderLine format)
		{
			try
			{
				if (str.Equals(VCFConstants.MISSING_VALUE_v4))
				{
					return null;
				}
				else
				{
					switch (format.Type)
					{
						case VCFHeaderLineType.Character:
							return str;
						case VCFHeaderLineType.Flag:
							bool b = Convert.ToBoolean(str) || str.Equals("1");
							if (b == false)
							{
								throw new VCFParsingError("VariantContext FLAG fields " + field + " cannot contain false values" + " as seen at " + Chr + ":" + Start);
							}
							return b;
						case VCFHeaderLineType.String:
							return str;
						case VCFHeaderLineType.Integer:
							return Convert.ToInt32(str);
                        case VCFHeaderLineType.Float:
							return Convert.ToDouble(str);
						default:
							throw new VCFParsingError("Unexpected type for field" + field);
					}
				}
			}
			catch (FormatException e)
			{
				throw new VCFParsingError("Could not decode field " + field + " with value " + str + " of declared type " + format.Type);
			}
		}

		private void fullyDecodeGenotypes(VariantContextBuilder builder, VCFHeader header)
		{
			GenotypesContext gc = new GenotypesContext();
			foreach (Genotype g in Genotypes)
			{
				gc.Add(fullyDecodeGenotypes(g, header));
			}
			builder.SetGenotypes(gc,false);
		}
		private Genotype fullyDecodeGenotypes(Genotype g, VCFHeader header)
		{
			IDictionary<string, object> map = fullyDecodeAttributes(g.ExtendedAttributes, header, true);
            var g2=new GenotypeBuilder(g);
            g2.AddAttributes(map);
            return g2.make();
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// tribble integration routines -- not for public consumption
		//
		// ---------------------------------------------------------------------------------------------------------
		public  string Chr
		{
			get
			{
				return contig;
			}
		}

		public  int Start
		{
			get
			{
				return (int)start;
			}
		}

		public  int End
		{
			get
			{
				return (int)stop;
			}
		}

		public  bool hasSymbolicAlleles()
		{            
			return hasSymbolicAlleles(Alleles);
		}
		public static bool hasSymbolicAlleles(IList<Allele> alleles)
		{
            return alleles.Any(x => x.Symbolic);
		}

		public  Allele AltAlleleWithHighestAlleleCount
		{
			get
			{
				// optimization: for bi-allelic sites, just return the 1only alt allele
				if (Biallelic)
				{
					return getAlternateAllele(0);
				}
                return AlternateAlleles.MaxBy(a=>getCalledChrCount(a));
            }
		}

		/// <summary>
		/// Lookup the index of allele in this variant context
		/// </summary>
		/// <param name="allele"> the allele whose index we want to get </param>
		/// <returns> the index of the allele into getAlleles(), or -1 if it cannot be found </returns>
		public  int getAlleleIndex(Allele allele)
		{
			return Alleles.IndexOf(allele);
		}

		/// <summary>
		/// Return the allele index #getAlleleIndex for each allele in alleles
		/// </summary>
		/// <param name="alleles"> the alleles we want to look up </param>
		/// <returns> a list of indices for each allele, in order </returns>
		public  IList<int> getAlleleIndices(ICollection<Allele> alleles)
		{
            return alleles.Select(x => getAlleleIndex(x)).ToList();
		}

		public  int[] getGLIndecesOfAlternateAllele(Allele targetAllele)
		{
			int index = getAlleleIndex(targetAllele);
			if (index == -1)
			{
				throw new System.ArgumentException("Allele " + targetAllele + " not in this VariantContex " + this);
			}
			return GenotypeLikelihoods.getPLIndecesOfAlleles(0, index);
		}
	}

}