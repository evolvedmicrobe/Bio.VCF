using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Bio.VCF
{
    /// <summary>
    /// Class to mimic a Java linked list, note not actually using a linked list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class LinkedHashSet<T> : IEnumerable<T>,ISet<T>
    {
        HashSet<T> set=new HashSet<T>();
        List<T> order = new List<T>();
        bool oneAdded = false;

        public LinkedHashSet(bool isReadOnly=false)
        {
            this.IsReadOnly = IsReadOnly;
        }
        
        public LinkedHashSet(IEnumerable<T> toAdd,bool isReadOnly=false)
        {
            foreach(var v in toAdd)
            {
                this.Add(v);
            }
            this.IsReadOnly=true;
        }

        /// <summary>
        /// To implement a linked list in the dictionary
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        
        //[Serializable]
        //struct InternalLink<T>: IComparable<T>
        //{
        //    public T Before;
        //    public T After;
        //    public T Self;
           
        //    public override bool Equals(object obj)
        //    {
        //        if (obj is InternalLink<T>)
        //        {
        //            var il = (InternalLink<T>)obj;
        //            return il.Self.Equals(this.Self);
        //        }
        //        else
        //            return false;
        //    }
        //    public override int GetHashCode()
        //    {
        //        return Self.GetHashCode();
        //    }
        //    public override string ToString()
        //    {
        //        return Self.ToString();
        //    }
           
        //}

        public bool Add(T item)
        {
            if (!IsReadOnly)
            {
                if (!set.Contains(item))
                {
                    set.Add(item);
                    order.Add(item);
                    return true;
                }
                else { return false; }
            }
            else
            {
                throw new Exception("Can't add to read only collection");
            }
        }
        public void AddRange(IEnumerable<T> items)
        {
            if (!IsReadOnly)
            {
            foreach(var v in items)
            {
                this.Add(v);
            }
                }
            else
            {
                throw new Exception("Can't add to read only collection");
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            set.ExceptWith(other);
            order = order.Where(x => !other.Contains(x)).ToList();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            set.IntersectWith(other);
            order = order.Where(x => other.Contains(x)).ToList();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return set.IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return set.IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return set.IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return set.IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return set.Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return set.SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException("Not implementing symmetric exception as ordering might not be guaranteed");
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException("Not implementing symmetric exception as ordering might not be guaranteed");
        }

        void ICollection<T>.Add(T item)
        {
            if (!IsReadOnly)
            {
            if (!set.Contains(item))
            {
                set.Add(item);
                order.Add(item);
            }
                }
            else
            {
                throw new Exception("Can't add to read only collection");
            }
        }

        public void Clear()
        {
            if (!IsReadOnly)
            {
                set.Clear();
                order.Clear();
            }
        }

        public bool Contains(T item)
        {
            return set.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            order.CopyTo(array,arrayIndex);
        }

        public int Count
        {
            get { return order.Count; }
        }

        public bool IsReadOnly
        {
            get;private set;
        }

        public bool Remove(T item)
        {
            if (!IsReadOnly)
            {
            if (set.Contains(item))
            {
                set.Remove(item);
                order.Remove(item);
                return true;
            }
            else { return false; }
                }
            else
            {
                throw new Exception("Can't add to read only collection");
            }
        }
        public void RemoveRange(IEnumerable<T> items)
        {
            if (!IsReadOnly)
            {
            foreach (var v in items)
            {
                if (!Remove(v))
                {
                    throw new Exception("Tried to remove element not in the collection");
                }
            }
            }
            else
            {
                throw new Exception("Can't add to read only collection");
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return order.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return order.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return order.GetEnumerator();
        }
    }
    
}
