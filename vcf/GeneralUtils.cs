using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace Bio.VCF
{


	/// <summary>
	/// Constants and utility methods used throughout the VCF/BCF/VariantContext classes
	/// </summary>
	public class GeneralUtils
	{

		/// <summary>
		/// Setting this to true causes the VCF/BCF/VariantContext classes to emit debugging information
		/// to standard error
		/// </summary>
		public const bool DEBUG_MODE_ENABLED = false;

		/// <summary>
		/// The smallest log10 value we'll emit from normalizeFromLog10 and other functions
		/// where the real-space value is 0.0.
		/// </summary>
		public const double LOG10_P_OF_ZERO = -1000000.0;

		
		/// <summary>
		/// normalizes the log10-based array.  ASSUMES THAT ALL ARRAY ENTRIES ARE <= 0 (<= 1 IN REAL-SPACE).
		/// </summary>
		/// <param name="array"> the array to be normalized </param>
		/// <returns> a newly allocated array corresponding the normalized values in array </returns>
		public static double[] normalizeFromLog10(double[] array)
		{
			return normalizeFromLog10(array, false);
		}

		/// <summary>
		/// normalizes the log10-based array.  ASSUMES THAT ALL ARRAY ENTRIES ARE <= 0 (<= 1 IN REAL-SPACE).
		/// </summary>
		/// <param name="array">             the array to be normalized </param>
		/// <param name="takeLog10OfOutput"> if true, the output will be transformed back into log10 units </param>
		/// <returns> a newly allocated array corresponding the normalized values in array, maybe log10 transformed </returns>
		public static double[] normalizeFromLog10(double[] array, bool takeLog10OfOutput)
		{
			return normalizeFromLog10(array, takeLog10OfOutput, false);
		}

		/// <summary>
		/// See #normalizeFromLog10 but with the additional option to use an approximation that keeps the calculation always in log-space
		/// </summary>
		/// <param name="array"> </param>
		/// <param name="takeLog10OfOutput"> </param>
		/// <param name="keepInLogSpace">
		/// 
		/// @return </param>
		public static double[] normalizeFromLog10(double[] array, bool takeLog10OfOutput, bool keepInLogSpace)
		{
			// for precision purposes, we need to add (or really subtract, since they're
			// all negative) the largest value; also, we need to convert to normal-space.
			double maxValue = array.Max();

			// we may decide to just normalize in log space without converting to linear space
			if (keepInLogSpace)
			{
				for (int i = 0; i < array.Length; i++)
				{
					array[i] -= maxValue;
				}
				return array;
			}

			// default case: go to linear space
			double[] normalized = new double[array.Length];

			for (int i = 0; i < array.Length; i++)
			{
				normalized[i] = Math.Pow(10, array[i] - maxValue);
			}

			// normalize
			double sum = 0.0;
			for (int i = 0; i < array.Length; i++)
			{
				sum += normalized[i];
			}
			for (int i = 0; i < array.Length; i++)
			{
				double x = normalized[i] / sum;
				if (takeLog10OfOutput)
				{
					x = Math.Log10(x);
					if (x < LOG10_P_OF_ZERO || double.IsInfinity(x))
					{
						x = array[i] - maxValue;
					}
				}

				normalized[i] = x;
			}

			return normalized;
		}


		public static IList<T> cons<T>(T elt, IList<T> l)
		{
			List<T> l2 = new List<T>();
			l2.Add(elt);
			if (l != null)
			{
				l2.AddRange(l);
			}
			return l2;
		}

		/// <summary>
		/// Make all combinations of N size of objects
		/// 
		/// if objects = [A, B, C]
		/// if N = 1 => [[A], [B], [C]]
		/// if N = 2 => [[A, A], [B, A], [C, A], [A, B], [B, B], [C, B], [A, C], [B, C], [C, C]]
		/// </summary>
		/// <param name="objects"> </param>
		/// <param name="n"> </param>
		/// @param <T> </param>
		/// <param name="withReplacement"> if false, the resulting permutations will only contain unique objects from objects
		/// @return </param>
		public static IList<IList<T>> makePermutations<T>(IList<T> objects, int n, bool withReplacement)
		{
			IList<IList<T>> combinations = new List<IList<T>>();
			if (n == 1)
			{
                return objects.Select(x => { var q = new List<T>(1); q.Add(x); return (IList<T>)q ; }).ToList();

			}
			else if(n>1)
			{
				IList<IList<T>> sub = makePermutations(objects, n - 1, withReplacement);
				foreach (IList<T> subI in sub)
				{
					foreach (T a in objects)
					{
						if (withReplacement || !subI.Contains(a))
						{
							combinations.Add(cons(a, subI));
						}
					}
				}
			}
			return combinations;
		}

        internal static int[][] ReturnRectangularIntArray(int Size1, int Size2)
        {
            return Enumerable.Range(0, Size1).Select(x => new int[Size2]).ToArray();
        }

		/// <summary>
		/// Compares double values for equality (within 1e-6), or inequality.
		/// </summary>
		/// <param name="a"> the first double value </param>
		/// <param name="b"> the second double value </param>
		/// <returns> -1 if a is greater than b, 0 if a is equal to be within 1e-6, 1 if b is greater than a. </returns>
		public static sbyte CompareDoubles(double a, double b)
		{
			return CompareDoubles(a, b, 1e-6);
		}

		/// <summary>
		/// Compares double values for equality (within epsilon), or inequality.
		/// </summary>
		/// <param name="a">       the first double value </param>
		/// <param name="b">       the second double value </param>
		/// <param name="epsilon"> the precision within which two double values will be considered equal </param>
		/// <returns> -1 if a is greater than b, 0 if a is equal to be within epsilon, 1 if b is greater than a. </returns>
		public static sbyte CompareDoubles(double a, double b, double epsilon)
		{
			if (Math.Abs(a - b) < epsilon)
			{
				return 0;
			}
			if (a > b)
			{
				return -1;
			}
			return 1;
		}

	}



}