using System;
using System.Collections;
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
	using Invariant = com.google.java.contract.Invariant;
	using Requires = com.google.java.contract.Requires;
	using BCF2Type = org.broadinstitute.variant.bcf2.BCF2Type;
	using BCF2Utils = org.broadinstitute.variant.bcf2.BCF2Utils;
	using VCFCompoundHeaderLine = Bio.VCF.VCFCompoundHeaderLine;
	using VCFHeaderLineCount = Bio.VCF.VCFHeaderLineCount;
	using VariantContext = org.broadinstitute.variant.variantcontext.VariantContext;


	/// <summary>
	/// See #BCFWriter for documentation on this classes role in encoding BCF2 files
	/// 
	/// @author Mark DePristo
	/// @since 06/12
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Invariant({ "headerLine != null", "dictionaryOffsetType.isIntegerType()", "dictionaryOffset >= 0" }) public abstract class BCF2FieldEncoder
	public abstract class BCF2FieldEncoder
	{
		/// <summary>
		/// The header line describing the field we will encode values of
		/// </summary>
		internal readonly VCFCompoundHeaderLine headerLine;

		/// <summary>
		/// The BCF2 type we'll use to encoder this field, if it can be determined statically.
		/// If not, this variable must be null
		/// </summary>
		internal readonly BCF2Type staticType;

		/// <summary>
		/// The integer offset into the strings map of the BCF2 file corresponding to this
		/// field.
		/// </summary>
		internal readonly int dictionaryOffset;

		/// <summary>
		/// The integer type we use to encode our dictionary offset in the BCF2 file
		/// </summary>
		internal readonly BCF2Type dictionaryOffsetType;

		// ----------------------------------------------------------------------
		//
		// Constructor
		//
		// ----------------------------------------------------------------------

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"headerLine != null", "dict != null"}) private BCF2FieldEncoder(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict, final org.broadinstitute.variant.bcf2.BCF2Type staticType)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private BCF2FieldEncoder(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict, BCF2Type staticType)
		{
			this.headerLine = headerLine;
			this.staticType = staticType;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer offset = dict.get(getField());
			int? offset = dict[Field];
			if (offset == null)
			{
				throw new IllegalStateException("Format error: could not find string " + Field + " in header as required by BCF");
			}
			this.dictionaryOffset = offset;
			dictionaryOffsetType = BCF2Utils.determineIntegerType(offset);
		}

		// ----------------------------------------------------------------------
		//
		// Basic accessors
		//
		// ----------------------------------------------------------------------

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") public final String getField()
		public string Field
		{
			get
			/// <summary>
			/// Write the field key (dictionary offset and type) into the BCF2Encoder stream
			/// </summary>
			/// <param name="encoder"> where we write our dictionary offset </param>
			/// <exception cref="IOException"> </exception>
			{
				return headerLine.ID;
			}
		}
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("encoder != null") public final void writeFieldKey(final BCF2Encoder encoder) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void writeFieldKey(BCF2Encoder encoder)
		{
			encoder.encodeTypedInt(dictionaryOffset, dictionaryOffsetType);
		}

		public override string ToString()
		{
			return "BCF2FieldEncoder for " + Field + " with count " + CountType + " encoded with " + this.GetType().SimpleName;
		}

		// ----------------------------------------------------------------------
		//
		// methods to determine the number of encoded elements
		//
		// ----------------------------------------------------------------------

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") protected final Bio.VCF.VCFHeaderLineCount getCountType()
		protected internal VCFHeaderLineCount CountType
		{
			get
			{
				return headerLine.CountType;
			}
		}

		/// <summary>
		/// True if this field has a constant, fixed number of elements (such as 1 for an atomic integer)
		/// 
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != (hasValueDeterminedNumElements() || hasContextDeterminedNumElements())") public boolean hasConstantNumElements()
		public virtual bool hasConstantNumElements()
		{
			return CountType == VCFHeaderLineCount.INTEGER;
		}

		/// <summary>
		/// True if the only way to determine how many elements this field contains is by
		/// inspecting the actual value directly, such as when the number of elements
		/// is a variable length list per site or per genotype.
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != (hasConstantNumElements() || hasContextDeterminedNumElements())") public boolean hasValueDeterminedNumElements()
		public virtual bool hasValueDeterminedNumElements()
		{
			return CountType == VCFHeaderLineCount.UNBOUNDED;
		}

		/// <summary>
		/// True if this field has a non-fixed number of elements that depends only on the properties
		/// of the current VariantContext, such as one value per Allele or per genotype configuration.
		/// 
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != (hasValueDeterminedNumElements() || hasConstantNumElements())") public boolean hasContextDeterminedNumElements()
		public virtual bool hasContextDeterminedNumElements()
		{
			return !hasConstantNumElements() && !hasValueDeterminedNumElements();
		}

		/// <summary>
		/// Get the number of elements, assuming this field has a constant number of elements.
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("hasConstantNumElements()") @Ensures("result >= 0") public int numElements()
		public virtual int numElements()
		{
			return headerLine.Count;
		}

		/// <summary>
		/// Get the number of elements by looking at the actual value provided
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("hasValueDeterminedNumElements()") @Ensures("result >= 0") public int numElements(final Object value)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual int numElements(object value)
		{
			return numElementsFromValue(value);
		}

		/// <summary>
		/// Get the number of elements, assuming this field has context-determined number of elements.
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("hasContextDeterminedNumElements()") @Ensures("result >= 0") public int numElements(final org.broadinstitute.variant.variantcontext.VariantContext vc)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual int numElements(VariantContext vc)
		{
			return headerLine.getCount(vc);
		}

		/// <summary>
		/// A convenience access for the number of elements, returning
		/// the number of encoded elements, either from the fixed number
		/// it has, from the VC, or from the value itself. </summary>
		/// <param name="vc"> </param>
		/// <param name="value">
		/// @return </param>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result >= 0") public final int numElements(final org.broadinstitute.variant.variantcontext.VariantContext vc, final Object value)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public int numElements(VariantContext vc, object value)
		{
			if (hasConstantNumElements())
			{
				return numElements();
			}
			else if (hasContextDeterminedNumElements())
			{
				return numElements(vc);
			}
			else
			{
				return numElements(value);
			}
		}

		/// <summary>
		/// Given a value, return the number of elements we will encode for it.
		/// 
		/// Assumes the value is encoded as a List
		/// </summary>
		/// <param name="value">
		/// @return </param>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("hasValueDeterminedNumElements()") @Ensures("result >= 0") protected int numElementsFromValue(final Object value)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		protected internal virtual int numElementsFromValue(object value)
		{
			if (value == null)
			{
				return 0;
			}
			else if (value is IList)
			{
				return ((IList) value).Count;
			}
			else
			{
				return 1;
			}
		}

		// ----------------------------------------------------------------------
		//
		// methods to determine the BCF2 type of the encoded values
		//
		// ----------------------------------------------------------------------

		/// <summary>
		/// Is the BCF2 type of this field static, or does it have to be determine from
		/// the actual field value itself?
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result || isDynamicallyTyped()") public final boolean isStaticallyTyped()
		public bool StaticallyTyped
		{
			get
			/// <summary>
			/// Is the BCF2 type of this field static, or does it have to be determine from
			/// the actual field value itself?
			/// @return
			/// </summary>
			{
				return !DynamicallyTyped;
			}
		}
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result || isStaticallyTyped()") public final boolean isDynamicallyTyped()
		public bool DynamicallyTyped
		{
			get
			{
				return staticType == null;
			}
		}

		/// <summary>
		/// Get the BCF2 type for this field, either from the static type of the
		/// field itself or by inspecting the value itself.
		/// 
		/// @return
		/// </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public final org.broadinstitute.variant.bcf2.BCF2Type getType(final Object value)
		public BCF2Type getType(object value)
		{
			return DynamicallyTyped ? getDynamicType(value) : StaticType;
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("isStaticallyTyped()") @Ensures("result != null") public final org.broadinstitute.variant.bcf2.BCF2Type getStaticType()
		public BCF2Type StaticType
		{
			get
			{
				return staticType;
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("isDynamicallyTyped()") @Ensures("result != null") public org.broadinstitute.variant.bcf2.BCF2Type getDynamicType(final Object value)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public virtual BCF2Type getDynamicType(object value)
		{
			throw new IllegalStateException("BUG: cannot get dynamic type for statically typed BCF2 field " + Field);
		}

		// ----------------------------------------------------------------------
		//
		// methods to encode values, including the key abstract method
		//
		// ----------------------------------------------------------------------

		/// <summary>
		/// Key abstract method that should encode a value of the given type into the encoder.
		/// 
		/// Value will be of a type appropriate to the underlying encoder.  If the genotype field is represented as
		/// an int[], this will be value, and the encoder needs to handle encoding all of the values in the int[].
		/// 
		/// The argument should be used, not the getType() method in the superclass as an outer loop might have
		/// decided a more general type (int16) to use, even through this encoder could have been done with int8.
		/// 
		/// If minValues > 0, then encodeValue must write in at least minValues items from value.  If value is atomic,
		/// this means that minValues - 1 MISSING values should be added to the encoder.  If minValues is a collection
		/// type (int[]) then minValues - values.length should be added.  This argument is intended to handle padding
		/// of values in genotype fields.
		/// </summary>
		/// <param name="encoder"> </param>
		/// <param name="value"> </param>
		/// <param name="type"> </param>
		/// <param name="minValues"> </param>
		/// <exception cref="IOException"> </exception>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"encoder != null", "isDynamicallyTyped() || type == getStaticType()", "minValues >= 0"}) public abstract void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException;
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public abstract void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues);

		// ----------------------------------------------------------------------
		//
		// Subclass to encode Strings
		//
		// ----------------------------------------------------------------------

		public class StringOrCharacter : BCF2FieldEncoder
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public StringOrCharacter(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict)
			public StringOrCharacter(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict) : base(headerLine, dict, BCF2Type.CHAR)
			{
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = javaStringToBCF2String(value);
				string s = javaStringToBCF2String(value);
				encoder.encodeRawString(s, Math.Max(s.Length, minValues));
			}

			//
			// Regardless of what the header says, BCF2 strings and characters are always encoded
			// as arrays of CHAR type, which has a variable number of elements depending on the
			// exact string being encoded
			//
			public override bool hasConstantNumElements()
			{
				return false;
			}
			public override bool hasContextDeterminedNumElements()
			{
				return false;
			}
			public override bool hasValueDeterminedNumElements()
			{
				return true;
			}
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected int numElementsFromValue(final Object value)
			protected internal override int numElementsFromValue(object value)
			{
				return value == null ? 0 : javaStringToBCF2String(value).Length;
			}

			/// <summary>
			/// Recode the incoming object to a String, compacting it into a
			/// BCF2 string if the value is a list.
			/// </summary>
			/// <param name="value"> a String or List<String> to encode, or null </param>
			/// <returns> a non-null string to encode </returns>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") private String javaStringToBCF2String(final Object value)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			private string javaStringToBCF2String(object value)
			{
				if (value == null)
				{
					return "";
				}
				else if (value is IList)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<String> l = (java.util.List<String>)value;
					IList<string> l = (IList<string>)value;
					if (l.Count == 0)
					{
						return "";
					}
					else
					{
						return BCF2Utils.collapseStringList(l);
					}
				}
				else
				{
					return (string)value;
				}
			}
		}

		// ----------------------------------------------------------------------
		//
		// Subclass to encode FLAG
		//
		// ----------------------------------------------------------------------

		public class Flag : BCF2FieldEncoder
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public Flag(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict)
			public Flag(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict) : base(headerLine, dict, BCF2Type.INT8)
			{
				if (!headerLine.FixedCount || headerLine.Count != 0)
				{
					throw new IllegalStateException("Flag encoder only supports atomic flags for field " + Field);
				}
			}

			public override int numElements()
			{
				return 1; // the header says 0 but we will write 1 value
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Override @Requires({"minValues <= 1", "value != null", "value instanceof Boolean", "((Boolean)value) == true"}) public void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues)
			{
				encoder.encodeRawBytes(1, StaticType);
			}
		}

		// ----------------------------------------------------------------------
		//
		// Subclass to encode FLOAT
		//
		// ----------------------------------------------------------------------

		public class Float : BCF2FieldEncoder
		{
			internal readonly bool isAtomic;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public Float(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict)
			public Float(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict) : base(headerLine, dict, BCF2Type.FLOAT)
			{
				isAtomic = hasConstantNumElements() && numElements() == 1;
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues)
			{
				int count = 0;
				// TODO -- can be restructured to avoid toList operation
				if (isAtomic)
				{
					// fast path for fields with 1 fixed float value
					if (value != null)
					{
						encoder.encodeRawFloat((double?)value);
						count++;
					}
				}
				else
				{
					// handle generic case
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Double> doubles = toList(Double.class, value);
					IList<double?> doubles = toList(typeof(double?), value);
					foreach (Double d in doubles)
					{
						if (d != null) // necessary because .,. => [null, null] in VC
						{
							encoder.encodeRawFloat(d);
							count++;
						}
					}
				}
				for (; count < minValues; count++)
				{
					encoder.encodeRawMissingValue(type);
				}
			}
		}

		// ----------------------------------------------------------------------
		//
		// Subclass to encode int[]
		//
		// ----------------------------------------------------------------------

		public class IntArray : BCF2FieldEncoder
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public IntArray(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict)
			public IntArray(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict) : base(headerLine, dict, null)
			{
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: protected int numElementsFromValue(final Object value)
			protected internal override int numElementsFromValue(object value)
			{
				return value == null ? 0 : ((int[])value).Length;
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public org.broadinstitute.variant.bcf2.BCF2Type getDynamicType(final Object value)
			public override BCF2Type getDynamicType(object value)
			{
				return value == null ? BCF2Type.INT8 : BCF2Utils.determineIntegerType((int[])value);
			}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("value == null || ((int[])value).length <= minValues") @Override public void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues)
			{
				int count = 0;
				if (value != null)
				{
					foreach (int i in (@int[])value)
					{
						encoder.encodeRawInt(i, type);
						count++;
					}
				}
				for (; count < minValues; count++)
				{
					encoder.encodeRawMissingValue(type);
				}
			}
		}

		// ----------------------------------------------------------------------
		//
		// Subclass to encode List<Integer>
		//
		// ----------------------------------------------------------------------

		/// <summary>
		/// Specialized int encoder for atomic (non-list) integers
		/// </summary>
		public class AtomicInt : BCF2FieldEncoder
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public AtomicInt(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict)
			public AtomicInt(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict) : base(headerLine, dict, null)
			{
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public org.broadinstitute.variant.bcf2.BCF2Type getDynamicType(final Object value)
			public override BCF2Type getDynamicType(object value)
			{
				return value == null ? BCF2Type.INT8 : BCF2Utils.determineIntegerType((int?)value);
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues)
			{
				int count = 0;
				if (value != null)
				{
					encoder.encodeRawInt((int?)value, type);
					count++;
				}
				for (; count < minValues; count++)
				{
					encoder.encodeRawMissingValue(type);
				}
			}
		}

		public class GenericInts : BCF2FieldEncoder
		{
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public GenericInts(final Bio.VCF.VCFCompoundHeaderLine headerLine, final java.util.Map<String, Integer> dict)
			public GenericInts(VCFCompoundHeaderLine headerLine, IDictionary<string, int?> dict) : base(headerLine, dict, null)
			{
			}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: public org.broadinstitute.variant.bcf2.BCF2Type getDynamicType(final Object value)
			public override BCF2Type getDynamicType(object value)
			{
				return value == null ? BCF2Type.INT8 : BCF2Utils.determineIntegerType(toList(typeof(int?), value));
			}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void encodeValue(final BCF2Encoder encoder, final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type, final int minValues) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
			public override void encodeValue(BCF2Encoder encoder, object value, BCF2Type type, int minValues)
			{
				int count = 0;
				foreach (Integer i in toList(typeof(Integer), value))
				{
					if (i != null) // necessary because .,. => [null, null] in VC
					{
						encoder.encodeRawInt(i, type);
						count++;
					}
				}
				for (; count < minValues; count++)
				{
					encoder.encodeRawMissingValue(type);
				}
			}
		}


		// ----------------------------------------------------------------------
		//
		// Helper methods
		//
		// ----------------------------------------------------------------------

		/// <summary>
		/// Helper function that takes an object and returns a list representation
		/// of it:
		/// 
		/// o == null => []
		/// o is a list => o
		/// else => [o]
		/// </summary>
		/// <param name="o">
		/// @return </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
//ORIGINAL LINE: private final static <T> java.util.List<T> toList(final Class c, final Object o)
		private static IList<T> toList<T>(Type c, object o)
		{
			if (o == null)
			{
				return Collections.emptyList();
			}
			else if (o is IList)
			{
				return (IList<T>)o;
			}
			else
			{
				return Collections.singletonList((T)o);
			}
		}
	}

}