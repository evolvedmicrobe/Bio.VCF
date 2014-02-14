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

namespace Bio.VCF
{


	/// <summary>
	/// A special class representing a contig VCF header line.  Nows the true contig order and sorts on that
	/// 
	/// @author mdepristo
	/// </summary>
	public class VCFContigHeaderLine : VCFSimpleHeaderLine
	{
		internal readonly int? contigIndex;


		/// <summary>
		/// create a VCF contig header line
		/// </summary>
		/// <param name="line">      the header line </param>
		/// <param name="version">   the vcf header version </param>
		/// <param name="key">            the key for this header line </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public VCFContigHeaderLine(final String line, final VCFHeaderVersion version, final String key, int contigIndex)
		public VCFContigHeaderLine(string line, VCFHeaderVersion version, string key, int contigIndex) : base(line, version, key, null)
		{
			this.contigIndex = contigIndex;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public VCFContigHeaderLine(final java.util.Map<String, String> mapping, int contigIndex)
		public VCFContigHeaderLine(IDictionary<string, string> mapping, int contigIndex) : base(VCFHeader.CONTIG_KEY, mapping, null)
		{
			this.contigIndex = contigIndex;
		}

		public virtual int? ContigIndex
		{
			get
			{
				return contigIndex;
			}
		}

		/// <summary>
		/// IT IS CRITIAL THAT THIS BE OVERRIDDEN SO WE SORT THE CONTIGS IN THE CORRECT ORDER
		/// </summary>
		/// <param name="other">
		/// @return </param>
		public override int CompareTo(object other)
		{
			if (other is VCFContigHeaderLine)
			{
				return contigIndex.Value.CompareTo(((VCFContigHeaderLine) other).contigIndex);
			}
			else
			{
				return base.CompareTo(other);
			}
		}
	}
}