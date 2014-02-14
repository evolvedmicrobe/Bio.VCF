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

	using Ensures = com.google.java.contract.Ensures;
	using Requires = com.google.java.contract.Requires;
	using SAMSequenceDictionary = net.sf.samtools.SAMSequenceDictionary;
	using SAMSequenceRecord = net.sf.samtools.SAMSequenceRecord;
	using Tribble = org.broad.tribble.Tribble;
	using DynamicIndexCreator = org.broad.tribble.index.DynamicIndexCreator;
	using Index = org.broad.tribble.index.Index;
	using IndexFactory = org.broad.tribble.index.IndexFactory;
	using LittleEndianOutputStream = org.broad.tribble.util.LittleEndianOutputStream;
	using VCFHeader = Bio.VCF.VCFHeader;
	using VariantContext = org.broadinstitute.variant.variantcontext.VariantContext;


	/// <summary>
	/// this class writes VCF files
	/// </summary>
	internal abstract class IndexingVariantContextWriter : VariantContextWriter
	{
		private readonly string name;
		private readonly SAMSequenceDictionary refDict;

		private OutputStream outputStream;
		private PositionalOutputStream positionalOutputStream = null;
		private DynamicIndexCreator indexer = null;
		private LittleEndianOutputStream idxStream = null;

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"name != null", "! ( location == null && output == null )", "! ( enableOnTheFlyIndexing && location == null )"}) protected IndexingVariantContextWriter(final String name, final File location, final OutputStream output, final net.sf.samtools.SAMSequenceDictionary refDict, final boolean enableOnTheFlyIndexing)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		protected internal IndexingVariantContextWriter(string name, File location, OutputStream output, SAMSequenceDictionary refDict, bool enableOnTheFlyIndexing)
		{
			outputStream = output;
			this.name = name;
			this.refDict = refDict;

			if (enableOnTheFlyIndexing)
			{
				try
				{
					idxStream = new LittleEndianOutputStream(new FileOutputStream(Tribble.indexFile(location)));
					//System.out.println("Creating index on the fly for " + location);
					indexer = new DynamicIndexCreator(IndexFactory.IndexBalanceApproach.FOR_SEEK_TIME);
					indexer.initialize(location, indexer.defaultBinSize());
					positionalOutputStream = new PositionalOutputStream(output);
					outputStream = positionalOutputStream;
				}
				catch (IOException ex)
				{
					// No matter what we keep going, since we don't care if we can't create the index file
					idxStream = null;
					indexer = null;
					positionalOutputStream = null;
				}
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") public OutputStream getOutputStream()
		public virtual OutputStream OutputStream
		{
			get
			{
				return outputStream;
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") public String getStreamName()
		public virtual string StreamName
		{
			get
			{
				return name;
			}
		}

		public abstract void writeHeader(VCFHeader header);

		/// <summary>
		/// attempt to close the VCF file
		/// </summary>
		public virtual void close()
		{
			try
			{
				// try to close the index stream (keep it separate to help debugging efforts)
				if (indexer != null)
				{
					Index index = indexer.finalizeIndex(positionalOutputStream.Position);
					setIndexSequenceDictionary(index, refDict);
					index.write(idxStream);
					idxStream.close();
				}

				// close the underlying output stream as well
				outputStream.close();
			}
			catch (IOException e)
			{
				throw new Exception("Unable to close index for " + StreamName, e);
			}
		}

		/// <returns> the reference sequence dictionary used for the variant contexts being written </returns>
		public virtual SAMSequenceDictionary RefDict
		{
			get
			{
				return refDict;
			}
		}

		/// <summary>
		/// add a record to the file
		/// </summary>
		/// <param name="vc">      the Variant Context object </param>
		public virtual void add(VariantContext vc)
		{
			// if we are doing on the fly indexing, add the record ***before*** we write any bytes
			if (indexer != null)
			{
				indexer.addFeature(vc, positionalOutputStream.Position);
			}
		}

		/// <summary>
		/// Returns a reasonable "name" for this writer, to display to the user if something goes wrong
		/// </summary>
		/// <param name="location"> </param>
		/// <param name="stream">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected static final String writerName(final File location, final OutputStream stream)
		protected internal static string writerName(File location, OutputStream stream)
		{
			return location == null ? stream.ToString() : location.AbsolutePath;
		}

		// a constant we use for marking sequence dictionary entries in the Tribble index property list
		private const string SequenceDictionaryPropertyPredicate = "DICT:";

		private static void setIndexSequenceDictionary(Index index, SAMSequenceDictionary dict)
		{
			foreach (SAMSequenceRecord seq in dict.Sequences)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String contig = SequenceDictionaryPropertyPredicate + seq.getSequenceName();
				string contig = SequenceDictionaryPropertyPredicate + seq.SequenceName;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String length = String.valueOf(seq.getSequenceLength());
				string length = Convert.ToString(seq.SequenceLength);
				index.addProperty(contig,length);
			}
		}
	}

	internal sealed class PositionalOutputStream : OutputStream
	{
		private readonly OutputStream @out;
		private long position = 0;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public PositionalOutputStream(final OutputStream out)
		public PositionalOutputStream(OutputStream @out)
		{
			this.@out = @out;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void write(final byte[] bytes) throws IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void write(sbyte[] bytes)
		{
			write(bytes, 0, bytes.Length);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void write(final byte[] bytes, final int startIndex, final int numBytes) throws IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void write(sbyte[] bytes, int startIndex, int numBytes)
		{
			position += numBytes;
			@out.write(bytes, startIndex, numBytes);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void write(int c) throws IOException
		public void write(int c)
		{
			position++;
			@out.write(c);
		}

		public long Position
		{
			get
			{
				return position;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void close() throws IOException
		public override void close()
		{
			base.close();
			@out.close();
		}
	}
}