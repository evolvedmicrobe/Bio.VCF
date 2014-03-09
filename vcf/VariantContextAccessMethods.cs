using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bio.VCF.NewCollections;

namespace Bio.VCF
{
    //This is a partial class that contains a lot of methods that didn't seem primary

    partial class VariantContext
    {
        /// <summary>
        /// This method subsets down to a set of samples.
        /// 
        /// At the same time returns the alleles to just those in use by the samples,
        /// if rederiveAllelesFromGenotypes is true, otherwise the full set of alleles
        /// in this VC is returned as the set of alleles in the subContext, even if
        /// some of those alleles aren't in the samples
        /// 
        /// WARNING: BE CAREFUL WITH rederiveAllelesFromGenotypes UNLESS YOU KNOW WHAT YOU ARE DOING
        /// </summary>
        /// <param name="sampleNames">    the sample names </param>
        /// <param name="rederiveAllelesFromGenotypes"> if true, returns the alleles to just those in use by the samples, true should be default </param>
        /// <returns> new VariantContext subsetting to just the given samples </returns>
        public VariantContext SubContextFromSamples(ISet<string> sampleNames, bool rederiveAllelesFromGenotypes)
        {
            if (sampleNames.SetEquals(SampleNames) && !rederiveAllelesFromGenotypes)
            {
                return this; // fast path when you don't have any work to do
            }
            else
            {
                VariantContextBuilder builder = new VariantContextBuilder(this);
                GenotypesContext newGenotypes = genotypes.subsetToSamples(sampleNames);
                if (rederiveAllelesFromGenotypes)
                {
                    builder.SetAlleles(allelesOfGenotypes(newGenotypes));
                }
                else
                {
                    builder.SetAlleles(alleles);
                }

                builder.SetGenotypes(newGenotypes);
                return builder.make();
            }
        }

        /// <seealso cref= #subContextFromSamples(java.util.Set, boolean) with rederiveAllelesFromGenotypes = true
        /// </seealso>
        /// <param name="sampleNames">
        /// @return </param>
        public VariantContext SubContextFromSamples(ISet<string> sampleNames)
        {
            return SubContextFromSamples(sampleNames, true);
        }
        public VariantContext SubContextFromSample(string sampleName)
        {
            var singleton = new HashSet<string>();
            singleton.Add(sampleName);
            return SubContextFromSamples(singleton);
        }

        // ---------------------------------------------------------------------------------------------------------
        //
        // validation: extra-strict validation routines for paranoid users
        //
        // ---------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Run all extra-strict validation tests on a Variant Context object
        /// </summary>
        /// <param name="reportedReference">   the reported reference allele </param>
        /// <param name="observedReference">   the actual reference allele </param>
        /// <param name="rsIDs">               the true dbSNP IDs </param>
        public void extraStrictValidation(Allele reportedReference, Allele observedReference, ISet<string> rsIDs)
        {
            // validate the reference
            validateReferenceBases(reportedReference, observedReference);

            // validate the RS IDs
            ValidateRSIDs(rsIDs);

            // validate the altenate alleles
            ValidateAlternateAlleles();

            // validate the AN and AC fields
            ValidateChromosomeCounts();

            // TODO: implement me
            //checkReferenceTrack();
        }

        public void validateReferenceBases(Allele reportedReference, Allele observedReference)
        {
            if (reportedReference != null && !reportedReference.BasesMatch(observedReference))
            {
                throw new Exception(string.Format("the REF allele is incorrect for the record at position {0}:{1:D}, fasta says {2} vs. VCF says {3}", Chr, Start, observedReference.BaseString, reportedReference.BaseString));
            }
        }

        public void ValidateRSIDs(ISet<string> rsIDs)
        {
            if (rsIDs != null && HasID)
            {
                foreach (string id in ID.Split(VCFConstants.ID_FIELD_SEPARATOR_AS_ARRAY, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (id.StartsWith("rs") && !rsIDs.Contains(id))
                    {
                        throw new Exception(string.Format("the rsID {0} for the record at position {1}:{2:D} is not in dbSNP", id, Chr, Start));
                    }
                }
            }
        }

        public void ValidateAlternateAlleles()
        {
            if (!HasGenotypes)
            {
                return;
            }
            IList<Allele> reportedAlleles = Alleles;
            ISet<Allele> observedAlleles = new HashSet<Allele>();
            observedAlleles.Add(Reference);
            foreach (Genotype g in Genotypes)
            {
                if (g.Called)
                {
                    foreach (Allele a in g.Alleles)
                    { observedAlleles.Add(a); }
                }
            }
            if (observedAlleles.Contains(Allele.NO_CALL))
            {
                observedAlleles.Remove(Allele.NO_CALL);
            }

            if (reportedAlleles.Count != observedAlleles.Count)
            {
                throw new Exception(string.Format("one or more of the ALT allele(s) for the record at position {0}:{1:D} are not observed at all in the sample genotypes", Chr, Start));
            }

            int originalSize = reportedAlleles.Count;
            // take the intersection and see if things change
            observedAlleles.IntersectWith(reportedAlleles);
            if (observedAlleles.Count != originalSize)
            {
                throw new Exception(string.Format("one or more of the ALT allele(s) for the record at position {0}:{1:D} are not observed at all in the sample genotypes", Chr, Start));
            }
        }

        public void ValidateChromosomeCounts()
        {
            if (!HasGenotypes)
            {
                return;
            }

            // AN
            if (HasAttribute(VCFConstants.ALLELE_NUMBER_KEY))
            {
                int reportedAN = Convert.ToInt32(GetAttribute(VCFConstants.ALLELE_NUMBER_KEY).ToString());
                int observedAN = CalledChrCount;
                if (reportedAN != observedAN)
                {
                    throw new Exception(string.Format("the Allele Number (AN) tag is incorrect for the record at position {0}:{1:D}, {2:D} vs. {3:D}", Chr, Start, reportedAN, observedAN));
                }
            }

            // AC
            if (HasAttribute(VCFConstants.ALLELE_COUNT_KEY))
            {
                List<int?> observedACs = new List<int?>();

                // if there are alternate alleles, record the relevant tags
                if (AlternateAlleles.Count > 0)
                {
                    foreach (Allele allele in AlternateAlleles)
                    {
                        observedACs.Add(GetCalledChrCount(allele));
                    }
                }
                else // otherwise, set them to 0
                {
                    observedACs.Add(0);
                }

                if (GetAttribute(VCFConstants.ALLELE_COUNT_KEY) is IList)
                {
                    observedACs.Sort();
                    IList reportedACs = (IList)GetAttribute(VCFConstants.ALLELE_COUNT_KEY);
                    //reportedACs.Sort();
                    if (observedACs.Count != reportedACs.Count)
                    {
                        throw new Exception(string.Format("the Allele Count (AC) tag doesn't have the correct number of values for the record at position {0}:{1:D}, {2:D} vs. {3:D}", Chr, Start, reportedACs.Count, observedACs.Count));
                    }
                    for (int i = 0; i < observedACs.Count; i++)
                    {
                        if (Convert.ToInt32(reportedACs[i].ToString()) != observedACs[i])
                        {
                            throw new Exception(string.Format("the Allele Count (AC) tag is incorrect for the record at position {0}:{1:D}, {2} vs. {3:D}", Chr, Start, reportedACs[i], observedACs[i]));
                        }
                    }
                }
                else
                {
                    if (observedACs.Count != 1)
                    {
                        throw new Exception(string.Format("the Allele Count (AC) tag doesn't have enough values for the record at position {0}:{1:D}", Chr, Start));
                    }
                    int reportedAC = Convert.ToInt32(GetAttribute(VCFConstants.ALLELE_COUNT_KEY).ToString());
                    if (reportedAC != observedACs[0])
                    {
                        throw new Exception(string.Format("the Allele Count (AC) tag is incorrect for the record at position {0}:{1:D}, {2:D} vs. {3:D}", Chr, Start, reportedAC, observedACs[0]));
                    }
                }
            }
        }


        // ---------------------------------------------------------------------------------------------------------
        //
        // utility routines
        //
        // ---------------------------------------------------------------------------------------------------------

        private void determineType()
        {
            if (type == null)
            {
                switch (NAlleles)
                {
                    case 0:
                        throw new Exception("Unexpected error: requested type of VariantContext with no alleles!" + this);
                    case 1:
                        // note that this doesn't require a reference allele.  You can be monomorphic independent of having a
                        // reference allele
                        type = VariantType.NO_VARIATION;
                        break;
                    default:
                        determinePolymorphicType();
                        break;
                }
            }
        }

        private void determinePolymorphicType()
        {
            type = null;

            // do a pairwise comparison of all alleles against the reference allele
            foreach (Allele allele in alleles)
            {
                if (allele == REF)
                {
                    continue;
                }

                // find the type of this allele relative to the reference
                VariantType biallelicType = typeOfBiallelicVariant(REF, allele);

                // for the first alternate allele, set the type to be that one
                if (type == null)
                {
                    type = biallelicType;
                }
                // if the type of this allele is different from that of a previous one, assign it the MIXED type and quit
                else if (biallelicType != type)
                {
                    type = VariantType.MIXED;
                    return;
                }
            }
        }

        private static VariantType typeOfBiallelicVariant(Allele reference, Allele allele)
        {
            if (reference.Symbolic)
            {
                throw new Exception("Unexpected error: encountered a record with a symbolic reference allele");
            }

            if (allele.Symbolic)
            {
                return VariantType.SYMBOLIC;
            }

            if (reference.Length == allele.Length)
            {
                if (allele.Length == 1)
                {
                    return VariantType.SNP;
                }
                else
                {
                    return VariantType.MNP;
                }
            }

            // Important note: previously we were checking that one allele is the prefix of the other.  However, that's not an
            // appropriate check as can be seen from the following example:
            // REF = CTTA and ALT = C,CT,CA
            // This should be assigned the INDEL type but was being marked as a MIXED type because of the prefix check.
            // In truth, it should be absolutely impossible to return a MIXED type from this method because it simply
            // performs a pairwise comparison of a single alternate allele against the reference allele (whereas the MIXED type
            // is reserved for cases of multiple alternate alleles of different types).  Therefore, if we've reached this point
            // in the code (so we're not a SNP, MNP, or symbolic allele), we absolutely must be an INDEL.

            return VariantType.INDEL;

            // old incorrect logic:
            // if (oneIsPrefixOfOther(ref, allele))
            //     return Type.INDEL;
            // else
            //     return Type.MIXED;
        }


	
    }
}
