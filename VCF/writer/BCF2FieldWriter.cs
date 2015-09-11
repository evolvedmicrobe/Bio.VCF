using System;
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

namespace org.broadinstitute.variant.variantcontext.writer
{

	using Ensures = com.google.java.contract.Ensures;
	using Requires = com.google.java.contract.Requires;
	using BCF2Type = org.broadinstitute.variant.bcf2.BCF2Type;
	using BCF2Utils = org.broadinstitute.variant.bcf2.BCF2Utils;
	using VCFHeader = Bio.VCF.VCFHeader;
	using Allele = org.broadinstitute.variant.variantcontext.Allele;
	using Genotype = org.broadinstitute.variant.variantcontext.Genotype;
	using VariantContext = org.broadinstitute.variant.variantcontext.VariantContext;


	/// <summary>
	/// See #BCFWriter for documentation on this classes role in encoding BCF2 files
	/// 
	/// @author Mark DePristo
	/// @since 06/12
	/// </summary>
	public abstract class BCF2FieldWriter
	{
		private readonly VCFHeader header;
		private readonly BCF2FieldEncoder fieldEncoder;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"header != null", "fieldEncoder != null"}) protected BCF2FieldWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		protected internal BCF2FieldWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder)
		{
			this.header = header;
			this.fieldEncoder = fieldEncoder;
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") protected Bio.VCF.VCFHeader getHeader()
		protected internal virtual VCFHeader Header
		{
			get
			{
				return header;
			}
		}
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") protected BCF2FieldEncoder getFieldEncoder()
		protected internal virtual BCF2FieldEncoder FieldEncoder
		{
			get
			{
				return fieldEncoder;
			}
		}
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") protected String getField()
		protected internal virtual string Field
		{
			get
			{
				return FieldEncoder.Field;
			}
		}
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("vc != null") public void start(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual void start(BCF2Encoder encoder, VariantContext vc)
		{
			fieldEncoder.writeFieldKey(encoder);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void done(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual void done(BCF2Encoder encoder, VariantContext vc) // TODO -- overload done so that we null out values and test for correctness
		{
		}

		public override string ToString()
		{
			return "BCF2FieldWriter " + this.GetType().SimpleName + " with encoder " + FieldEncoder;
		}

		// --------------------------------------------------------------------------------
		//
		// Sites writers
		//
		// --------------------------------------------------------------------------------

		public abstract class SiteWriter : BCF2FieldWriter
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected SiteWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			protected internal SiteWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract void site(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException;
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public abstract void site(BCF2Encoder encoder, VariantContext vc);
		}

		public class GenericSiteWriter : SiteWriter
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public GenericSiteWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			public GenericSiteWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void site(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void site(BCF2Encoder encoder, VariantContext vc)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object rawValue = vc.getAttribute(getField(), null);
				object rawValue = vc.getAttribute(Field, null);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Type type = getFieldEncoder().getType(rawValue);
				BCF2Type type = FieldEncoder.getType(rawValue);
				if (rawValue == null)
				{
					// the value is missing, just write in null
					encoder.encodeType(0, type);
				}
				else
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int valueCount = getFieldEncoder().numElements(vc, rawValue);
					int valueCount = FieldEncoder.numElements(vc, rawValue);
					encoder.encodeType(valueCount, type);
					FieldEncoder.encodeValue(encoder, rawValue, type, valueCount);
				}
			}
		}

		// --------------------------------------------------------------------------------
		//
		// Genotypes writers
		//
		// --------------------------------------------------------------------------------

		public abstract class GenotypesWriter : BCF2FieldWriter
		{
			internal int nValuesPerGenotype = -1;
			internal BCF2Type encodingType = null;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected GenotypesWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			protected internal GenotypesWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{

				if (fieldEncoder.hasConstantNumElements())
				{
					nValuesPerGenotype = FieldEncoder.numElements();
				}
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @Requires({"encodingType != null", "nValuesPerGenotype >= 0 || ! getFieldEncoder().hasConstantNumElements()"}) @Ensures("nValuesPerGenotype >= 0") public void start(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void start(BCF2Encoder encoder, VariantContext vc)
			{
				// writes the key information
				base.start(encoder, vc);

				// only update if we need to
				if (!FieldEncoder.hasConstantNumElements())
				{
					if (FieldEncoder.hasContextDeterminedNumElements())
						// we are cheap -- just depends on genotype of allele counts
					{
						nValuesPerGenotype = FieldEncoder.numElements(vc);
					}
					else
						// we have to go fishing through the values themselves (expensive)
					{
						nValuesPerGenotype = computeMaxSizeOfGenotypeFieldFromValues(vc);
					}
				}

				encoder.encodeType(nValuesPerGenotype, encodingType);
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"encodingType != null", "nValuesPerGenotype >= 0"}) public void addGenotype(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public virtual void addGenotype(BCF2Encoder encoder, VariantContext vc, Genotype g)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object fieldValue = g.getExtendedAttribute(getField(), null);
				object fieldValue = g.getExtendedAttribute(Field, null);
				FieldEncoder.encodeValue(encoder, fieldValue, encodingType, nValuesPerGenotype);
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures({"result >= 0"}) protected int numElements(final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			protected internal virtual int numElements(VariantContext vc, Genotype g)
			{
				return FieldEncoder.numElements(vc, g.getExtendedAttribute(Field));
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures({"result >= 0"}) private final int computeMaxSizeOfGenotypeFieldFromValues(final org.broadinstitute.variant.variantcontext.VariantContext vc)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			private int computeMaxSizeOfGenotypeFieldFromValues(VariantContext vc)
			{
				int size = -1;

				foreach (Genotype g in vc.Genotypes)
				{
					size = Math.Max(size, numElements(vc, g));
				}

				return size;
			}
		}

		public class StaticallyTypeGenotypesWriter : GenotypesWriter
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public StaticallyTypeGenotypesWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			public StaticallyTypeGenotypesWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{
				encodingType = FieldEncoder.StaticType;
			}
		}

		public class IntegerTypeGenotypesWriter : GenotypesWriter
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public IntegerTypeGenotypesWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			public IntegerTypeGenotypesWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void start(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void start(BCF2Encoder encoder, VariantContext vc)
			{
				// the only value that is dynamic are integers
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Integer> values = new java.util.ArrayList<Integer>(vc.getNSamples());
				IList<int?> values = new List<int?>(vc.NSamples);
				foreach (Genotype g in vc.Genotypes)
				{
					foreach (Object i in BCF2Utils.toList(g.getExtendedAttribute(Field, null)))
					{
						if (i != null) // we know they are all integers
						{
							values.Add((int?)i);
						}
					}
				}

				encodingType = BCF2Utils.determineIntegerType(values);
				base.start(encoder, vc);
			}
		}

		public class IGFGenotypesWriter : GenotypesWriter
		{
			internal readonly IntGenotypeFieldAccessors.Accessor ige;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public IGFGenotypesWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder, final IntGenotypeFieldAccessors.Accessor ige)
			public IGFGenotypesWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder, IntGenotypeFieldAccessors.Accessor ige) : base(header, fieldEncoder)
			{
				this.ige = ige;

				if (!(fieldEncoder is BCF2FieldEncoder.IntArray))
				{
					throw new System.ArgumentException("BUG: IntGenotypesWriter requires IntArray encoder for field " + Field);
				}
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void start(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void start(BCF2Encoder encoder, VariantContext vc)
			{
				// TODO
				// TODO this piece of code consumes like 10% of the runtime alone because fo the vc.getGenotypes() iteration
				// TODO
				encodingType = BCF2Type.INT8;
				foreach (Genotype g in vc.Genotypes)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] pls = ige.getValues(g);
					int[] pls = ige.getValues(g);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Type plsType = getFieldEncoder().getType(pls);
					BCF2Type plsType = FieldEncoder.getType(pls);
					encodingType = BCF2Utils.maxIntegerType(encodingType, plsType);
					if (encodingType == BCF2Type.INT32)
					{
						break; // stop early
					}
				}

				base.start(encoder, vc);
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void addGenotype(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void addGenotype(BCF2Encoder encoder, VariantContext vc, Genotype g)
			{
				FieldEncoder.encodeValue(encoder, ige.getValues(g), encodingType, nValuesPerGenotype);
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected int numElements(final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g)
			protected internal override int numElements(VariantContext vc, Genotype g)
			{
				return ige.getSize(g);
			}
		}

		public class FTGenotypesWriter : StaticallyTypeGenotypesWriter
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public FTGenotypesWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			public FTGenotypesWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void addGenotype(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void addGenotype(BCF2Encoder encoder, VariantContext vc, Genotype g)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fieldValue = g.getFilters();
				string fieldValue = g.Filters;
				FieldEncoder.encodeValue(encoder, fieldValue, encodingType, nValuesPerGenotype);
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected int numElements(final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g)
			protected internal override int numElements(VariantContext vc, Genotype g)
			{
				return FieldEncoder.numElements(vc, g.Filters);
			}
		}

		public class GTWriter : GenotypesWriter
		{
			internal readonly IDictionary<Allele, int?> alleleMapForTriPlus = new Dictionary<Allele, int?>(5);
			internal Allele @ref, alt1;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public GTWriter(final Bio.VCF.VCFHeader header, final BCF2FieldEncoder fieldEncoder)
			public GTWriter(VCFHeader header, BCF2FieldEncoder fieldEncoder) : base(header, fieldEncoder)
			{
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void start(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void start(BCF2Encoder encoder, VariantContext vc)
			{
				if (vc.NAlleles > BCF2Utils.MAX_ALLELES_IN_GENOTYPES)
				{
					throw new IllegalStateException("Current BCF2 encoder cannot handle sites " + "with > " + BCF2Utils.MAX_ALLELES_IN_GENOTYPES + " alleles, but you have " + vc.NAlleles + " at " + vc.Chr + ":" + vc.Start);
				}

				encodingType = BCF2Type.INT8;
				buildAlleleMap(vc);
				nValuesPerGenotype = vc.getMaxPloidy(2);

				base.start(encoder, vc);
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void addGenotype(final BCF2Encoder encoder, final org.broadinstitute.variant.variantcontext.VariantContext vc, final org.broadinstitute.variant.variantcontext.Genotype g) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void addGenotype(BCF2Encoder encoder, VariantContext vc, Genotype g)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int samplePloidy = g.getPloidy();
				int samplePloidy = g.Ploidy;
				for (int i = 0; i < nValuesPerGenotype; i++)
				{
					if (i < samplePloidy)
					{
						// we encode the actual allele
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.variantcontext.Allele a = g.getAllele(i);
						Allele a = g.getAllele(i);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offset = getAlleleOffset(a);
						int offset = getAlleleOffset(a);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int encoded = ((offset+1) << 1) | (g.isPhased() ? 0x01 : 0x00);
						int encoded = ((offset + 1) << 1) | (g.Phased ? 0x01 : 0x00);
						encoder.encodeRawBytes(encoded, encodingType);
					}
					else
					{
						// we need to pad with missing as we have ploidy < max for this sample
						encoder.encodeRawBytes(encodingType.MissingBytes, encodingType);
					}
				}
			}

			/// <summary>
			/// Fast path code to determine the offset.
			/// 
			/// Inline tests for == against ref (most common, first test)
			/// == alt1 (second most common, second test)
			/// == NO_CALL (third)
			/// and finally in the map from allele => offset for all alt 2+ alleles
			/// </summary>
			/// <param name="a"> the allele whose offset we wish to determine </param>
			/// <returns> the offset (from 0) of the allele in the list of variant context alleles (-1 means NO_CALL) </returns>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("a != null") private final int getAlleleOffset(final org.broadinstitute.variant.variantcontext.Allele a)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			private int getAlleleOffset(Allele a)
			{
				if (a == @ref)
				{
					return 0;
				}
				else if (a == alt1)
				{
					return 1;
				}
				else if (a == Allele.NO_CALL)
				{
					return -1;
				}
				else
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer o = alleleMapForTriPlus.get(a);
					int? o = alleleMapForTriPlus[a];
					if (o == null)
					{
						throw new IllegalStateException("BUG: Couldn't find allele offset for allele " + a);
					}
					return o;
				}
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private final void buildAlleleMap(final org.broadinstitute.variant.variantcontext.VariantContext vc)
			private void buildAlleleMap(VariantContext vc)
			{
				// these are fast path options to determine the offsets for
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nAlleles = vc.getNAlleles();
				int nAlleles = vc.NAlleles;
				@ref = vc.Reference;
				alt1 = nAlleles > 1 ? vc.getAlternateAllele(0) : null;

				if (nAlleles > 2)
				{
					// for multi-allelics we need to clear the map, and add additional looks
					alleleMapForTriPlus.Clear();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<org.broadinstitute.variant.variantcontext.Allele> alleles = vc.getAlleles();
					IList<Allele> alleles = vc.Alleles;
					for (int i = 2; i < alleles.Count; i++)
					{
						alleleMapForTriPlus[alleles[i]] = i;
					}
				}
			}
		}
	}


}