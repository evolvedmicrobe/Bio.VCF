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
using System;
using System.Collections;
using System.Collections.Generic;

namespace Bio.VCF
{


	/// <summary>
	/// @author ebanks
	/// A class representing a key=value entry for FILTER fields in the VCF header
	/// </summary>
	public class VCFFilterHeaderLine : VCFSimpleHeaderLine
	{

		/// <summary>
		/// create a VCF filter header line
		/// </summary>
		/// <param name="name">         the name for this header line </param>
		/// <param name="description">  the description for this header line </param>
		public VCFFilterHeaderLine(string name, string description) : base("FILTER", name, description)
		{
		}

		/// <summary>
		/// Convenience constructor for FILTER whose description is the name </summary>
		/// <param name="name"> </param>
		public VCFFilterHeaderLine(string name) : base("FILTER", name, name)
		{
		}

		/// <summary>
		/// create a VCF info header line
		/// </summary>
		/// <param name="line">      the header line </param>
		/// <param name="version">   the vcf header version </param>
		public VCFFilterHeaderLine(string line, VCFHeaderVersion version) : base(line, version, "FILTER", "ID", "Description")
		{
		}
	}
}