using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;


namespace Bio.VCF
{

//TODO: This class has the stench of java all over it, clean up later
	/// <summary>
	/// Represents an ordered collection of Genotype objects
	/// </summary>
    public class GenotypesContext : IList<Genotype>
    {
        #region Static Members
        /// <summary>
        /// static constant value for an empty GenotypesContext.  Useful since so many VariantContexts have no genotypes
        /// </summary>
        public static readonly GenotypesContext NO_GENOTYPES = new GenotypesContext(new List<Genotype>(0), new Dictionary<string, int>(0), new List<string>());

        #region StaticFactoryMethods
        /// <summary>
        /// Basic creation routine </summary>
        /// <returns> an empty, mutable GenotypeContext </returns>
        public static GenotypesContext create()
        {
            return new GenotypesContext();
        }

        /// <summary>
        /// Basic creation routine </summary>
        /// <returns> an empty, mutable GenotypeContext with initial capacity for nGenotypes </returns>
        public static GenotypesContext create(int nGenotypes)
        {
            return new GenotypesContext(nGenotypes);
        }
        /// <summary>
        /// Create a fully resolved GenotypeContext containing genotypes, sample lookup table,
        /// and sorted sample names
        /// </summary>
        /// <param name="genotypes"> our genotypes in arbitrary </param>
        /// <param name="sampleNameToOffset"> map optimized for efficient lookup.  Each genotype in genotypes must have its
        /// sample name in sampleNameToOffset, with a corresponding integer value that indicates the offset of that
        /// genotype in the vector of genotypes </param>
        /// <param name="sampleNamesInOrder"> a list of sample names, one for each genotype in genotypes, sorted in alphabetical
        /// order. </param>
        /// <returns> an mutable GenotypeContext containing genotypes with already present lookup data </returns>
        public static GenotypesContext create(List<Genotype> genotypes, IDictionary<string, int> sampleNameToOffset, IList<string> sampleNamesInOrder)
        {
            return new GenotypesContext(genotypes, sampleNameToOffset, sampleNamesInOrder);
        }

        /// <summary>
        /// Create a fully resolved GenotypeContext containing genotypes
        /// </summary>
        /// <param name="genotypes"> our genotypes in arbitrary </param>
        /// <returns> an mutable GenotypeContext containing genotypes </returns>
        public static GenotypesContext create(List<Genotype> genotypes)
        {
            return genotypes == null ? NO_GENOTYPES : new GenotypesContext(genotypes);
        }

        /// <summary>
        /// Create a fully resolved GenotypeContext containing genotypes
        /// </summary>
        /// <param name="genotypes"> our genotypes in arbitrary </param>
        /// <returns> an mutable GenotypeContext containing genotypes </returns>
        public static GenotypesContext create(params Genotype[] genotypes)
        {
            return create(new List<Genotype>(genotypes));
        }

        /// <summary>
        /// Create a freshly allocated GenotypeContext containing the genotypes in toCopy
        /// </summary>
        /// <param name="toCopy"> the GenotypesContext to copy </param>
        /// <returns> an mutable GenotypeContext containing genotypes </returns>
        public static GenotypesContext copy(GenotypesContext toCopy)
        {
            return create(new List<Genotype>(toCopy.Genotypes));
        }

        /// <summary>
        /// Create a GenotypesContext containing the genotypes in iteration order contained
        /// in toCopy
        /// </summary>
        /// <param name="toCopy"> the collection of genotypes </param>
        /// <returns> an mutable GenotypeContext containing genotypes </returns>
        public static GenotypesContext copy(ICollection<Genotype> toCopy)
        {
            return toCopy == null ? NO_GENOTYPES : create(new List<Genotype>(toCopy));
        }
        #endregion
        #endregion

        /// <summary>
        /// sampleNamesInOrder a list of sample names, one for each genotype in genotypes, sorted in alphabetical order
        /// </summary>
        internal IList<string> sampleNamesInOrder = null;

        /// <summary>
        /// a map optimized for efficient lookup.  Each genotype in genotypes must have its
        /// sample name in sampleNameToOffset, with a corresponding integer value that indicates the offset of that
        /// genotype in the vector of genotypes
        /// </summary>
        internal IDictionary<string, int> sampleNameToOffset = null;

        /// <summary>
        /// An ArrayList of genotypes contained in this context
        /// 
        /// WARNING: TO ENABLE THE LAZY VERSION OF THIS CLASS, NO METHODS SHOULD DIRECTLY
        /// ACCESS THIS VARIABLE.  USE getGenotypes() INSTEAD.
        /// 
        /// </summary>
        internal List<Genotype> notToBeDirectlyAccessedGenotypes;

        public bool Mutable
        {get;set;}

        /// <summary>
        /// Cached value of the maximum ploidy observed among all samples
        /// </summary>
        private int maxPloidy = -1;

        // ---------------------------------------------------------------------------
        //
        // private constructors -- you have to use static create methods to make these classes
        //
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Create an empty GenotypeContext
        /// </summary>
        protected internal GenotypesContext()
            : this(10)
        {
        }

        /// <summary>
        /// Create an empty GenotypeContext, with initial capacity for n elements
        /// </summary>
        protected internal GenotypesContext(int n)
            : this(new List<Genotype>(n))
        {
        }

        /// <summary>
        /// Create an GenotypeContext containing genotypes
        /// </summary>
        protected internal GenotypesContext(List<Genotype> genotypes)
        {
            this.notToBeDirectlyAccessedGenotypes = genotypes;
            this.sampleNameToOffset = null;
        }

        /// <summary>
        /// Create a fully resolved GenotypeContext containing genotypes, sample lookup table,
        /// and sorted sample names
        /// </summary>
        /// <param name="genotypes"> our genotypes in arbitrary </param>
        /// <param name="sampleNameToOffset"> map optimized for efficient lookup.  Each genotype in genotypes must have its
        /// sample name in sampleNameToOffset, with a corresponding integer value that indicates the offset of that
        /// genotype in the vector of genotypes </param>
        /// <param name="sampleNamesInOrder"> a list of sample names, one for each genotype in genotypes, sorted in alphabetical
        /// order. </param>
        protected internal GenotypesContext(List<Genotype> genotypes, IDictionary<string, int> sampleNameToOffset, IList<string> sampleNamesInOrder)
        {
            this.notToBeDirectlyAccessedGenotypes = genotypes;
            this.sampleNameToOffset = sampleNameToOffset;
            this.sampleNamesInOrder = sampleNamesInOrder;
        }


        // ---------------------------------------------------------------------------
        //
        // Mutability methods
        //
        // ---------------------------------------------------------------------------

        public void checkImmutability()
        {
            if (!Mutable)
            {
                throw new Exception("GenotypeMap is currently immutable, but a mutator method was invoked on it");
            }
        }

        // ---------------------------------------------------------------------------
        //
        // caches
        //
        // ---------------------------------------------------------------------------

        protected internal virtual void invalidateSampleNameMap()
        {
            sampleNameToOffset = null;
        }

        protected internal virtual void invalidateSampleOrdering()
        {
            sampleNamesInOrder = null;
        }

        protected internal virtual void ensureSampleOrdering()
        {
            if (sampleNamesInOrder == null)
            {
                sampleNamesInOrder = (from name in Genotypes orderby name.SampleName select name.SampleName).ToList();
            }
        }

        protected internal virtual void ensureSampleNameMap()
        {
            if (sampleNameToOffset == null)
            {
                Enumerable.Range(0, Count).ToDictionary(o => new KeyValuePair<string, int>(Genotypes[o].SampleName, o));
            }
        }

        // ---------------------------------------------------------------------------
        //
        // Lazy methods
        //
        // ---------------------------------------------------------------------------

        public bool LazyWithData
        {
            get
            {
                return this is LazyGenotypesContext && ((LazyGenotypesContext)this).UnparsedGenotypeData != null;
            }
        }

        // ---------------------------------------------------------------------------
        //
        // Map methods
        //
        // ---------------------------------------------------------------------------

        protected virtual internal List<Genotype> Genotypes
        {
            get
            {
                return notToBeDirectlyAccessedGenotypes;
            }
            private set
            {
                checkImmutability();
                invalidateSampleNameMap();
                invalidateSampleOrdering();
                notToBeDirectlyAccessedGenotypes = value;
            }
        }

        public void Clear()
        {
            checkImmutability();
            invalidateSampleNameMap();
            invalidateSampleOrdering();
            Genotypes.Clear();
        }

        public int Count
        {
            get { return Genotypes.Count; }
        }

        public virtual bool Empty
        {
            get
            {
                return Genotypes.Count == 0;
            }
        }

        /// <summary>
        /// Adds a single genotype to this context.
        /// 
        /// There are many constraints on this input, and important
        /// impacts on the performance of other functions provided by this
        /// context.
        /// 
        /// First, the sample name of genotype must be unique within this
        /// context.  However, this is not enforced in the code itself, through
        /// you will invalid the contract on this context if you add duplicate
        /// samples and are running with CoFoJa enabled.
        /// 
        /// Second, adding genotype also updates the sample name -> index map,
        /// so add() followed by containsSample and related function is an efficient
        /// series of operations.
        /// 
        /// Third, adding the genotype invalidates the sorted list of sample names, to
        /// add() followed by any of the SampleNamesInOrder operations is inefficient, as
        /// each SampleNamesInOrder must rebuild the sorted list of sample names at
        /// an O(n log n) cost.
        /// </summary>
        /// <param name="genotype">
        /// @return </param>
        public void Add(Genotype genotype)
        {
            checkImmutability();
            invalidateSampleOrdering();
            if (sampleNameToOffset != null)
            {
                // update the name map by adding entries
                sampleNameToOffset[genotype.SampleName] = Count;
            }
            Genotypes.Add(genotype);
        }
      
        /// <summary>
        /// Adds all of the genotypes to this context
        /// 
        /// See <seealso cref="#add(Genotype)"/> for important information about this functions
        /// constraints and performance costs
        /// </summary>
        /// <param name="genotypes">
        public void AddRange(IEnumerable<Genotype> genotypes)
        {
            checkImmutability();
            invalidateSampleOrdering();
            if (sampleNameToOffset != null)
            {
                // update the name map by adding entries
                int pos = Count;
                foreach (Genotype g in genotypes)
                {
                    sampleNameToOffset[g.SampleName] = pos;
                    pos += 1;
                }
            }
            Genotypes.AddRange(genotypes);
        }
       

        /// <summary>
        /// What is the max ploidy among all samples?  Returns defaultPloidy if no genotypes are present
        /// </summary>
        /// <param name="defaultPloidy"> the default ploidy, if all samples are no-called
        /// @return </param>
        public int getMaxPloidy(int defaultPloidy)
        {
            if (defaultPloidy < 0)
            {
                throw new System.ArgumentException("defaultPloidy must be greater than or equal to 0");
            }

            if (maxPloidy == -1)
            {
                maxPloidy = 0; // necessary in the case where there are no genotypes
                foreach (Genotype g in Genotypes)
                {
                    maxPloidy = Math.Max(g.Ploidy, maxPloidy);
                }

                // everything is no called so we return the default ploidy
                if (maxPloidy == 0)
                {
                    maxPloidy = defaultPloidy;
                }
            }

            return maxPloidy;
        }

        private int? getSampleIndex(string sampleName)
        {
            ensureSampleNameMap();
            int ret;
            bool hasValue = sampleNameToOffset.TryGetValue(sampleName, out ret);
            if (!hasValue)
            {
                return null;
            }
            return ret;
        }


        /// <summary>
        /// Note that remove requires us to invalidate our sample -> index
        /// cache.  The loop:
        /// 
        /// GenotypesContext gc = ...
        /// for ( sample in samples )
        ///   if ( gc.containsSample(sample) )
        ///     gc.remove(sample)
        /// 
        /// is extremely inefficient, as each call to remove invalidates the cache
        /// and containsSample requires us to rebuild it, an O(n) operation.
        /// 
        /// If you must remove many samples from the GC, use either removeAll or retainAll
        /// to avoid this O(n * m) operation.
        /// </summary>
        /// <param name="i">
        /// @return </param>
        public void RemoveAt(int i)
        {
            checkImmutability();
            invalidateSampleNameMap();
            invalidateSampleOrdering();
            Genotypes.RemoveAt(i);
        }
        public void RemoveAll(IEnumerable<Genotype> objects)
        {
            checkImmutability();
            invalidateSampleNameMap();
            invalidateSampleOrdering();
            bool toRet = true;
            foreach (var o in objects)
            {
                toRet = toRet & Genotypes.Remove(o);
            }
            if (!toRet)
                throw new ArgumentException("Tried to remove genotype from context that was not in the collection");
        }
        public void retainAll(IEnumerable<Genotype> typesToKeep)
        {
            checkImmutability();
            invalidateSampleNameMap();
            invalidateSampleOrdering();
            notToBeDirectlyAccessedGenotypes = notToBeDirectlyAccessedGenotypes.Where(x => typesToKeep.Contains(x)).ToList();

        }
        public Genotype Set(int i, Genotype genotype)
        {
            checkImmutability();
            Genotype prev = Genotypes[i] = genotype;

            invalidateSampleOrdering();
            if (sampleNameToOffset != null)
            {
                // update the name map by removing the old entry and replacing it with the new one
                sampleNameToOffset.Remove(prev.SampleName);
                sampleNameToOffset[genotype.SampleName] = i;
            }

            return prev;
        }

        public Genotype[] ToArray()
        {
            return Genotypes.ToArray();
        }


        /// <summary>
        /// Iterate over the Genotypes in this context in the order specified by sampleNamesInOrder
        /// </summary>
        /// <param name="sampleNamesInOrder"> a Iterable of String, containing exactly one entry for each Genotype sample name in
        /// this context </param>
        /// <returns> a Iterable over the genotypes in this context. </returns>
        public IEnumerable<Genotype> IterateInSampleNameOrder(IEnumerable<string> sampleNamesInOrder)
        {
            return sampleNamesInOrder.Select(x => Genotypes[sampleNameToOffset[x]]);
        }
        /// <summary>
        /// Iterate over the Genotypes in this context in their sample name order (A, B, C)
        /// regardless of the underlying order in the vector of genotypes </summary>
        /// <returns> a Iterable over the genotypes in this context. </returns>
        public IEnumerable<Genotype> IterateInSampleNameOrder()
        {
            return IterateInSampleNameOrder(SampleNamesOrderedByName);
        }

        /// <returns> The set of sample names for all genotypes in this context, in arbitrary order </returns>
        public ISet<string> SampleNames
        {
            get
            {
                ensureSampleNameMap();
                return new HashSet<string>(sampleNameToOffset.Keys);
            }
        }

        /// <returns> The set of sample names for all genotypes in this context, in their natural ordering (A, B, C) </returns>

        public virtual IList<string> SampleNamesOrderedByName
        {
            get
            {
                ensureSampleOrdering();
                return sampleNamesInOrder;
            }
        }

        public bool ContainsSample(string sample)
        {
            ensureSampleNameMap();
            return sampleNameToOffset.ContainsKey(sample);
        }
        public bool ContainsSamples(ICollection<string> samples)
        {
            return SampleNames.IsSupersetOf(samples);
        }

        /// <summary>
        /// Return a freshly allocated subcontext of this context containing only the samples
        /// listed in samples.  Note that samples can contain names not in this context, they
        /// will just be ignored.
        /// </summary>
        /// <param name="samples">
        /// @return </param>
        public GenotypesContext subsetToSamples(ISet<string> samples)
        {
            int nSamples = samples.Count;

            if (nSamples == 0)
            {
                return NO_GENOTYPES;
            }
            else // nGenotypes < nSamples
            {
                GenotypesContext subset = create(samples.Count);
                foreach (String sample in samples)
                {
                    
                    Genotype g = Get(sample);
                    if (g != null)
                    {
                        subset.Add(g);
                    }
                }
                return subset;
            }
        }
        Genotype Get(string sampleName)
        {
            int? offset = getSampleIndex(sampleName);
            return offset.HasValue? Genotypes[offset.Value]: null ;
        }
        public override string ToString()
        {
            IList<string> gS = new List<string>();
            foreach (Genotype g in this.IterateInSampleNameOrder())
            {
                gS.Add(g.ToString());
            }
            return "[" + String.Join(",", gS) + "]";
        }
        public int IndexOf(Genotype item)
        {
            return Genotypes.IndexOf(item);
        }
        public void Insert(int index, Genotype item)
        {
            Set(index, item);
        }

        public Genotype this[int index]
        {
            get
            {
                return Genotypes[index];
            }
            set
            {
                Set(index, value);
            }
        }

        /// <summary>
        /// Gets sample associated with this sampleName, or null if none is found
        /// </summary>
        /// <param name="sampleName">

        public Genotype this[string sampleName]
        {
            get
            {
                int? offset = getSampleIndex(sampleName);
                var toR = offset.HasValue ? Genotypes[offset.Value] : null;
                return toR;
            }
        }
        public bool Contains(Genotype item)
        {
            return Genotypes.Contains(item);
        }

        public void CopyTo(Genotype[] array, int arrayIndex)
        {
            Genotypes.CopyTo(array, arrayIndex);
        }
        public bool IsReadOnly
        {
            get { return !Mutable; }
        }

        public bool Remove(Genotype item)
        {
            checkImmutability();
            invalidateSampleNameMap();
            invalidateSampleOrdering();
            return Genotypes.Remove(item);
        }
        public IEnumerator<Genotype> GetEnumerator()
        {
            return Genotypes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

}