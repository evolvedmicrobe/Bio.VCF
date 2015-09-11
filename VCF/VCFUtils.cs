using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;



namespace Bio.VCF
{

	public static class VCFUtils
	{
        /// <summary>
        /// Returns a string as bytes, also useful as 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
       
        public static byte[] StringToBytes(string str)
        {
            //Think this should be the same as UTF8 for most cases
           return System.Text.Encoding.ASCII.GetBytes(str);
        }	

	}

}