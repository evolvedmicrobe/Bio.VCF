using System.Runtime.CompilerServices;
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

	using VCFHeader = Bio.VCF.VCFHeader;
	using VariantContext = org.broadinstitute.variant.variantcontext.VariantContext;


	/// <summary>
	/// This class writes VCF files, allowing records to be passed in unsorted.
	/// It also enforces that it is never passed records of the same chromosome with any other chromosome in between them.
	/// </summary>
	internal abstract class SortingVariantContextWriterBase : VariantContextWriter
	{

		// The VCFWriter to which to actually write the sorted VCF records
		private readonly VariantContextWriter innerWriter;

		// the current queue of un-emitted records
		private readonly LinkedList<VCFRecord> queue;

		// The locus until which we are permitted to write out (inclusive)
		protected internal int? mostUpstreamWritableLoc;
		protected internal const int BEFORE_MOST_UPSTREAM_LOC = 0; // No real locus index is <= 0

		// The set of chromosomes already passed over and to which it is forbidden to return
		private readonly Set<string> finishedChromosomes;

		// Should we call innerWriter.close() in close()
		private readonly bool takeOwnershipOfInner;

		// --------------------------------------------------------------------------------
		//
		// Constructors
		//
		// --------------------------------------------------------------------------------

		/// <summary>
		/// create a local-sorting VCF writer, given an inner VCF writer to write to
		/// </summary>
		/// <param name="innerWriter">        the VCFWriter to write to </param>
		/// <param name="takeOwnershipOfInner"> Should this Writer close innerWriter when it's done with it </param>
		public SortingVariantContextWriterBase(VariantContextWriter innerWriter, bool takeOwnershipOfInner)
		{
			this.innerWriter = innerWriter;
			this.finishedChromosomes = new SortedSet<string>();
			this.takeOwnershipOfInner = takeOwnershipOfInner;

			// has to be PriorityBlockingQueue to be thread-safe
			this.queue = new PriorityBlockingQueue<VCFRecord>(50, new VariantContextComparator());

			this.mostUpstreamWritableLoc = BEFORE_MOST_UPSTREAM_LOC;
		}

		public SortingVariantContextWriterBase(VariantContextWriter innerWriter) : this(innerWriter, false); / / by default, don't own inner
		{
		}

		// --------------------------------------------------------------------------------
		//
		// public interface functions
		//
		// --------------------------------------------------------------------------------

		public override void writeHeader(VCFHeader header)
		{
			innerWriter.writeHeader(header);
		}

		/// <summary>
		/// attempt to close the VCF file; we need to flush the queue first
		/// </summary>
		public override void close()
		{
			stopWaitingToSort();

			if (takeOwnershipOfInner)
			{
				innerWriter.close();
			}
		}


		/// <summary>
		/// add a record to the file
		/// </summary>
		/// <param name="vc">      the Variant Context object </param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void add(VariantContext vc)
		{
			/* Note that the code below does not prevent the successive add()-ing of: (chr1, 10), (chr20, 200), (chr15, 100)
			   since there is no implicit ordering of chromosomes:
			 */
			VCFRecord firstRec = queue.First.Value;
			if (firstRec != null && !vc.Chr.Equals(firstRec.vc.Chr)) // if we hit a new contig, flush the queue
			{
				if (finishedChromosomes.contains(vc.Chr))
				{
					throw new System.ArgumentException("Added a record at " + vc.Chr + ":" + vc.Start + ", but already finished with chromosome" + vc.Chr);
				}

				finishedChromosomes.add(firstRec.vc.Chr);
				stopWaitingToSort();
			}

			noteCurrentRecord(vc); // possibly overwritten

			queue.AddLast(new VCFRecord(vc));
			emitSafeRecords();
		}

		/// <summary>
		/// Gets a string representation of this object. </summary>
		/// <returns> a string representation of this object </returns>
		public override string ToString()
		{
			return this.GetType().Name;
		}

		// --------------------------------------------------------------------------------
		//
		// protected interface functions for subclasses to use
		//
		// --------------------------------------------------------------------------------

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void stopWaitingToSort()
		{
			emitRecords(true);
			mostUpstreamWritableLoc = BEFORE_MOST_UPSTREAM_LOC;
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		protected internal virtual void emitSafeRecords()
		{
			emitRecords(false);
		}

		protected internal virtual void noteCurrentRecord(VariantContext vc)
		{
			// did the user break the contract by giving a record too late?
			if (mostUpstreamWritableLoc != null && vc.Start < mostUpstreamWritableLoc) // went too far back, since may have already written anything that is <= mostUpstreamWritableLoc
			{
				throw new System.ArgumentException("Permitted to write any record upstream of position " + mostUpstreamWritableLoc + ", but a record at " + vc.Chr + ":" + vc.Start + " was just added.");
			}
		}

		// --------------------------------------------------------------------------------
		//
		// private implementation functions
		//
		// --------------------------------------------------------------------------------

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void emitRecords(bool emitUnsafe)
		{
			while (queue.Count > 0)
			{
				VCFRecord firstRec = queue.First.Value;

				// No need to wait, waiting for nothing, or before what we're waiting for:
				if (emitUnsafe || mostUpstreamWritableLoc == null || firstRec.vc.Start <= mostUpstreamWritableLoc)
				{
					queue.RemoveFirst();
					innerWriter.add(firstRec.vc);
				}
				else
				{
					break;
				}
			}
		}

		private class VariantContextComparator : IComparer<VCFRecord>
		{
			public virtual int Compare(VCFRecord r1, VCFRecord r2)
			{
				return r1.vc.Start - r2.vc.Start;
			}
		}

		private class VCFRecord
		{
			public VariantContext vc;

			public VCFRecord(VariantContext vc)
			{
				this.vc = vc;
			}
		}
	}
}