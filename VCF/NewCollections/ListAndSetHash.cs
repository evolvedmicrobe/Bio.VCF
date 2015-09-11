using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bio.VCF
{
    /// <summary>
    /// This class is designed to aid in the parsing of filter lines from VCF files, at various points in the code
    /// a filter line, e.g. “q10;s50” is parsed in to a list (q10,s50) and a hashset (q10, s50).  To keep only one copy
    /// of each redundant list and hashset around, this class will return hashsets and lists for a given filter line in a
    /// VCF file
    /// </summary>
    public class ListAndSetHash
    {
        /// <summary>
        /// A read only list and hash with identical items
        /// </summary>
        public class ListAndHash
        {
            public IList<string> List;
            public ISet<string> Hash;
        }
        AbstractVCFCodec parentCodec;
        /// <summary>
        /// Map of unparsed strings to IList
        /// </summary>
        private Dictionary<string, ListAndHash> _strToListAndHash = new Dictionary<string, ListAndHash>();
        /// <summary>
        /// Constructor used to get the codec and map from these things 
        /// </summary>
        /// <param name="baseCodec">The base codec, necessary to get the string cache from</param>
        public ListAndSetHash(AbstractVCFCodec baseCodec)
        {
            this.parentCodec = baseCodec;
            ListAndHash passList = new ListAndHash() { List = new List<string>().AsReadOnly(), Hash = new ImmutableHashSet<string>() };
            _strToListAndHash[VCFConstants.PASSES_FILTERS_v3] = passList;
            _strToListAndHash[VCFConstants.PASSES_FILTERS_v4] = passList;
        }
        private ListAndHash createNewCachedFilter(string filterString)
        {
            List<string> fFields=null;
            // otherwise we have to parse and cache the value
            if (filterString.IndexOf(VCFConstants.FILTER_CODE_SEPARATOR) == -1)
            {
                fFields = new List<string>(1);
                fFields.Add(filterString);
            }
            else
            {
                fFields = (from x in filterString.Split(VCFConstants.FILTER_CODE_SEPARATOR_CHAR_ARRAY, StringSplitOptions.RemoveEmptyEntries) select parentCodec.GetCachedString(x)).ToList();
            }
            var list = fFields.AsReadOnly();
            var hash = (from x in _strToListAndHash.Values where x.Hash.SetEquals(list) select x.Hash).FirstOrDefault();
            if (hash == null)
                hash = new ImmutableHashSet<string>(list);
            ListAndHash toAdd = new ListAndHash() { List = list, Hash = hash };
            _strToListAndHash[filterString] = toAdd;
            return toAdd;
        }
        public ListAndHash this[string filterString]
        {
            get
            {
                if (filterString == VCFConstants.UNFILTERED)
                { return null; }
                ListAndHash toReturn;
                bool val = _strToListAndHash.TryGetValue(filterString, out toReturn);
                if (!val)
                {
                    return createNewCachedFilter(filterString);
                }
                else
                {
                    return toReturn;
                }
            }
           

        }
        public bool ContainsKey(string filterString)
        {
            return _strToListAndHash.ContainsKey(filterString);
        }

    }
}
