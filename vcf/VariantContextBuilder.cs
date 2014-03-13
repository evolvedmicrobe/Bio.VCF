using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Bio.VCF
{
	/// <summary>
	/// Builder class for VariantContext
	/// 
	/// Some basic assumptions here:
	/// 
	/// 1 -- data isn't protectively copied.  If you provide an attribute map to
	/// the build, and modify it later, the builder will see this and so will any
	/// resulting variant contexts.  It's best not to modify collections provided
	/// to a builder.
	/// 
	/// 2 -- the system uses the standard builder model, allowing the simple construction idiom:
	/// 
	///   builder.source("a").genotypes(gc).id("x").make() => VariantContext
	/// 
	/// 3 -- The best way to copy a VariantContext is:
	/// 
	///   new VariantContextBuilder(vc).make() => a copy of VC
	/// 
	/// 4 -- validation of arguments is done at the during the final make() call, so a
	/// VariantContextBuilder can exist in an inconsistent state as long as those issues
	/// are resolved before the call to make() is issued.
	/// 
	/// @author depristo
	/// </summary>
	public class VariantContextBuilder
	{
		/// <summary>
		/// An alias as well for chromosome
		/// </summary>
		public string Contig{ get; set; }

		/// <summary>
		/// Tells us that the resulting VariantContext should have log10PError
		/// </summary>
		public double Log10PError {
			get;
			set;
		}

		/// <summary>
		/// Tells us that the resulting VariantContext should have source field set to source 
		/// </summary>
		public string Source {
			get;
			set;
		}

		private long start_Renamed = -1;
		private long stop_Renamed = -1;
		private ICollection<Allele> alleles_Renamed = null;
		private GenotypesContext genotypes_Renamed = GenotypesContext.NO_GENOTYPES;
		private ISet<string> filters_Renamed = null;
		//Nigel dictionary was originally string/object
		//TODO: See if I can avoid casting here by object->string
		private IDictionary<string, object> attributes_Renamed = null;
		private bool attributesCanBeModified = false;
		/// <summary>
		/// enum of what must be validated </summary>
		//NIGEL: Modify to use bit flags on these guys
		private VariantContext.Validation toValidate;
		//private readonly EnumSet<VariantContext.Validation> toValidate = EnumSet.noneOf(typeof(VariantContext.Validation));
		/// <summary>
		/// Create an empty VariantContextBuilder where all values adopt their default values.  Note that
		/// source, chr, start, stop, and alleles must eventually be filled in, or the resulting VariantContext
		/// will throw an error.
		/// </summary>
		public VariantContextBuilder ()
		{
			ID = VCFConstants.EMPTY_ID_FIELD;
			Log10PError = VariantContext.NO_LOG10_PERROR;
			FullyDecoded = false;
		}

		/// <summary>
		/// Create an empty VariantContextBuilder where all values adopt their default values, but the bare min.
		/// of info (source, chr, start, stop, and alleles) have been provided to start.
		/// </summary>
		public VariantContextBuilder (string source, string contig, long start, long stop, ICollection<Allele> alleles) : this ()
		{
			this.Source = source;
			this.Contig = contig;
			this.start_Renamed = start;
			this.stop_Renamed = stop;
			this.alleles_Renamed = alleles;
			this.attributes_Renamed = new Dictionary<string, object> ();// Collections.emptyMap(); // immutable
			toValidate = toValidate | VariantContext.Validation.ALLELES;//using as bit flags
		}

		/// <summary>
		/// Returns a new builder based on parent -- the new VC will have all fields initialized
		/// to their corresponding values in parent.  This is the best way to create a derived VariantContext
		/// </summary>
		/// <param name="parent">  Cannot be null </param>
		public VariantContextBuilder (VariantContext parent) : this ()
		{
			if (parent == null) {
				throw new System.ArgumentException ("BUG: VariantContextBuilder parent argument cannot be null in VariantContextBuilder");
			}
			this.alleles_Renamed = parent.alleles;
			this.attributes_Renamed = (IDictionary<string,object>)parent.Attributes;
			this.attributesCanBeModified = false;
			this.Contig = parent.contig;
			this.filters_Renamed = (ISet<string>)parent.FiltersMaybeNull;
			this.genotypes_Renamed = parent.genotypes;
			this.ID = parent.ID;
			this.Log10PError = parent.Log10PError;
			this.Source = parent.Source;
			this.start_Renamed = parent.Start;
			this.stop_Renamed = parent.End;
			this.FullyDecoded = parent.FullyDecoded;
		}

		public VariantContextBuilder (VariantContextBuilder parent) : this ()
		{
			if (parent == null) {
				throw new System.ArgumentException ("BUG: VariantContext parent argument cannot be null in VariantContextBuilder");
			}
			this.alleles_Renamed = parent.alleles_Renamed;
			this.attributesCanBeModified = false;
			this.Contig = parent.Contig;
			this.genotypes_Renamed = parent.genotypes_Renamed;
			this.ID = parent.ID;
			this.Log10PError = parent.Log10PError;
			this.Source = parent.Source;
			this.start_Renamed = parent.start_Renamed;
			this.stop_Renamed = parent.stop_Renamed;
			this.FullyDecoded = parent.FullyDecoded;
			this.Attributes = parent.attributes_Renamed;
			this.SetFilters (parent.filters_Renamed);
		}

		public virtual VariantContextBuilder copy ()
		{
			return new VariantContextBuilder (this);
		}

		/// <summary>
		/// Tells this builder to use this collection of alleles for the resulting VariantContext
		/// </summary>
		/// <param name="alleles"> </param>
		/// <returns> this builder </returns>
		public void SetAlleles (ICollection<Allele> alleles)
		{
			this.alleles_Renamed = alleles;
			toValidate = toValidate | VariantContext.Validation.ALLELES;
		}

		public void SetAlleles (IList<string> alleleStrings)
		{
			IList<Allele> alleles = new List<Allele> (alleleStrings.Count);
			for (int i = 0; i < alleleStrings.Count; i++) {
				alleles.Add (Allele.Create (alleleStrings [i], i == 0));
			}
			SetAlleles (alleles);
		}

		public void SetAlleles (params string[] alleleStrings)
		{
			SetAlleles (alleleStrings);
		}

		public IList<Allele> Alleles {
			get {
				return new List<Allele> (alleles_Renamed);
			}
		}

		/// <summary>
		/// Tells this builder to use this map of attributes alleles for the resulting VariantContext
		/// 
		/// Attributes can be null -> meaning there are no attributes.  After
		/// calling this routine the builder assumes it can modify the attributes
		/// object here, if subsequent calls are made to set attribute values </summary>
		/// <param name="attributes"> </param>
		public IDictionary<string,object> Attributes {
			set {
				if (value != null) {
					this.attributes_Renamed = value;
				} else {
					this.attributes_Renamed = new Dictionary<string, object> ();
				}
				this.attributesCanBeModified = true;
			}
		}

		/// <summary>
		/// Puts the key -> value mapping into this builder's attributes
		/// </summary>
		/// <param name="key"> </param>
		/// <param name="value">
		public virtual VariantContextBuilder attribute (string key, object value)
		{
			makeAttributesModifiable ();
			attributes_Renamed [key] = value;
			return this;
		}

		/// <summary>
		/// Removes key if present in the attributes
		/// </summary>
		/// <param name="key">
		/// @return </param>
		public virtual VariantContextBuilder rmAttribute (string key)
		{
			makeAttributesModifiable ();
			attributes_Renamed.Remove (key);
			return this;
		}

		/// <summary>
		/// Makes the attributes field modifiable.  In many cases attributes is just a pointer to an immutable
		/// collection, so methods that want to add / remove records require the attributes to be copied to a
		/// </summary>
		private void makeAttributesModifiable ()
		{
			if (!attributesCanBeModified) {
				this.attributesCanBeModified = true;
				this.attributes_Renamed = new Dictionary<string, object> (attributes_Renamed);
			}
		}

		/// <summary>
		/// This builder's filters are set to this value
		/// 
		/// filters can be null -> meaning there are no filters </summary>
		/// <param name="filters"> </param>
		public void SetFilters (ISet<string> filters)
		{
			this.filters_Renamed = filters;
		}

		/// <summary>
		/// <seealso cref="#filters"/>
		/// </summary>
		/// <param name="filters">
		
		public void SetFilters (params string[] filters)
		{
			this.SetFilters (new HashSet<string> (filters));
		}

		public void AddFilter (string filter)
		{
			if (this.filters_Renamed == null) {
				this.filters_Renamed = new HashSet<string> ();
			}
			this.filters_Renamed.Add (filter);			
		}

		/// <summary>
		/// Tells this builder that the resulting VariantContext should have PASS filters
		/// 
		/// @return
		/// </summary>
		public void SetPassFilters ()
		{
			//Believe we should not set it to pass if other filters are not there, so am adding now
			if (this.filters_Renamed != null) {
				throw new VCFParsingError ("Cannot set filters to pass after it has already been set.");
			}
			SetFilters (VariantContext.PASSES_FILTERS);
		}

		/// <summary>
		/// Tells this builder that the resulting VariantContext be unfiltered
		/// 
		/// @return
		/// </summary>
		public void SetUnFiltered ()
		{
			if (this.filters_Renamed != null) {
				throw new VCFParsingError ("Cannot set filters to pass after it has already been set.");
			}
			this.filters_Renamed = null;			
		}

		/// <summary>
		/// Tells this builder that the resulting VariantContext should use this genotypes GenotypeContext
		/// 
		/// Note that genotypes can be null -> meaning there are no genotypes
		/// </summary>
		/// <param name="genotypes"> </param>
		public void SetGenotypes (GenotypesContext genotypes, bool requireValidation = true)
		{
			this.genotypes_Renamed = genotypes;
			if (genotypes != null && requireValidation) {
				toValidate = toValidate | VariantContext.Validation.GENOTYPES;
			}
			//TODO: Perhaps if the genotypes are reset we should remove the validation flag if it was not required but was already present?
		}

		/// <summary>
		/// Tells this builder that the resulting VariantContext should use a GenotypeContext containing genotypes
		/// 
		/// Note that genotypes can be null -> meaning there are no genotypes
		/// </summary>
		/// <param name="genotypes"> </param>
		public void SetGenotypes (ICollection<Genotype> genotypes)
		{
			this.SetGenotypes (GenotypesContext.copy (genotypes));
		}

		/// <summary>
		/// Tells this builder that the resulting VariantContext should use a GenotypeContext containing genotypes </summary>
		/// <param name="genotypes"> </param>
		public void SetGenotypes (params Genotype[] genotypes)
		{
			this.SetGenotypes (GenotypesContext.copy (genotypes));
		}

		/// <summary>
		/// Tells this builder that the resulting VariantContext should not contain any GenotypeContext
		/// </summary>
		public virtual VariantContextBuilder noGenotypes ()
		{
			this.genotypes_Renamed = null;
			return this;
		}

		/// <summary>
		/// Tells us that the resulting VariantContext should have ID </summary>
		/// <param name="ID">
		/// @return </param>
		public string ID {
			get;
			set;
		}

		/// <summary>
		/// Tells us that the resulting VariantContext should have the specified location </summary>
		/// <param name="contig"> </param>
		/// <param name="start"> </param>
		/// <param name="stop">
		/// @return </param>
		public virtual VariantContextBuilder loc (string contig, long start, long stop)
		{
			this.Contig = contig;
			this.start_Renamed = start;
			this.stop_Renamed = stop;
			toValidate = toValidate | VariantContext.Validation.ALLELES;
			return this;
		}

		/// <summary>
		/// Tells us that the resulting VariantContext should have the specified contig start </summary>
		/// <param name="start">
		/// @return </param>
		public long Start {
			set {
				this.start_Renamed = value;
				toValidate = toValidate | VariantContext.Validation.ALLELES;
			}
		}

		/// <summary>
		/// Tells us that the resulting VariantContext should have the specified contig stop </summary>
		/// <param name="stop">
		public long Stop {
			set {
				this.stop_Renamed = value;
			}
			get { return stop_Renamed; }
		}

		public virtual VariantContextBuilder computeEndFromAlleles (IList<Allele> alleles, int start)
		{
			return computeEndFromAlleles (alleles, start, -1);
		}

		/// <summary>
		/// Compute the end position for this VariantContext from the alleles themselves
		/// 
		/// assigns this builder the stop position computed.
		/// </summary>
		/// <param name="alleles"> the list of alleles to consider.  The reference allele must be the first one </param>
		/// <param name="start"> the known start position of this event </param>
		/// <param name="endForSymbolicAlleles"> the end position to use if any of the alleles is symbolic.  Can be -1
		///                              if no is expected but will throw an error if one is found </param>
		/// <returns> this builder </returns>
		public virtual VariantContextBuilder computeEndFromAlleles (IList<Allele> alleles, int start, int endForSymbolicAlleles)
		{
			this.Stop = (VariantContextUtils.ComputeEndFromAlleles (alleles, start, endForSymbolicAlleles));
			return this;
		}

		/// <returns> true if this builder contains fully decoded data
		/// 
		/// See VariantContext for more information </returns>
		public bool FullyDecoded {
			get;
			set;
		}

		/// <summary>
		/// Takes all of the builder data provided up to this point, and instantiates
		/// a freshly allocated VariantContext with all of the builder data.  This
		/// VariantContext is validated as appropriate and if not failing QC (and
		/// throwing an exception) is returned.
		/// 
		/// Note that this function can be called multiple times to create multiple
		/// VariantContexts from the same builder.
		/// </summary>
		public VariantContext make ()
		{
			return new VariantContext (Source, ID, Contig, start_Renamed, stop_Renamed, alleles_Renamed, genotypes_Renamed, Log10PError, filters_Renamed, attributes_Renamed, FullyDecoded, toValidate);
		}
		//These methods seemed to possible introduce problems without returning value, so am skipping for now

		#region MethodsNotPortedFromJava

		#endregion
	}
}