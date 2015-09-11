using System;


namespace Bio.VCF
{
	public sealed class VCFConstants
	{		
		// reserved INFO/FORMAT field keys
		public const string ANCESTRAL_ALLELE_KEY = "AA";
		public const string ALLELE_COUNT_KEY = "AC";
		public const string MLE_ALLELE_COUNT_KEY = "MLEAC";
		public const string ALLELE_FREQUENCY_KEY = "AF";
		public const string MLE_ALLELE_FREQUENCY_KEY = "MLEAF";
		public const string MLE_PER_SAMPLE_ALLELE_COUNT_KEY = "MLPSAC";
		public const string MLE_PER_SAMPLE_ALLELE_FRACTION_KEY = "MLPSAF";
		public const string ALLELE_NUMBER_KEY = "AN";
		public const string RMS_BASE_QUALITY_KEY = "BQ";
		public const string CIGAR_KEY = "CIGAR";
		public const string DBSNP_KEY = "DB";
		public const string DEPTH_KEY = "DP";
		public const string DOWNSAMPLED_KEY = "DS";
		public const string EXPECTED_ALLELE_COUNT_KEY = "EC";
		public const string END_KEY = "END";

		public const string GENOTYPE_FILTER_KEY = "FT";
		public const string GENOTYPE_KEY = "GT";
		public const string GENOTYPE_POSTERIORS_KEY = "GP";
		public const string GENOTYPE_QUALITY_KEY = "GQ";
		public const string GENOTYPE_ALLELE_DEPTHS = "AD";
		public const string GENOTYPE_PL_KEY = "PL"; // phred-scaled genotype likelihoods
		[Obsolete]
		public const string GENOTYPE_LIKELIHOODS_KEY = "GL"; // log10 scaled genotype likelihoods

		public const string HAPMAP2_KEY = "H2";
		public const string HAPMAP3_KEY = "H3";
		public const string HAPLOTYPE_QUALITY_KEY = "HQ";
		public const string RMS_MAPPING_QUALITY_KEY = "MQ";
		public const string MAPPING_QUALITY_ZERO_KEY = "MQ0";
		public const string SAMPLE_NUMBER_KEY = "NS";
		public const string PHASE_QUALITY_KEY = "PQ";
		public const string PHASE_SET_KEY = "PS";
		public const string OLD_DEPTH_KEY = "RD";
		public const string STRAND_BIAS_KEY = "SB";
		public const string SOMATIC_KEY = "SOMATIC";
		public const string VALIDATED_KEY = "VALIDATED";
		public const string THOUSAND_GENOMES_KEY = "1000G";

		// separators
		public const string FORMAT_FIELD_SEPARATOR = ":";
		public const string GENOTYPE_FIELD_SEPARATOR = ":";
		public const char GENOTYPE_FIELD_SEPARATOR_CHAR = ':';
		public const string FIELD_SEPARATOR = "\t";
		public const char FIELD_SEPARATOR_CHAR = '\t';
        public static readonly char[] FIELD_SEPARATOR_CHAR_AS_ARRAY = new char[]{'\t'};
		public const string FILTER_CODE_SEPARATOR = ";";
        public static readonly char[] FILTER_CODE_SEPARATOR_CHAR_ARRAY =new char[]{ ';'};
		public const string INFO_FIELD_ARRAY_SEPARATOR = ",";
		public const char INFO_FIELD_ARRAY_SEPARATOR_CHAR = ',';
		public const string ID_FIELD_SEPARATOR = ";";
        public static readonly char[] ID_FIELD_SEPARATOR_AS_ARRAY = new char[]{';'};
		public const string INFO_FIELD_SEPARATOR = ";";
		public const char INFO_FIELD_SEPARATOR_CHAR = ';';
        public static readonly char[] INFO_FIELD_SEPARATOR_CHAR_AS_ARRAY = new char[] { INFO_FIELD_SEPARATOR_CHAR };
        public static readonly char[] COMMA_AS_CHAR_ARRAY = new char[] { ',' };
        public const char COMMA_AS_CHAR = ',';
		public const string UNPHASED = "/";
        public const char UNPHASED_AS_CHAR = '/';
		public const string PHASED = "|";
        public const char PHASED_AS_CHAR='|';
		public const string PHASED_SWITCH_PROB_v3 = "\\";
		public const string PHASING_TOKENS = "/|\\";
        public static readonly char[] PHASING_TOKENS_AS_CHAR_ARRAY = new char[] { '/', '|','\\' };

		// header lines
		public const string FILTER_HEADER_START = "##FILTER";
		public const string FORMAT_HEADER_START = "##FORMAT";
		public const string INFO_HEADER_START = "##INFO";
		public const string ALT_HEADER_START = "##ALT";
		public const string CONTIG_HEADER_KEY = "contig";
		public static readonly string CONTIG_HEADER_START = "##" + CONTIG_HEADER_KEY;

		// old indel alleles
		public const char DELETION_ALLELE_v3 = 'D';
		public const char INSERTION_ALLELE_v3 = 'I';

		// missing/default values
		public const string UNFILTERED = ".";
		public const string PASSES_FILTERS_v3 = "0";
		public const string PASSES_FILTERS_v4 = "PASS";
		public const string EMPTY_ID_FIELD = ".";
		public const string EMPTY_INFO_FIELD = ".";
        public const char EMPTY_INFO_FIELD_AS_CHAR = '.';
		public const string EMPTY_ALTERNATE_ALLELE_FIELD = ".";
		public const string MISSING_VALUE_v4 = ".";
		public const string MISSING_QUALITY_v3 = "-1";
		public static readonly double MISSING_QUALITY_v3_DOUBLE = Convert.ToDouble(MISSING_QUALITY_v3);

		public const string MISSING_GENOTYPE_QUALITY_v3 = "-1";
		public const string MISSING_HAPLOTYPE_QUALITY_v3 = "-1";
		public const string MISSING_DEPTH_v3 = "-1";
		public const string UNBOUNDED_ENCODING_v4 = ".";
		public const string UNBOUNDED_ENCODING_v3 = "-1";
		public const string PER_ALLELE_COUNT = "A";
		public const string PER_GENOTYPE_COUNT = "G";
		public const string EMPTY_ALLELE = ".";
		public const string EMPTY_GENOTYPE = "./.";
		public const int MAX_GENOTYPE_QUAL = 99;

		public const double VCF_ENCODING_EPSILON = 0.00005; // when we consider fields equal(), used in the Qual compare
		public const string REFSAMPLE_DEPTH_KEY = "REFDEPTH";
	}
}