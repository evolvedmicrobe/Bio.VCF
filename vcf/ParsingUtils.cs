using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Bio.VCF
{
	/// <summary>
	/// TODO: This class is pure java nonsense, remove and replace with standard framework calls later...
	/// </summary>
	public class ParsingUtils
	{
		/// <summary>
		/// Split the string into tokesn separated by the given delimiter.  Profiling has
		/// revealed that the standard string.split() method typically takes > 1/2
		/// the total time when used for parsing ascii files.
		/// </summary>
		/// <param name="aString"> the string to split </param>
		/// <param name="tokens">  an array to hold the parsed tokens </param>
		/// <param name="delim">   character that delimits tokens </param>
		/// <returns> the number of tokens parsed </returns>
		public static int Split (string aString, string[] tokens, char delim)
		{
			return Split (aString, tokens, delim, false);
		}

		/// <summary>
		/// Split the string into tokens separated by the given delimiter.  Profiling has
		/// revealed that the standard string.split() method typically takes > 1/2
		/// the total time when used for parsing ascii files.
		/// </summary>
		/// <param name="aString">                the string to split </param>
		/// <param name="tokens">                 an array to hold the parsed tokens </param>
		/// <param name="delim">                  character that delimits tokens </param>
		/// <param name="condenseTrailingTokens"> if true and there are more tokens than will fit in the tokens array,
		///                               condense all trailing tokens into the last token </param>
		/// <returns> the number of tokens parsed </returns>
		public static int Split (string aString, string[] tokens, char delim, bool condenseTrailingTokens)
		{

			int maxTokens = tokens.Length;
			int nTokens = 0;
			int start = 0;
			int end = aString.IndexOf (delim);
			if (end == 0) {
				if (aString.Length > 1) {
					start = 1;
					end = aString.IndexOf (delim, start);
				} else {
					return 0;
				}
			}

			if (end < 0) {
				tokens [nTokens++] = aString;
				return nTokens;
			}

			while ((end > 0) && (nTokens < maxTokens)) {
				tokens [nTokens++] = aString.Substring (start, end - start);
				start = end + 1;
				end = aString.IndexOf (delim, start);
			}

			// condense if appropriate
			if (condenseTrailingTokens && nTokens == maxTokens) {
				tokens [nTokens - 1] = tokens [nTokens - 1] + delim + aString.Substring (start);
			}
            // Add the trailing string
            else if (nTokens < maxTokens) {
				string trailingString = aString.Substring (start);
				tokens [nTokens++] = trailingString;
			}
			return nTokens;
		}

		/// <summary>
		/// Split the string into tokens separated by tab or space(s).  This method
		/// was added so support wig and bed files, which apparently accept space delimiters.
		/// <p/>
		/// Note:  TODO REGEX expressions are not used for speed.  This should be re-evaluated with JDK 1.5 or later
		/// </summary>
		/// <param name="aString"> the string to split </param>
		/// <param name="tokens">  an array to hold the parsed tokens </param>
		/// <returns> the number of tokens parsed </returns>
		public static int SplitWhitespace (string aString, string[] tokens)
		{

			int maxTokens = tokens.Length;
			int nTokens = 0;
			int start = 0;
			int tabEnd = aString.IndexOf ('\t');
			int spaceEnd = aString.IndexOf (' ');
			int end = tabEnd < 0 ? spaceEnd : spaceEnd < 0 ? tabEnd : Math.Min (spaceEnd, tabEnd);
			while ((end > 0) && (nTokens < maxTokens)) {
				//tokens[nTokens++] = new String(aString.toCharArray(), start, end-start); //  aString.substring(start, end);
				tokens [nTokens++] = aString.Substring (start, end - start);

				start = end + 1;
				// Gobble up any whitespace before next token -- don't gobble tabs, consecutive tabs => empty cell
				while (start < aString.Length && aString [start] == ' ') {
					start++;
				}

				tabEnd = aString.IndexOf ('\t', start);
				spaceEnd = aString.IndexOf (' ', start);
				end = tabEnd < 0 ? spaceEnd : spaceEnd < 0 ? tabEnd : Math.Min (spaceEnd, tabEnd);

			}

			// Add the trailing string
			if (nTokens < maxTokens) {
				string trailingString = aString.Substring (start);
				tokens [nTokens++] = trailingString;
			}
			return nTokens;
		}

		public static bool IsSorted<T> (IEnumerable<T> iterable) where T : IComparable<T>
		{
			T previous = iterable.FirstOrDefault ();
			foreach (var next in iterable) {
				if (previous.CompareTo (next) > 0) {
					return false;
				}
			}
			return true;
		}

		public static IList<T> SortList<T> (IList<T> toSort)
		{
			var q = toSort.ToList ();
			q.Sort ();
			return q;
		}

		public static string sortedString<T, V> (IDictionary<T, V> c)
		{
			List<T> t = new List<T> (c.Keys);
			t.Sort ();
			List<String> pairs = new List<String> ();
			foreach (T key in t) {
				pairs.Add (key + "=" + c [key].ToString ());
			}

			return "{" + String.Join (", ", pairs) + "}";
		}
	}
}