using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
	using TribbleException = org.broad.tribble.TribbleException;
	using ParsingUtils = org.broad.tribble.util.ParsingUtils;
	using Bio.VCF;
	using org.broadinstitute.variant.variantcontext;


	/// <summary>
	/// this class writes VCF files
	/// </summary>
	internal class VCFWriter : IndexingVariantContextWriter
	{
		private static readonly string VERSION_LINE = VCFHeader.METADATA_INDICATOR + VCFHeaderVersion.VCF4_1.FormatString + "=" + VCFHeaderVersion.VCF4_1.VersionString;

		// should we write genotypes or just sites?
		protected internal readonly bool doNotWriteGenotypes;

		// the VCF header we're storing
		protected internal VCFHeader mHeader = null;

		private readonly bool allowMissingFieldsInHeader;

		/// <summary>
		/// The VCF writer uses an internal Writer, based by the ByteArrayOutputStream lineBuffer,
		/// to temp. buffer the header and per-site output before flushing the per line output
		/// in one go to the super.getOutputStream.  This results in high-performance, proper encoding,
		/// and allows us to avoid flushing explicitly the output stream getOutputStream, which
		/// allows us to properly compress vcfs in gz format without breaking indexing on the fly
		/// for uncompressed streams.
		/// </summary>
		private const int INITIAL_BUFFER_SIZE = 1024 * 16;
		private readonly ByteArrayOutputStream lineBuffer = new ByteArrayOutputStream(INITIAL_BUFFER_SIZE);
		private readonly Writer writer;

		/// <summary>
		/// The encoding used for VCF files.  ISO-8859-1
		/// </summary>
		private readonly Charset charset;

		private IntGenotypeFieldAccessors intGenotypeFieldAccessors = new IntGenotypeFieldAccessors();

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public VCFWriter(final File location, final OutputStream output, final net.sf.samtools.SAMSequenceDictionary refDict, final boolean enableOnTheFlyIndexing, boolean doNotWriteGenotypes, final boolean allowMissingFieldsInHeader)
		public VCFWriter(File location, OutputStream output, SAMSequenceDictionary refDict, bool enableOnTheFlyIndexing, bool doNotWriteGenotypes, bool allowMissingFieldsInHeader) : base(writerName(location, output), location, output, refDict, enableOnTheFlyIndexing)
		{
			this.doNotWriteGenotypes = doNotWriteGenotypes;
			this.allowMissingFieldsInHeader = allowMissingFieldsInHeader;
			this.charset = Charset.forName("ISO-8859-1");
			this.writer = new OutputStreamWriter(lineBuffer, charset);
		}

		// --------------------------------------------------------------------------------
		//
		// VCFWriter interface functions
		//
		// --------------------------------------------------------------------------------

		/// <summary>
		/// Write String s to the internal buffered writer.
		/// 
		/// flushBuffer() must be called to actually write the data to the true output stream.
		/// </summary>
		/// <param name="s"> the string to write </param>
		/// <exception cref="IOException"> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void write(final String s) throws IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private void write(string s)
		{
			writer.write(s);
		}

		/// <summary>
		/// Actually write the line buffer contents to the destination output stream.
		/// 
		/// After calling this function the line buffer is reset, so the contents of the buffer can be reused
		/// </summary>
		/// <exception cref="IOException"> </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void flushBuffer() throws IOException
		private void flushBuffer()
		{
			writer.flush();
			OutputStream.write(lineBuffer.toByteArray());
			lineBuffer.reset();
		}

		public override void writeHeader(VCFHeader header)
		{
			// note we need to update the mHeader object after this call because they header
			// may have genotypes trimmed out of it, if doNotWriteGenotypes is true
			try
			{
				mHeader = writeHeader(header, writer, doNotWriteGenotypes, VersionLine, StreamName);
				flushBuffer();
			}
			catch (IOException e)
			{
				throw new Exception("Couldn't write file " + StreamName, e);
			}
		}

		public static string VersionLine
		{
			get
			{
				return VERSION_LINE;
			}
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static VCFHeader writeHeader(VCFHeader header, final Writer writer, final boolean doNotWriteGenotypes, final String versionLine, final String streamNameForError)
		public static VCFHeader writeHeader(VCFHeader header, Writer writer, bool doNotWriteGenotypes, string versionLine, string streamNameForError)
		{
			header = doNotWriteGenotypes ? new VCFHeader(header.MetaDataInSortedOrder) : header;

			try
			{
				// the file format field needs to be written first
				writer.write(versionLine + "\n");

				foreach (VCFHeaderLine line in header.MetaDataInSortedOrder)
				{
					if (VCFHeaderVersion.isFormatString(line.Key))
					{
						continue;
					}

					writer.write(VCFHeader.METADATA_INDICATOR);
					writer.write(line.ToString());
					writer.write("\n");
				}

				// write out the column line
				writer.write(VCFHeader.HEADER_INDICATOR);
				bool isFirst = true;
				foreach (string field in VCFHeader.HEADER_FIELDS)
				{
					if (isFirst)
					{
						isFirst = false; // don't write out a field separator
					}
					else
					{
						writer.write(VCFConstants.FIELD_SEPARATOR);
					}
					writer.write(field.ToString());
				}

				if (header.hasGenotypingData())
				{
					writer.write(VCFConstants.FIELD_SEPARATOR);
					writer.write("FORMAT");
					foreach (string sample in header.GenotypeSampleNames)
					{
						writer.write(VCFConstants.FIELD_SEPARATOR);
						writer.write(sample);
					}
				}

				writer.write("\n");
				writer.flush(); // necessary so that writing to an output stream will work
			}
			catch (IOException e)
			{
				throw new Exception("IOException writing the VCF header to " + streamNameForError, e);
			}

			return header;
		}

		/// <summary>
		/// attempt to close the VCF file
		/// </summary>
		public override void close()
		{
			// try to close the vcf stream
			try
			{
				// TODO -- would it be useful to null out the line buffer so we don't have it around unnecessarily?
				writer.close();
			}
			catch (IOException e)
			{
				throw new Exception("Unable to close " + StreamName, e);
			}

			base.close();
		}

		/// <summary>
		/// add a record to the file
		/// </summary>
		/// <param name="vc">      the Variant Context object </param>
		public override void add(VariantContext vc)
		{
			if (mHeader == null)
			{
				throw new IllegalStateException("The VCF Header must be written before records can be added: " + StreamName);
			}

			if (doNotWriteGenotypes)
			{
				vc = (new VariantContextBuilder(vc)).noGenotypes().make();
			}

			try
			{
				base.add(vc);

				IDictionary<Allele, string> alleleMap = buildAlleleMap(vc);

				// CHROM
				write(vc.Chr);
				write(VCFConstants.FIELD_SEPARATOR);

				// POS
				write(Convert.ToString(vc.Start));
				write(VCFConstants.FIELD_SEPARATOR);

				// ID
				string ID = vc.ID;
				write(ID);
				write(VCFConstants.FIELD_SEPARATOR);

				// REF
				string refString = vc.Reference.DisplayString;
				write(refString);
				write(VCFConstants.FIELD_SEPARATOR);

				// ALT
				if (vc.Variant)
				{
					Allele altAllele = vc.getAlternateAllele(0);
					string alt = altAllele.DisplayString;
					write(alt);

					for (int i = 1; i < vc.AlternateAlleles.Count; i++)
					{
						altAllele = vc.getAlternateAllele(i);
						alt = altAllele.DisplayString;
						write(",");
						write(alt);
					}
				}
				else
				{
					write(VCFConstants.EMPTY_ALTERNATE_ALLELE_FIELD);
				}
				write(VCFConstants.FIELD_SEPARATOR);

				// QUAL
				if (!vc.hasLog10PError())
				{
					write(VCFConstants.MISSING_VALUE_v4);
				}
				else
				{
					write(formatQualValue(vc.PhredScaledQual));
				}
				write(VCFConstants.FIELD_SEPARATOR);

				// FILTER
				string filters = getFilterString(vc);
				write(filters);
				write(VCFConstants.FIELD_SEPARATOR);

				// INFO
				IDictionary<string, string> infoFields = new SortedDictionary<string, string>();
				foreach (KeyValuePair<string, object> field in vc.Attributes)
				{
					string key = field.Key;

					if (!mHeader.hasInfoLine(key))
					{
						fieldIsMissingFromHeaderError(vc, key, "INFO");
					}

					string outputValue = formatVCFField(field.Value);
					if (outputValue != null)
					{
						infoFields[key] = outputValue;
					}
				}
				writeInfoString(infoFields);

				// FORMAT
				GenotypesContext gc = vc.Genotypes;
				if (gc.LazyWithData && ((LazyGenotypesContext)gc).UnparsedGenotypeData is string)
				{
					write(VCFConstants.FIELD_SEPARATOR);
					write(((LazyGenotypesContext) gc).UnparsedGenotypeData.ToString());
				}
				else
				{
					IList<string> genotypeAttributeKeys = calcVCFGenotypeKeys(vc, mHeader);
					if (genotypeAttributeKeys.Count > 0)
					{
						foreach (String format in genotypeAttributeKeys)
						{
							if (!mHeader.hasFormatLine(format))
							{
								fieldIsMissingFromHeaderError(vc, format, "FORMAT");
							}
						}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String genotypeFormatString = org.broad.tribble.util.ParsingUtils.join(VCFConstants.GENOTYPE_FIELD_SEPARATOR, genotypeAttributeKeys);
						string genotypeFormatString = ParsingUtils.join(VCFConstants.GENOTYPE_FIELD_SEPARATOR, genotypeAttributeKeys);

						write(VCFConstants.FIELD_SEPARATOR);
						write(genotypeFormatString);

						addGenotypeData(vc, alleleMap, genotypeAttributeKeys);
					}
				}

				write("\n");
				// note that we cannot call flush here if we want block gzipping to work properly
				// calling flush results in all gzipped blocks for each variant
				flushBuffer();
			}
			catch (IOException e)
			{
				throw new Exception("Unable to write the VCF object to " + StreamName, e);
			}
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private static Map<Allele, String> buildAlleleMap(final VariantContext vc)
		private static IDictionary<Allele, string> buildAlleleMap(VariantContext vc)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Map<Allele, String> alleleMap = new HashMap<Allele, String>(vc.getAlleles().size()+1);
			IDictionary<Allele, string> alleleMap = new Dictionary<Allele, string>(vc.Alleles.Count + 1);
			alleleMap[Allele.NO_CALL] = VCFConstants.EMPTY_ALLELE; // convenience for lookup

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final List<Allele> alleles = vc.getAlleles();
			IList<Allele> alleles = vc.Alleles;
			for (int i = 0; i < alleles.Count; i++)
			{
				alleleMap[alleles[i]] = Convert.ToString(i);
			}

			return alleleMap;
		}

		// --------------------------------------------------------------------------------
		//
		// implementation functions
		//
		// --------------------------------------------------------------------------------

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private final String getFilterString(final VariantContext vc)
		private string getFilterString(VariantContext vc)
		{
			if (vc.Filtered)
			{
				foreach (String filter in vc.Filters)
				{
					if (!mHeader.hasFilterLine(filter))
					{
						fieldIsMissingFromHeaderError(vc, filter, "FILTER");
					}
				}

				return ParsingUtils.join(";", ParsingUtils.sortList(vc.Filters));
			}
			else if (vc.filtersWereApplied())
			{
				return VCFConstants.PASSES_FILTERS_v4;
			}
			else
			{
				return VCFConstants.UNFILTERED;
			}
		}

		private const string QUAL_FORMAT_STRING = "%.2f";
		private const string QUAL_FORMAT_EXTENSION_TO_TRIM = ".00";

		private string formatQualValue(double qual)
		{
			string s = string.format(QUAL_FORMAT_STRING, qual);
			if (s.EndsWith(QUAL_FORMAT_EXTENSION_TO_TRIM))
			{
				s = s.Substring(0, s.Length - QUAL_FORMAT_EXTENSION_TO_TRIM.Length);
			}
			return s;
		}

		/// <summary>
		/// create the info string; assumes that no values are null
		/// </summary>
		/// <param name="infoFields"> a map of info fields </param>
		/// <exception cref="IOException"> for writer </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeInfoString(Map<String, String> infoFields) throws IOException
		private void writeInfoString(IDictionary<string, string> infoFields)
		{
			if (infoFields.Count == 0)
			{
				write(VCFConstants.EMPTY_INFO_FIELD);
				return;
			}

			bool isFirst = true;
			foreach (KeyValuePair<string, string> entry in infoFields)
			{
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					write(VCFConstants.INFO_FIELD_SEPARATOR);
				}

				string key = entry.Key;
				write(key);

				if (!entry.Value.Equals(""))
				{
					VCFInfoHeaderLine metaData = mHeader.getInfoHeaderLine(key);
					if (metaData == null || metaData.CountType != VCFHeaderLineCount.INTEGER || metaData.Count != 0)
					{
						write("=");
						write(entry.Value);
					}
				}
			}
		}

		/// <summary>
		/// add the genotype data
		/// </summary>
		/// <param name="vc">                     the variant context </param>
		/// <param name="genotypeFormatKeys">  Genotype formatting string </param>
		/// <param name="alleleMap">              alleles for this context </param>
		/// <exception cref="IOException"> for writer </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void addGenotypeData(VariantContext vc, Map<Allele, String> alleleMap, List<String> genotypeFormatKeys) throws IOException
		private void addGenotypeData(VariantContext vc, IDictionary<Allele, string> alleleMap, IList<string> genotypeFormatKeys)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int ploidy = vc.getMaxPloidy(2);
			int ploidy = vc.getMaxPloidy(2);

			foreach (string sample in mHeader.GenotypeSampleNames)
			{
				write(VCFConstants.FIELD_SEPARATOR);

				Genotype g = vc.getGenotype(sample);
				if (g == null)
				{
					g = GenotypeBuilder.createMissing(sample, ploidy);
				}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final List<String> attrs = new ArrayList<String>(genotypeFormatKeys.size());
				IList<string> attrs = new List<string>(genotypeFormatKeys.Count);
				foreach (string field in genotypeFormatKeys)
				{
					if (field.Equals(VCFConstants.GENOTYPE_KEY))
					{
						if (!g.Available)
						{
							throw new IllegalStateException("GTs cannot be missing for some samples if they are available for others in the record");
						}

						writeAllele(g.getAllele(0), alleleMap);
						for (int i = 1; i < g.Ploidy; i++)
						{
							write(g.Phased ? VCFConstants.PHASED : VCFConstants.UNPHASED);
							writeAllele(g.getAllele(i), alleleMap);
						}

						continue;
					}
					else
					{
						string outputValue;
						if (field.Equals(VCFConstants.GENOTYPE_FILTER_KEY))
						{
							outputValue = g.Filtered ? g.Filters : VCFConstants.PASSES_FILTERS_v4;
						}
						else
						{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IntGenotypeFieldAccessors.Accessor accessor = intGenotypeFieldAccessors.getAccessor(field);
							IntGenotypeFieldAccessors.Accessor accessor = intGenotypeFieldAccessors.getAccessor(field);
							if (accessor != null)
							{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] intValues = accessor.getValues(g);
								int[] intValues = accessor.getValues(g);
								if (intValues == null)
								{
									outputValue = VCFConstants.MISSING_VALUE_v4;
								}
								else if (intValues.Length == 1) // fast path
								{
									outputValue = Convert.ToString(intValues[0]);
								}
								else
								{
									StringBuilder sb = new StringBuilder();
									sb.Append(intValues[0]);
									for (int i = 1; i < intValues.Length; i++)
									{
										sb.Append(",");
										sb.Append(intValues[i]);
									}
									outputValue = sb.ToString();
								}
							}
							else
							{
								object val = g.hasExtendedAttribute(field) ? g.getExtendedAttribute(field) : VCFConstants.MISSING_VALUE_v4;

								VCFFormatHeaderLine metaData = mHeader.getFormatHeaderLine(field);
								if (metaData != null)
								{
									int numInFormatField = metaData.getCount(vc);
									if (numInFormatField > 1 && val.Equals(VCFConstants.MISSING_VALUE_v4))
									{
										// If we have a missing field but multiple values are expected, we need to construct a new string with all fields.
										// For example, if Number=2, the string has to be ".,."
										StringBuilder sb = new StringBuilder(VCFConstants.MISSING_VALUE_v4);
										for (int i = 1; i < numInFormatField; i++)
										{
											sb.Append(",");
											sb.Append(VCFConstants.MISSING_VALUE_v4);
										}
										val = sb.ToString();
									}
								}

								// assume that if key is absent, then the given string encoding suffices
								outputValue = formatVCFField(val);
							}
						}

						if (outputValue != null)
						{
							attrs.Add(outputValue);
						}
					}
				}

				// strip off trailing missing values
				for (int i = attrs.Count - 1; i >= 0; i--)
				{
					if (isMissingValue(attrs[i]))
					{
						attrs.RemoveAt(i);
					}
					else
					{
						break;
					}
				}

				for (int i = 0; i < attrs.Count; i++)
				{
					if (i > 0 || genotypeFormatKeys.Contains(VCFConstants.GENOTYPE_KEY))
					{
						write(VCFConstants.GENOTYPE_FIELD_SEPARATOR);
					}
					write(attrs[i]);
				}
			}
		}

		private bool isMissingValue(string s)
		{
			// we need to deal with the case that it's a list of missing values
			return (countOccurrences(VCFConstants.MISSING_VALUE_v4[0], s) + countOccurrences(',', s) == s.Length);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void writeAllele(Allele allele, Map<Allele, String> alleleMap) throws IOException
		private void writeAllele(Allele allele, IDictionary<Allele, string> alleleMap)
		{
			string encoding = alleleMap[allele];
			if (encoding == null)
			{
				throw new TribbleException.InternalCodecException("Allele " + allele + " is not an allele in the variant context");
			}
			write(encoding);
		}

		/// <summary>
		/// Takes a double value and pretty prints it to a String for display
		/// 
		/// Large doubles => gets %.2f style formatting
		/// Doubles < 1 / 10 but > 1/100 </>=> get %.3f style formatting
		/// Double < 1/100 => %.3e formatting </summary>
		/// <param name="d">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static final String formatVCFDouble(final double d)
		public static string formatVCFDouble(double d)
		{
			string format;
			if (d < 1)
			{
				if (d < 0.01)
				{
					if (Math.Abs(d) >= 1e-20)
					{
						format = "%.3e";
					}
					else
					{
						// return a zero format
						return "0.00";
					}
				}
				else
				{
					format = "%.3f";
				}
			}
			else
			{
				format = "%.2f";
			}

			return string.format(format, d);
		}

		public static string formatVCFField(object val)
		{
			string result;
			if (val == null)
			{
				result = VCFConstants.MISSING_VALUE_v4;
			}
			else if (val is double?)
			{
				result = formatVCFDouble((double?) val);
			}
			else if (val is bool?)
			{
				result = (bool?)val ? "" : null; // empty string for true, null for false
			}
			else if (val is IList)
			{
				result = formatVCFField(((IList)val).ToArray());
			}
			else if (val.GetType().IsArray)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int length = Array.getLength(val);
				int length = Array.getLength(val);
				if (length == 0)
				{
					return formatVCFField(null);
				}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuilder sb = new StringBuilder(formatVCFField(Array.get(val, 0)));
				StringBuilder sb = new StringBuilder(formatVCFField(Array.get(val, 0)));
				for (int i = 1; i < length; i++)
				{
					sb.Append(",");
					sb.Append(formatVCFField(Array.get(val, i)));
				}
				result = sb.ToString();
			}
			else
			{
				result = val.ToString();
			}

			return result;
		}

		/// <summary>
		/// Determine which genotype fields are in use in the genotypes in VC </summary>
		/// <param name="vc"> </param>
		/// <returns> an ordered list of genotype fields in use in VC.  If vc has genotypes this will always include GT first </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public static List<String> calcVCFGenotypeKeys(final VariantContext vc, final VCFHeader header)
		public static IList<string> calcVCFGenotypeKeys(VariantContext vc, VCFHeader header)
		{
			Set<string> keys = new HashSet<string>();

			bool sawGoodGT = false;
			bool sawGoodQual = false;
			bool sawGenotypeFilter = false;
			bool sawDP = false;
			bool sawAD = false;
			bool sawPL = false;
			foreach (Genotype g in vc.Genotypes)
			{
				keys.addAll(g.ExtendedAttributes.Keys);
				if (g.Available)
				{
					sawGoodGT = true;
				}
				if (g.hasGQ())
				{
					sawGoodQual = true;
				}
				if (g.hasDP())
				{
					sawDP = true;
				}
				if (g.hasAD())
				{
					sawAD = true;
				}
				if (g.hasPL())
				{
					sawPL = true;
				}
				if (g.Filtered)
				{
					sawGenotypeFilter = true;
				}
			}

			if (sawGoodQual)
			{
				keys.add(VCFConstants.GENOTYPE_QUALITY_KEY);
			}
			if (sawDP)
			{
				keys.add(VCFConstants.DEPTH_KEY);
			}
			if (sawAD)
			{
				keys.add(VCFConstants.GENOTYPE_ALLELE_DEPTHS);
			}
			if (sawPL)
			{
				keys.add(VCFConstants.GENOTYPE_PL_KEY);
			}
			if (sawGenotypeFilter)
			{
				keys.add(VCFConstants.GENOTYPE_FILTER_KEY);
			}

			IList<string> sortedList = ParsingUtils.sortList(new List<string>(keys));

			// make sure the GT is first
			if (sawGoodGT)
			{
				IList<string> newList = new List<string>(sortedList.Count + 1);
				newList.Add(VCFConstants.GENOTYPE_KEY);
				newList.AddRange(sortedList);
				sortedList = newList;
			}

			if (sortedList.Count == 0 && header.hasGenotypingData())
			{
				// this needs to be done in case all samples are no-calls
				return Collections.singletonList(VCFConstants.GENOTYPE_KEY);
			}
			else
			{
				return sortedList;
			}
		}


		private static int countOccurrences(char c, string s)
		{
			   int count = 0;
			   for (int i = 0; i < s.Length; i++)
			   {
				   count += s[i] == c ? 1 : 0;
			   }
			   return count;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private final void fieldIsMissingFromHeaderError(final VariantContext vc, final String id, final String field)
		private void fieldIsMissingFromHeaderError(VariantContext vc, string id, string field)
		{
			if (!allowMissingFieldsInHeader)
			{
				throw new IllegalStateException("Key " + id + " found in VariantContext field " + field + " at " + vc.Chr + ":" + vc.Start + " but this key isn't defined in the VCFHeader.  We require all VCFs to have" + " complete VCF headers by default.");
			}
		}
	}

}