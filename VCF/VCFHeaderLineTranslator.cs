using System.Collections.Generic;
using System.Text;

namespace Bio.VCF
{
	/// <summary>
	/// A class for translating between vcf header versions
	/// </summary>
	public class VCFHeaderLineTranslator
	{
		private static IDictionary<VCFHeaderVersion, VCFLineParser> mapping;

		static VCFHeaderLineTranslator()
		{
			mapping = new Dictionary<VCFHeaderVersion, VCFLineParser>();
			mapping[VCFHeaderVersion.VCF4_0] = new VCF4Parser();
			mapping[VCFHeaderVersion.VCF4_1] = new VCF4Parser();
			mapping[VCFHeaderVersion.VCF3_3] = new VCF3Parser();
			mapping[VCFHeaderVersion.VCF3_2] = new VCF3Parser();
		}

		public static IDictionary<string, string> parseLine(VCFHeaderVersion version, string valueLine, params string[] expectedTagOrder)
		{
			return mapping[version].parseLine(valueLine,expectedTagOrder);
		}
	}

	internal interface VCFLineParser
	{
		IDictionary<string, string> parseLine(string valueLine,params string[] expectedTagOrder);
	}


	/// <summary>
	/// a class that handles the to and from disk for VCF 4 lines
	/// </summary>
	internal class VCF4Parser : VCFLineParser
	{
		/// <summary>
		/// parse a VCF4 line </summary>
		/// <param name="valueLine"> the line </param>
		/// <returns> a mapping of the tags parsed out </returns>
		public virtual IDictionary<string, string> parseLine(string valueLine, params string[] expectedTagOrder)
		{
			// our return map
			OrderedGenericDictionary<string, string> ret = new OrderedGenericDictionary<string, string>();

			// a builder to store up characters as we go
			StringBuilder builder = new StringBuilder();

			// store the key when we're parsing out the values
			string key = "";

			// where are we in the stream of characters?
			int index = 0;

			// are we inside a quotation? we don't special case ',' then
			bool inQuote = false;

			// a little switch machine to parse out the tags. Regex ended up being really complicated and ugly [yes, but this machine is getting ugly now... MAD]
			foreach (char c in valueLine.ToCharArray())
			{
				if (c == '\"')
				{
					inQuote = !inQuote;
				}
				else if (inQuote)
				{
					builder.Append(c);
				}
				else
				{
					switch (c)
					{
						case ('<') : // if we see a open bracket at the beginning, ignore it
							if (index == 0)
							{
								break;
							}
                            else
                                goto case '>';
						case ('>') : // if we see a close bracket, and we're at the end, add an entry to our list
							if (index == valueLine.Length - 1)
							{
								ret[key] = builder.ToString().Trim();
							}
								break;
						case ('=') : // at an equals, copy the key and reset the builder
							key = builder.ToString().Trim();
							builder = new StringBuilder();
							break;
						case (',') : // drop the current key value to the return map
							ret[key] = builder.ToString().Trim();
							builder = new StringBuilder();
							break;
						default: // otherwise simply append to the current string
							builder.Append(c);
						break;
					}
				}

				index++;
			}

			// validate the tags against the expected list
			index = 0;
			if (expectedTagOrder != null)
			{
				if (ret.Count > expectedTagOrder.Length)
				{
					throw new VCFParsingError("unexpected tag count " + ret.Count + " in line " + valueLine);
				}
				foreach (string str in ret.Keys)
				{
					if (!expectedTagOrder[index].Equals(str))
					{
						throw new VCFParsingError("Unexpected tag " + str + " in line " + valueLine);
					}
					index++;
				}
			}
			return ret;
		}
	}

	internal class VCF3Parser : VCFLineParser
	{

		public virtual IDictionary<string, string> parseLine(string valueLine, params string[] expectedTagOrder)
		{
			// our return map
            OrderedGenericDictionary<string, string> ret = new OrderedGenericDictionary<string, string>();

			// a builder to store up characters as we go
			StringBuilder builder = new StringBuilder();

			// where are we in the stream of characters?
			int index = 0;
			// where in the expected tag order are we?
			int tagIndex = 0;

			// are we inside a quotation? we don't special case ',' then
			bool inQuote = false;

			// a little switch machine to parse out the tags. Regex ended up being really complicated and ugly
			foreach (char c in valueLine.ToCharArray())
			{
				switch (c)
				{
					case ('\"') : // a quote means we ignore ',' in our strings, keep track of it
						inQuote = !inQuote;
						break;
					case (',') : // drop the current key value to the return map
						if (!inQuote)
						{
							ret[expectedTagOrder[tagIndex++]] = builder.ToString();
							builder = new StringBuilder();
							break;
						}
                        else
                            goto default;
					default: // otherwise simply append to the current string
						builder.Append(c);
					break;
				}
				index++;
			}
			ret[expectedTagOrder[tagIndex++]] = builder.ToString();

			// validate the tags against the expected list
			index = 0;
			if (tagIndex != expectedTagOrder.Length)
			{
				throw new System.ArgumentException("Unexpected tag count " + tagIndex + ", we expected " + expectedTagOrder.Length);
			}
			foreach (string str in ret.Keys)
			{
				if (!expectedTagOrder[index].Equals(str))
				{
					throw new System.ArgumentException("Unexpected tag " + str + " in string " + valueLine);
				}
				index++;
			}
			return ret;
		}
	}
}