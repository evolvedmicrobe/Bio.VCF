using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System.IO.Compression;
using Bio.VCF.NewCollections;

namespace Bio.VCF
{
    //A note on bgzf compressed input streams.  This seems to be a rare case, but according to the 
    //sam tools documentation bgzf can be decompressed like gzip files directly.  As such there should
    //be no need for an additional decompressing stream, but will need to test this assumption with 
    //later tests, as all things *nix seem to suffer from cryptic binary incompatibilities.
	//using BlockCompressedInputStream = net.sf.samtools.util.BlockCompressedInputStream;

	public abstract class AbstractVCFCodec 
	{       
		public static readonly int MAX_ALLELE_SIZE_BEFORE_WARNING = (int)Math.Pow(2, 20);
        //TODO: This seems like it should be replaced with HEADERFIELDS.Length
		protected internal const int NUM_STANDARD_FIELDS = 8; // INFO is the 8th column
		// we have to store the list of strings that make up the header until they're needed
		protected internal VCFHeader header = null;
		protected internal VCFHeaderVersion version = null;
		// a mapping of the allele
		protected internal Dictionary<string, List<Allele>> alleleMap = new Dictionary<string, List<Allele>>(3);

        //NOTE: In the original java version the code did not reallocate arrays which was supposed to be a performance benefit
        //however, it looks like virtually all of these will be Gen-0 collections and so should be exceptionally fast allocate/deallocate
        //so am demoting them.  Also, promoting these local variables to the class level seems like a recipe for bugs...
		// for ParsingUtils.split
		//protected internal string[] GTValueArray = new string[100];
		//protected internal string[] genotypeKeyArray = new string[100];
		//protected internal string[] infoFieldArray = new string[1000];
		//protected internal string[] infoValueArray = new string[1000];

		// for performance testing purposes
		public static bool validate = true;

		// a key optimization -- we need a per thread string parts array, so we don't allocate a big array over and over
		// todo: make this thread safe?
		//string[] parts = null;
		string[] genotypeParts = null;
        
        //AVOIDING OPTIMIZATION IN .NET, UNREFERENCED GENERATION 0 OBJECT SHOULD BE ALLOCATED AND COLLECTED QUICKLY
        protected internal readonly string[] locParts = new string[6];

		/// <summary>
        ///For performance we cache the hashmap of filter encodings for quick lookup, the lists in it are read only 
		/// </summary>
        protected ListAndSetHash filterHash;

		// we store a name to give to each of the variant contexts we emit
		protected internal string name = "Unknown";

		protected internal int lineNo = 0;

		//protected internal IDictionary<string, string> stringCache = new Dictionary<string, string>();

		protected internal bool warnedAboutNoEqualsForNonFlag = false;

        //protected string Name = "Name Not Set";

        /// <summary>
        /// Classes representing types that can be parsed by the codec
        /// </summary>
        protected List<VCFHeaderVersion> AcceptableVersions;

		/// <summary>
		/// If true, then we'll magically fix up VCF headers on the fly when we read them in
		/// </summary>
		protected internal bool doOnTheFlyModifications = true;

		protected internal AbstractVCFCodec(string name,params VCFHeaderVersion[] acceptableVersions)
		{
            Name = name;
            AcceptableVersions = new List<VCFHeaderVersion>();
            AcceptableVersions.AddRange(acceptableVersions);
            filterHash=new ListAndSetHash(this);
		}  
		/// <param name="reader"> the line reader to take header lines from </param>
		/// <returns> the number of header lines </returns>        
        public virtual VCFHeader readHeader(StreamReader reader)
        {
            IList<string> headerStrings = new List<string>();
            string line;
            try
            {
                bool foundHeaderVersion = false;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNo++;
                    if (line.StartsWith(VCFHeader.METADATA_INDICATOR))
                    {
                        string[] lineFields = line.Substring(2).Split('=');
                        if (lineFields.Length == 2 && VCFHeaderVersion.IsFormatString(lineFields[0]))
                        {
                            if (!VCFHeaderVersion.IsVersionString(lineFields[1]))
                            {throw new VCFParsingError("Header: " + lineFields[1] + " is not a supported version");}
                            foundHeaderVersion = true;
                            version = VCFHeaderVersion.ToHeaderVersion(lineFields[1]);
                            if(!this.AcceptableVersions.Contains(version))
                            {throw new VCFParsingError("This codec is strictly for "+Name+"; please use a different codec for " + lineFields[1]);}                            
                        }
                        headerStrings.Add(line);
                    }
                    else if (line.StartsWith(VCFHeader.HEADER_INDICATOR))//should be only one such line
                    {
                        if (!foundHeaderVersion)
                        {throw new VCFParsingError("We never saw a header line specifying VCF version");}
                        headerStrings.Add(line);
                        return parseHeaderFromLines(headerStrings, version);
                    }
                    else
                    {throw new VCFParsingError("We never saw the required CHROM header line (starting with one #) for the input VCF file");}
                }
            }
            catch (IOException e)
            {
                throw new Exception("IO Exception ", e);
            }
            throw new VCFParsingError("We never saw the required CHROM header line (starting with one #) for the input VCF file");		
        }        
		/// <summary>
		/// parse the filter string, first checking to see if we already have parsed it in a previous attempt </summary>
		/// <param name="filterString"> the string to parse </param>
		/// <returns> a set of the filters applied </returns>
		protected internal abstract void validateFilters(string filterString);
		/// <summary>
		/// create a VCF header from a set of header record lines
		/// </summary>
		/// <param name="headerStrings"> a list of strings that represent all the ## and # entries </param>
		/// <returns> a VCFHeader object </returns>
		protected internal virtual VCFHeader parseHeaderFromLines(IList<string> headerStrings, VCFHeaderVersion version)
		{
			this.version = version;
			ISet<VCFHeaderLine> metaData = new LinkedHashSet<VCFHeaderLine>();
			ISet<string> sampleNames = new LinkedHashSet<string>();
			int contigCounter = 0;
			// iterate over all the passed in strings
			foreach (string str in headerStrings)
			{
                if (!str.StartsWith(VCFHeader.METADATA_INDICATOR))//presumably the #CHROM POS ID REF ALT QUAL FILTER INFO   etc. line
				{
					string[] strings = str.Substring(1).Split(VCFConstants.FIELD_SEPARATOR_CHAR);
					//check for null last string, grrr...
					if (String.IsNullOrEmpty (strings.Last ())) {
						strings = strings.Take (strings.Length - 1).ToArray ();
					}
					if (strings.Length <VCFHeader.HEADER_FIELDS.Length)
					{
						throw new VCFParsingError("There are not enough columns present in the header line: " + str);
					}
					//Verify Arrays
                    var misMatchedColumns=Enumerable.Range(0, VCFHeader.HEADER_FIELDS.Length).Where(x => VCFHeader.HEADER_FIELDS[x] != strings[x]).Select(x=>strings[x]).ToArray();
                    if (misMatchedColumns.Length > 0)
                    {
                        throw new VCFParsingError("We were not expecting column name '" + misMatchedColumns[0]+ " in that position");
                    }
                    int arrayIndex = VCFHeader.HEADER_FIELDS.Length;//start after verified columns
                    bool sawFormatTag = false;
					if (arrayIndex < strings.Length)
					{
						if (!strings[arrayIndex].Equals("FORMAT"))
						{
							throw new VCFParsingError("we were expecting column name 'FORMAT' but we saw '" + strings[arrayIndex] + "'");
						}
						sawFormatTag = true;
						arrayIndex++;
					}
					while (arrayIndex < strings.Length)
					{
						sampleNames.Add(strings[arrayIndex++]);
					}
					if (sawFormatTag && sampleNames.Count == 0)
					{
						throw new VCFParsingError("The FORMAT field was provided but there is no genotype/sample data");
					}

				}
				else
				{
					if (str.StartsWith(VCFConstants.INFO_HEADER_START))
					{
						VCFInfoHeaderLine info = new VCFInfoHeaderLine(str.Substring(7), version);
						metaData.Add(info);
					}
					else if (str.StartsWith(VCFConstants.FILTER_HEADER_START))
					{
						VCFFilterHeaderLine filter = new VCFFilterHeaderLine(str.Substring(9), version);
						metaData.Add(filter);
					}
					else if (str.StartsWith(VCFConstants.FORMAT_HEADER_START))
					{
						VCFFormatHeaderLine format = new VCFFormatHeaderLine(str.Substring(9), version);
						metaData.Add(format);
					}
					else if (str.StartsWith(VCFConstants.CONTIG_HEADER_START))
					{
						VCFContigHeaderLine contig = new VCFContigHeaderLine(str.Substring(9), version, VCFConstants.CONTIG_HEADER_START.Substring(2), contigCounter++);
						metaData.Add(contig);
					}
					else if (str.StartsWith(VCFConstants.ALT_HEADER_START))
					{
                        //TODO: Consider giving Alt header lines their own class
						VCFSimpleHeaderLine alt = new VCFSimpleHeaderLine(str.Substring(6), version, VCFConstants.ALT_HEADER_START.Substring(2), "ID", "Description");
						metaData.Add(alt);
					}
					else
					{
						int equals = str.IndexOf("=");
						if (equals != -1)
						{
							metaData.Add(new VCFHeaderLine(str.Substring(2, equals - 2), str.Substring(equals + 1)));
						}
					}
				}
			}
			this.header = new VCFHeader(metaData, sampleNames);
			if (doOnTheFlyModifications)
			{
				this.header = VCFStandardHeaderLines.repairStandardHeaderLines(this.header);
			}
			return this.header;
		}
		/// <summary>
		/// the fast decode function </summary>
		/// <param name="line"> the line of text for the record </param>
		/// <returns> a feature, (not guaranteed complete) that has the correct start and stop </returns>
        public VariantContext decodeLoc(string line)
		{
			return decodeLine(line, false);
		}
		/// <summary>
		/// decode the line into a feature (VariantContext) </summary>
		/// <param name="line"> the line </param>
		/// <returns> a VariantContext </returns>
		public VariantContext decode(string line)
		{
			return decodeLine(line, true);
		}
		private VariantContext decodeLine(string line, bool includeGenotypes)
		{
			// the same line reader is not used for parsing the header and parsing lines, if we see a #, we've seen a header line
			if (line.StartsWith(VCFHeader.HEADER_INDICATOR))
			{
                //TODO: Possibly raise exception?  At least in one scenario, the VCF header has already been parsed before this is called
                //seems like this should always be true based on statement below
                throw new VCFParsingError("While decoding genotype lines came across a commented header line.  Problem is with line:\n " + line);
			}
			// our header cannot be null, we need the genotype sample names and counts
			if (header == null)
			{
				throw new VCFParsingError("VCF Header cannot be null when decoding a record");
			}

            //I think this is not necessary
            int parseSize=Math.Min(header.ColumnCount,NUM_STANDARD_FIELDS+1);

            //TODO: Original bit of code here could do lazy genotype initalization and so could split off the 
            //first 8 columns, leaving any genotype data still lumped together in the 9th column (if present).
           // string[] parts=line.Split(VCFConstants.FIELD_SEPARATOR_CHAR_AS_ARRAY,parseSize,StringSplitOptions.None);
            string[] parts = FastStringUtils.Split(line, VCFConstants.FIELD_SEPARATOR_CHAR, parseSize, StringSplitOptions.None);
                
            
            //ND - Modified this heavily, as it is imposssible for header to be null at this stage.
			// if we have don't have a header, or we have a header with no genotyping data check that we have eight columns.
            // Otherwise check that we have nine (normal colummns + genotyping data)
			if ((!header.hasGenotypingData() && parts.Length != NUM_STANDARD_FIELDS) ||
                (header.hasGenotypingData() && parts.Length != (NUM_STANDARD_FIELDS + 1)))
			{
				throw new VCFParsingError("Line " + lineNo + ": there aren't enough columns for line " + line + " (we expected " + (header == null ? NUM_STANDARD_FIELDS : NUM_STANDARD_FIELDS + 1) + " tokens, and saw " + parts.Length + " )");
			}
			return parseVCFLine(parts, includeGenotypes);
		}
		/// <summary>
		/// Parses a line from a VCF File
		/// </summary>
		/// <param name="parts">An array of length >8 where the 9th element contains unsplit genotype data (if present)</param>
		/// <param name="includeGenotypes"> Whether or not to also parse the genotype data </param>
		/// <returns></returns>
		private VariantContext parseVCFLine(string[] parts, bool includeGenotypes)
		{
			VariantContextBuilder builder = new VariantContextBuilder();
			builder.Source=Name;
			// increment the line count
			lineNo++;
			// parse out the required fields
			string chr = GetCachedString(parts[0]);
			builder.Contig=chr;
			int pos = -1;
			try
			{
				pos = Convert.ToInt32(parts[1]);
			}
			catch (FormatException e)
			{
				generateException(parts[1] + " is not a valid start position in the VCF format");
			}
			builder.Start=pos;
			if (parts[2].Length == 0)
			{
				generateException("The VCF specification requires a valid ID field");
			}
			else if (parts[2].Equals(VCFConstants.EMPTY_ID_FIELD))
			{
                builder.ID= VCFConstants.EMPTY_ID_FIELD;
			}
			else
			{
				builder.ID=parts[2];
			}
			string refe = GetCachedString(parts[3].ToUpper());
			string alts = GetCachedString(parts[4].ToUpper());
			builder.Log10PError=parseQual(parts[5]);

            string filterStr = GetCachedString(parts[6]);
            var filters = filterHash[filterStr];
            if (filters != null)//means filter data present
			{	
                builder.SetFilters(filters.Hash);
			}

			IDictionary<string, object> attrs = parseInfo(parts[7]);
			builder.Attributes=attrs;

			if (attrs.ContainsKey(VCFConstants.END_KEY))
			{
				// update stop with the end key if provided
				try
				{
					builder.Stop=Convert.ToInt32(attrs[VCFConstants.END_KEY].ToString());
				}
				catch (Exception e)
				{
					generateException("the END value in the INFO field is not valid");
				}
			}
			else
			{
				builder.Stop=(pos + refe.Length - 1);
			}

			// get our alleles, filters, and setup an attribute map
			IList<Allele> alleles = parseAlleles(refe, alts, lineNo);
			builder.SetAlleles(alleles);

			// do we have genotyping data
			if (parts.Length > NUM_STANDARD_FIELDS && includeGenotypes)
			{
				int nGenotypes = header.NGenotypeSamples;
				LazyGenotypesContext lazy = new LazyGenotypesContext(this,alleles,chr,pos,parts[8], nGenotypes);
                // did we resort the sample names?  If so, we need to load the genotype data
				if (!header.SamplesWereAlreadySorted)
				{
					lazy.Decode();
				}
				builder.SetGenotypes(lazy,false);
			}

			VariantContext vc=null;
			try
			{
				vc = builder.make();
			}
			catch (Exception e)
			{
				generateException(e.Message);
			}
			return vc;
		}
		/// <summary>
		/// get the name of this codec </summary>
		/// <returns> our set name </returns>
		public string Name
		{
			get
			{return name;}
			set
			{this.name = value;}
		}
		/// <summary>
		/// Return a cached copy of the supplied string. Used to avoid allocating multiple strings for
        /// the same chromosome, etc.
        /// 
        /// NOTE: This currently uses the frameworks String.Intern method to ensure that only
        /// one copy of the string literal is kept around.  However, the String.Intern method 
        /// allocates memory that is never GCed until the process determinates.  If this becomes
        /// problematic, may switch to using a local dictionary (which for some reason was used in 
        /// the original java version of the code).  For now using the frameworks version though.
		/// </summary>
		/// <param name="str"> string </param>
		/// <returns> interned string </returns>
		protected internal string GetCachedString(string str)
		{
            return String.Intern(str);
		}
		/// <summary>
		/// parse out the info fields into a dictionary of key/values </summary>
		/// <param name="infoField"> the fields </param>
		/// <returns> a mapping of keys to objects </returns>
		private IDictionary<string, object> parseInfo(string infoField)
		{
			IDictionary<string, object> attributes = new Dictionary<string, object>();

			if (infoField.Length == 0)
			{
				generateException("The VCF specification requires a valid info field");
			}
			if (!infoField.Equals(VCFConstants.EMPTY_INFO_FIELD))
			{
				if (infoField.IndexOf("\t") != -1 || infoField.IndexOf(" ") != -1)
				{
					generateException("The VCF specification does not allow for whitespace in the INFO field");
				}
                string[] infoFieldArray = infoField.Split(VCFConstants.INFO_FIELD_SEPARATOR_CHAR_AS_ARRAY);
				//int infoFieldSplitSize = ParsingUtils.Split(infoField, infoFieldArray, VCFConstants.INFO_FIELD_SEPARATOR_CHAR, false);
				for (int i = 0; i < infoFieldArray.Length; i++)
				{
					string key;
					object value;
					int eqI = infoFieldArray[i].IndexOf("=");
					if (eqI != -1)
					{
						key = infoFieldArray[i].Substring(0, eqI);
						string valueString = infoFieldArray[i].Substring(eqI + 1);

						// split on the INFO field separator
                        string[] infoValueArray = valueString.Split(VCFConstants.INFO_FIELD_ARRAY_SEPARATOR_CHAR);
						//int infoValueSplitSize = ParsingUtils.Split(valueString, infoValueArray, VCFConstants.INFO_FIELD_ARRAY_SEPARATOR_CHAR, false);
						if (infoValueArray.Length==1)// infoValueSplitSize == 1)
						{
							value = infoValueArray[0];
							VCFInfoHeaderLine headerLine = header.getInfoHeaderLine(key);
							if (headerLine != null && headerLine.Type == VCFHeaderLineType.Flag && value.Equals("0"))
							{
								// deal with the case where a flag field has =0, such as DB=0, by skipping the add
								continue;
							}
						}
						else
						{
							//List<string> valueList = new List<string>(infoValueArray);
							value = infoValueArray;
						}
					}
					else
					{
						key = infoFieldArray[i];
						VCFInfoHeaderLine headerLine = header.getInfoHeaderLine(key);
						if (headerLine != null && headerLine.Type != VCFHeaderLineType.Flag)
						{
                            //Note: This was originally a warning that was only thrown once
						    throw new VCFParsingError("Found info key " + key + " without a = value, but the header says the field is of type " + headerLine.Type + " but this construct is only value for FLAG type fields");
                            //Old version just set this value
                           // value = VCFConstants.MISSING_VALUE_v4;
                        }
						else
						{
							value = true;
						}
					}
					// this line ensures that key/value pairs that look like key=; are parsed correctly as MISSING
					if ("".Equals(value))
					{
						value = VCFConstants.MISSING_VALUE_v4;
					}
                    //TODO: We should have enough information in the header here to 
					attributes[key] = value;
				}
			}

			return attributes;
		}
		/// <summary>
		/// create a an allele from an index and an array of alleles </summary>
		/// <param name="index"> the index </param>
		/// <param name="alleles"> the alleles </param>
		/// <returns> an Allele </returns>
		protected internal static Allele oneAllele(string index, IList<Allele> alleles)
		{
			if (index.Equals(VCFConstants.EMPTY_ALLELE))
			{
				return Allele.NO_CALL;
			}
			int i;
			try
			{
				i = Convert.ToInt32(index);
			}
			catch (FormatException e)
			{
				throw new VCFParsingError("The following invalid GT allele index was encountered in the file: " + index);
			}
			if (i >= alleles.Count)
			{
				throw new VCFParsingError("The allele with index " + index + " is not defined in the REF/ALT columns in the record");
			}
			return alleles[i];
		}
		/// <summary>
		/// parse genotype alleles from the genotype string </summary>
		/// <param name="GT">         GT string </param>
		/// <param name="alleles">    list of possible alleles </param>
		/// <param name="cache">      cache of alleles for GT </param>
		/// <returns> the allele list for the GT string </returns>
		protected internal static List<Allele> parseGenotypeAlleles(string GT, IList<Allele> alleles, Dictionary<string, List<Allele>> cache)
		{
			// cache results [since they are immutable] and return a single object for each genotype
			List<Allele> GTAlleles;// = cache.GetValueOrDefault(GT);
            bool hasCache = cache.TryGetValue(GT, out GTAlleles);
            if (!hasCache)
			{
                //string[] sp = GT.Split(VCFConstants.PHASING_TOKENS_AS_CHAR_ARRAY);

                //GTAlleles = new List<Allele>(sp.Length);
                //for (int i = 0; i < sp.Length; i++)
                //{
                //    GTAlleles.Add(oneAllele(sp[i], alleles));
                //}
                GTAlleles = GT.Split(VCFConstants.PHASING_TOKENS_AS_CHAR_ARRAY).Select(x => oneAllele(x, alleles)).ToList();
				cache[GT] = GTAlleles;
			}
			return GTAlleles;
		}
		/// <summary>
		/// parse out the qual value </summary>
		/// <param name="qualString"> the quality string </param>
		/// <returns> return a double </returns>
		protected internal static double parseQual(string qualString)
		{
			// if we're the VCF 4 missing char, return immediately
			if (qualString.Equals(VCFConstants.MISSING_VALUE_v4))
			{
				return VariantContext.NO_LOG10_PERROR;
			}

			double val = Convert.ToDouble(qualString);

			// check to see if they encoded the missing qual score in VCF 3 style, with either the -1 or -1.0.  check for val < 0 to save some CPU cycles
			if ((val < 0) && (Math.Abs(val - VCFConstants.MISSING_QUALITY_v3_DOUBLE) < VCFConstants.VCF_ENCODING_EPSILON))
			{
				return VariantContext.NO_LOG10_PERROR;
			}

			// scale and return the value
			return val / -10.0;
		}
		/// <summary>
		/// parse out the alleles </summary>
		/// <param name="reference"> the reference base </param>
		/// <param name="alts"> a string of alternates to break into alleles </param>
		/// <param name="lineNo">  the line number for this record </param>
		/// <returns> a list of alleles, and a pair of the shortest and longest sequence </returns>
		protected internal static IList<Allele> parseAlleles(string reference, string alts, int lineNo)
		{
			IList<Allele> alleles = new List<Allele>(2); // we are almost always biallelic
			// ref
			checkAllele(reference, true, lineNo);
			Allele refAllele = Allele.Create(reference, true);
			alleles.Add(refAllele);

			if (alts.IndexOf(",") == -1) // only 1 alternatives, don't call string split
			{
				parseSingleAltAllele(alleles, alts, lineNo);
			}
			else
			{
                foreach (string alt in alts.Split(VCFConstants.COMMA_AS_CHAR_ARRAY, StringSplitOptions.RemoveEmptyEntries))
				{
					parseSingleAltAllele(alleles, alt, lineNo);
				}
			}
			return alleles;
		}
		/// <summary>
		/// check to make sure the allele is an acceptable allele </summary>
		/// <param name="allele"> the allele to check </param>
		/// <param name="isRef"> are we the reference allele? </param>
		/// <param name="lineNo">  the line number for this record </param>
		private static void checkAllele(string allele, bool isRef, int lineNo)
		{
			if (allele == null || allele.Length == 0)
			{
				generateException("Empty alleles are not permitted in VCF records", lineNo);
			}

			if (MAX_ALLELE_SIZE_BEFORE_WARNING != -1 && allele.Length > MAX_ALLELE_SIZE_BEFORE_WARNING)
			{
				throw new VCFParsingError(string.Format("Allele detected with length {0:D} exceeding max size {1:D} at approximately line {2:D}, likely resulting in degraded VCF processing performance", allele.Length, MAX_ALLELE_SIZE_BEFORE_WARNING, lineNo));
			}

			if (isSymbolicAllele(allele))
			{
				if (isRef)
				{
					generateException("Symbolic alleles not allowed as reference allele: " + allele, lineNo);
				}
			}
			else
			{
				// check for VCF3 insertions or deletions
				if ((allele[0] == VCFConstants.DELETION_ALLELE_v3) || (allele[0] == VCFConstants.INSERTION_ALLELE_v3))
				{
					generateException("Insertions/Deletions are not supported when reading 3.x VCF's. Please" + " convert your file to VCF4 using VCFTools, available at http://vcftools.sourceforge.net/index.html", lineNo);
				}

				if (!Allele.AcceptableAlleleBases(allele))
				{
					generateException("Unparsable vcf record with allele " + allele, lineNo);
				}

				if (isRef && allele.Equals(VCFConstants.EMPTY_ALLELE))
				{
					generateException("The reference allele cannot be missing", lineNo);
				}
			}
		}
		/// <summary>
		/// return true if this is a symbolic allele (e.g. <SOMETAG>) or
		/// structural variation breakend (with [ or ]), otherwise false </summary>
		/// <param name="allele"> the allele to check </param>
		/// <returns> true if the allele is a symbolic allele, otherwise false </returns>
		private static bool isSymbolicAllele(string allele)
		{
			return (allele != null && allele.Length > 2 && ((allele.StartsWith("<") && allele.EndsWith(">")) || (allele.Contains("[") || allele.Contains("]"))));
		}
		/// <summary>
		/// parse a single allele, given the allele list </summary>
		/// <param name="alleles"> the alleles available </param>
		/// <param name="alt"> the allele to parse </param>
		/// <param name="lineNo">  the line number for this record </param>
		private static void parseSingleAltAllele(IList<Allele> alleles, string alt, int lineNo)
		{
			checkAllele(alt, false, lineNo);

			Allele allele = Allele.Create(alt, false);
			if (!allele.NoCall)
			{
				alleles.Add(allele);
			}
		}
		public static bool canDecodeFile(string potentialInput, string MAGIC_HEADER_LINE)
		{
            try
			{
                if(isVCFStream(new FileStream(potentialInput,FileMode.Open), MAGIC_HEADER_LINE) )
                    return true;
                else
                {
                    bool isCompressed=false;
                    using(GZipStream stream=new GZipStream(new FileStream(potentialInput,FileMode.Open),CompressionMode.Decompress))
                   {
                        isCompressed=isVCFStream(stream,MAGIC_HEADER_LINE);
                    }
                    if(!isCompressed)
                    {
                        //Do we need to check for a BGZF file to?
                        //isVCFStream(new BlockCompressedInputStream(new FileInputStream(potentialInput)), MAGIC_HEADER_LINE);
                    }
                    return isCompressed;
                } 
			}
			catch //(Exception e)
			{
				return false;
			}
		}
		private static bool isVCFStream(Stream stream, string MAGIC_HEADER_LINE)
		{
			try
			{
				byte[] buff = new byte[MAGIC_HEADER_LINE.Length];
				stream.Read(buff, 0, MAGIC_HEADER_LINE.Length);
                return buff.SequenceEqual(VCFUtils.StringToBytes(MAGIC_HEADER_LINE));
	//            String firstLine = new String(buff);
	//            return firstLine.startsWith(MAGIC_HEADER_LINE);
			}
			catch// (Exception e)
			{
				return false;
			}
			finally
			{
				stream.Close();
			}

		}

        protected abstract IList<String> parseFilters(String filterString);

		/// <summary>
		/// Create a genotype map
		/// </summary>
		/// <param name="str"> the string </param>
		/// <param name="alleles"> the list of alleles </param>
		/// <returns> a mapping of sample name to genotype object </returns>
		public LazyGenotypesContext.LazyData CreateGenotypeMap(string str, IList<Allele> alleles, string chr, int pos)
		{            
            if (genotypeParts == null)
                genotypeParts = new String[header.ColumnCount - NUM_STANDARD_FIELDS];
            try
            {
                FastStringUtils.Split(str, VCFConstants.FIELD_SEPARATOR_CHAR, genotypeParts);
            }
            catch(Exception e)
            {
                throw new VCFParsingError("Could not parse genotypes, was expecting " + (genotypeParts.Length - 1).ToString() + " but found " + str.Split(VCFConstants.FIELD_SEPARATOR_CHAR).Length.ToString(), e);
			}
			List<Genotype> genotypes = new List<Genotype>(genotypeParts.Length);
			// get the format keys
			//int nGTKeys = ParsingUtils.Split(genotypeParts[0], genotypeKeyArray, VCFConstants.GENOTYPE_FIELD_SEPARATOR_CHAR);
            string[] genotypeKeyArray = genotypeParts[0].Split(VCFConstants.GENOTYPE_FIELD_SEPARATOR_CHAR);
            int genotypeAlleleLocation = Array.IndexOf(genotypeKeyArray, VCFConstants.GENOTYPE_KEY);
            if (version != VCFHeaderVersion.VCF4_1 && genotypeAlleleLocation == -1)
            {
                generateException("Unable to find the GT field for the record; the GT field is required in VCF4.0");
            }
			// clear out our allele mapping
			alleleMap.Clear();
            GenotypeBuilder gb = new GenotypeBuilder();
                    
			// cycle through the genotype strings
			for (int genotypeOffset = 1; genotypeOffset < genotypeParts.Length; genotypeOffset++)
			{
                Genotype curGenotype;
                string sampleName = header.GenotypeSampleNames[genotypeOffset - 1];
                var currentGeno = genotypeParts[genotypeOffset];
                //shortcut for null alleles
                if (currentGeno == "./.")
                {
                    curGenotype = GenotypeBuilder.CreateMissing(sampleName, 2);
                }
                else if (currentGeno == ".")
                {
                    curGenotype = GenotypeBuilder.CreateMissing(sampleName, 1);
                }
                else
                {
                    gb.Reset(false);
                    gb.SampleName = sampleName;
                    string[] GTValueArray = FastStringUtils.Split(currentGeno, VCFConstants.GENOTYPE_FIELD_SEPARATOR_CHAR, int.MaxValue, StringSplitOptions.None);
                    // cycle through the sample names
                    // check to see if the value list is longer than the key list, which is a problem
                    if (genotypeKeyArray.Length < GTValueArray.Length)
                    {
                        generateException("There are too many keys for the sample " + sampleName + ", line is: keys = " + genotypeParts[0] + ", values = " + genotypeParts[genotypeOffset]);
                    }
                    if (genotypeAlleleLocation > 0)
                    {
                        generateException("Saw GT field at position " + genotypeAlleleLocation + ", but it must be at the first position for genotypes when present");
                    }

                    //TODO: THIS IS A DAMNED MESS
                    //Code loops over all fields in the key and decodes them, adding them as information to the genotype builder, which then makes it.
                    if (genotypeKeyArray.Length > 0)
                    {
                        gb.MaxAttributes(genotypeKeyArray.Length - 1);
                        for (int i = 0; i < genotypeKeyArray.Length; i++)
                        {
                            string gtKey = genotypeKeyArray[i];
                            if (i >= GTValueArray.Length) { break; }
                            // todo -- all of these on the fly parsing of the missing value should be static constants
                            if (gtKey == VCFConstants.GENOTYPE_FILTER_KEY)
                            {
                                IList<string> filters = parseFilters(GetCachedString(GTValueArray[i]));
                                if (filters != null)
                                {
                                    gb.SetFilters(filters.ToList());
                                }
                            }
                            else if (GTValueArray[i] == VCFConstants.MISSING_VALUE_v4)
                            {
                                // don't add missing values to the map
                            }
                            else
                            {
                                if (gtKey == VCFConstants.GENOTYPE_QUALITY_KEY)
                                {
                                    if (GTValueArray[i] == VCFConstants.MISSING_GENOTYPE_QUALITY_v3)
                                    {
                                        gb.noGQ();
                                    }
                                    else
                                    {
                                        gb.GQ = ((int)Math.Round(Convert.ToDouble(GTValueArray[i])));
                                    }
                                }
                                else if (gtKey == VCFConstants.GENOTYPE_ALLELE_DEPTHS)
                                {
                                    gb.AD = (decodeInts(GTValueArray[i]));
                                }
                                else if (gtKey == VCFConstants.GENOTYPE_PL_KEY)
                                {
                                    gb.PL = (decodeInts(GTValueArray[i]));
                                }
                                else if (gtKey == VCFConstants.GENOTYPE_LIKELIHOODS_KEY)
                                {
                                    gb.PL = (GenotypeLikelihoods.fromGLField(GTValueArray[i]).AsPLs);
                                }
                                else if (gtKey.Equals(VCFConstants.DEPTH_KEY))
                                {
                                    gb.DP = (Convert.ToInt32(GTValueArray[i]));
                                }
                                else
                                {
                                    gb.AddAttribute(gtKey, GTValueArray[i]);
                                }
                            }
                        }
                    }

                    List<Allele> GTalleles;
                    if (genotypeAlleleLocation == -1)
                    {
                        GTalleles = new List<Allele>(0);
                    }
                    else { GTalleles = parseGenotypeAlleles(GTValueArray[genotypeAlleleLocation], alleles, alleleMap); }
                    gb.Alleles = GTalleles;
                    gb.Phased = genotypeAlleleLocation != -1 && GTValueArray[genotypeAlleleLocation].IndexOf(VCFConstants.PHASED_AS_CHAR) != -1;

                    // add it to the list
                    try
                    {
                        curGenotype= gb.Make();
                    }
                    catch (Exception e)
                    {
                        throw new VCFParsingError(e.Message + ", at position " + chr + ":" + pos);
                    }
                }
                genotypes.Add(curGenotype);
			}
			return new LazyGenotypesContext.LazyData(genotypes, header.SampleNamesInOrder, header.SampleNameToOffset);
		}

        private static bool GTFieldAllMissingData(string genotype)
        {
            bool missing=true;
            for (int i = 0; i < genotype.Length; i++)
            {
                int j = i % 2;
                if (j == 0 && genotype[i] != VCFConstants.EMPTY_INFO_FIELD_AS_CHAR)
                { missing = false; break; }
                else if (j == 1 && genotype[i] != VCFConstants.UNPHASED_AS_CHAR)
                { missing = false; break; }
            }
            return missing;
        }
        //private static unsafe bool ContainsPhase(string toCheck)
        //{
        //    fixed (char* src = toCheck)
        //    {
        //         int len = toCheck.Length;
        //         while (len > 0)
        //         {
        //             if(*src==VCFConstants.ph
        //             --len;
        //         }

        //    }
        //}
        private static int[] decodeInts(string str)
		{
			return str.Split(VCFConstants.COMMA_AS_CHAR_ARRAY).Select(x=>Convert.ToInt32(x)).ToArray();
		}

		/// <summary>
		/// Forces all VCFCodecs to not perform any on the fly modifications to the VCF header
		/// of VCF records.  Useful primarily for raw comparisons such as when comparing
		/// raw VCF records
		/// </summary>
		public void disableOnTheFlyModifications()
		{
			doOnTheFlyModifications = false;
		}
		protected internal void generateException(string message)
		{
			throw new VCFParsingError(string.Format("The provided VCF file is malformed at approximately line number {0:D}: {1}", lineNo, message));
		}

		protected internal static void generateException(string message, int lineNo)
		{
			throw new VCFParsingError(string.Format("The provided VCF file is malformed at approximately line number {0:D}: {1}", lineNo, message));
		}
	}

}