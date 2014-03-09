using System;
using System.Collections.Generic;

namespace Bio.VCF
{
	/// <summary>
	/// a base class for compound header lines, which include info lines and format lines (so far)
	/// </summary>
	public abstract class VCFCompoundHeaderLine : VCFHeaderLine, IVCFIDHeaderLine
	{

		public enum SupportedHeaderLineType
		{
			INFO = 0,
			FORMAT = 1
		}

		// the field types
		private string name;
		private int count = -1;
		private VCFHeaderLineCount countType;
		private string description;
		private VCFHeaderLineType type;

		// access methods
		public virtual string ID
		{
			get
			{
				return name;
			}
		}
		public virtual string Description
		{
			get
			{
				return description;
			}
		}
		public virtual VCFHeaderLineType Type
		{
			get
			{
				return type;
			}
		}
		public virtual VCFHeaderLineCount CountType
		{
			get
			{
				return countType;
			}
		}
		public virtual bool FixedCount
		{
			get
			{
				return countType == VCFHeaderLineCount.INTEGER;
			}
		}
		public virtual int Count
		{
			get
			{
				if (!FixedCount)
				{
					throw new VCFParsingError("Asking for header line count when type is not an integer");
				}
				return count;
			}
		}

		/// <summary>
		/// Get the number of values expected for this header field, given the properties of VariantContext vc
		/// 
		/// If the count is a fixed count, return that.  For example, a field with size of 1 in the header returns 1
		/// If the count is of type A, return vc.getNAlleles - 1
		/// If the count is of type G, return the expected number of genotypes given the number of alleles in VC and the
		///   max ploidy among all samples.  Note that if the max ploidy of the VC is 0 (there's no GT information
		///   at all, then implicitly assume diploid samples when computing G values.
		/// If the count is UNBOUNDED return -1
		/// </summary>
		/// <param name="vc">
		/// @return </param>
		public virtual int getCount(VariantContext vc)
		{
			switch (countType)
			{
				case Bio.VCF.VCFHeaderLineCount.INTEGER:
					return count;
				case Bio.VCF.VCFHeaderLineCount.UNBOUNDED:
					return -1;
				case Bio.VCF.VCFHeaderLineCount.A:
					return vc.NAlleles - 1;
				case Bio.VCF.VCFHeaderLineCount.G:
					int ploidy = vc.GetMaxPloidy(2);
					return GenotypeLikelihoods.numLikelihoods(vc.NAlleles, ploidy);
				default:
					throw new VCFParsingError("Unknown count type: " + countType);
			}
		}

		public virtual void setNumberToUnbounded()
		{
			countType = VCFHeaderLineCount.UNBOUNDED;
			count = -1;
		}

		// our type of line, i.e. format, info, etc
		private readonly SupportedHeaderLineType lineType;

		/// <summary>
		/// create a VCF format header line
		/// </summary>
		/// <param name="name">         the name for this header line </param>
		/// <param name="count">        the count for this header line </param>
		/// <param name="type">         the type for this header line </param>
		/// <param name="description">  the description for this header line </param>
		/// <param name="lineType">     the header line type </param>
		protected internal VCFCompoundHeaderLine(string name, int count, VCFHeaderLineType type, string description, SupportedHeaderLineType lineType) : base(lineType.ToString(), "")
		{
			this.name = name;
			this.countType = VCFHeaderLineCount.INTEGER;
			this.count = count;
			this.type = type;
			this.description = description;
			this.lineType = lineType;
			validate();
		}

		/// <summary>
		/// create a VCF format header line
		/// </summary>
		/// <param name="name">         the name for this header line </param>
		/// <param name="count">        the count type for this header line </param>
		/// <param name="type">         the type for this header line </param>
		/// <param name="description">  the description for this header line </param>
		/// <param name="lineType">     the header line type </param>
		protected internal VCFCompoundHeaderLine(string name, VCFHeaderLineCount count, VCFHeaderLineType type, string description, SupportedHeaderLineType lineType) : base(lineType.ToString(), "")
		{
			this.name = name;
			this.countType = count;
			this.type = type;
			this.description = description;
			this.lineType = lineType;
			validate();
		}

		/// <summary>
		/// create a VCF format header line
		/// </summary>
		/// <param name="line">   the header line </param>
		/// <param name="version">      the VCF header version </param>
		/// <param name="lineType">     the header line type
		///  </param>
		protected internal VCFCompoundHeaderLine(string line, VCFHeaderVersion version, SupportedHeaderLineType lineType) : base(lineType.ToString(), "")
		{
			IDictionary<string, string> mapping = VCFHeaderLineTranslator.parseLine(version,line, "ID","Number","Type","Description");
			name = mapping["ID"];
			count = -1;
			string numberStr = mapping["Number"];
			if (numberStr.Equals(VCFConstants.PER_ALLELE_COUNT))
			{
				countType = VCFHeaderLineCount.A;
			}
			else if (numberStr.Equals(VCFConstants.PER_GENOTYPE_COUNT))
			{
				countType = VCFHeaderLineCount.G;
			}
			else if (((version == VCFHeaderVersion.VCF4_0 || version == VCFHeaderVersion.VCF4_1) && numberStr.Equals(VCFConstants.UNBOUNDED_ENCODING_v4)) || ((version == VCFHeaderVersion.VCF3_2 || version == VCFHeaderVersion.VCF3_3) && numberStr.Equals(VCFConstants.UNBOUNDED_ENCODING_v3)))
			{
				countType = VCFHeaderLineCount.UNBOUNDED;
			}
			else
			{
				countType = VCFHeaderLineCount.INTEGER;
				count = Convert.ToInt32(numberStr);
            }
			if (count < 0 && countType == VCFHeaderLineCount.INTEGER)
			{
				throw new VCFParsingError("Count < 0 for fixed size VCF header field " + name);
			}
			try
			{
				type = (VCFHeaderLineType) Enum.Parse(typeof(VCFHeaderLineType), mapping["Type"]);
			}
			catch (Exception e)
			{
				throw new VCFParsingError(mapping["Type"] + " is not a valid type in the VCF specification (note that types are case-sensitive)");
			}
			if (type == VCFHeaderLineType.Flag && !allowFlagValues())
			{
				throw new System.ArgumentException("Flag is an unsupported type for this kind of field");
			}

			description = mapping["Description"];
			if (description == null && ALLOW_UNBOUND_DESCRIPTIONS) // handle the case where there's no description provided
			{
				description = UNBOUND_DESCRIPTION;
			}

			this.lineType = lineType;
			validate();
		}

		private void validate()
		{
			if (name == null || Type == null || description == null || lineType == null)
			{
				throw new System.ArgumentException(string.Format("Invalid VCFCompoundHeaderLine: key={0} name={1} type={2} desc={3} lineType={4}", base.Key, name, type, description, lineType));
			}
			if (name.Contains("<") || name.Contains(">"))
			{
				throw new System.ArgumentException("VCFHeaderLine: ID cannot contain angle brackets");
			}
			if (name.Contains("="))
			{
				throw new System.ArgumentException("VCFHeaderLine: ID cannot contain an equals sign");
			}

			if (type == VCFHeaderLineType.Flag && count != 0)
			{
				count = 0;
				throw new VCFParsingError("FLAG fields must have a count value of 0, but saw " + count + " for header line " + ID + ". Changing it to 0 inside the code");
            }
		}

		/// <summary>
		/// make a string representation of this header line </summary>
		/// <returns> a string representation </returns>
		protected internal override string toStringEncoding()
		{
			IDictionary<string, object> map = new OrderedGenericDictionary<string, object>();
			map["ID"] = name;
			object number;
			switch (countType)
			{
				case Bio.VCF.VCFHeaderLineCount.A:
					number = VCFConstants.PER_ALLELE_COUNT;
					break;
				case Bio.VCF.VCFHeaderLineCount.G:
					number = VCFConstants.PER_GENOTYPE_COUNT;
					break;
				case Bio.VCF.VCFHeaderLineCount.UNBOUNDED:
					number = VCFConstants.UNBOUNDED_ENCODING_v4;
					break;
				case Bio.VCF.VCFHeaderLineCount.INTEGER:
				default:
					number = count;
				break;
			}
			map["Number"] = number;
			map["Type"] = type;
			map["Description"] = description;
			return lineType.ToString() + "=" + VCFHeaderLine.toStringEncoding(map);
		}

		/// <summary>
		/// returns true if we're equal to another compounder header line </summary>
		/// <param name="o"> a compound header line </param>
		/// <returns> true if equal </returns>
		public override bool Equals(object o)
		{
			if (!(o is VCFCompoundHeaderLine))
			{
				return false;
			}
			VCFCompoundHeaderLine other = (VCFCompoundHeaderLine)o;
			return equalsExcludingDescription(other) && description.Equals(other.description);
		}

		public virtual bool equalsExcludingDescription(VCFCompoundHeaderLine other)
		{
			return count == other.count && countType == other.countType && type == other.type && lineType == other.lineType && name.Equals(other.name);
		}

		public virtual bool sameLineTypeAndName(VCFCompoundHeaderLine other)
		{
			return lineType == other.lineType && name.Equals(other.name);
		}

		/// <summary>
		/// do we allow flag (boolean) values? (i.e. booleans where you don't have specify the value, AQ means AQ=true) </summary>
		/// <returns> true if we do, false otherwise </returns>
		internal abstract bool allowFlagValues();

	}

}