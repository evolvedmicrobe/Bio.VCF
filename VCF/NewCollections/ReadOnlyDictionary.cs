using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Bio.VCF
{
    public class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        protected Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();


        public ReadOnlyDictionary()
        {

        }
        public ReadOnlyDictionary(IEnumerable<KeyValuePair<TKey,TValue>> set)
        {
            foreach (var kv in set)
            {
                _dict.Add(kv.Key, kv.Value);
            }
        }
        public void Add(TKey key, TValue value)
        {
            throw new Exception("Tried to write to read only dictionary");
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _dict.Keys; }
        }

        public bool Remove(TKey key)
        {
            throw new Exception("Tried to remove item from read only dictionary");
        }
        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dict.TryGetValue(key, out value);
        }
        public ICollection<TValue> Values
        {
            get { return _dict.Values; }
        }
        public TValue this[TKey key]
        {
            get
            {
                return _dict[key];
            }
            set
            {
                throw new Exception("Cannot set value in read only collection");
            }
        }
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new Exception("Tried to write to read only dictionary");
        }
        public void Clear()
        {
            throw new Exception("Tried to clear read only dictionary");
        }
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dict.Contains(item);
        }
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary)_dict).CopyTo(array, arrayIndex);
        }
        public int Count
        {
            get { return _dict.Count; }
        }
        public bool IsReadOnly
        {
            get { return true; }
        }
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new Exception("Cannot remove items from read only dictionary");
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
