using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Bio.VCF
{
    /// <summary>
    /// The static methods and forms from the VariantContext class, placed here to make the code cleaner
    /// </summary>
    public partial class VariantContext
    {
        protected const bool WARN_ABOUT_BAD_END = true;
        protected const int MAX_ALLELE_SIZE_FOR_NON_SV = 150;
        public static readonly GenotypesContext NO_GENOTYPES = GenotypesContext.NO_GENOTYPES;
        protected static readonly Validation NO_VALIDATION = Validation.NONE;
        public const double NO_LOG10_PERROR = CommonInfo.NO_LOG10_PERROR;
        public static readonly ISet<string> PASSES_FILTERS = new LinkedHashSet<string>(true);
        // ---------------------------------------------------------------------------------------------------------
        //
        // validation mode
        //
        // ---------------------------------------------------------------------------------------------------------
        [Flags]
        public enum Validation
        {
            NONE = 0,
            ALLELES = 1,
            GENOTYPES = 2
        }

    }
}
