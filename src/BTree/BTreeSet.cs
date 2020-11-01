using System.Collections;
using System.Collections.Generic;

namespace BTree
{
    public class BTreeSet<T> : ISet<T>
    {
        private BTree<T> _bTree;

        public int Count { get; }
        public bool IsReadOnly => false;

        public IEnumerator<T> GetEnumerator()
        {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            _bTree.Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            throw new System.NotImplementedException();
        }

        bool ISet<T>.Add(T item)
        {
            _bTree.Add(item);
            return true;
        }

        public void Clear()
        {
            throw new System.NotImplementedException();
        }

        public bool Contains(T item)
        {
            return _bTree.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(T item)
        {
            return _bTree.Remove(item);
        }
    }
}
