using System;
using System.Collections.Generic;


namespace Bio.VCF
{
	/// <summary>
	/// A convenient way to provide a single view on the many int and int[] field values we work with,
	/// for writing out the values.  This class makes writing out the inline AD, GQ, PL, DP fields
	/// easy and fast
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
	    /// Return an accessor for the field
	    /// </summary>
	    /// <param name="field"></param>
	    /// <returns></returns>
		public virtual Accessor GetAccessor(string field)
		{
			return intGenotypeFieldEncoders[field];
		}

		public abstract class Accessor
		{
			public abstract int[] getValues(Genotype g);

			public int getSize(Genotype g)
			{
				int[] v = getValues(g);
				return v == null ? 0 : v.Length;
			}
		}

		public abstract class AtomicAccessor : Accessor
		{
			private readonly int[] singleton = new int[1];

			public override int[] getValues(Genotype g)
			{
				singleton[0] = getValue(g);
				return singleton[0] == -1 ? null : singleton;
			}

			public abstract int getValue(Genotype g);
		}

		public class GQAccessor : AtomicAccessor
		{
			public override int getValue(Genotype g)
			{
				return Math.Min(g.GQ, VCFConstants.MAX_GENOTYPE_QUAL);
			}
		}

		public class DPAccessor : AtomicAccessor
		{
			public override int getValue(Genotype g)
			{
				return g.DP;
			}
		}

		public class ADAccessor : Accessor
		{
			public override int[] getValues(Genotype g)
			{
				return g.AD;
			}
		}

		public class PLAccessor : Accessor
		{
			public override int[] getValues(Genotype g)
			{
				return g.PL;
			}
		}
	}

}