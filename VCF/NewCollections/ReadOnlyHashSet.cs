using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PicardSharp.PicardSharp.org.broadinstitute.variant.vcf
{
    class ReadOnlyHashSet<T> : ISet<T>
    {
        private HashSet<T> _set = new HashSet<T>();
        public ReadOnlyHashSet()
        {
            
        }
        public bool Add(T item)
        {
            throw new Exception("Tried to modify read only collection");
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new Exception("Tried to modify read only collection");
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new Exception("Tried to modify read only collection");
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return _set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return _set.IsProperSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return _set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return _set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return _set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new Exception("Tried to modify read only collection");
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new Exception("Tried to modify read only collection");
        
        }

        void ICollection<T>.Add(T item)
        {
            throw new Exception("Tried to modify read only collection");
        }

        public void Clear()
        {
            throw new Exception("Tried to modify read only collection");
        
        }

        public bool Contains(T item)
        {
           return _set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _set.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            throw new Exception("Tried to modify read only collection");
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }
    }
}
