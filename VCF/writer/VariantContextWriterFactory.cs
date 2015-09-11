using System;

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

	using SAMSequenceDictionary = net.sf.samtools.SAMSequenceDictionary;


	/// <summary>
	/// Factory methods to create VariantContext writers
	/// 
	/// @author depristo
	/// @since 5/12
	/// </summary>
	public class VariantContextWriterFactory
	{

		public static readonly EnumSet<Options> DEFAULT_OPTIONS = EnumSet.of(Options.INDEX_ON_THE_FLY);
		public static readonly EnumSet<Options> NO_OPTIONS = EnumSet.noneOf(typeof(Options));

		private VariantContextWriterFactory()
		{
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter create(final java.io.File location, final net.sf.samtools.SAMSequenceDictionary refDict)
		public static VariantContextWriter create(File location, SAMSequenceDictionary refDict)
		{
			return create(location, openOutputStream(location), refDict, DEFAULT_OPTIONS);
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter create(final java.io.File location, final net.sf.samtools.SAMSequenceDictionary refDict, final java.util.EnumSet<Options> options)
		public static VariantContextWriter create(File location, SAMSequenceDictionary refDict, EnumSet<Options> options)
		{
			return create(location, openOutputStream(location), refDict, options);
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter create(final java.io.File location, final java.io.OutputStream output, final net.sf.samtools.SAMSequenceDictionary refDict)
		public static VariantContextWriter create(File location, OutputStream output, SAMSequenceDictionary refDict)
		{
			return create(location, output, refDict, DEFAULT_OPTIONS);
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter create(final java.io.OutputStream output, final net.sf.samtools.SAMSequenceDictionary refDict, final java.util.EnumSet<Options> options)
		public static VariantContextWriter create(OutputStream output, SAMSequenceDictionary refDict, EnumSet<Options> options)
		{
			return create(null, output, refDict, options);
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter create(final java.io.File location, final java.io.OutputStream output, final net.sf.samtools.SAMSequenceDictionary refDict, final java.util.EnumSet<Options> options)
		public static VariantContextWriter create(File location, OutputStream output, SAMSequenceDictionary refDict, EnumSet<Options> options)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean enableBCF = isBCFOutput(location, options);
			bool enableBCF = isBCFOutput(location, options);

			if (enableBCF)
			{
				return new BCF2Writer(location, output, refDict, options.contains(Options.INDEX_ON_THE_FLY), options.contains(Options.DO_NOT_WRITE_GENOTYPES));
			}
			else
			{
				return new VCFWriter(location, output, refDict, options.contains(Options.INDEX_ON_THE_FLY), options.contains(Options.DO_NOT_WRITE_GENOTYPES), options.contains(Options.ALLOW_MISSING_FIELDS_IN_HEADER));
			}
		}

		/// <summary>
		/// Should we output a BCF file based solely on the name of the file at location?
		/// </summary>
		/// <param name="location">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static boolean isBCFOutput(final java.io.File location)
		public static bool isBCFOutput(File location)
		{
			return isBCFOutput(location, EnumSet.noneOf(typeof(Options)));
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static boolean isBCFOutput(final java.io.File location, final java.util.EnumSet<Options> options)
		public static bool isBCFOutput(File location, EnumSet<Options> options)
		{
			return options.contains(Options.FORCE_BCF) || (location != null && location.Name.contains(".bcf"));
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter sortOnTheFly(final VariantContextWriter innerWriter, int maxCachingStartDistance)
		public static VariantContextWriter sortOnTheFly(VariantContextWriter innerWriter, int maxCachingStartDistance)
		{
			return sortOnTheFly(innerWriter, maxCachingStartDistance, false);
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VariantContextWriter sortOnTheFly(final VariantContextWriter innerWriter, int maxCachingStartDistance, boolean takeOwnershipOfInner)
		public static VariantContextWriter sortOnTheFly(VariantContextWriter innerWriter, int maxCachingStartDistance, bool takeOwnershipOfInner)
		{
			return new SortingVariantContextWriter(innerWriter, maxCachingStartDistance, takeOwnershipOfInner);
		}

		/// <summary>
		/// Returns a output stream writing to location, or throws an exception if this fails </summary>
		/// <param name="location">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected static java.io.OutputStream openOutputStream(final java.io.File location)
		protected internal static OutputStream openOutputStream(File location)
		{
			try
			{
				return new FileOutputStream(location);
			}
			catch (FileNotFoundException e)
			{
				throw new Exception(location + ": Unable to create VCF writer", e);
			}
		}
	}

}