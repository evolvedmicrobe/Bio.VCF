using System;
using System.Collections;
using System.Collections.Generic;

namespace Bio.VCF
{
	/// <summary>
	///
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