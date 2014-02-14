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
	/// Summary types for Genotype objects
	/// NOTE: These are used as list index values so must be 1..N 
	/// </summary>
	public enum GenotypeType
	{
		/// <summary>
		/// The sample is no-called (all alleles are NO_CALL </summary>
		NO_CALL=0,
		/// <summary>
		/// The sample is homozygous reference </summary>
		HOM_REF=1,
		/// <summary>
		/// The sample is heterozygous, with at least one ref and at least one one alt in any order </summary>
		HET=2,
		/// <summary>
		/// All alleles are non-reference </summary>
		HOM_VAR=3,
		/// <summary>
		/// There is no allele data availble for this sample (alleles.isEmpty) </summary>
		UNAVAILABLE=4,
		/// <summary>
		/// Some chromosomes are NO_CALL and others are called </summary>
		MIXED=5 // no-call and call in the same genotype

	}

}