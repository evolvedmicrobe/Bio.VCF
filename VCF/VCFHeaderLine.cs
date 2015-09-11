using System;
using System.Collections.Generic;
using System.Text;

namespace Bio.VCF
{

	/// <summary>
	/// @author ebanks
	///         <p/>
	///         Class VCFHeaderLine
	///         <p/>
	///         A class representing a key=value entry in the VCF header
	/// </summary>
	public class VCFHeaderLine : IComparable
	{
		protected internal const bool ALLOW_UNBOUND_DESCRIPTIONS = true;
		protected internal const string UNBOUND_DESCRIPTION = "Not provided in original VCF header";

		/// <summary>
		/// create a VCF header line
		/// </summary>
		/// <param name="key">     the key for this header line </param>
		/// <param name="value">   the value for this header line </param>
		public VCFHeaderLine(string key, string value)
		{
			if (key == null)
			{
				throw new System.ArgumentException("VCFHeaderLine: key cannot be null");
			}
			if (key.Contains("<") || key.Contains(">"))
			{
				throw new System.ArgumentException("VCFHeaderLine: key cannot contain angle brackets");
			}
			if (key.Contains("="))
			{
				throw new System.ArgumentException("VCFHeaderLine: key cannot contain an equals sign");
			}
			Key = key;
			Value = value;
		}

		/// <summary>
		/// Get the key
		/// </summary>
		/// <returns> the key </returns>
        public virtual string Key
        {
            private set;get;
        }

		/// <summary>
		/// Get the value
		/// </summary>
		/// <returns> the value </returns>
        public virtual string Value
        {
            private set;
            get;
        }

		public virtual string ToString()
		{
			return toStringEncoding();
		}

		/// <summary>
		/// Should be overloaded in sub classes to do subclass specific
		/// </summary>
		/// <returns> the string encoding </returns>
		protected internal virtual string toStringEncoding()
		{
			return Key + "=" + Value;
		}

		public virtual bool Equals(object o)
		{
			if (!(o is VCFHeaderLine))
			{
				return false;
			}
			return Key.Equals(((VCFHeaderLine)o).Key) && Value.Equals(((VCFHeaderLine)o).Value);
		}

		public virtual int CompareTo(object other)
		{
			return ToString().CompareTo(other.ToString());
		}

		/// <param name="line">    the line </param>
		/// <returns> true if the line is a VCF meta data line, or false if it is not </returns>
		public static bool isHeaderLine(string line)
		{
			return line != null && line.Length > 0 && VCFHeader.HEADER_INDICATOR.Equals(line.Substring(0,1));
		}

		/// <summary>
		/// create a string of a mapping pair for the target VCF version </summary>
		/// <param name="keyValues"> a mapping of the key->value pairs to output </param>
		/// <returns> a string, correctly formatted </returns>
		public static string toStringEncoding<T1>(IDictionary<string,T1> keyValues)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("<");
			bool start = true;
			foreach (KeyValuePair<string, T1> entry in keyValues)
			{
				if (start)
				{
					start = false;
				}
				else
				{
					builder.Append(",");
				}

				if (entry.Value == null)
				{
					throw new VCFParsingError("Header problem: unbound value at " + entry + " from " + keyValues);
				}

				builder.Append(entry.Key);
				builder.Append("=");
				builder.Append(entry.Value.ToString().Contains(",") || entry.Value.ToString().Contains(" ") || entry.Key.Equals("Description") ? "\"" + entry.Value.ToString() + "\"" : entry.Value.ToString());
			}
			builder.Append(">");
			return builder.ToString();
		}
	}
}