using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bio.VCF
{
    public class OrderedGenericDictionary<KeyT,ValueT> :IDictionary<KeyT, ValueT>
    {
        Dictionary<KeyT, ValueT> _dict = new Dictionary<KeyT, ValueT>();
        List<KeyT> _insertOrder = new List<KeyT>();
        public OrderedGenericDictionary(int InitialCount)
        {
            //skip this for now, not much memory savings
        }
        public OrderedGenericDictionary()
        {
            //skip this for now, not much memory savings
        }
        public void Add(KeyT key, ValueT value)
        {
            if (!_dict.ContainsKey(key))
            {
                _insertOrder.Add(key);
            }
            _dict.Add(key, value);
        }
        public void putAll(IDictionary<KeyT, ValueT> toAdd)
        {
            foreach (var q in toAdd)
            {
                this.Add(q);
            }
        }
        public bool ContainsKey(KeyT key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<KeyT> Keys
        {
            get { return _insertOrder.ToList(); }
        }

        public bool Remove(KeyT key)
        {
            if (!_dict.ContainsKey(key))
            {
                _dict.Remove(key);
                _insertOrder.Remove(key);
                return true;
            }
            return false;
        }

        public bool TryGetValue(KeyT key, out ValueT value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public ICollection<ValueT> Values
        {
            get 
            {
                return _insertOrder.Select(x => _dict[x]).ToList();
            }
        }

        public ValueT this[KeyT key]
        {
            get
            {
                return _dict[key];
            }
            set
            {
                this.Add(key, value);
            }
        }

        public void Add(KeyValuePair<KeyT, ValueT> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
            _insertOrder.Clear();
        }

        public bool Contains(KeyValuePair<KeyT, ValueT> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<KeyT, ValueT>[] array, int arrayIndex)
        {
            var toR=_insertOrder.Select(x => new KeyValuePair<KeyT, ValueT>(x, _dict[x])).ToArray();
            toR.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _insertOrder.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<KeyT, ValueT> item)
        {
            ValueT val;
            bool isIn=_dict.TryGetValue(item.Key,out val);
            if (isIn && val.Equals(item.Value))
            {
                _insertOrder.Remove(item.Key);
                _dict.Remove(item.Key);
                return true;
            }
            return false;
        }

        public IEnumerator<KeyValuePair<KeyT, ValueT>> GetEnumerator()
        {
            foreach (var key in _insertOrder)
            {
                yield return new KeyValuePair<KeyT, ValueT>(key, _dict[key]);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
