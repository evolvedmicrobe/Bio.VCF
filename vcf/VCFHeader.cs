using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;

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

namespace Bio.VCF
{	
	/// <summary>
	/// This class is really a POS.  It allows duplicate entries in the metadata,
	/// stores header lines in lots of places, and all around f*cking sucks.
	/// 
	/// todo -- clean this POS up
    /// 
    /// TODO: Nigel comment, doesn't look so bad anymore, mostly the caching might be unnecessary
    /// See if it is useful to cache the different types of header lines in different lists, or better to 
    /// just query for each type and have one central collection of all header lines in mMetaData
	/// 
	/// @author aaron
	///         <p/>
	///         Class VCFHeader
	///         <p/>
	///         A class representing the VCF header
	/// </summary>
	public class VCFHeader
    {
        #region Static Members

        // the mandatory header fields
        public static readonly string[] HEADER_FIELDS = new string[]
		{
			"CHROM",
			"POS",
			"ID",
			"REF",
			"ALT",
			"QUAL",
			"FILTER",
			"INFO"
		};
        // the character string that indicates meta data
        public const string METADATA_INDICATOR = "##";

        // the header string indicator
        public const string HEADER_INDICATOR = "#";

        public const string SOURCE_KEY = "source";
        public const string REFERENCE_KEY = "reference";
        public const string CONTIG_KEY = "contig";
        public const string INTERVALS_KEY = "intervals";
        public const string EXCLUDE_INTERVALS_KEY = "excludeIntervals";
        public const string INTERVAL_MERGING_KEY = "interval_merging";
        public const string INTERVAL_SET_RULE_KEY = "interval_set_rule";
        public const string INTERVAL_PADDING_KEY = "interval_padding";
        #endregion

        // the associated meta data
		private readonly LinkedHashSet<VCFHeaderLine> mMetaData = new LinkedHashSet<VCFHeaderLine>();
		private readonly IDictionary<string, VCFInfoHeaderLine> mInfoMetaData = new OrderedGenericDictionary<string, VCFInfoHeaderLine>();
        private readonly IDictionary<string, VCFFormatHeaderLine> mFormatMetaData = new OrderedGenericDictionary<string, VCFFormatHeaderLine>();
        private readonly IDictionary<string, VCFFilterHeaderLine> mFilterMetaData = new OrderedGenericDictionary<string, VCFFilterHeaderLine>();
        private readonly IDictionary<string, VCFHeaderLine> mOtherMetaData = new OrderedGenericDictionary<string, VCFHeaderLine>();
		private readonly IList<VCFContigHeaderLine> contigMetaData = new List<VCFContigHeaderLine>();

		// were the input samples sorted originally (or are we sorting them)?
		private bool writeEngineHeaders = true;
		private bool writeCommandLine = true;

		/// <summary>
		/// Create an empty VCF header with no header lines and no samples
		/// </summary>
		public VCFHeader() : this(new HashSet<VCFHeaderLine>(), new HashSet<string>())
		{
            
            SamplesWereAlreadySorted= true;
            
		}

		/// <summary>
		/// create a VCF header, given a list of meta data and auxillary tags
		/// </summary>
		/// <param name="metaData">   the meta data associated with this header </param>
		public VCFHeader(IEnumerable<VCFHeaderLine> metaData)
		{
            GenotypeSampleNames = new List<string>();
			mMetaData.AddRange(metaData);
			loadVCFVersion();
			loadMetaDataMaps();
		}
		/// <summary>
		/// Creates a shallow copy of the meta data in VCF header toCopy
		/// </summary>
		/// <param name="toCopy"> </param>
		public VCFHeader(VCFHeader toCopy) : this(toCopy.mMetaData)
		{
		}
		/// <summary>
		/// create a VCF header, given a list of meta data and auxillary tags
		/// </summary>
		/// <param name="metaData">            the meta data associated with this header </param>
		/// <param name="genotypeSampleNames"> the sample names </param>
		public VCFHeader(ISet<VCFHeaderLine> metaData, ISet<string> genotypeSampleNames) : this(metaData, new List<string>(genotypeSampleNames))
		{
            
		}
		public VCFHeader(ISet<VCFHeaderLine> metaData, IList<string> genotypeSampleNames) : this(metaData)
		{
			if (genotypeSampleNames.Count != (new HashSet<string>(genotypeSampleNames)).Count)
			{
				throw new VCFParsingError("BUG: VCF header has duplicate sample names");
			}
            GenotypeSampleNames.AddRange(genotypeSampleNames);
			SamplesWereAlreadySorted= ParsingUtils.IsSorted(genotypeSampleNames);
			buildVCFReaderMaps(genotypeSampleNames);
		}

		/// <summary>
		/// Tell this VCF header to use pre-calculated sample name ordering and the
		/// sample name -> offset map.  This assumes that all VariantContext created
		/// using this header (i.e., read by the VCFCodec) will have genotypes
		/// occurring in the same order
		/// </summary>
		/// <param name="genotypeSampleNamesInAppearenceOrder"> genotype sample names, must iterator in order of appearence </param>
		private void buildVCFReaderMaps(ICollection<string> genotypeSampleNamesInAppearenceOrder)
		{
			SampleNamesInOrder = new List<string>(genotypeSampleNamesInAppearenceOrder);
			SampleNameToOffset = new Dictionary<string, int>(genotypeSampleNamesInAppearenceOrder.Count);
            int i = 0;
			foreach (String name in genotypeSampleNamesInAppearenceOrder)
			{
				SampleNameToOffset[name] = i++;
			}
			SampleNamesInOrder.Sort();
		}


		/// <summary>
		/// Adds a header line to the header metadata.
		/// </summary>
		/// <param name="headerLine"> Line to add to the existing metadata component. </param>
		private void addMetaDataLine(VCFHeaderLine headerLine)
		{
			mMetaData.Add(headerLine);
			loadMetaDataMaps();
		}

		/// <returns> all of the VCF header lines of the ##contig form in order, or an empty list if none were present </returns>
		public IList<VCFContigHeaderLine> ContigLines
		{
			get
			{
				return new ReadOnlyCollection<VCFContigHeaderLine>(contigMetaData);
			}
		}
		/// <returns> all of the VCF FILTER lines in their original file order, or an empty list if none were present </returns>
		public IList<VCFFilterHeaderLine> FilterLines
		{
			get
			{
                return new ReadOnlyCollection<VCFFilterHeaderLine>(mFilterMetaData.Values.ToList());
			}
		}

		/// <returns> all of the VCF FILTER lines in their original file order, or an empty list if none were present </returns>
		public List<IVCFIDHeaderLine> IDHeaderLines
		{
			get
			{
                return mMetaData.Where(x => x is IVCFIDHeaderLine).Select(y=>y as IVCFIDHeaderLine).ToList();
			}
		}

		/// <summary>
		/// check our metadata for a VCF version tag, and throw an exception if the version is out of date
		/// or the version is not present
        /// TODO: Should only be one format line
		/// </summary>
		public virtual void loadVCFVersion()
		{
			IList<VCFHeaderLine> toRemove = new List<VCFHeaderLine>();
			foreach (VCFHeaderLine line in mMetaData)
			{
				if (VCFHeaderVersion.IsFormatString(line.Key))
				{
					toRemove.Add(line);
				}
			}
			// remove old header lines for now,
			mMetaData.RemoveRange(toRemove);

		}

		/// <summary>
		/// load the format/info meta data maps (these are used for quick lookup by key name)
		/// </summary>
        private void loadMetaDataMaps()
        {
            foreach (VCFHeaderLine line in mMetaData)
            {
                if (line is VCFInfoHeaderLine)
                {
                    VCFInfoHeaderLine infoLine = (VCFInfoHeaderLine)line;
                    addMetaDataMapBinding(mInfoMetaData, infoLine);
                }
                else if (line is VCFFormatHeaderLine)
                {
                    VCFFormatHeaderLine formatLine = (VCFFormatHeaderLine)line;
                    addMetaDataMapBinding(mFormatMetaData, formatLine);
                }
                else if (line is VCFFilterHeaderLine)
                {
                    VCFFilterHeaderLine filterLine = (VCFFilterHeaderLine)line;
                    mFilterMetaData[filterLine.ID] = filterLine;
                }
                else if (line is VCFContigHeaderLine)
                {
                    contigMetaData.Add((VCFContigHeaderLine)line);
                }
                else
                {
                    mOtherMetaData[line.Key] = line;
                }
            }

            if (hasFormatLine(VCFConstants.GENOTYPE_LIKELIHOODS_KEY) && !hasFormatLine(VCFConstants.GENOTYPE_PL_KEY))
            {
				Console.WriteLine ("Warning now we want PL fields, not just GL fields");
                //throw new VCFParsingError("Found " + VCFConstants.GENOTYPE_LIKELIHOODS_KEY + " format, but no " + VCFConstants.GENOTYPE_PL_KEY + " field.  We now only manage PL fields internally.");
            }
        }
		

		/// <summary>
		/// Add line to map, issuing warnings about duplicates
		/// </summary>
		/// <param name="map"> </param>
		/// <param name="line"> </param>
		private void addMetaDataMapBinding<T>(IDictionary<string, T> map, T line) where T : VCFCompoundHeaderLine
		{
			string key = line.ID;
			if (map.ContainsKey(key))
			{
				throw new VCFParsingError("Found duplicate VCF header lines for " + key );
				
			}
			else
			{
				map[key] = line;
			}
		}

		/// <summary>
		/// get the header fields in order they're presented in the input file (which is now required to be
		/// the order presented in the spec).
        /// 
        /// TODO: Phase this out
		/// </summary>
		/// <returns> a set of the header fields, in order </returns>
        [Obsolete]
        public virtual ISet<string> HeaderFields
        {
            get
            {
                var q=new LinkedHashSet<string>();
                q.AddRange(HEADER_FIELDS);
                return q;
            }
        }

		/// <summary>
		/// get the meta data, associated with this header, in sorted order
		/// </summary>
		/// <returns> a set of the meta data </returns>
		public virtual ReadOnlyCollection<VCFHeaderLine> MetaDataInInputOrder
		{
			get
			{
				return makeGetMetaDataSet(mMetaData);
			}
		}

		public ReadOnlyCollection<VCFHeaderLine> MetaDataInSortedOrder
		{
			get
			{
				return makeGetMetaDataSet(new SortedSet<VCFHeaderLine>(mMetaData));
			}
		}

		private static ReadOnlyCollection<VCFHeaderLine> makeGetMetaDataSet(ISet<VCFHeaderLine> headerLinesInSomeOrder)
		{            
			List<VCFHeaderLine> lines = new List<VCFHeaderLine>();
			lines.Add(new VCFHeaderLine(VCFHeaderVersion.VCF4_1.FormatString, VCFHeaderVersion.VCF4_1.VersionString));
            lines.AddRange(headerLinesInSomeOrder);
			ReadOnlyCollection<VCFHeaderLine> toReturn = new ReadOnlyCollection<VCFHeaderLine>(lines);
            return toReturn;
		}

		/// <summary>
		/// Get the VCFHeaderLine whose key equals key.  Returns null if no such line exists </summary>
		/// <param name="key">
		/// @return </param>
		public virtual VCFHeaderLine getMetaDataLine(string key)
		{
			foreach (VCFHeaderLine line in mMetaData)
			{
				if (line.Key.Equals(key))
				{
					return line;
				}
			}

			return null;
		}

		/// <summary>
		/// get the genotyping sample names, 
		// the list of auxillary tags
		/// </summary>
		/// <returns> a list of the genotype column names, which may be empty if hasGenotypingData() returns false </returns>
		public List<string> GenotypeSampleNames
		{
            get;
            private set;
		}

		public int NGenotypeSamples
		{
			get
			{
				return GenotypeSampleNames.Count;
			}
		}

		/// <summary>
		/// do we have genotyping data?
		/// </summary>
		/// <returns> true if we have genotyping columns, false otherwise </returns>
		public bool hasGenotypingData()
		{
			return NGenotypeSamples > 0;
		}

		/// <summary>
		/// were the input samples sorted originally?
		/// </summary>
		/// <returns> true if the input samples were sorted originally, false otherwise </returns>
        public bool SamplesWereAlreadySorted
        {
            get;
            private set;
        }

		/// <returns> the column count </returns>
		public virtual int ColumnCount
		{
			get
			{
				return HEADER_FIELDS.Length + (hasGenotypingData() ? GenotypeSampleNames.Count + 1 : 0);//add one for the FORMAT column
			}
		}

		/// <summary>
		/// Returns the INFO HeaderLines in their original ordering
		/// </summary>
		public ICollection<VCFInfoHeaderLine> InfoHeaderLines
		{
			get
			{
				return mInfoMetaData.Values;
			}
		}

		/// <summary>
		/// Returns the FORMAT HeaderLines in their original ordering
		/// </summary>
		public ICollection<VCFFormatHeaderLine> FormatHeaderLines
		{
			get
			{
				return mFormatMetaData.Values;
			}
		}

		/// <param name="id"> the header key name </param>
		/// <returns> the meta data line, or null if there is none </returns>
		public VCFInfoHeaderLine getInfoHeaderLine(string id)
		{
            if (mInfoMetaData.ContainsKey(id))
                return mInfoMetaData[id];
            else
                return null;
		}

		/// <param name="id">the header key name </param>
		/// <returns> the meta data line, or null if there is none </returns>
		public VCFFormatHeaderLine getFormatHeaderLine(string id)
		{
            return mFormatMetaData.ContainsKey(id) ? mFormatMetaData[id] : null;

		}

		/// <param name="id">the header key name </param>
		/// <returns> the meta data line, or null if there is none </returns>
		public virtual VCFFilterHeaderLine getFilterHeaderLine(string id)
		{
			return mFilterMetaData[id];
		}

		public bool hasInfoLine(string id)
		{
			return getInfoHeaderLine(id) != null;
		}
		public bool hasFormatLine(string id)
		{
			return getFormatHeaderLine(id) != null;
		}

		public virtual bool hasFilterLine(string id)
		{
			return getFilterHeaderLine(id) != null;
		}

		/// <param name="key">the header key name </param>
		/// <returns> the meta data line, or null if there is none </returns>
		public virtual VCFHeaderLine getOtherHeaderLine(string key)
		{
			return mOtherMetaData[key];
		}

		/// <summary>
		/// If true additional engine headers will be written to the VCF, otherwise only the walker headers will be output. </summary>
		/// <returns> true if additional engine headers will be written to the VCF </returns>
		public virtual bool WriteEngineHeaders
		{
			get
			{
				return writeEngineHeaders;
			}
			set
			{
				this.writeEngineHeaders = value;
			}
		}


		/// <summary>
		/// If true, and isWriteEngineHeaders also returns true, the command line will be written to the VCF. </summary>
		/// <returns> true if the command line will be written to the VCF </returns>
		public bool WriteCommandLine
		{
			get
			{
				return writeCommandLine;
			}
			set
			{
				this.writeCommandLine = value;
			}
		}

        // cache for efficient conversion of VCF -> VariantContext
        public List<string> SampleNamesInOrder
        {
            private set;
            get;
        }

        public Dictionary<string, int> SampleNameToOffset
        {
            private set;
            get;
        }

		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append("[VCFHeader:");
			foreach (VCFHeaderLine line in mMetaData)
			{
				b.Append("\n\t").Append(line);
			}
			return b.Append("\n]").ToString();
		}
	}

}