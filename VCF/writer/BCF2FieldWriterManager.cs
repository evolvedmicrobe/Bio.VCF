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
	using GeneralUtils = org.broadinstitute.variant.utils.GeneralUtils;
	using Bio.VCF;


	/// <summary>
	/// See #BCFWriter for documentation on this classes role in encoding BCF2 files
	/// 
	/// @author Mark DePristo
	/// @since 06/12
	/// </summary>
	public class BCF2FieldWriterManager
	{
		internal readonly IDictionary<string, BCF2FieldWriter.SiteWriter> siteWriters = new Dictionary<string, BCF2FieldWriter.SiteWriter>();
		internal readonly IDictionary<string, BCF2FieldWriter.GenotypesWriter> genotypesWriters = new Dictionary<string, BCF2FieldWriter.GenotypesWriter>();
		internal readonly IntGenotypeFieldAccessors intGenotypeFieldAccessors = new IntGenotypeFieldAccessors();

		public BCF2FieldWriterManager()
		{
		}

		/// <summary>
		/// Setup the FieldWriters appropriate to each INFO and FORMAT in the VCF header
		/// 
		/// Must be called before any of the getter methods will work
		/// </summary>
		/// <param name="header"> a VCFHeader containing description for every INFO and FORMAT field we'll attempt to write out to BCF </param>
		/// <param name="encoder"> the encoder we are going to use to write out the BCF2 data </param>
		/// <param name="stringDictionary"> a map from VCFHeader strings to their offsets for encoding </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public void setup(final VCFHeader header, final BCF2Encoder encoder, final java.util.Map<String, Integer> stringDictionary)
		public virtual void setup(VCFHeader header, BCF2Encoder encoder, IDictionary<string, int?> stringDictionary)
		{
			foreach (VCFInfoHeaderLine line in header.InfoHeaderLines)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String field = line.getID();
				string field = line.ID;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BCF2FieldWriter.SiteWriter writer = createInfoWriter(header, line, encoder, stringDictionary);
				BCF2FieldWriter.SiteWriter writer = createInfoWriter(header, line, encoder, stringDictionary);
				add(siteWriters, field, writer);
			}

			foreach (VCFFormatHeaderLine line in header.FormatHeaderLines)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String field = line.getID();
				string field = line.ID;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BCF2FieldWriter.GenotypesWriter writer = createGenotypesWriter(header, line, encoder, stringDictionary);
				BCF2FieldWriter.GenotypesWriter writer = createGenotypesWriter(header, line, encoder, stringDictionary);
				add(genotypesWriters, field, writer);
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"field != null", "writer != null"}) @Ensures("map.containsKey(field)") private final <T> void add(final java.util.Map<String, T> map, final String field, final T writer)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private void add<T>(IDictionary<string, T> map, string field, T writer)
		{
			if (map.ContainsKey(field))
			{
				throw new IllegalStateException("BUG: field " + field + " already seen in VCFHeader while building BCF2 field encoders");
			}
			map[field] = writer;
		}

		// -----------------------------------------------------------------
		//
		// Master routine to look at the header, a specific line, and
		// build an appropriate SiteWriter for that header element
		//
		// -----------------------------------------------------------------

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private BCF2FieldWriter.SiteWriter createInfoWriter(final VCFHeader header, final VCFInfoHeaderLine line, final BCF2Encoder encoder, final java.util.Map<String, Integer> dict)
		private BCF2FieldWriter.SiteWriter createInfoWriter(VCFHeader header, VCFInfoHeaderLine line, BCF2Encoder encoder, IDictionary<string, int?> dict)
		{
			return new BCF2FieldWriter.GenericSiteWriter(header, createFieldEncoder(line, encoder, dict, false));
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private BCF2FieldEncoder createFieldEncoder(final VCFCompoundHeaderLine line, final BCF2Encoder encoder, final java.util.Map<String, Integer> dict, final boolean createGenotypesEncoders)
		private BCF2FieldEncoder createFieldEncoder(VCFCompoundHeaderLine line, BCF2Encoder encoder, IDictionary<string, int?> dict, bool createGenotypesEncoders)
		{

			if (createGenotypesEncoders && intGenotypeFieldAccessors.getAccessor(line.ID) != null)
			{
				if (GeneralUtils.DEBUG_MODE_ENABLED && line.Type != VCFHeaderLineType.Integer)
				{
					Console.Error.WriteLine("Warning: field " + line.ID + " expected to encode an integer but saw " + line.Type + " for record " + line);
				}
				return new BCF2FieldEncoder.IntArray(line, dict);
			}
			else if (createGenotypesEncoders && line.ID.Equals(VCFConstants.GENOTYPE_KEY))
			{
				return new BCF2FieldEncoder.GenericInts(line, dict);
			}
			else
			{
				switch (line.Type)
				{
					case char?:
					case string:
						return new BCF2FieldEncoder.StringOrCharacter(line, dict);
					case Flag:
						return new BCF2FieldEncoder.Flag(line, dict);
					case float?:
						return new BCF2FieldEncoder.Float(line, dict);
					case int?:
						if (line.FixedCount && line.Count == 1)
						{
							return new BCF2FieldEncoder.AtomicInt(line, dict);
						}
						else
						{
							return new BCF2FieldEncoder.GenericInts(line, dict);
						}
					default:
						throw new System.ArgumentException("Unexpected type for field " + line.ID);
				}
			}
		}

		// -----------------------------------------------------------------
		//
		// Master routine to look at the header, a specific line, and
		// build an appropriate Genotypes for that header element
		//
		// -----------------------------------------------------------------

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private BCF2FieldWriter.GenotypesWriter createGenotypesWriter(final VCFHeader header, final VCFFormatHeaderLine line, final BCF2Encoder encoder, final java.util.Map<String, Integer> dict)
		private BCF2FieldWriter.GenotypesWriter createGenotypesWriter(VCFHeader header, VCFFormatHeaderLine line, BCF2Encoder encoder, IDictionary<string, int?> dict)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String field = line.getID();
			string field = line.ID;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BCF2FieldEncoder fieldEncoder = createFieldEncoder(line, encoder, dict, true);
			BCF2FieldEncoder fieldEncoder = createFieldEncoder(line, encoder, dict, true);

			if (field.Equals(VCFConstants.GENOTYPE_KEY))
			{
				return new BCF2FieldWriter.GTWriter(header, fieldEncoder);
			}
			else if (line.ID.Equals(VCFConstants.GENOTYPE_FILTER_KEY))
			{
				return new BCF2FieldWriter.FTGenotypesWriter(header, fieldEncoder);
			}
			else if (intGenotypeFieldAccessors.getAccessor(field) != null)
			{
				return new BCF2FieldWriter.IGFGenotypesWriter(header, fieldEncoder, intGenotypeFieldAccessors.getAccessor(field));
			}
			else if (line.Type == VCFHeaderLineType.Integer)
			{
				return new BCF2FieldWriter.IntegerTypeGenotypesWriter(header, fieldEncoder);
			}
			else
			{
				return new BCF2FieldWriter.StaticallyTypeGenotypesWriter(header, fieldEncoder);
			}
		}

		// -----------------------------------------------------------------
		//
		// Accessors to get site / genotype writers
		//
		// -----------------------------------------------------------------

		/// <summary>
		/// Get a site writer specialized to encode values for site info field </summary>
		/// <param name="field"> key found in the VCF header INFO records </param>
		/// <returns> non-null writer if one can be found, or null if none exists for field </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public BCF2FieldWriter.SiteWriter getSiteFieldWriter(final String field)
		public virtual BCF2FieldWriter.SiteWriter getSiteFieldWriter(string field)
		{
			return getWriter(field, siteWriters);
		}

		/// <summary>
		/// Get a genotypes writer specialized to encode values for genotypes field </summary>
		/// <param name="field"> key found in the VCF header FORMAT records </param>
		/// <returns> non-null writer if one can be found, or null if none exists for field </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public BCF2FieldWriter.GenotypesWriter getGenotypeFieldWriter(final String field)
		public virtual BCF2FieldWriter.GenotypesWriter getGenotypeFieldWriter(string field)
		{
			return getWriter(field, genotypesWriters);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"map != null", "key != null"}) public <T> T getWriter(final String key, final java.util.Map<String, T> map)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual T getWriter<T>(string key, IDictionary<string, T> map)
		{
			return map[key];
		}
	}

}