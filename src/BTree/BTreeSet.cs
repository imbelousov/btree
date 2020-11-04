using System;
using System.Collections;
using System.Collections.Generic;

namespace BTree
{
    public class BTreeSet<T> : ISet<T>
    {
        private readonly BTree<T> _bTree;

        public int Count => throw new NotImplementedException();
        public bool IsReadOnly => false;

        public BTreeSet(BTree<T> bTree)
        {
            _bTree = bTree;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            return _bTree.Enumerate(false).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual void ExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
                _bTree.Remove(item);
        }

        public virtual void IntersectWith(IEnumerable<T> other)
        {
            var toRemove = new List<T>();
            foreach (var item in other)
            {
                if (_bTree.Contains(item))
                    continue;
                toRemove.Add(item);
            }
            foreach (var item in toRemove)
                _bTree.Remove(item);
        }

        public virtual bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual bool Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual bool SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual void UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual bool Add(T item)
        {
            _bTree.Add(item);
            return true;
        }

        void ICollection<T>.Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            Add(item);
        }

        public virtual void Clear()
        {
            throw new NotImplementedException();
        }

        public virtual bool Contains(T item)
        {
            return _bTree.Contains(item);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in _bTree.Enumerate(false))
                array[arrayIndex++] = item;
        }

        public virtual bool Remove(T item)
        {
            return _bTree.Remove(item);
        }
    }
}
