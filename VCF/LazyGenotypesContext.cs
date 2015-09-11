using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;

namespace Bio.VCF
{
	/// <summary>
	/// Lazy-loading GenotypesContext.  A lazy-loading context has access to the
	/// VCFParser and a unparsed string of genotype data.  If the user attempts to manipulate
	/// the genotypes contained in this context, we decode the data and become a full blown
	/// GenotypesContext.  However, if the user never does this we are spared a lot of expense
	/// decoding the genotypes unnecessarily.
	/// 
	/// 
	/// TODO: Consider changing to work more cleanly with .NET Lazy initalizers
	/// </summary>
	public class LazyGenotypesContext : GenotypesContext
	{
		private static readonly List<Genotype> EMPTY = new List<Genotype> (0);
		/// <summary>
		/// The parent codec, which will be used to parse the genotypes.
		/// TODO: Figure out if this should be static, so we don't need to keep a reference
		/// in each instance to, presumably, the same codec.
		/// </summary>
		private AbstractVCFCodec codec;
		internal readonly IList<Allele> alleles;
		internal readonly string contig;
		internal readonly int start;
		/// <summary>
		/// nUnparsedGenotypes the number of genotypes contained in the unparsedGenotypes data
		/// (known already in the parser).  Useful for isEmpty and size() optimizations
		/// </summary>
		internal readonly int nUnparsedGenotypes;
		/// <summary>
		/// True if we've already decoded the values in unparsedGenotypeData
		/// </summary>
		internal bool ParsedAlready = false;

		/// <summary>
		/// Returns the data used in the full GenotypesContext constructor
		/// </summary>
		public class LazyData
		{
			internal readonly List<Genotype> genotypes;
			internal readonly IDictionary<string, int> sampleNameToOffset;
			internal readonly IList<string> sampleNamesInOrder;

			public LazyData (List<Genotype> genotypes, IList<string> sampleNamesInOrder, IDictionary<string, int> sampleNameToOffset)
			{
				this.genotypes = genotypes;
				this.sampleNamesInOrder = sampleNamesInOrder;
				this.sampleNameToOffset = sampleNameToOffset;
			}
		}

		/// <summary>
		/// Creates a new lazy loading genotypes context using the LazyParser to create
		/// genotypes data on demand.
		/// </summary>
		/// <param name="parser"> the parser to be used to load on-demand genotypes data </param>
		/// <param name="unparsedGenotypeData"> the encoded genotypes data that we will decode if necessary </param>
		/// <param name="nUnparsedGenotypes"> the number of genotypes that will be produced if / when we actually decode the genotypes data </param>
		public LazyGenotypesContext (AbstractVCFCodec codec, IList<Allele> alleles, string contig, int start, string unparsedGenotypeData, int nUnparsedGenotypes)
            : base (EMPTY)
		{
			this.codec = codec;
			this.alleles = alleles;
			this.contig = contig;
			this.start = start;
			//Note: This formally kept a reference to a parser interface, decided that was too much overhead though so skipped it.  
			//this.parser = parser;
			this.UnparsedGenotypeData = unparsedGenotypeData;
			this.nUnparsedGenotypes = nUnparsedGenotypes;
		}

		/// <summary>
		/// Overrides the genotypes accessor.  If we haven't already, decode the genotypes data
		/// and store the decoded results in the appropriate variables.  Otherwise we just
		/// returned the decoded result directly.  Note some care needs to be taken here as
		/// the value in notToBeDirectlyAccessedGenotypes may diverge from what would be produced
		/// by decode, if after the first decode the genotypes themselves are replaced
		/// </summary>
		protected internal override List<Genotype> Genotypes {
			get {
				Decode ();
				return notToBeDirectlyAccessedGenotypes;
			}
		}

		/// <summary>
		/// Force us to decode the genotypes, if not already done
		/// </summary>
		public void Decode ()
		{
			if (!ParsedAlready) {
				LazyData parsed = parse (UnparsedGenotypeData);
				notToBeDirectlyAccessedGenotypes = parsed.genotypes;
				sampleNamesInOrder = parsed.sampleNamesInOrder;
				sampleNameToOffset = parsed.sampleNameToOffset;
				ParsedAlready = true;
				UnparsedGenotypeData = null; // don't hold the unparsed data any longer
				// warning -- this path allows us to create a VariantContext that doesn't run validateGenotypes()
				// That said, it's not such an important routine -- it's just checking that the genotypes
				// are well formed w.r.t. the alleles list, but this will be enforced within the VCFCodec
			}
		}

		internal int FastNoCallCount {
			get {
				if (ParsedAlready) {

					throw new VCFParsingError ("Should not call fast method on loaded genotype");
				} else {
					var sp = UnparsedGenotypeData.Split ('\t');
					int count = 0;
					for (int i = 0; i < sp.Length; i++) {
						var ac = sp [i];
						if (ac == "./." || ac == ".") {
							count++;
						}
					}
					return count;

				}
			}
		}

		[MethodImpl (MethodImplOptions.Synchronized)]
		public LazyGenotypesContext.LazyData parse (string data)
		{
			return codec.CreateGenotypeMap (data, alleles, contig, start);
		}

		/// <summary>
		/// Overrides the ensure* functionality.  If the data hasn't been loaded
		/// yet and we want to build the cache, just decode it and we're done.  If we've
		/// already decoded the data, though, go through the super class
		/// </summary>
		[MethodImpl (MethodImplOptions.Synchronized)]
		protected internal override void ensureSampleNameMap ()
		{
			if (!ParsedAlready) {
				Decode (); // will load up all of the necessary data
			} else {
				base.ensureSampleNameMap ();
			}
		}

		[MethodImpl (MethodImplOptions.Synchronized)]
		protected internal override void ensureSampleOrdering ()
		{
			if (!ParsedAlready) {
				Decode (); // will load up all of the necessary data
			} else {
				base.ensureSampleOrdering ();
			}
		}

		[MethodImpl (MethodImplOptions.Synchronized)]
		protected internal override void invalidateSampleNameMap ()
		{
			// if the cache is invalidated, and we haven't loaded our data yet, do so
			if (!ParsedAlready) {
				Decode ();
			}
			base.invalidateSampleNameMap ();
		}

		[MethodImpl (MethodImplOptions.Synchronized)]
		protected internal override void invalidateSampleOrdering ()
		{
			// if the cache is invalidated, and we haven't loaded our data yet, do so
			if (!ParsedAlready) {
				Decode ();
			}
			base.invalidateSampleOrdering ();
		}

		public override bool Empty {
			get {
				lock (this) {
					// optimization -- we know the number of samples in the unparsed data, so use it here to
					// avoid parsing just to know if the genotypes context is empty
					return ParsedAlready ? base.Count == 0 : nUnparsedGenotypes == 0;
				}
			}
		}

		public int Count {
			get {
				// optimization -- we know the number of samples in the unparsed data, so use it here to
				// avoid parsing just to know the size of the context
				return ParsedAlready ? base.Count : nUnparsedGenotypes;
			}
		}

		public string UnparsedGenotypeData {
			get;
			private set;
		}
	}
}