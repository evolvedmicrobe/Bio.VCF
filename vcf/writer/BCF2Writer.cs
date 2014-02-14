using System;
using System.Diagnostics;
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
	using SAMSequenceDictionary = net.sf.samtools.SAMSequenceDictionary;
	using BCF2Codec = org.broadinstitute.variant.bcf2.BCF2Codec;
	using BCF2Type = org.broadinstitute.variant.bcf2.BCF2Type;
	using BCF2Utils = org.broadinstitute.variant.bcf2.BCF2Utils;
	using BCFVersion = org.broadinstitute.variant.bcf2.BCFVersion;
	using GeneralUtils = org.broadinstitute.variant.utils.GeneralUtils;
	using VCFConstants = Bio.VCF.VCFConstants;
	using VCFContigHeaderLine = Bio.VCF.VCFContigHeaderLine;
	using VCFHeader = Bio.VCF.VCFHeader;
	using org.broadinstitute.variant.variantcontext;
	using VCFUtils = Bio.VCF.VCFUtils;


	/// <summary>
	/// VariantContextWriter that emits BCF2 binary encoding
	/// 
	/// Overall structure of this writer is complex for efficiency reasons
	/// 
	/// -- The BCF2Writer manages the low-level BCF2 encoder, the mappings
	/// from contigs and strings to offsets, the VCF header, and holds the
	/// lower-level encoders that map from VC and Genotype fields to their
	/// specific encoders.  This class also writes out the standard BCF2 fields
	/// like POS, contig, the size of info and genotype data, QUAL, etc.  It
	/// has loops over the INFO and GENOTYPES to encode each individual datum
	/// with the generic field encoders, but the actual encoding work is
	/// done with by the FieldWriters classes themselves
	/// 
	/// -- BCF2FieldWriter are specialized classes for writing out SITE and
	/// genotype information for specific SITE/GENOTYPE fields (like AC for
	/// sites and GQ for genotypes).  These are objects in themselves because
	/// the manage all of the complexity of relating the types in the VCF header
	/// with the proper encoding in BCF as well as the type representing this
	/// in java.  Relating all three of these pieces of information together
	/// is the main complexity challenge in the encoder.  The piece of code
	/// that determines which FieldWriters to associate with each SITE and
	/// GENOTYPE field is the BCF2FieldWriterManager.  These FieldWriters
	/// are specialized for specific combinations of encoders (see below)
	/// and contexts (genotypes) for efficiency, so they smartly manage
	/// the writing of PLs (encoded as int[]) directly into the lowest
	/// level BCFEncoder.
	/// 
	/// -- At the third level is the BCF2FieldEncoder, relatively simple
	/// pieces of code that handle the task of determining the right
	/// BCF2 type for specific field values, as well as reporting back
	/// information such as the number of elements used to encode it
	/// (simple for atomic values like Integer but complex for PLs
	/// or lists of strings)
	/// 
	/// -- At the lowest level is the BCF2Encoder itself.  This provides
	/// just the limited encoding methods specified by the BCF2 specification.  This encoder
	/// doesn't do anything but make it possible to conveniently write out valid low-level
	/// BCF2 constructs.
	/// 
	/// @author Mark DePristo
	/// @since 06/12
	/// </summary>
	internal class BCF2Writer : IndexingVariantContextWriter
	{
		public const int MAJOR_VERSION = 2;
		public const int MINOR_VERSION = 1;

		private const bool ALLOW_MISSING_CONTIG_LINES = false;

		private readonly OutputStream outputStream; // Note: do not flush until completely done writing, to avoid issues with eventual BGZF support
		private VCFHeader header;
		private readonly IDictionary<string, int?> contigDictionary = new Dictionary<string, int?>();
		private readonly IDictionary<string, int?> stringDictionaryMap = new LinkedHashMap<string, int?>();
		private readonly bool doNotWriteGenotypes;
		private string[] sampleNames = null;

		private readonly BCF2Encoder encoder = new BCF2Encoder(); // initialized after the header arrives
		internal readonly BCF2FieldWriterManager fieldManager = new BCF2FieldWriterManager();

		/// <summary>
		/// cached results for whether we can write out raw genotypes data.
		/// </summary>
		private VCFHeader lastVCFHeaderOfUnparsedGenotypes = null;
		private bool canPassOnUnparsedGenotypeDataForLastVCFHeader = false;


//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public BCF2Writer(final File location, final OutputStream output, final net.sf.samtools.SAMSequenceDictionary refDict, final boolean enableOnTheFlyIndexing, final boolean doNotWriteGenotypes)
		public BCF2Writer(File location, OutputStream output, SAMSequenceDictionary refDict, bool enableOnTheFlyIndexing, bool doNotWriteGenotypes) : base(writerName(location, output), location, output, refDict, enableOnTheFlyIndexing)
		{
			this.outputStream = OutputStream;
			this.doNotWriteGenotypes = doNotWriteGenotypes;
		}

		// --------------------------------------------------------------------------------
		//
		// Interface functions
		//
		// --------------------------------------------------------------------------------

		public override void writeHeader(VCFHeader header)
		{
			// make sure the header is sorted correctly
			header = new VCFHeader(header.MetaDataInSortedOrder, header.GenotypeSampleNames);

			// create the config offsets map
			if (header.ContigLines.Count == 0)
			{
				if (ALLOW_MISSING_CONTIG_LINES)
				{
					if (GeneralUtils.DEBUG_MODE_ENABLED)
					{
						Console.Error.WriteLine("No contig dictionary found in header, falling back to reference sequence dictionary");
					}
					createContigDictionary(VCFUtils.makeContigHeaderLines(RefDict, null));
				}
				else
				{
					throw new IllegalStateException("Cannot write BCF2 file with missing contig lines");
				}
			}
			else
			{
				createContigDictionary(header.ContigLines);
			}

			// set up the map from dictionary string values -> offset
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ArrayList<String> dict = org.broadinstitute.variant.bcf2.BCF2Utils.makeDictionary(header);
			List<string> dict = BCF2Utils.makeDictionary(header);
			for (int i = 0; i < dict.Count; i++)
			{
				stringDictionaryMap[dict[i]] = i;
			}

			sampleNames = header.GenotypeSampleNames.ToArray();

			// setup the field encodings
			fieldManager.setup(header, encoder, stringDictionaryMap);

			try
			{
				// write out the header into a byte stream, get it's length, and write everything to the file
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ByteArrayOutputStream capture = new ByteArrayOutputStream();
				ByteArrayOutputStream capture = new ByteArrayOutputStream();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final OutputStreamWriter writer = new OutputStreamWriter(capture);
				OutputStreamWriter writer = new OutputStreamWriter(capture);
				this.header = VCFWriter.writeHeader(header, writer, doNotWriteGenotypes, VCFWriter.VersionLine, "BCF2 stream");
				writer.append('\0'); // the header is null terminated by a byte
				writer.close();

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] headerBytes = capture.toByteArray();
				sbyte[] headerBytes = capture.toByteArray();
				(new BCFVersion(MAJOR_VERSION, MINOR_VERSION)).write(outputStream);
				BCF2Type.INT32.write(headerBytes.Length, outputStream);
				outputStream.write(headerBytes);
			}
			catch (IOException e)
			{
				throw new Exception("BCF2 stream: Got IOException while trying to write BCF2 header", e);
			}
		}

		public override void add(VariantContext vc)
		{
			if (doNotWriteGenotypes)
			{
				vc = (new VariantContextBuilder(vc)).noGenotypes().make();
			}
			vc = vc.fullyDecode(header, false);

			base.add(vc); // allow on the fly indexing

			try
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] infoBlock = buildSitesData(vc);
				sbyte[] infoBlock = buildSitesData(vc);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] genotypesBlock = buildSamplesData(vc);
				sbyte[] genotypesBlock = buildSamplesData(vc);

				// write the two blocks to disk
				writeBlock(infoBlock, genotypesBlock);
			}
			catch (IOException e)
			{
				throw new Exception("Error writing record to BCF2 file: " + vc.ToString(), e);
			}
		}

		public override void close()
		{
			try
			{
				outputStream.flush();
				outputStream.close();
			}
			catch (IOException e)
			{
				throw new Exception("Failed to close BCF2 file");
			}
			base.close();
		}

		// --------------------------------------------------------------------------------
		//
		// implicit block
		//
		// The first four records of BCF are inline untype encoded data of:
		//
		// 4 byte integer chrom offset
		// 4 byte integer start
		// 4 byte integer ref length
		// 4 byte float qual
		//
		// --------------------------------------------------------------------------------
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private byte[] buildSitesData(VariantContext vc) throws IOException
		private sbyte[] buildSitesData(VariantContext vc)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int contigIndex = contigDictionary.get(vc.getChr());
			int contigIndex = contigDictionary[vc.Chr];
			if (contigIndex == -1)
			{
				throw new IllegalStateException(string.Format("Contig {0} not found in sequence dictionary from reference", vc.Chr));
			}

			// note use of encodeRawValue to not insert the typing byte
			encoder.encodeRawValue(contigIndex, BCF2Type.INT32);

			// pos.  GATK is 1 based, BCF2 is 0 based
			encoder.encodeRawValue(vc.Start - 1, BCF2Type.INT32);

			// ref length.  GATK is closed, but BCF2 is open so the ref length is GATK end - GATK start + 1
			// for example, a SNP is in GATK at 1:10-10, which has ref length 10 - 10 + 1 = 1
			encoder.encodeRawValue(vc.End - vc.Start + 1, BCF2Type.INT32);

			// qual
			if (vc.hasLog10PError())
			{
				encoder.encodeRawFloat((float) vc.PhredScaledQual);
			}
			else
			{
				encoder.encodeRawMissingValue(BCF2Type.FLOAT);
			}

			// info fields
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nAlleles = vc.getNAlleles();
			int nAlleles = vc.NAlleles;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nInfo = vc.getAttributes().size();
			int nInfo = vc.Attributes.Count;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nGenotypeFormatFields = getNGenotypeFormatFields(vc);
			int nGenotypeFormatFields = getNGenotypeFormatFields(vc);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int nSamples = header.getNGenotypeSamples();
			int nSamples = header.NGenotypeSamples;

			encoder.encodeRawInt((nAlleles << 16) | (nInfo & 0x0000FFFF), BCF2Type.INT32);
			encoder.encodeRawInt((nGenotypeFormatFields << 24) | (nSamples & 0x00FFFFF), BCF2Type.INT32);

			buildID(vc);
			buildAlleles(vc);
			buildFilter(vc);
			buildInfo(vc);

			return encoder.RecordBytes;
		}


		/// <summary>
		/// Can we safely write on the raw (undecoded) genotypes of an input VC?
		/// 
		/// The cache depends on the undecoded lazy data header == lastVCFHeaderOfUnparsedGenotypes, in
		/// which case we return the previous result.  If it's not cached, we use the BCF2Util to
		/// compare the VC header with our header (expensive) and cache it.
		/// </summary>
		/// <param name="lazyData">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private boolean canSafelyWriteRawGenotypesBytes(final org.broadinstitute.variant.bcf2.BCF2Codec.LazyData lazyData)
		private bool canSafelyWriteRawGenotypesBytes(BCF2Codec.LazyData lazyData)
		{
			if (lazyData.header != lastVCFHeaderOfUnparsedGenotypes)
			{
				// result is already cached
				canPassOnUnparsedGenotypeDataForLastVCFHeader = BCF2Utils.headerLinesAreOrderedConsistently(this.header,lazyData.header);
				lastVCFHeaderOfUnparsedGenotypes = lazyData.header;
			}

			return canPassOnUnparsedGenotypeDataForLastVCFHeader;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private org.broadinstitute.variant.bcf2.BCF2Codec.LazyData getLazyData(final VariantContext vc)
		private BCF2Codec.LazyData getLazyData(VariantContext vc)
		{
			if (vc.Genotypes.LazyWithData)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final LazyGenotypesContext lgc = (LazyGenotypesContext)vc.getGenotypes();
				LazyGenotypesContext lgc = (LazyGenotypesContext)vc.Genotypes;

				if (lgc.UnparsedGenotypeData is BCF2Codec.LazyData && canSafelyWriteRawGenotypesBytes((BCF2Codec.LazyData) lgc.UnparsedGenotypeData))
				{
					return (BCF2Codec.LazyData)lgc.UnparsedGenotypeData;
				}
				else
				{
					lgc.decode(); // WARNING -- required to avoid keeping around bad lazy data for too long
				}
			}

			return null;
		}

		/// <summary>
		/// Try to get the nGenotypeFields as efficiently as possible.
		/// 
		/// If this is a lazy BCF2 object just grab the field count from there,
		/// otherwise do the whole counting by types test in the actual data
		/// </summary>
		/// <param name="vc">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private final int getNGenotypeFormatFields(final VariantContext vc)
		private int getNGenotypeFormatFields(VariantContext vc)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Codec.LazyData lazyData = getLazyData(vc);
			BCF2Codec.LazyData lazyData = getLazyData(vc);
			return lazyData != null ? lazyData.nGenotypeFields : VCFWriter.calcVCFGenotypeKeys(vc, header).Count;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void buildID(VariantContext vc) throws IOException
		private void buildID(VariantContext vc)
		{
			encoder.encodeTypedString(vc.ID);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void buildAlleles(VariantContext vc) throws IOException
		private void buildAlleles(VariantContext vc)
		{
			foreach (Allele allele in vc.Alleles)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] s = allele.getDisplayBases();
				sbyte[] s = allele.DisplayBases;
				if (s == null)
				{
					throw new IllegalStateException("BUG: BCF2Writer encountered null padded allele" + allele);
				}
				encoder.encodeTypedString(s);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void buildFilter(VariantContext vc) throws IOException
		private void buildFilter(VariantContext vc)
		{
			if (vc.Filtered)
			{
				encodeStringsByRef(vc.Filters);
			}
			else if (vc.filtersWereApplied())
			{
				encodeStringsByRef(Collections.singleton(VCFConstants.PASSES_FILTERS_v4));
			}
			else
			{
				encoder.encodeTypedMissing(BCF2Type.INT8);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void buildInfo(VariantContext vc) throws IOException
		private void buildInfo(VariantContext vc)
		{
			foreach (KeyValuePair<string, object> infoFieldEntry in vc.Attributes)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String field = infoFieldEntry.getKey();
				string field = infoFieldEntry.Key;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BCF2FieldWriter.SiteWriter writer = fieldManager.getSiteFieldWriter(field);
				BCF2FieldWriter.SiteWriter writer = fieldManager.getSiteFieldWriter(field);
				if (writer == null)
				{
					errorUnexpectedFieldToWrite(vc, field, "INFO");
				}
				writer.start(encoder, vc);
				writer.site(encoder, vc);
				writer.done(encoder, vc);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private byte[] buildSamplesData(final VariantContext vc) throws IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private sbyte[] buildSamplesData(VariantContext vc)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Codec.LazyData lazyData = getLazyData(vc);
			BCF2Codec.LazyData lazyData = getLazyData(vc); // has critical side effects
			if (lazyData != null)
			{
				// we never decoded any data from this BCF file, so just pass it back
				return lazyData.bytes;
			}

			// we have to do work to convert the VC into a BCF2 byte stream
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final List<String> genotypeFields = VCFWriter.calcVCFGenotypeKeys(vc, header);
			IList<string> genotypeFields = VCFWriter.calcVCFGenotypeKeys(vc, header);
			foreach (String field in genotypeFields)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final BCF2FieldWriter.GenotypesWriter writer = fieldManager.getGenotypeFieldWriter(field);
				BCF2FieldWriter.GenotypesWriter writer = fieldManager.getGenotypeFieldWriter(field);
				if (writer == null)
				{
					errorUnexpectedFieldToWrite(vc, field, "FORMAT");
				}

				Debug.Assert(writer != null);

				writer.start(encoder, vc);
				foreach (String name in sampleNames)
				{
					Genotype g = vc.getGenotype(name);
					if (g == null)
					{
						g = GenotypeBuilder.createMissing(name, writer.nValuesPerGenotype);
					}
					writer.addGenotype(encoder, vc, g);
				}
				writer.done(encoder, vc);
			}
			return encoder.RecordBytes;
		}

		/// <summary>
		/// Throws a meaningful error message when a field (INFO or FORMAT) is found when writing out a file
		/// but there's no header line for it.
		/// </summary>
		/// <param name="vc"> </param>
		/// <param name="field"> </param>
		/// <param name="fieldType"> </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private final void errorUnexpectedFieldToWrite(final VariantContext vc, final String field, final String fieldType)
		private void errorUnexpectedFieldToWrite(VariantContext vc, string field, string fieldType)
		{
			throw new IllegalStateException("Found field " + field + " in the " + fieldType + " fields of VariantContext at " + vc.Chr + ":" + vc.Start + " from " + vc.Source + " but this hasn't been defined in the VCFHeader");
		}

		// --------------------------------------------------------------------------------
		//
		// Low-level block encoding
		//
		// --------------------------------------------------------------------------------

		/// <summary>
		/// Write the data in the encoder to the outputstream as a length encoded
		/// block of data.  After this call the encoder stream will be ready to
		/// start a new data block
		/// </summary>
		/// <exception cref="IOException"> </exception>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"infoBlock.length > 0", "genotypesBlock.length >= 0"}) private void writeBlock(final byte[] infoBlock, final byte[] genotypesBlock) throws IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private void writeBlock(sbyte[] infoBlock, sbyte[] genotypesBlock)
		{
			BCF2Type.INT32.write(infoBlock.Length, outputStream);
			BCF2Type.INT32.write(genotypesBlock.Length, outputStream);
			outputStream.write(infoBlock);
			outputStream.write(genotypesBlock);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("! strings.isEmpty()") @Ensures("result.isIntegerType()") private final org.broadinstitute.variant.bcf2.BCF2Type encodeStringsByRef(final Collection<String> strings) throws IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private BCF2Type encodeStringsByRef(ICollection<string> strings)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final List<Integer> offsets = new ArrayList<Integer>(strings.size());
			IList<int?> offsets = new List<int?>(strings.Count);

			// iterate over strings until we find one that needs 16 bits, and break
			foreach (String string in strings)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer got = stringDictionaryMap.get(string);
				int? got = stringDictionaryMap[string];
				if (got == null)
				{
					throw new IllegalStateException("Format error: could not find string " + string + " in header as required by BCF");
				}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int offset = got;
				int offset = got;
				offsets.Add(offset);
			}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Type type = org.broadinstitute.variant.bcf2.BCF2Utils.determineIntegerType(offsets);
			BCF2Type type = BCF2Utils.determineIntegerType(offsets);
			encoder.encodeTyped(offsets, type);
			return type;
		}

		/// <summary>
		/// Create the contigDictionary from the contigLines extracted from the VCF header
		/// </summary>
		/// <param name="contigLines"> </param>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("contigDictionary.isEmpty()") private final void createContigDictionary(final Collection<Bio.VCF.VCFContigHeaderLine> contigLines)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private void createContigDictionary(ICollection<VCFContigHeaderLine> contigLines)
		{
			int offset = 0;
			foreach (VCFContigHeaderLine contig in contigLines)
			{
				contigDictionary[contig.ID] = offset++;
			}
		}
	}

}