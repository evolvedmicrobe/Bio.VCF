using System;
using System.Collections.Generic;

namespace Bio.VCF
{

	/// <summary>
	/// Manages header lines for standard VCF INFO and FORMAT fields
	/// 
	/// Provides simple mechanisms for registering standard lines,
	/// looking them up, and adding them to headers
	/// 
	/// @author Mark DePristo/Nigel Delaney
	/// </summary>
	public class VCFStandardHeaderLines
	{
		/// <summary>
		/// Enabling this causes us to repair header lines even if only their descriptions differ
		/// </summary>
		private const bool REPAIR_BAD_DESCRIPTIONS = false;
		private static Standards<VCFFormatHeaderLine> formatStandards = new Standards<VCFFormatHeaderLine>();
		private static Standards<VCFInfoHeaderLine> infoStandards = new Standards<VCFInfoHeaderLine>();

		/// <summary>
		/// Walks over the VCF header and repairs the standard VCF header lines in it, returning a freshly
		/// allocated VCFHeader with standard VCF header lines repaired as necessary
		/// </summary>
		/// <param name="header">
		/// @return </param>
    	public static VCFHeader repairStandardHeaderLines(VCFHeader header)
		{
			ISet<VCFHeaderLine> newLines = new LinkedHashSet<VCFHeaderLine>();
			foreach (VCFHeaderLine line in header.MetaDataInInputOrder)
			{
                VCFHeaderLine cur = line;
				if (line is VCFFormatHeaderLine)
				{
					cur = formatStandards.repair((VCFFormatHeaderLine) line);
				}
				else if (line is VCFInfoHeaderLine)
				{
					cur = infoStandards.repair((VCFInfoHeaderLine) line);
				}

				newLines.Add(cur);
			}

			return new VCFHeader(newLines, header.GenotypeSampleNames);
		}

		/// <summary>
		/// Adds header lines for each of the format fields in IDs to header, returning the set of
		/// IDs without standard descriptions, unless throwErrorForMissing is true, in which
		/// case this situation results in a TribbleException
		/// </summary>
		/// <param name="IDs">
		/// @return </param>
		public static ISet<string> addStandardFormatLines(ISet<VCFHeaderLine> headerLines, bool throwErrorForMissing, ICollection<string> IDs)
		{
			return formatStandards.addToHeader(headerLines, IDs, throwErrorForMissing);
		}

		/// <seealso cref= #addStandardFormatLines(java.util.Set, boolean, java.util.Collection)
		/// </seealso>
		/// <param name="headerLines"> </param>
		/// <param name="throwErrorForMissing"> </param>
		/// <param name="IDs">
		/// @return </param>
		public static ISet<string> addStandardFormatLines(ISet<VCFHeaderLine> headerLines, bool throwErrorForMissing, params string[] IDs)
		{
			return addStandardFormatLines(headerLines, throwErrorForMissing, IDs);
		}

		/// <summary>
		/// Returns the standard format line for ID.  If none exists, return null or throw an exception, depending
		/// on throwErrorForMissing
		/// </summary>
		/// <param name="ID"> </param>
		/// <param name="throwErrorForMissing">
		/// @return </param>
		public static VCFFormatHeaderLine getFormatLine(string ID, bool throwErrorForMissing)
		{
			return formatStandards.get(ID, throwErrorForMissing);
		}

		/// <summary>
		/// Returns the standard format line for ID.  If none exists throw an exception
		/// </summary>
		/// <param name="ID">
		/// @return </param>
		public static VCFFormatHeaderLine getFormatLine(string ID)
		{
			return formatStandards.get(ID, true);
		}
		private static void registerStandard(VCFFormatHeaderLine line)
		{
			formatStandards.add(line);
		}

		/// <summary>
		/// Adds header lines for each of the info fields in IDs to header, returning the set of
		/// IDs without standard descriptions, unless throwErrorForMissing is true, in which
		/// case this situation results in a TribbleException
		/// </summary>
		/// <param name="IDs">
		public static ISet<string> addStandardInfoLines(ISet<VCFHeaderLine> headerLines, bool throwErrorForMissing, ICollection<string> IDs)
		{
			return infoStandards.addToHeader(headerLines, IDs, throwErrorForMissing);
		}

		/// <seealso cref= #addStandardFormatLines(java.util.Set, boolean, java.util.Collection)
		/// </seealso>
		/// <param name="IDs">
		/// @return </param>
		public static ISet<string> addStandardInfoLines(ISet<VCFHeaderLine> headerLines, bool throwErrorForMissing, params string[] IDs)
		{
			return addStandardInfoLines(headerLines, throwErrorForMissing, IDs);
		}

		/// <summary>
		/// Returns the standard info line for ID.  If none exists, return null or throw an exception, depending
		/// on throwErrorForMissing
		/// </summary>
		/// <param name="ID"> </param>
		/// <param name="throwErrorForMissing">
		/// @return </param>
		public static VCFInfoHeaderLine getInfoLine(string ID, bool throwErrorForMissing)
		{
			return infoStandards.get(ID, throwErrorForMissing);
		}

		/// <summary>
		/// Returns the standard info line for ID.  If none exists throw an exception
		/// </summary>
		/// <param name="ID">
		/// @return </param>
		public static VCFInfoHeaderLine getInfoLine(string ID)
		{
			return getInfoLine(ID, true);
		}

		private static void registerStandard(VCFInfoHeaderLine line)
		{
			infoStandards.add(line);
		}


		//
		// VCF header line constants
		//
		static VCFStandardHeaderLines()
		{
			// FORMAT lines
			registerStandard(new VCFFormatHeaderLine(VCFConstants.GENOTYPE_KEY, 1, VCFHeaderLineType.String, "Genotype"));
			registerStandard(new VCFFormatHeaderLine(VCFConstants.GENOTYPE_QUALITY_KEY, 1, VCFHeaderLineType.Integer, "Genotype Quality"));
			registerStandard(new VCFFormatHeaderLine(VCFConstants.DEPTH_KEY, 1, VCFHeaderLineType.Integer, "Approximate read depth (reads with MQ=255 or with bad mates are filtered)"));
			registerStandard(new VCFFormatHeaderLine(VCFConstants.GENOTYPE_PL_KEY, VCFHeaderLineCount.G, VCFHeaderLineType.Integer, "Normalized, Phred-scaled likelihoods for genotypes as defined in the VCF specification"));
			registerStandard(new VCFFormatHeaderLine(VCFConstants.GENOTYPE_ALLELE_DEPTHS, VCFHeaderLineCount.UNBOUNDED, VCFHeaderLineType.Integer, "Allelic depths for the ref and alt alleles in the order listed"));
			registerStandard(new VCFFormatHeaderLine(VCFConstants.GENOTYPE_FILTER_KEY, 1, VCFHeaderLineType.String, "Genotype-level filter"));

			// INFO lines
			registerStandard(new VCFInfoHeaderLine(VCFConstants.END_KEY, 1, VCFHeaderLineType.Integer, "Stop position of the interval"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.MLE_ALLELE_COUNT_KEY, VCFHeaderLineCount.A, VCFHeaderLineType.Integer, "Maximum likelihood expectation (MLE) for the allele counts (not necessarily the same as the AC), for each ALT allele, in the same order as listed"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.MLE_ALLELE_FREQUENCY_KEY, VCFHeaderLineCount.A, VCFHeaderLineType.Float, "Maximum likelihood expectation (MLE) for the allele frequency (not necessarily the same as the AF), for each ALT allele, in the same order as listed"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.DOWNSAMPLED_KEY, 0, VCFHeaderLineType.Flag, "Were any of the samples downsampled?"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.DBSNP_KEY, 0, VCFHeaderLineType.Flag, "dbSNP Membership"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.DEPTH_KEY, 1, VCFHeaderLineType.Integer, "Approximate read depth; some reads may have been filtered"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.STRAND_BIAS_KEY, 1, VCFHeaderLineType.Float, "Strand Bias"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.ALLELE_FREQUENCY_KEY, VCFHeaderLineCount.A, VCFHeaderLineType.Float, "Allele Frequency, for each ALT allele, in the same order as listed"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.ALLELE_COUNT_KEY, VCFHeaderLineCount.A, VCFHeaderLineType.Integer, "Allele count in genotypes, for each ALT allele, in the same order as listed"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.ALLELE_NUMBER_KEY, 1, VCFHeaderLineType.Integer, "Total number of alleles in called genotypes"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.MAPPING_QUALITY_ZERO_KEY, 1, VCFHeaderLineType.Integer, "Total Mapping Quality Zero Reads"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.RMS_MAPPING_QUALITY_KEY, 1, VCFHeaderLineType.Float, "RMS Mapping Quality"));
			registerStandard(new VCFInfoHeaderLine(VCFConstants.SOMATIC_KEY, 0, VCFHeaderLineType.Flag, "Somatic event"));
		}

		private class Standards<T> where T : VCFCompoundHeaderLine
		{
			private readonly IDictionary<string, T> standards = new Dictionary<string, T>();
            //TODO: Have this method actually repair instead of throwing an error.
			public T repair(T line)
			{
				T standard = get(line.ID, false);
				if (standard != null)
				{
					bool badCountType = line.CountType != standard.CountType;
					bool badCount = line.FixedCount && !badCountType && line.Count != standard.Count;
					bool badType = line.Type != standard.Type;
					bool badDesc = !line.Description.Equals(standard.Description);
					bool needsRepair = badCountType || badCount || badType || (REPAIR_BAD_DESCRIPTIONS && badDesc);

                    //TODO: This is ridiculous, shouldn't the fields be standardized or not??
                    if (needsRepair)
                    {
                        if (GeneralUtils.DEBUG_MODE_ENABLED)
                        {
                            //TODO: This originally threw an error, probably should do the same here....
                            throw new VCFParsingError("Repairing standard header line for field " + line.ID + " because" + (badCountType ? " -- count types disagree; header has " + line.CountType + " but standard is " + standard.CountType : "") + (badType ? " -- type disagree; header has " + line.Type + " but standard is " + standard.Type : "") + (badCount ? " -- counts disagree; header has " + line.Count + " but standard is " + standard.Count : "") + (badDesc ? " -- descriptions disagree; header has '" + line.Description + "' but standard is '" + standard.Description + "'" : ""));
                        }
                        else
                            return standard;
                    }
				}
				return line;				
			}
			public ISet<string> addToHeader(ISet<VCFHeaderLine> headerLines, ICollection<string> IDs, bool throwErrorForMissing)
			{
				ISet<string> missing = new HashSet<string>();
				foreach (String ID in IDs)
				{
					T line = get(ID, throwErrorForMissing);
					if (line == null)
					{
						missing.Add(ID);
					}
					else
					{
						headerLines.Add(line);
					}
				}

				return missing;
			}

			public void add(T line)
			{
				if (standards.ContainsKey(line.ID))
				{
					throw new VCFParsingError("Attempting to add multiple standard header lines for ID " + line.ID);
				}
				standards[line.ID] = line;
			}

			public T get(string ID, bool throwErrorForMissing)
			{
                T val;
                bool HasVal = standards.TryGetValue(ID, out val);
                if (throwErrorForMissing && !HasVal)
				{
					throw new VCFParsingError("Couldn't find a standard VCF header line for field " + ID);
				}
                return HasVal ? val : null;
			}
		}
	}

}