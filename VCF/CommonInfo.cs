using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.ObjectModel;

namespace Bio.VCF
{

	/// <summary>
	/// Common utility routines for VariantContext and Genotype
	/// 
	/// @author depristo
	/// </summary>
	public sealed class CommonInfo
	{
		public const double NO_LOG10_PERROR = 1.0;

        private static ISet<string> NO_FILTERS = new HashSet<string>();
		private static IDictionary<string, object> NO_ATTRIBUTES = new Dictionary<string, object>();

		private double log10PError = NO_LOG10_PERROR;
		private string name = null;
		private ISet<string> filters = null;
		private IDictionary<string, object> attributes = NO_ATTRIBUTES;

		public CommonInfo(string name, double log10PError, ISet<string> filters, IDictionary<string, object> attributes)
		{
			this.name = name;
			Log10PError = log10PError;
			this.filters = filters;
			if (attributes != null && attributes.Count > 0)
			{
				this.attributes = attributes;
			}
		}

		/// <returns> the name </returns>
		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				if (value == null)
				{
					throw new System.ArgumentException("Name cannot be null " + this);
				}
				this.name = value;
			}
		}



		// ---------------------------------------------------------------------------------------------------------
		//
		// Filter
		//
		// ---------------------------------------------------------------------------------------------------------

		public ISet<string> FiltersMaybeNull
		{
			get
			{
				return filters;
			}
		}

		public ISet<string> Filters
		{
			get
			{
                return filters == null ? NO_FILTERS : new ImmutableHashSet<string>();
			}
		}

		public bool filtersWereApplied()
		{
			return filters != null;
		}

		public bool Filtered
		{
			get
			{
				return filters == null ? false : filters.Count > 0;
			}
		}

		public bool NotFiltered
		{
			get
			{
				return !Filtered;
			}
		}

		public void addFilter(string filter)
		{
			if (filters == null) // immutable -> mutable
			{
				filters = new HashSet<string>();
			}

			if (filter == null)
			{
				throw new System.ArgumentException("BUG: Attempting to add null filter " + this);
			}
			if (Filters.Contains(filter))
			{
				throw new System.ArgumentException("BUG: Attempting to add duplicate filter " + filter + " at " + this);
			}
			filters.Add(filter);
		}

		public void addFilters(ICollection<string> filters)
		{
			if (filters == null)
			{
				throw new System.ArgumentException("BUG: Attempting to add null filters at" + this);
			}
			foreach (string f in filters)
			{
				addFilter(f);
			}
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// Working with log error rates
		//
		// ---------------------------------------------------------------------------------------------------------

		public bool hasLog10PError()
		{
			return Log10PError != NO_LOG10_PERROR;
		}

		/// <returns> the -1 * log10-based error estimate </returns>
		public double Log10PError
		{
			get
			{
				return log10PError;
			}
			set
			{
				if (value > 0 && value != NO_LOG10_PERROR)
				{
					throw new System.ArgumentException("BUG: log10PError cannot be > 0 : " + this.log10PError);
				}
				if (double.IsInfinity(this.log10PError))
				{
					throw new System.ArgumentException("BUG: log10PError should not be Infinity");
				}
				if (double.IsNaN(this.log10PError))
				{
					throw new System.ArgumentException("BUG: log10PError should not be NaN");
				}
				this.log10PError = value;
			}
		}
		public double PhredScaledQual
		{
			get
			{
				return Log10PError * -10;
			}
		}


		// ---------------------------------------------------------------------------------------------------------
		//
		// Working with attributes
		//
		// ---------------------------------------------------------------------------------------------------------
		public void clearAttributes()
		{
			attributes = new Dictionary<string, object>();
		}

		/// <returns> the attribute map </returns>
		public IDictionary<string, object> Attributes
		{
			get
			{
                return new ReadOnlyDictionary<string, object>(attributes);
			}
			set
			{
				clearAttributes();
				putAttributes(value);
			}
		}

		// todo -- define common attributes as enum


		public void putAttribute(string key, object value)
		{
			putAttribute(key, value, false);
		}

		public void putAttribute(string key, object value, bool allowOverwrites)
		{
			if (!allowOverwrites && hasAttribute(key))
			{
				throw new Exception("Attempting to overwrite key->value binding: key = " + key + " this = " + this);
			}

			if (attributes == NO_ATTRIBUTES) // immutable -> mutable
			{
				attributes = new Dictionary<string, object>();
			}

			attributes[key] = value;
		}

		public void removeAttribute(string key)
		{
			if (attributes == NO_ATTRIBUTES) // immutable -> mutable
			{
				attributes = new Dictionary<string, object>();
			}
			attributes.Remove(key);
		}

		public void putAttributes(IDictionary<string,object> map)
		{
			if (map != null)
			{
				// for efficiency, we can skip the validation if the map is empty
				if (attributes.Count == 0)
				{
					if (attributes == NO_ATTRIBUTES) // immutable -> mutable
					{
						attributes = new Dictionary<string, object>(map);
					}
                }
				else
				{
					foreach (KeyValuePair<string, object> elt in map)
					{
						putAttribute(elt.Key, elt.Value, false);
					}
				}
			}
		}

		public bool hasAttribute(string key)
		{
			return attributes.ContainsKey(key);
		}

		public int NumAttributes
		{
			get
			{
				return attributes.Count;
			}
		}

		/// <param name="key">    the attribute key
		/// </param>
		/// <returns> the attribute value for the given key (or null if not set) </returns>
		public object getAttribute(string key)
		{
			return attributes[key];
		}

		public object getAttribute(string key, object defaultValue)
		{
			if (hasAttribute(key))
			{
				return attributes[key];
			}
			else
			{
				return defaultValue;
			}
		}

		public string getAttributeAsString(string key, string defaultValue)
		{
			object x = getAttribute(key);
			if (x == null)
			{
				return defaultValue;
			}
			if (x is string)
			{
				return (string)x;
			}
			return Convert.ToString(x); // throws an exception if this isn't a string
		}

		public int getAttributeAsInt(string key, int defaultValue)
		{
			object x = getAttribute(key);
			if (x == null || x == VCFConstants.MISSING_VALUE_v4)
			{
				return defaultValue;
			}
			if (x is int)
			{
				return (int)x;
			}
			return Convert.ToInt32((string)x); // throws an exception if this isn't a string
		}

		public double getAttributeAsDouble(string key, double defaultValue)
		{
			object x = getAttribute(key);
			if (x == null)
			{
				return defaultValue;
			}
			if (x is double)
			{
				return (double)x;
			}
			if (x is int)
			{
				return (int)x;
			}
			return Convert.ToDouble((string)x); // throws an exception if this isn't a string
		}

		public bool getAttributeAsBoolean(string key, bool defaultValue)
		{
			object x = getAttribute(key);
			if (x == null)
			{
				return defaultValue;
			}
			if (x is bool)
			{
				return (bool)x;
			}
			return Convert.ToBoolean((string)x); // throws an exception if this isn't a string
		}

	//    public String getAttributeAsString(String key)      { return (String.valueOf(getExtendedAttribute(key))); } // **NOTE**: will turn a null Object into the String "null"
	//    public int getAttributeAsInt(String key)            { Object x = getExtendedAttribute(key); return x instanceof Integer ? (Integer)x : Integer.valueOf((String)x); }
	//    public double getAttributeAsDouble(String key)      { Object x = getExtendedAttribute(key); return x instanceof Double ? (Double)x : Double.valueOf((String)x); }
	//    public boolean getAttributeAsBoolean(String key)      { Object x = getExtendedAttribute(key); return x instanceof Boolean ? (Boolean)x : Boolean.valueOf((String)x); }
	//    public Integer getAttributeAsIntegerNoException(String key)  { try {return getAttributeAsInt(key);} catch (Exception e) {return null;} }
	//    public Double getAttributeAsDoubleNoException(String key)    { try {return getAttributeAsDouble(key);} catch (Exception e) {return null;} }
	//    public String getAttributeAsStringNoException(String key)    { if (getExtendedAttribute(key) == null) return null; return getAttributeAsString(key); }
	//    public Boolean getAttributeAsBooleanNoException(String key)  { try {return getAttributeAsBoolean(key);} catch (Exception e) {return null;} }
	}
}