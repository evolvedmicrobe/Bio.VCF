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
	using Requires = com.google.java.contract.Requires;
	using BCF2Type = org.broadinstitute.variant.bcf2.BCF2Type;
	using BCF2Utils = org.broadinstitute.variant.bcf2.BCF2Utils;


	/// <summary>
	/// See #BCFWriter for documentation on this classes role in encoding BCF2 files
	/// 
	/// @author Mark DePristo
	/// @since 06/12
	/// </summary>
	public sealed class BCF2Encoder
	{
		// TODO -- increase default size?
		public const int WRITE_BUFFER_INITIAL_SIZE = 16384;
		private ByteArrayOutputStream encodeStream = new ByteArrayOutputStream(WRITE_BUFFER_INITIAL_SIZE);

		// --------------------------------------------------------------------------------
		//
		// Functions to return the data being encoded here
		//
		// --------------------------------------------------------------------------------

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("result != null") public byte[] getRecordBytes()
		public sbyte[] RecordBytes
		{
			get
			{
				sbyte[] bytes = encodeStream.toByteArray();
				encodeStream.reset();
				return bytes;
			}
		}

		// --------------------------------------------------------------------------------
		//
		// Writing typed values (have type byte)
		//
		// --------------------------------------------------------------------------------

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTypedMissing(final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTypedMissing(BCF2Type type)
		{
			encodeType(0, type);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTyped(final Object value, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTyped(object value, BCF2Type type)
		{
			if (value == null)
			{
				encodeTypedMissing(type);
			}
			else
			{
				switch (type)
				{
					case BCF2Type.INT8:
					case BCF2Type.INT16:
					case BCF2Type.INT32:
						encodeTypedInt((int?)value, type);
						break;
					case BCF2Type.FLOAT:
						encodeTypedFloat((double?) value);
						break;
					case BCF2Type.CHAR:
						encodeTypedString((string) value);
						break;
					default:
						throw new System.ArgumentException("Illegal type encountered " + type);
				}
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTypedInt(final int v) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTypedInt(int v)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Type type = org.broadinstitute.variant.bcf2.BCF2Utils.determineIntegerType(v);
			BCF2Type type = BCF2Utils.determineIntegerType(v);
			encodeTypedInt(v, type);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("type.isIntegerType()") @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTypedInt(final int v, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTypedInt(int v, BCF2Type type)
		{
			encodeType(1, type);
			encodeRawInt(v, type);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTypedString(final String s) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTypedString(string s)
		{
			encodeTypedString(s.Bytes);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTypedString(final byte[] s) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTypedString(sbyte[] s)
		{
			if (s == null)
			{
				encodeType(0, BCF2Type.CHAR);
			}
			else
			{
				encodeType(s.Length, BCF2Type.CHAR);
				for (int i = 0; i < s.Length; i++)
				{
					encodeRawChar(s[i]);
				}
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTypedFloat(final double d) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTypedFloat(double d)
		{
			encodeType(1, BCF2Type.FLOAT);
			encodeRawFloat(d);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeTyped(List<? extends Object> v, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeTyped<T1>(IList<T1> v, BCF2Type type) where T1 : Object
		{
			if (type == BCF2Type.CHAR && v.Count != 0)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String s = org.broadinstitute.variant.bcf2.BCF2Utils.collapseStringList((List<String>) v);
				string s = BCF2Utils.collapseStringList((IList<string>) v);
				v = stringToBytes(s);
			}

			encodeType(v.Count, type);
			encodeRawValues(v, type);
		}

		// --------------------------------------------------------------------------------
		//
		// Writing raw values (don't have a type byte)
		//
		// --------------------------------------------------------------------------------

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final <T extends Object> void encodeRawValues(final Collection<T> v, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawValues<T>(ICollection<T> v, BCF2Type type) where T : Object
		{
			foreach (T v1 in v)
			{
				encodeRawValue(v1, type);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final <T extends Object> void encodeRawValue(final T value, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawValue<T>(T value, BCF2Type type) where T : Object
		{
			try
			{
				if (value == type.MissingJavaValue)
				{
					encodeRawMissingValue(type);
				}
				else
				{
					switch (type)
					{
						case BCF2Type.INT8:
						case BCF2Type.INT16:
						case BCF2Type.INT32:
							encodeRawBytes((int?) value, type);
							break;
						case BCF2Type.FLOAT:
							encodeRawFloat((double?) value);
							break;
						case BCF2Type.CHAR:
							encodeRawChar((sbyte?) value);
							break;
						default:
							throw new System.ArgumentException("Illegal type encountered " + type);
					}
				}
			}
			catch (ClassCastException e)
			{
				throw new ClassCastException("BUG: invalid type cast to " + type + " from " + value);
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeRawMissingValue(final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawMissingValue(BCF2Type type)
		{
			encodeRawBytes(type.MissingBytes, type);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("size >= 0") public final void encodeRawMissingValues(final int size, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawMissingValues(int size, BCF2Type type)
		{
			for (int i = 0; i < size; i++)
			{
				encodeRawMissingValue(type);
			}
		}

		// --------------------------------------------------------------------------------
		//
		// low-level encoders
		//
		// --------------------------------------------------------------------------------

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void encodeRawChar(final byte c) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawChar(sbyte c)
		{
			encodeStream.write(c);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public final void encodeRawFloat(final double value) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawFloat(double value)
		{
			encodeRawBytes(float.floatToIntBits((float) value), BCF2Type.FLOAT);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("size >= 0") @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeType(final int size, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeType(int size, BCF2Type type)
		{
			if (size <= BCF2Utils.MAX_INLINE_ELEMENTS)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int typeByte = org.broadinstitute.variant.bcf2.BCF2Utils.encodeTypeDescriptor(size, type);
				int typeByte = BCF2Utils.encodeTypeDescriptor(size, type);
				encodeStream.write(typeByte);
			}
			else
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int typeByte = org.broadinstitute.variant.bcf2.BCF2Utils.encodeTypeDescriptor(org.broadinstitute.variant.bcf2.BCF2Utils.OVERFLOW_ELEMENT_MARKER, type);
				int typeByte = BCF2Utils.encodeTypeDescriptor(BCF2Utils.OVERFLOW_ELEMENT_MARKER, type);
				encodeStream.write(typeByte);
				// write in the overflow size
				encodeTypedInt(size);
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeRawInt(final int value, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawInt(int value, BCF2Type type)
		{
			type.write(value, encodeStream);
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Ensures("encodeStream.size() > old(encodeStream.size())") public final void encodeRawBytes(final int value, final org.broadinstitute.variant.bcf2.BCF2Type type) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawBytes(int value, BCF2Type type)
		{
			type.write(value, encodeStream);
		}

		// --------------------------------------------------------------------------------
		//
		// utility functions
		//
		// --------------------------------------------------------------------------------

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires({"s != null", "sizeToWrite >= 0"}) public void encodeRawString(final String s, final int sizeToWrite) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public void encodeRawString(string s, int sizeToWrite)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] bytes = s.getBytes();
			sbyte[] bytes = s.Bytes;
			for (int i = 0; i < sizeToWrite; i++)
			{
				if (i < bytes.Length)
				{
					encodeRawChar(bytes[i]);
				}
				else
				{
					encodeRawMissingValue(BCF2Type.CHAR);
				}
			}
		}

		/// <summary>
		/// Totally generic encoder that examines o, determines the best way to encode it, and encodes it
		/// 
		/// This method is incredibly slow, but it's only used for UnitTests so it doesn't matter
		/// </summary>
		/// <param name="o">
		/// @return </param>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("o != null") public final org.broadinstitute.variant.bcf2.BCF2Type encode(final Object o) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		public BCF2Type encode(object o)
		{
			if (o == null)
			{
				throw new System.ArgumentException("Generic encode cannot deal with null values");
			}

			if (o is IList)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Type type = determineBCFType(((List) o).get(0));
				BCF2Type type = determineBCFType(((IList) o)[0]);
				encodeTyped((IList) o, type);
				return type;
			}
			else
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.broadinstitute.variant.bcf2.BCF2Type type = determineBCFType(o);
				BCF2Type type = determineBCFType(o);
				encodeTyped(o, type);
				return type;
			}
		}

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Requires("arg != null") private final org.broadinstitute.variant.bcf2.BCF2Type determineBCFType(final Object arg)
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private BCF2Type determineBCFType(object arg)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object toType = arg instanceof List ? ((List)arg).get(0) : arg;
			object toType = arg is IList ? ((IList)arg)[0] : arg;

			if (toType is int?)
			{
				return BCF2Utils.determineIntegerType((int?) toType);
			}
			else if (toType is string)
			{
				return BCF2Type.CHAR;
			}
			else if (toType is double?)
			{
				return BCF2Type.FLOAT;
			}
			else
			{
				throw new System.ArgumentException("No native encoding for Object of type " + arg.GetType().Name);
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private final List<Byte> stringToBytes(final String v) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not allowed in .NET:
		private IList<sbyte?> stringToBytes(string v)
		{
			if (v == null || v.Equals(""))
			{
				return Collections.emptyList();
			}
			else
			{
				// TODO -- this needs to be optimized away for efficiency
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] bytes = v.getBytes();
				sbyte[] bytes = v.Bytes;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final List<Byte> l = new ArrayList<Byte>(bytes.length);
				IList<sbyte?> l = new List<sbyte?>(bytes.Length);
				for (int i = 0; i < bytes.Length; i++)
				{
					l.Add(bytes[i]);
				}
				return l;
			}
		}
	}
}