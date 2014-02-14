using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bio.VCF
{
    public class ImmutableHashSet<T> : IEnumerable<T>,ISet<T>
    {
        HashSet<T> _set=new HashSet<T>();
        const string EXCEPTION="Attempt to modify ImmutableHashSetClass";
        public ImmutableHashSet()
        {
        }
        public ImmutableHashSet(IEnumerable<T> toAdd,bool isReadOnly=false)
        {
            foreach(var v in toAdd)
            {
                _set.Add(v);
            }
        }        
        public bool Add(T item)
        {
                throw new Exception(EXCEPTION);
        }
        public void AddRange(IEnumerable<T> items)
        {
                throw new Exception(EXCEPTION);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
                throw new Exception(EXCEPTION);
        }

        public void IntersectWith(IEnumerable<T> other)
        {throw new Exception(EXCEPTION);}

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
            return _set.IsSubsetOf(other);
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
        {throw new Exception(EXCEPTION);}


        public void UnionWith(IEnumerable<T> other)
        {throw new Exception(EXCEPTION);}

        void ICollection<T>.Add(T item)
        {throw new Exception(EXCEPTION);}


        public void Clear()
        { throw new Exception(EXCEPTION); }

        public bool Contains(T item)
        {
            return _set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _set.CopyTo(array,arrayIndex);
        }
        public int Count
        {
            get { return _set.Count; }
        }

        public bool IsReadOnly
        {
            get{return true;}
            
        }
        public bool Remove(T item)
       {throw new Exception(EXCEPTION);}

        
        public void RemoveRange(IEnumerable<T> items)
        { throw new Exception(EXCEPTION); }


        public IEnumerator<T> GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _set.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _set.GetEnumerator();
        }
    }
    
}
