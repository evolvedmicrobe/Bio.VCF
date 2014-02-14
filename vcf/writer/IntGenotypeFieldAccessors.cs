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

	using VCFConstants = Bio.VCF.VCFConstants;
	using Genotype = org.broadinstitute.variant.variantcontext.Genotype;


	/// <summary>
	/// A convenient way to provide a single view on the many int and int[] field values we work with,
	/// for writing out the values.  This class makes writing out the inline AD, GQ, PL, DP fields
	/// easy and fast
	/// 
	/// @author Mark DePristo
	/// @since 6/12
	/// </summary>
	internal class IntGenotypeFieldAccessors
	{
		// initialized once per writer to allow parallel writers to work
		private readonly Dictionary<string, Accessor> intGenotypeFieldEncoders = new Dictionary<string, Accessor>();

		public IntGenotypeFieldAccessors()
		{
			intGenotypeFieldEncoders[VCFConstants.DEPTH_KEY] = new IntGenotypeFieldAccessors.DPAccessor();
			intGenotypeFieldEncoders[VCFConstants.GENOTYPE_ALLELE_DEPTHS] = new IntGenotypeFieldAccessors.ADAccessor();
			intGenotypeFieldEncoders[VCFConstants.GENOTYPE_PL_KEY] = new IntGenotypeFieldAccessors.PLAccessor();
			intGenotypeFieldEncoders[VCFConstants.GENOTYPE_QUALITY_KEY] = new IntGenotypeFieldAccessors.GQAccessor();
		}

		/// <summary>
		/// Return an accessor for field, or null if none exists </summary>
		/// <param name="field">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public Accessor getAccessor(final String field)
		public virtual Accessor getAccessor(string field)
		{
			return intGenotypeFieldEncoders[field];
		}

		public abstract class Accessor
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public abstract int[] getValues(final org.broadinstitute.variant.variantcontext.Genotype g);
			public abstract int[] getValues(Genotype g);

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public final int getSize(final org.broadinstitute.variant.variantcontext.Genotype g)
			public int getSize(Genotype g)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] v = getValues(g);
				int[] v = getValues(g);
				return v == null ? 0 : v.Length;
			}
		}

		private abstract class AtomicAccessor : Accessor
		{
			private readonly int[] singleton = new int[1];

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public int[] getValues(final org.broadinstitute.variant.variantcontext.Genotype g)
			public override int[] getValues(Genotype g)
			{
				singleton[0] = getValue(g);
				return singleton[0] == -1 ? null : singleton;
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public abstract int getValue(final org.broadinstitute.variant.variantcontext.Genotype g);
			public abstract int getValue(Genotype g);
		}

		public class GQAccessor : AtomicAccessor
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public int getValue(final org.broadinstitute.variant.variantcontext.Genotype g)
			public override int getValue(Genotype g)
			{
				return Math.Min(g.GQ, VCFConstants.MAX_GENOTYPE_QUAL);
			}
		}

		public class DPAccessor : AtomicAccessor
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public int getValue(final org.broadinstitute.variant.variantcontext.Genotype g)
			public override int getValue(Genotype g)
			{
				return g.DP;
			}
		}

		public class ADAccessor : Accessor
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public int[] getValues(final org.broadinstitute.variant.variantcontext.Genotype g)
			public override int[] getValues(Genotype g)
			{
				return g.AD;
			}
		}

		public class PLAccessor : Accessor
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public int[] getValues(final org.broadinstitute.variant.variantcontext.Genotype g)
			public override int[] getValues(Genotype g)
			{
				return g.PL;
			}
		}
	}

}