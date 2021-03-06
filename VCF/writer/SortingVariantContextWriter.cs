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

	using VariantContext = org.broadinstitute.variant.variantcontext.VariantContext;

	/// <summary>
	/// this class writes VCF files, allowing records to be passed in unsorted (up to a certain genomic distance away)
	/// </summary>
	internal class SortingVariantContextWriter : SortingVariantContextWriterBase
	{

		// the maximum START distance between records that we'll cache
		private int maxCachingStartDistance;

		/// <summary>
		/// create a local-sorting VCF writer, given an inner VCF writer to write to
		/// </summary>
		/// <param name="innerWriter">        the VCFWriter to write to </param>
		/// <param name="maxCachingStartDistance"> the maximum start distance between records that we'll cache </param>
		/// <param name="takeOwnershipOfInner"> Should this Writer close innerWriter when it's done with it </param>
		public SortingVariantContextWriter(VariantContextWriter innerWriter, int maxCachingStartDistance, bool takeOwnershipOfInner) : base(innerWriter, takeOwnershipOfInner)
		{
			this.maxCachingStartDistance = maxCachingStartDistance;
		}

		public SortingVariantContextWriter(VariantContextWriter innerWriter, int maxCachingStartDistance) : this(innerWriter, maxCachingStartDistance, false); / / by default, don't own inner
		{
		}

		protected internal override void noteCurrentRecord(VariantContext vc)
		{
			base.noteCurrentRecord(vc); // first, check for errors

			// then, update mostUpstreamWritableLoc:
			int mostUpstreamWritableIndex = vc.Start - maxCachingStartDistance;
			this.mostUpstreamWritableLoc = Math.Max(BEFORE_MOST_UPSTREAM_LOC, mostUpstreamWritableIndex);
		}
	}
}