using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bio.VCF
{
    public class VCFParsingError : Exception
    {
        public VCFParsingError(string message, Exception innerException = null):base("Error parsing VCF file.\n"+message,innerException)
        {
            
        }

    }
}
