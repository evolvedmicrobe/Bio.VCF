using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
	/// A feature codec for the VCF3 specification, to read older VCF files.  VCF3 has been
	/// depreciated in favor of VCF4 (See VCF codec for the latest information)
	/// 
	/// <p>
	/// Reads historical VCF3 encoded files (1000 Genomes Pilot results, for example)
	/// </p>
	/// 
	/// <p> </summary>
	/// See also: <seealso cref= <a href="http://vcftools.sourceforge.net/specs.html">VCF specification</a><br> </seealso>
	/// See also: <seealso cref= <a href="http://www.ncbi.nlm.nih.gov/pubmed/21653522">VCF spec. publication</a>
	/// </p>
	/// 
    /// TODO: Lots of overlap with later format parser, should be combined.
	/// @author Mark DePristo
	/// @since 2010 </seealso>
	public class VCF3Codec : AbstractVCFCodec
	{
        public VCF3Codec():base("VCFv3",VCFHeaderVersion.VCF3_2,VCFHeaderVersion.VCF3_3)
        {}
		public const string VCF3_MAGIC_HEADER = "##fileformat=VCFv3";
        

		
		
        protected internal override void validateFilters(string filterString)
        {
            if (filterString.Length == 0)
            {
                generateException("The VCF specification requires a valid filter status");
            }
        }

    	public bool canDecode(string potentialInput)
		{
			return canDecodeFile(potentialInput, VCF3_MAGIC_HEADER);
		}
        /// <summary>
        /// parse the filter string, first checking to see if we already have parsed it in a previous attempt </summary>
        /// <param name="filterString"> the string to parse </param>
        /// <returns> a set of the filters applied </returns>
        protected override IList<String> parseFilters(String filterString)
        {
            // null for unfiltered
            if (filterString.Equals(VCFConstants.UNFILTERED))
                return null;
            
            if (filterString.Equals(VCFConstants.PASSES_FILTERS_v3))
                return new List<String>(0);

            validateFilters(filterString);

            return filterHash[filterString].List;
        }
	}

}