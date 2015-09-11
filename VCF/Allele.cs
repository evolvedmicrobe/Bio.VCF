using System;
using System.Collections.Generic;
using System.Linq;

namespace Bio.VCF
{
	/// <summary>
	/// Immutable representation of an allele
	/// 
	/// Types of alleles:
	/// 
	/// Ref: a t C g a // C is the reference base
	/// 
	///    : a t G g a // C base is a G in some individuals
	/// 
	///    : a t - g a // C base is deleted w.r.t. the reference
	/// 
	///    : a t CAg a // A base is inserted w.r.t. the reference sequence
	/// 
	/// In these cases, where are the alleles?
	/// 
	/// SNP polymorphism of C/G  -> { C , G } -> C is the reference allele
	/// 1 base deletion of C     -> { C , - } -> C is the reference allele
	/// 1 base insertion of A    -> { - ; A } -> Null is the reference allele
	/// 
	/// Suppose I see a the following in the population:
	/// 
	/// Ref: a t C g a // C is the reference base
	///    : a t G g a // C base is a G in some individuals
	///    : a t - g a // C base is deleted w.r.t. the reference
	/// 
	/// How do I represent this?  There are three segregating alleles:
	/// 
	///  { C , G , - }
	/// 
	/// Now suppose I have this more complex example:
	/// 
	/// Ref: a t C g a // C is the reference base
	///    : a t - g a
	///    : a t - - a
	///    : a t CAg a
	/// 
	/// There are actually four segregating alleles:
	/// 
	///   { C g , - g, - -, and CAg } over bases 2-4
	/// 
	/// However, the molecular equivalence explicitly listed above is usually discarded, so the actual
	/// segregating alleles are:
	/// 
	///   { C g, g, -, C a g }
	/// 
	/// Critically, it should be possible to apply an allele to a reference sequence to create the
	/// correct haplotype sequence:
	/// 
	/// Allele + reference => haplotype
	/// 
	/// For convenience, we are going to create Alleles where the GenomeLoc of the allele is stored outside of the
	/// Allele object itself.  So there's an idea of an A/C polymorphism independent of it's surrounding context.
	/// 
	/// Given list of alleles it's possible to determine the "type" of the variation
	/// 
	///      A / C @ loc => SNP with
	///      - / A => INDEL
	/// 
	/// If you know where allele is the reference, you can determine whether the variant is an insertion or deletion.
	/// 
	/// Alelle also supports is concept of a NO_CALL allele.  This Allele represents a haplotype that couldn't be
	/// determined. This is usually represented by a '.' allele.
	/// 
	/// Note that Alleles store all bases as bytes, in **UPPER CASE**.  So 'atc' == 'ATC' from the perspective of an
	/// Allele.
	/// 
	/// </summary>
	public class Allele : IComparable<Allele>
	{
		private static readonly byte[] EMPTY_ALLELE_BASES = new byte[0];

		private bool isRef = false;
		private bool isNoCall = false;
		private bool isSymbolic = false;

		private byte[] bases = null;
        /// <summary>
        /// A generic static NO_CALL allele
        /// </summary>
		public const string NO_CALL_STRING = ".";
		
		protected internal Allele(byte[] bases, bool isRef)
		{
			// null alleles are no longer allowed
			if (WouldBeNullAllele(bases))
			{
				throw new System.ArgumentException("Null alleles are not supported");
			}

			// no-calls are represented as no bases
			if (WouldBeNoCallAllele(bases))
			{
				this.bases = EMPTY_ALLELE_BASES;
				isNoCall = true;
				if (isRef)
				{
					throw new System.ArgumentException("Cannot tag a NoCall allele as the reference allele");
				}
				return;
			}

			if (WouldBeSymbolicAllele(bases))
			{
				isSymbolic = true;
				if (isRef)
				{
					throw new System.ArgumentException("Cannot tag a symbolic allele as the reference allele");
				}
			}
			else
			{
                convertbyte(bases);
			}
			this.isRef = isRef;
			this.bases = bases;
			if (!AcceptableAlleleBases(bases))
			{
				throw new System.ArgumentException("Unexpected base in allele bases \'" + convertbyte(bases) + "\'");
			}
		}
        string convertbyte(byte[] bases)
        {
            string str = new String(bases.Select(x=>(char)x).ToArray());
            return str;
        }
        void byteToUpper(byte[] bases)
        {
            int offSet = -32;//(sbyte)('A' - 'a');
            for (int i = 0; i < bases.Length; i++)
            {
                if (bases[i] >= 'a' && bases[i] <= 'z')
                { 
                    //In theory could bit shift here
                    bases[i] =(byte)(bases[i] + offSet);
                }
            }
        }

        protected internal Allele(string bases, bool isRef)
            : this(bases.Select(x=>(byte)x).ToArray(), isRef)
		{
		}

		/// <summary>
		/// Creates a new allele based on the provided one.  Ref state will be copied unless ignoreRefState is true
		/// (in which case the returned allele will be non-Ref).
		/// 
		/// This method is efficient because it can skip the validation of the bases (since the original allele was already validated)
		/// </summary>
		/// <param name="allele">  the allele from which to copy the bases </param>
		/// <param name="ignoreRefState">  should we ignore the reference state of the input allele and use the default ref state? </param>
		protected internal Allele(Allele allele, bool ignoreRefState)
		{
			this.bases = allele.bases;
			this.isRef = ignoreRefState ? false : allele.isRef;
			this.isNoCall = allele.isNoCall;
			this.isSymbolic = allele.isSymbolic;
		}


		private static readonly Allele REF_A = new Allele("A", true);
		private static readonly Allele ALT_A = new Allele("A", false);
		private static readonly Allele REF_C = new Allele("C", true);
		private static readonly Allele ALT_C = new Allele("C", false);
		private static readonly Allele REF_G = new Allele("G", true);
		private static readonly Allele ALT_G = new Allele("G", false);
		private static readonly Allele REF_T = new Allele("T", true);
		private static readonly Allele ALT_T = new Allele("T", false);
		private static readonly Allele REF_N = new Allele("N", true);
		private static readonly Allele ALT_N = new Allele("N", false);
		public static readonly Allele NO_CALL = new Allele(NO_CALL_STRING, false);

		// ---------------------------------------------------------------------------------------------------------
		//
		// creation routines
		//
		// ---------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Create a new Allele that includes bases and if tagged as the reference allele if isRef == true.  If bases
		/// == '-', a Null allele is created.  If bases ==  '.', a no call Allele is created.
		/// </summary>
		/// <param name="bases"> the DNA sequence of this variation, '-', of '.' </param>
		/// <param name="isRef"> should we make this a reference allele? </param>
		/// <exception cref="IllegalArgumentException"> if bases contains illegal characters or is otherwise malformated </exception>
		public static Allele Create(byte[] bases, bool isRef)
		{
			if (bases == null)
			{
				throw new System.ArgumentException("create: the Allele base string cannot be null; use new Allele() or new Allele(\"\") to create a Null allele");
			}

			if (bases.Length == 1)
			{
				// optimization to return a static constant Allele for each single base object
				switch (bases[0])
				{
					case (byte)'.':
						if (isRef)
						{
							throw new System.ArgumentException("Cannot tag a NoCall allele as the reference allele");
						}
						return NO_CALL;
                    case (byte)'A':
                    case (byte)'a':
					    return isRef ? REF_A : ALT_A;
                    case (byte)'C':
                    case (byte)'c':
					    return isRef ? REF_C : ALT_C;
                    case (byte)'G':
                    case (byte)'g':
					    return isRef ? REF_G : ALT_G;
                    case (byte)'T':
                    case (byte)'t':
					    return isRef ? REF_T : ALT_T;
                    case (byte)'N':
                    case (byte)'n':
					    return isRef ? REF_N : ALT_N;
					default:
						throw new System.ArgumentException("Illegal base [" + (char)bases[0] + "] seen in the allele");
				}
			}
			else
			{
				return new Allele(bases, isRef);
			}
		}

		public static Allele Create(byte nucleotide, bool isRef)
		{
            return Create(new byte[] { nucleotide }, isRef);
		}

        public static Allele Create(byte nucleotide)
		{
            return Create(nucleotide, false);
		}

		public static Allele Extend(Allele left, byte[] right)
		{
			if (left.Symbolic)
			{
				throw new System.ArgumentException("Cannot extend a symbolic allele");
			}
			byte[] bases = new byte[left.Length + right.Length];
			Array.Copy(left.Bases, 0, bases, 0, left.Length);
			Array.Copy(right, 0, bases, left.Length, right.Length);

			return Create(bases, left.Reference);
		}

		/// <param name="bases">  bases representing an allele </param>
		/// <returns> true if the bases represent the null allele </returns>
		public static bool WouldBeNullAllele(byte[] bases)
		{
			return (bases.Length == 1 && bases[0] == '-') || bases.Length == 0;
		}

		/// <param name="bases">  bases representing an allele </param>
		/// <returns> true if the bases represent the NO_CALL allele </returns>
		public static bool WouldBeNoCallAllele(byte[] bases)
		{
			return bases.Length == 1 && bases[0] == '.';
		}

		/// <param name="bases">  bases representing an allele </param>
		/// <returns> true if the bases represent a symbolic allele </returns>
		public static bool WouldBeSymbolicAllele(byte[] bases)
		{
			if (bases.Length <= 2)
			{
				return false;
			}
			else
			{
                return (bases[0] == '<' && bases[bases.Length - 1] == '>') || (bases.Contains((byte)'[') || bases.Contains((byte)']'));
			}
		}

		/// <param name="bases">  bases representing an allele </param>
		/// <returns> true if the bases represent the well formatted allele </returns>
		public static bool AcceptableAlleleBases(string bases)
		{
            
			return AcceptableAlleleBases(VCFUtils.StringToBytes(bases), true);
		}

		public static bool AcceptableAlleleBases(string bases, bool allowNsAsAcceptable)
		{
			return AcceptableAlleleBases(VCFUtils.StringToBytes(bases), allowNsAsAcceptable);
		}

		/// <param name="bases">  bases representing an allele </param>
		/// <returns> true if the bases represent the well formatted allele </returns>
		public static bool AcceptableAlleleBases(byte[] bases)
		{
			return AcceptableAlleleBases(bases, true); // default: N bases are acceptable
		}

		public static bool AcceptableAlleleBases(byte[] bases, bool allowNsAsAcceptable)
		{
			if (WouldBeNullAllele(bases))
			{
				return false;
			}

			if (WouldBeNoCallAllele(bases) || WouldBeSymbolicAllele(bases))
			{
				return true;
			}

			foreach (byte bp in bases)
			{
				switch (bp)
				{
					case (byte)'A':
                    case (byte)'C':
                    case (byte)'G':
                    case (byte)'T':
                    case (byte)'a':
                    case (byte)'c':
                    case (byte)'g':
                    case (byte)'t':
						    break;
                    case (byte)'N':
                    case (byte)'n':
						if (allowNsAsAcceptable)
						{
							break;
						}
						else
						{
							return false;
						}
					default:
						return false;
				}
			}

			return true;
		}

		/// <seealso cref= Allele(byte[], boolean)
		/// </seealso>
		/// <param name="bases">  bases representing an allele </param>
		/// <param name="isRef">  is this the reference allele? </param>
		public static Allele Create(string bases, bool isRef)
		{
			return Create(VCFUtils.StringToBytes(bases), isRef);
		}


		/// Creates a non-Ref allele.  <seealso cref= Allele(byte[], boolean) for full information
		/// </seealso>
		/// <param name="bases">  bases representing an allele </param>
		public static Allele Create(string bases)
		{
			return Create(bases, false);
		}

		/// Creates a non-Ref allele.  <seealso cref= Allele(byte[], boolean) for full information
		/// </seealso>
		/// <param name="bases">  bases representing an allele </param>
		public static Allele Create(byte[] bases)
		{
			return Create(bases, false);
		}

		/// <summary>
		/// Creates a new allele based on the provided one.  Ref state will be copied unless ignoreRefState is true
		/// (in which case the returned allele will be non-Ref).
		/// 
		/// This method is efficient because it can skip the validation of the bases (since the original allele was already validated)
		/// </summary>
		/// <param name="allele">  the allele from which to copy the bases </param>
		/// <param name="ignoreRefState">  should we ignore the reference state of the input allele and use the default ref state? </param>
		public static Allele Create(Allele allele, bool ignoreRefState)
		{
			return new Allele(allele, ignoreRefState);
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// accessor routines
		//
		// ---------------------------------------------------------------------------------------------------------

		// Returns true if this is the NO_CALL allele
		public bool NoCall
		{
			get
			{
				return isNoCall;
			}
		}
		// Returns true if this is not the NO_CALL allele
		public bool Called
		{
			get
			{
				return !NoCall;
			}
		}

		// Returns true if this Allele is the reference allele
		public bool Reference
		{
			get
			{
				return isRef;
			}
		}
		// Returns true if this Allele is not the reference allele
		public bool NonReference
		{
			get
			{
				return !Reference;
			}
		}

		// Returns true if this Allele is symbolic (i.e. no well-defined base sequence)
		public bool Symbolic
		{
			get
			{
				return isSymbolic;
			}
		}

		// Returns a nice string representation of this object
		public override string ToString()
		{
			return (NoCall ? NO_CALL_STRING : DisplayString) + (Reference ? "*" : "");
		}

		/// <summary>
		/// Return the DNA bases segregating in this allele.  Note this isn't reference polarized,
		/// so the Null allele is represented by a vector of length 0
		/// </summary>
		/// <returns> the segregating bases </returns>
		public byte[] Bases
		{
			get
			{
				return isSymbolic ? EMPTY_ALLELE_BASES : bases;
			}
		}

		/// <summary>
		/// Return the DNA bases segregating in this allele in String format.
		/// This is useful, because toString() adds a '*' to reference alleles and getBases() returns garbage when you call toString() on it.
		/// </summary>
		/// <returns> the segregating bases </returns>
		public string BaseString
		{
			get
			{
				return NoCall ? NO_CALL_STRING : new string(Bases.Select(x=>(char)x).ToArray());
			}
		}

		/// <summary>
		/// Return the printed representation of this allele.
		/// Same as getBaseString(), except for symbolic alleles.
		/// For symbolic alleles, the base string is empty while the display string contains <TAG>.
		/// </summary>
		/// <returns> the allele string representation </returns>
		public string DisplayString
		{
			get
			{
                return new string(bases.Select(x => (char)x).ToArray());
			}
		}

		/// <summary>
		/// Same as #getDisplayString() but returns the result as byte[].
		/// 
		/// Slightly faster then getDisplayString()
		/// </summary>
		/// <returns> the allele string representation </returns>
		public byte[] DisplayBases
		{
			get
			{
				return bases;
			}
		}

		/// <param name="other">  the other allele
		/// </param>
		/// <returns> true if these alleles are equal </returns>
		public bool Equals(object other)
		{
			return (!(other is Allele) ? false : Equals((Allele)other, false));
		}

		/// <returns> hash code </returns>
		public int GetHashCode()
		{
			int hash = 1;
			for (int i = 0; i < bases.Length; i++)
			{
				hash += (i + 1) * bases[i];
			}
			return hash;
		}

		/// <summary>
		/// Returns true if this and other are equal.  If ignoreRefState is true, then doesn't require both alleles has the
		/// same ref tag
		/// </summary>
		/// <param name="other">            allele to compare to </param>
		/// <param name="ignoreRefState">   if true, ignore ref state in comparison </param>
		/// <returns> true if this and other are equal </returns>
		public bool Equals(Allele other, bool ignoreRefState)
		{
			return this == other || (isRef == other.isRef || ignoreRefState) && isNoCall == other.isNoCall && (bases == other.bases || bases.SequenceEqual(other.bases));
		}

		/// <param name="test">  bases to test against
		/// </param>
		/// <returns>  true if this Allele contains the same bases as test, regardless of its reference status; handles Null and NO_CALL alleles </returns>
		public bool BasesMatch(byte[] test)
		{
			return !isSymbolic && (bases == test || bases.SequenceEqual(test));
		}

		/// <param name="test">  bases to test against
		/// </param>
		/// <returns>  true if this Allele contains the same bases as test, regardless of its reference status; handles Null and NO_CALL alleles </returns>
		public bool BasesMatch(string test)
		{
			return BasesMatch(VCFUtils.StringToBytes(test.ToUpper()));
		}

		/// <param name="test">  allele to test against
		/// </param>
		/// <returns>  true if this Allele contains the same bases as test, regardless of its reference status; handles Null and NO_CALL alleles </returns>
		public bool BasesMatch(Allele test)
		{
			return BasesMatch(test.Bases);
		}

		/// <returns> the length of this allele.  Null and NO_CALL alleles have 0 length. </returns>
		public int Length
		{
            get
            {
                return isSymbolic ? 0 : bases.Length;
            }
		}

		// ---------------------------------------------------------------------------------------------------------
		//
		// useful static functions
		//
		// ---------------------------------------------------------------------------------------------------------

		public static Allele GetMatchingAllele(ICollection<Allele> allAlleles, byte[] alleleBases)
		{
			foreach (Allele a in allAlleles)
			{
				if (a.BasesMatch(alleleBases))
				{
					return a;
				}
			}

			if (WouldBeNoCallAllele(alleleBases))
			{
				return NO_CALL;
			}
			else
			{
				return null; // couldn't find anything
			}
		}

		public virtual int CompareTo(Allele other)
		{
			if (Reference && other.NonReference)
			{
				return -1;
			}
			else if (NonReference && other.Reference)
			{
				return 1;
			}
			else
			{
				return BaseString.CompareTo(other.BaseString); // todo -- potential performance issue
			}
		}

		public static bool OneIsPrefixOfOther(Allele a1, Allele a2)
		{
			if (a2.Length >= a1.Length)
			{
				return FirstIsPrefixOfSecond(a1, a2);
			}
			else
			{
				return FirstIsPrefixOfSecond(a2, a1);
			}
		}

		private static bool FirstIsPrefixOfSecond(Allele a1, Allele a2)
		{
			string a1String = a1.BaseString;
			return a2.BaseString.Substring(0, a1String.Length).Equals(a1String);
		}
	}

}