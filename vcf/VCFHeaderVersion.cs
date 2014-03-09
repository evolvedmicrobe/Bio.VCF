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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Bio.VCF
{

	
	/// <summary>
	/// information that identifies each header version
	/// </summary>
	public class VCFHeaderVersion
	{
		public static readonly VCFHeaderVersion VCF3_2=new VCFHeaderVersion("VCRv3.2","format");
		public static readonly VCFHeaderVersion VCF3_3=new VCFHeaderVersion("VCFv3.3","fileformat");
		public static readonly VCFHeaderVersion VCF4_0=new VCFHeaderVersion("VCFv4.0","fileformat");
        public static readonly VCFHeaderVersion VCF4_1 = new VCFHeaderVersion("VCFv4.1", "fileformat");
        public static VCFHeaderVersion[] PossibleVersions=new VCFHeaderVersion[] {VCF3_2,VCF3_3,VCF4_0,VCF4_1};
		
        public String VersionString {get;private set;}
        public String FormatString { get; private set; }

		/// <summary>
		/// create the enum, privately, using: </summary>
		/// <param name="vString"> the version string </param>
		/// <param name="fString"> the format string </param>
		public VCFHeaderVersion(string vString, string fString)
		{
			this.VersionString = vString;
			this.FormatString = fString;
		}

		/// <summary>
		/// get the header version </summary>
		/// <param name="version"> the version string </param>
		/// <returns> a VCFHeaderVersion object </returns>
		public static VCFHeaderVersion ToHeaderVersion(String version)
		{
			version = clean(version);
			var versions=from x in PossibleVersions where version==x.VersionString select x;
            return versions.FirstOrDefault();
		}

		/// <summary>
		/// are we a valid version string of some type </summary>
		/// <param name="version"> the version string </param>
		/// <returns> true if we're valid of some type, false otherwise </returns>
        public static bool IsVersionString(String version)
        {
            return ToHeaderVersion(version) != null;
        }

		/// <summary>
		/// are we a valid format string for some type </summary>
		/// <param name="format"> the format string </param>
		/// <returns> true if we're valid of some type, false otherwise </returns>
		public static bool IsFormatString(String format)
		{
			format = clean(format);
            return PossibleVersions.Count(x=>x.FormatString==format)>0;
		}
		public static VCFHeaderVersion GetHeaderVersion(String versionLine)
        {
            versionLine = clean(versionLine);
            String[] lineFields = versionLine.Split('=');
            if (lineFields.Length != 2 || !IsFormatString(lineFields[0].Substring(2)))
                throw new VCFParsingError(versionLine + " is not a valid VCF version line");
            VCFHeaderVersion vcfHV = ToHeaderVersion(lineFields[1]);
            if (vcfHV==null)
                throw new VCFParsingError(lineFields[1] + " is not a supported version");
            return vcfHV;
        }

		/// <summary>
		/// Utility function to clean up a VCF header string
		/// </summary>
		/// <param name="s"> string </param>
		/// <returns>  trimmed version of s </returns>
		private static String clean(String s)
		{
			return s.Trim();
		}



	}


}