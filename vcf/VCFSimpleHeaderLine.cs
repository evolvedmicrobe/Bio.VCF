using System.Collections.Generic;

namespace Bio.VCF
{
	/// <summary>
	/// A class representing a key=value entry for simple VCF header types
	/// </summary>
	public class VCFSimpleHeaderLine : VCFHeaderLine, IVCFIDHeaderLine
	{

		private string name;
        private OrderedGenericDictionary<string, string> genericFields = new OrderedGenericDictionary<string, string>();

		/// <summary>
		/// create a VCF filter header line
		/// </summary>
		/// <param name="key">            the key for this header line </param>
		/// <param name="name">           the name for this header line </param>
		/// <param name="description">    description for this header line </param>
		public VCFSimpleHeaderLine(string key, string name, string description) : base(key, "")
		{
            IDictionary<string, string> map = new OrderedGenericDictionary<string, string>();
			map["Description"] = description;
			initialize(name, map);
		}

		/// <summary>
		/// create a VCF info header line
		/// </summary>
		/// <param name="line">      the header line </param>
		/// <param name="version">   the vcf header version </param>
		/// <param name="key">            the key for this header line </param>
		/// <param name="expectedTagOrdering"> the tag ordering expected for this header line </param>
		public VCFSimpleHeaderLine(string line, VCFHeaderVersion version, string key, params string[] expectedTagOrdering) 
            : this(key, VCFHeaderLineTranslator.parseLine(version, line, expectedTagOrdering), expectedTagOrdering)
		{

		}

		public VCFSimpleHeaderLine(string key, IDictionary<string, string> mapping, params string[] expectedTagOrdering) : base(key, "")
		{
			name = mapping["ID"];
			initialize(name, mapping);
		}

		protected internal void initialize(string name, IDictionary<string, string> genericFields)
		{
			if (name == null || genericFields == null || genericFields.Count == 0)
			{
				throw new System.ArgumentException(string.Format("Invalid VCFSimpleHeaderLine: key={0} name={1}", base.Key, name));
			}
			if (name.Contains("<") || name.Contains(">"))
			{
				throw new System.ArgumentException("VCFHeaderLine: ID cannot contain angle brackets");
			}
			if (name.Contains("="))
			{
				throw new System.ArgumentException("VCFHeaderLine: ID cannot contain an equals sign");
			}

			this.name = name;
			this.genericFields.putAll(genericFields);
            
		}

		protected internal override string toStringEncoding()
		{
            OrderedGenericDictionary<string, string> map = new OrderedGenericDictionary<string, string>();
			map["ID"] = name;
			map.putAll(genericFields);
			return Key + "=" + VCFHeaderLine.toStringEncoding(map);
		}

		public override bool Equals(object o)
		{
			if (!(o is VCFSimpleHeaderLine))
			{
				return false;
			}
			VCFSimpleHeaderLine other = (VCFSimpleHeaderLine)o;
			if (!name.Equals(other.name) || genericFields.Count != other.genericFields.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, string> entry in genericFields)
			{
				if (!entry.Value.Equals(other.genericFields[entry.Key]))
				{
					return false;
				}
			}

			return true;
		}

		public virtual string ID
		{
			get
			{
				return name;
			}
		}
	}


}