using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace Bio.VCF
{
    
	/// <summary>
	/// This class writes VCF files
	/// </summary>
	public class VCFWriter
    {
        #region Statics and constants

        private static readonly string VERSION_LINE = VCFHeader.METADATA_INDICATOR + VCFHeaderVersion.VCF4_1.FormatString + "=" + VCFHeaderVersion.VCF4_1.VersionString;
        private const string QUAL_FORMAT_STRING = "%.2f";
        private const string QUAL_FORMAT_EXTENSION_TO_TRIM = ".00";
        public static string VersionLine
        {
            get
            {
                return VERSION_LINE;
            }
        }

        private static IDictionary<Allele, string> buildAlleleMap(VariantContext vc)
        {
            IDictionary<Allele, string> alleleMap = new Dictionary<Allele, string>(vc.Alleles.Count + 1);
            alleleMap[Allele.NO_CALL] = VCFConstants.EMPTY_ALLELE; // convenience for lookup

            IList<Allele> alleles = vc.Alleles;
            for (int i = 0; i < alleles.Count; i++)
            {
                alleleMap[alleles[i]] = Convert.ToString(i);
            }

            return alleleMap;
        }
        /// <summary>
        /// The encoding used for VCF files.  ISO-8859-1
        /// </summary>
        private readonly string encodingName = "ISO-8859-1";
        private static string formatVCFDouble(double d)
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

            return string.Format(format, d);
        }

        protected static string formatVCFField(object val)
        {
            string result;
            if (val == null)
            {
                result = VCFConstants.MISSING_VALUE_v4;
            }
            else if (val is double)
            {
                result = formatVCFDouble((double)val);
            }
            else if (val is bool)
            {
                result = (bool)val ? "" : null; // empty string for true, null for false <- ND - huh? 
            }
            //This used to cover IList and arrays, think they both must be strings though
            else if (val is IEnumerable<string>)
            {
                result = String.Join(",", (val as IEnumerable<string>).Select(x => x.ToString()));
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
        private static IList<string> calcVCFGenotypeKeys(VariantContext vc)
        {
            //TODO: not sure who wrote this, these boolean flags should be removed though
            HashSet<string> keys = new HashSet<string>();

            bool sawGoodGT = false;
            bool sawGoodQual = false;
            bool sawGenotypeFilter = false;
            bool sawDP = false;
            bool sawAD = false;
            bool sawPL = false;
            foreach (Genotype g in vc.Genotypes)
            {
                //todo, make this a string later
                foreach (string s in g.ExtendedAttributes.Keys.Select(x => x.ToString()))
                {
                    keys.Add(s);
                }
                if (g.Available)
                {
                    sawGoodGT = true;
                }
                if (g.HasGQ)
                {
                    sawGoodQual = true;
                }
                if (g.HasDP)
                {
                    sawDP = true;
                }
                if (g.HasAD)
                {
                    sawAD = true;
                }
                if (g.HasPL)
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
            }
            if (sawDP)
            {
                keys.Add(VCFConstants.DEPTH_KEY);
            }
            if (sawAD)
            {
                keys.Add(VCFConstants.GENOTYPE_ALLELE_DEPTHS);
            }
            if (sawPL)
            {
                keys.Add(VCFConstants.GENOTYPE_PL_KEY);
            }
            if (sawGenotypeFilter)
            {
                keys.Add(VCFConstants.GENOTYPE_FILTER_KEY);
            }

            IList<string> sortedList = ParsingUtils.SortList(new List<string>(keys));

            // make sure the GT is first
            if (sawGoodGT)
            {
                IList<string> newList = new List<string>(sortedList.Count + 1);
                newList.Add(VCFConstants.GENOTYPE_KEY);
                foreach (string s in sortedList)
                { newList.Add(s); }
                sortedList = newList;
            }
            if (sortedList.Count == 0)
            {
                // this needs to be done in case all samples are no-calls
                return new List<string>() { VCFConstants.GENOTYPE_KEY };
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


        #endregion

        #region Public Methods
        public void WriteVariants(string outFileName, IEnumerable<VariantContext> variants,VCFHeader header)
        {
            System.Text.Encoding enc = System.Text.Encoding.GetEncoding(encodingName);
            writer = new StreamWriter(outFileName, false, enc);
            writeHeader(header);
            foreach (var vc in variants)
            {
                writer.Write(getVariantLinetoWrite(vc));
            }
            writer.Close();
        }
        public VCFWriter(VCFWriterOptions opts = VCFWriterOptions.NONE)
        {
            this.doNotWriteGenotypes = opts.HasFlag(VCFWriterOptions.DO_NOT_WRITE_GENOTYPES);
            this.allowMissingFieldsInHeader = opts.HasFlag(VCFWriterOptions.ALLOW_MISSING_FIELDS_IN_HEADER);
        }
        #endregion

        // should we write genotypes or just sites?
		protected internal readonly bool doNotWriteGenotypes;

		// the VCF header we're storing
		protected internal VCFHeader mHeader = null;


		private readonly bool allowMissingFieldsInHeader;

		private StreamWriter writer;

		private IntGenotypeFieldAccessors intGenotypeFieldAccessors = new IntGenotypeFieldAccessors();

		

        /// <summary>
        /// Add a record to the file
        /// </summary>
        /// <param name="vc">The Variant Context object </param>
        protected string getVariantLinetoWrite(VariantContext vc)
        {
     
            if (doNotWriteGenotypes)
            {
                vc = (new VariantContextBuilder(vc)).noGenotypes().make();
            }
            try
            {
                //Convert alleles to 1,2,3,etc. numbering
                IDictionary<Allele, string> alleleMap = buildAlleleMap(vc);
                // CHROM
                StringBuilder lineToWrite = new StringBuilder();
                //Add chr, pos, id, ref
                lineToWrite.Append(String.Join(VCFConstants.FIELD_SEPARATOR, vc.Chr, vc.Start.ToString(), vc.ID, vc.Reference.DisplayString));
                // ALT
                if (vc.Variant)
                {
                    Allele altAllele = vc.GetAlternateAllele(0);
                    string alt = altAllele.DisplayString;
                    lineToWrite.Append(alt);

                    for (int i = 1; i < vc.AlternateAlleles.Count; i++)
                    {
                        altAllele = vc.GetAlternateAllele(i);
                        alt = altAllele.DisplayString;
                        lineToWrite.Append(",");
                        lineToWrite.Append(alt);
                    }
                }
                else
                {
                    lineToWrite.Append(VCFConstants.EMPTY_ALTERNATE_ALLELE_FIELD);
                }
                lineToWrite.Append(VCFConstants.FIELD_SEPARATOR);

                // QUAL
                if (!vc.HasLog10PError)
                {
                    lineToWrite.Append(VCFConstants.MISSING_VALUE_v4);
                }
                else
                {
                    lineToWrite.Append(formatQualValue(vc.PhredScaledQual));
                }
                lineToWrite.Append(VCFConstants.FIELD_SEPARATOR);

                // FILTER
                string filters = getFilterString(vc);
                lineToWrite.Append(filters);
                lineToWrite.Append(VCFConstants.FIELD_SEPARATOR);

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
                lineToWrite.Append(getInfoString(infoFields)); ;

                // FORMAT
                GenotypesContext gc = vc.Genotypes;
                if (gc.LazyWithData && ((LazyGenotypesContext)gc).UnparsedGenotypeData is string)
                {
                    lineToWrite.Append(VCFConstants.FIELD_SEPARATOR);
                    lineToWrite.Append(((LazyGenotypesContext)gc).UnparsedGenotypeData.ToString());
                }
                else
                {
                    IList<string> genotypeAttributeKeys = calcVCFGenotypeKeys(vc);
                    if (genotypeAttributeKeys.Count > 0)
                    {
                        foreach (String format in genotypeAttributeKeys)
                        {
                            if (!mHeader.hasFormatLine(format))
                            {
                                fieldIsMissingFromHeaderError(vc, format, "FORMAT");
                            }
                        }

                        string genotypeFormatString = String.Join(VCFConstants.GENOTYPE_FIELD_SEPARATOR, genotypeAttributeKeys);
                        lineToWrite.Append(VCFConstants.FIELD_SEPARATOR);
                        lineToWrite.Append(genotypeFormatString);
                        lineToWrite.Append(getGenotypeDataText(vc, alleleMap, genotypeAttributeKeys));
                    }
                }
                lineToWrite.Append("\n");
                return lineToWrite.ToString();
            }
            catch (IOException e)
            {
                throw new Exception("Unable to write the VCF object:\n " + vc.ToString() + "\n", e);
            }
        }

        void writeHeader(VCFHeader header)
		{
          
			header = doNotWriteGenotypes ? new VCFHeader(header.MetaDataInSortedOrder) : header;
			try
			{
				// the file format field needs to be written first
				writer.Write(VERSION_LINE + "\n");

				foreach (VCFHeaderLine line in header.MetaDataInSortedOrder)
				{
					if (VCFHeaderVersion.IsFormatString(line.Key))
					{
						continue;
					}

					writer.Write(VCFHeader.METADATA_INDICATOR);
					writer.Write(line.ToString());
					writer.Write("\n");
				}

				// write out the column line
				writer.Write(VCFHeader.HEADER_INDICATOR);
				bool isFirst = true;
				foreach (string field in VCFHeader.HEADER_FIELDS)
				{
					if (isFirst)
					{
						isFirst = false; // don't write out a field separator
					}
					else
					{
						writer.Write(VCFConstants.FIELD_SEPARATOR);
					}
					writer.Write(field.ToString());
				}
				if (header.hasGenotypingData())
				{
					writer.Write(VCFConstants.FIELD_SEPARATOR);
					writer.Write("FORMAT");
					foreach (string sample in header.GenotypeSampleNames)
					{
						writer.Write(VCFConstants.FIELD_SEPARATOR);
						writer.Write(sample);
					}
				}
				writer.Write("\n");
	        }
			catch (IOException e)
			{
				throw new Exception("IOException writing the VCF header." , e);
			}

			
		}	


		// --------------------------------------------------------------------------------
		//
		// implementation functions
		//
		// --------------------------------------------------------------------------------
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

				return String.Join(";", ParsingUtils.SortList(vc.Filters.ToList()).ToArray());
			}
			else if (vc.FiltersWereApplied)
			{
				return VCFConstants.PASSES_FILTERS_v4;
			}
			else
			{
				return VCFConstants.UNFILTERED;
			}
		}

		

		private string formatQualValue(double qual)
		{
            //TODO: Format is bad right now.
			string s = String.Format(QUAL_FORMAT_STRING, qual);
			if (s.EndsWith(QUAL_FORMAT_EXTENSION_TO_TRIM))
			{
				s = s.Substring(0, s.Length - QUAL_FORMAT_EXTENSION_TO_TRIM.Length);
			}
			return s;
		}

		/// <summary>
		/// Create the info string; assumes that no values are null
		/// </summary>
		/// <param name="infoFields"> a map of info fields </param>
		/// <exception cref="IOException"> for writer </exception>
		private string getInfoString(IDictionary<string, string> infoFields)
		{
			if (infoFields.Count == 0)
			{
                return VCFConstants.EMPTY_INFO_FIELD;
			}
			bool isFirst = true;
            string toReturn = "";
			foreach (KeyValuePair<string, string> entry in infoFields)
			{
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					toReturn+=VCFConstants.INFO_FIELD_SEPARATOR;
				}

				string key = entry.Key;
				toReturn+=key;

				if (!entry.Value.Equals(""))
				{
					VCFInfoHeaderLine metaData = mHeader.getInfoHeaderLine(key);
					if (metaData == null || metaData.CountType != VCFHeaderLineCount.INTEGER || metaData.Count != 0)
					{
						toReturn+="=";
						 toReturn+=entry.Value;
					}
				}
			}
            return toReturn;
		}

		/// <summary>
		/// add the genotype data
		/// </summary>
		/// <param name="vc">                     the variant context </param>
		/// <param name="genotypeFormatKeys">  Genotype formatting string </param>
		/// <param name="alleleMap">              alleles for this context </param>
		/// <exception cref="IOException"> for writer </exception>
		private string getGenotypeDataText(VariantContext vc, IDictionary<Allele, string> alleleMap, IList<string> genotypeFormatKeys)
		{
            StringBuilder sbn = new StringBuilder();
			int ploidy = vc.GetMaxPloidy(2);
			foreach (string sample in mHeader.GenotypeSampleNames)
			{
				sbn.Append(VCFConstants.FIELD_SEPARATOR);

				Genotype g = vc.GetGenotype(sample);
				if (g == null)
				{
					g = GenotypeBuilder.CreateMissing(sample, ploidy);
				}
				IList<string> attrs = new List<string>(genotypeFormatKeys.Count);
				foreach (string field in genotypeFormatKeys)
				{
					if (field.Equals(VCFConstants.GENOTYPE_KEY))
					{
						if (!g.Available)
						{
							throw new Exception("GTs cannot be missing for some samples if they are available for others in the record");
						}

						sbn.Append(getAlleleText(g.getAllele(0), alleleMap));
						for (int i = 1; i < g.Ploidy; i++)
						{
                            sbn.Append(g.Phased ? VCFConstants.PHASED : VCFConstants.UNPHASED);
							sbn.Append(getAlleleText(g.getAllele(i), alleleMap));
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
							IntGenotypeFieldAccessors.Accessor accessor = intGenotypeFieldAccessors.GetAccessor(field);
							if (accessor != null)
							{

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
								object val = g.HasExtendedAttribute(field) ? g.GetExtendedAttribute(field) : VCFConstants.MISSING_VALUE_v4;

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
						sbn.Append(VCFConstants.GENOTYPE_FIELD_SEPARATOR);
					}
					sbn.Append(attrs[i]);
				}
			}
            return sbn.ToString();
		}

		private bool isMissingValue(string s)
		{
			// we need to deal with the case that it's a list of missing values
			return (countOccurrences(VCFConstants.MISSING_VALUE_v4[0], s) + countOccurrences(',', s) == s.Length);
		}

		private string getAlleleText(Allele allele, IDictionary<Allele, string> alleleMap)
		{
			string encoding = alleleMap[allele];
			if (encoding == null)
			{
				throw new Exception("Allele " + allele + " is not an allele in the variant context");
			}
            return encoding;
		}

		/// <summary>
		/// Takes a double value and pretty prints it to a String for display
		/// 
		/// Large doubles => gets %.2f style formatting
		/// Doubles < 1 / 10 but > 1/100 </>=> get %.3f style formatting
		/// Double < 1/100 => %.3e formatting </summary>
		/// <param name="d">
		/// @return </param>

		
		private void fieldIsMissingFromHeaderError(VariantContext vc, string id, string field)
		{
			if (!allowMissingFieldsInHeader)
			{
				throw new Exception("Key " + id + " found in VariantContext field " + field + " at " + vc.Chr + ":" + vc.Start + " but this key isn't defined in the VCFHeader.  We require all VCFs to have" + " complete VCF headers by default.");
			}
		}
	}

}