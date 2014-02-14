using System.Text;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;


namespace Bio.VCF
{
    public static class FastStringUtils
    {
        /// <summary>
        /// Fills the output array with that many divisions of the input string
        /// </summary>
        /// <param name="toSeperate"></param>
        /// <param name="separator"></param>
        /// <param name="outputArray"></param>

        /// <returns></returns>
        public static void Split(string toSeparate, char separator, string[] outputArray)
        {

            if (outputArray.Length == 0 )
                throw new ArgumentException("Cannot fill an empty array");

            if (outputArray.Length == 1)
            {
                outputArray[0] = toSeparate;
            }
             SplitByCharacters(toSeparate,outputArray, separator);
        }    
        public static String[] Split(string toSeperate, char separator, int count, StringSplitOptions options)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "Count cannot be less than zero.");
            if ((options != StringSplitOptions.None) && (options != StringSplitOptions.RemoveEmptyEntries))
                throw new ArgumentException("Illegal enum value: " + options + ".");

            if (toSeperate.Length == 0 && (options & StringSplitOptions.RemoveEmptyEntries) != 0)
                return new string[0];

            if (count == 1)
            {
                return count == 0 ? new string[0] : new String[1] { toSeperate};
            }
            return SplitByCharacters(toSeperate, separator, count, options != 0);
        }    
       
        private static unsafe string[] SplitByCharacters(string toSep,char sep, int count, bool removeEmpty)
        {
            int[] split_points = null;
            int total_points = 0;
            --count;
            int Length=toSep.Length;
          
                fixed (char* src = toSep)
                {                    
                        char* src_ptr = src;
                        //char* sep_ptr_end = sep_src + sep.Length;
                        int len = toSep.Length;
                        while (len > 0)
                        {
                                if (sep == *src_ptr)
                                {
                                    if (split_points == null)
                                    {
                                        split_points = new int[8];
                                    }
                                    else if (split_points.Length == total_points)
                                    {
                                        Array.Resize(ref split_points, split_points.Length * 2);
                                    }

                                    split_points[total_points++] = Length - len;
                                    if (total_points == count && !removeEmpty)
                                        len = 0;                                   
                                }
                            ++src_ptr;
                            --len;
                        }                    
                }          

            if (total_points == 0)
                return new string[] { toSep };

            var res = new string[Math.Min(total_points, count) + 1];
            int prev_index = 0;
            int i = 0;
            if (!removeEmpty)
            {
                for (; i < total_points; ++i)
                {
                    var start = split_points[i];
                    res[i]=toSep.Substring(prev_index,start - prev_index);
                    //res[i] = SubstringUnchecked(prev_index, start - prev_index);
                    prev_index = start + 1;
                }
                res[i] = toSep.Substring(prev_index, Length - prev_index);
            }
            else
            {
                int used = 0;
                int length;
                for (; i < total_points; ++i)
                {
                    var start = split_points[i];
                    length = start - prev_index;
                    if (length != 0)
                    {
                        if (used == count)
                            break;
                        res[used++] = toSep.Substring(prev_index,length);// SubstringUnchecked(prev_index, length);
                    }

                    prev_index = start + 1;
                }

                length = Length - prev_index;
                if (length != 0)
                    res[used++] = toSep.Substring(prev_index,length);// SubstringUnchecked(prev_index, length);
                if (used != res.Length)
                    Array.Resize(ref res, used);
            }
            return res;
        }
        private static unsafe void SplitByCharacters(string toSep,  string[] outputArray,char sep)
        {
            int count = outputArray.Length;
            int[] split_points = new int[outputArray.Length-1];
            int total_points = 0;
            --count;
            int Length = toSep.Length;
            fixed (char* src = toSep)
            {
                char* src_ptr = src;
                int len = toSep.Length;
                while (len > 0)
                {
                    if (sep == *src_ptr)
                    {
                        split_points[total_points++] = Length - len;
                        //if we have hit the end, stop
                        if (total_points == count)
                            len = 0;
                    }
                    ++src_ptr;
                    --len;
                }
            }
            //Now have 0 to n-1 of arrays to go with
            if(total_points!=(outputArray.Length-1))
            {
                throw new ArgumentException("Did not find enough tokens during parsing");
            }
            int prev_index = 0;
            int i = 0;
                for (; i < total_points; ++i)
                {
                    var start = split_points[i];
                    outputArray[i] = toSep.Substring(prev_index, start - prev_index);
                    prev_index = start + 1;
                }

             if(total_points!=(outputArray.Length-1) || prev_index==(outputArray.Length-1))
            {
                throw new ArgumentException("Did not find enough tokens during parsing");
            }
            //now to add the last bit
                outputArray[i] = toSep.Substring(prev_index, Length - prev_index);
            }

        
    }
}