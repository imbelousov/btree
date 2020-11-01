using System;
using System.Collections.Generic;

namespace BTree
{
    public class BTreeBase<T>
    {
        private readonly int _t;
        private readonly IComparer<T> _comparer;

        protected BTreeNode Root { get; set; }

        public BTreeBase(int t, IComparer<T> comparer)
        {
            _t = t;
            _comparer = comparer;
        }

        public virtual void Insert(T key)
        {
            InitIfNeeded();
            InsertInternal(key);
        }

        public virtual bool Remove(T key)
        {
            InitIfNeeded();
            return RemoveInternal(key);
        }

        public virtual bool Update(T key, Func<T, T> updater)
        {
            InitIfNeeded();
            return UpdateInternal(key, updater);
        }

        public virtual bool Contains(T key)
        {
            InitIfNeeded();
            return ContainsInternal(key);
        }

        protected virtual void Read(BTreeNode node)
        {
        }

        protected virtual void ReadRoot(BTreeNode rootNode)
        {
        }

        protected virtual void Write(BTreeNode node)
        {
        }

        protected virtual void WriteRoot(BTreeNode rootNode)
        {
        }

        protected virtual void Delete(BTreeNode node)
        {
        }

        protected int MaxKeysCount => 2 * _t - 1;

        protected int MaxChildrenCount => 2 * _t;

        protected virtual BTreeNode AllocateNode()
        {
            return new BTreeNode
            {
                Keys = new T[MaxKeysCount],
                Children = new BTreeNode[MaxChildrenCount],
                IsLeaf = true
            };
        }

        private void InsertInternal(T key)
        {
            var r = Root;
            if (r.N == 2 * _t - 1)
            {
                var s = AllocateNode();
                Root = s;
                s.IsLeaf = false;
                s.Children[0] = r;
                SplitChild(s, 0);
                InsertNonFull(s, key);
                WriteRoot(Root);
            }
            else
                InsertNonFull(r, key);
        }

        private bool RemoveInternal(T key)
        {
            if (!Remove(Root, key))
                return false;
            if (Root.N == 0 && !Root.IsLeaf)
            {
                var r = Root;
                Root = r.Children[0];
                WriteRoot(Root);
                Delete(r);
            }
            return true;
        }

        private bool UpdateInternal(T key, Func<T, T> updater)
        {
            var (node, i) = DeepSearch(Root, key);
            if (i < 0)
                return false;
            var oldKey = node.Keys[i];
            var newKey = updater(oldKey);
            if (_comparer.Compare(oldKey, newKey) == 0)
            {
                node.Keys[i] = newKey;
                Write(node);
            }
            else
            {
                InsertInternal(newKey);
                RemoveInternal(oldKey);
            }
            return true;
        }

        private bool ContainsInternal(T key)
        {
            var (_, i) = DeepSearch(Root, key);
            return i >= 0;
        }

        private void InitIfNeeded()
        {
            if (Root != null)
                return;
            Root = AllocateNode();
            ReadRoot(Root);
            Read(Root);
        }

        private (BTreeNode, int) DeepSearch(BTreeNode node, T key)
        {
            var i = BinarySearch(node, key);
            if (i >= 0)
                return (node, i);
            if (node.IsLeaf)
                return (null, -1);
            i = ~i;
            Read(node.Children[i]);
            return DeepSearch(node.Children[i], key);
        }

        private int BinarySearch(BTreeNode node, T key)
        {
            var l = 0;
            var r = node.N - 1;
            var i = 0;
            while (r >= l)
            {
                i = (l + r) / 2;
                var cmp = _comparer.Compare(key, node.Keys[i]);
                if (cmp < 0)
                    r = i - 1;
                else if (cmp > 0)
                    l = i + 1;
                else
                {
                    while (i < node.N - 1 && _comparer.Compare(key, node.Keys[i + 1]) == 0)
                        i++;
                    return i;
                }
            }
            while (i < node.N && _comparer.Compare(key, node.Keys[i]) > 0)
                i++;
            return ~i;
        }

        private void SplitChild(BTreeNode node, int i)
        {
            var rightChild = AllocateNode();
            var leftChild = node.Children[i];
            rightChild.IsLeaf = leftChild.IsLeaf;
            rightChild.N = _t - 1;
            for (var j = 0; j < _t - 1; j++)
                rightChild.Keys[j] = leftChild.Keys[j + _t];
            if (!leftChild.IsLeaf)
            {
                for (var j = 0; j < _t; j++)
                    rightChild.Children[j] = leftChild.Children[j + _t];
            }
            leftChild.N = _t - 1;
            for (var j = node.N + 1; j > i + 1; j--)
                node.Children[j] = node.Children[j - 1];
            node.Children[i + 1] = rightChild;
            for (var j = node.N; j > i; j--)
                node.Keys[j] = node.Keys[j - 1];
            node.Keys[i] = leftChild.Keys[_t - 1];
            node.N++;
            Write(leftChild);
            Write(rightChild);
            Write(node);
        }

        private void InsertNonFull(BTreeNode node, T key)
        {
            var i = node.N;
            if (node.IsLeaf)
            {
                while (i > 0 && _comparer.Compare(key, node.Keys[i - 1]) < 0)
                {
                    node.Keys[i] = node.Keys[i - 1];
                    i--;
                }
                node.Keys[i] = key;
                node.N++;
                Write(node);
            }
            else
            {
                while (i > 0 && _comparer.Compare(key, node.Keys[i - 1]) < 0)
                    i--;
                i++;
                Read(node.Children[i - 1]);
                if (node.Children[i - 1].N == 2 * _t - 1)
                {
                    SplitChild(node, i - 1);
                    if (_comparer.Compare(key, node.Keys[i - 1]) > 0)
                        i++;
                }
                InsertNonFull(node.Children[i - 1], key);
            }
        }

        private bool Remove(BTreeNode node, T key)
        {
            var i = BinarySearch(node, key);
            if (i >= 0)
            {
                if (node.IsLeaf)
                    RemoveFromLeaf(node, i);
                else
                    RemoveFromNonLeaf(node, i);
                return true;
            }
            else
            {
                i = ~i;
                if (node.IsLeaf)
                    return false;
                var isLast = i == node.N;
                Read(node.Children[i]);
                if (node.Children[i].N < _t)
                    Fill(node, i);
                if (isLast && i > node.N)
                    i--;
                return Remove(node.Children[i], key);
            }
        }

        private void RemoveFromLeaf(BTreeNode node, int i)
        {
            for (var j = i + 1; j < node.N; j++)
                node.Keys[j - 1] = node.Keys[j];
            node.N--;
            Write(node);
        }

        private void RemoveFromNonLeaf(BTreeNode node, int i)
        {
            Read(node.Children[i]);
            if (node.Children[i].N >= _t)
            {
                var pred = GetPred(node, i);
                node.Keys[i] = pred;
                Remove(node.Children[i], pred);
                Write(node);
                return;
            }
            Read(node.Children[i + 1]);
            if (node.Children[i + 1].N >= _t)
            {
                var succ = GetSucc(node, i);
                node.Keys[i] = succ;
                Remove(node.Children[i + 1], succ);
                Write(node);
                return;
            }
            var key = node.Keys[i];
            Merge(node, i);
            Remove(node.Children[i], key);
            Write(node);
        }

        private T GetPred(BTreeNode node, int i)
        {
            var cur = node.Children[i];
            while (!cur.IsLeaf)
            {
                cur = cur.Children[cur.N];
                Read(cur);
            }
            return cur.Keys[cur.N - 1];
        }

        private T GetSucc(BTreeNode node, int i)
        {
            var cur = node.Children[i + 1];
            while (!cur.IsLeaf)
            {
                cur = cur.Children[0];
                Read(cur);
            }
            return cur.Keys[0];
        }

        private void Merge(BTreeNode node, int i)
        {
            var child = node.Children[i];
            var sibling = node.Children[i + 1];
            Read(child);
            Read(sibling);
            child.Keys[_t - 1] = node.Keys[i];
            for (var j = 0; j < sibling.N; j++)
                child.Keys[j + _t] = sibling.Keys[j];
            if (!child.IsLeaf)
            {
                for (var j = 0; j <= sibling.N; j++)
                    child.Children[j + _t] = sibling.Children[j];
            }
            for (var j = i + 1; j < node.N; j++)
                node.Keys[j - 1] = node.Keys[j];
            for (var j = i + 2; j <= node.N; j++)
                node.Children[j - 1] = node.Children[j];
            child.N += sibling.N + 1;
            node.N--;
            Write(child);
            Delete(sibling);
        }

        private void Fill(BTreeNode node, int i)
        {
            if (i != 0)
            {
                Read(node.Children[i - 1]);
                if (node.Children[i - 1].N >= _t)
                {
                    BorrowFromPrev(node, i);
                    Write(node);
                    return;
                }
            }
            if (i != node.N)
            {
                Read(node.Children[i + 1]);
                if (node.Children[i + 1].N >= _t)
                {
                    BorrowFromNext(node, i);
                    Write(node);
                    return;
                }
            }
            if (i != node.N)
                Merge(node, i);
            else
                Merge(node, i - 1);
            Write(node);
        }

        private void BorrowFromPrev(BTreeNode node, int i)
        {
            var child = node.Children[i];
            var sibling = node.Children[i - 1];
            Read(child);
            Read(sibling);
            for (var j = child.N - 1; j >= 0; j--)
                child.Keys[j + 1] = child.Keys[j];
            if (!child.IsLeaf)
            {
                for (var j = child.N; j >= 0; j--)
                    child.Children[j + 1] = child.Children[j];
            }
            child.Keys[0] = node.Keys[i - 1];
            if (!child.IsLeaf)
                child.Children[0] = sibling.Children[sibling.N];
            node.Keys[i - 1] = sibling.Keys[sibling.N - 1];
            child.N++;
            sibling.N--;
            Write(child);
            Write(sibling);
        }

        private void BorrowFromNext(BTreeNode node, int i)
        {
            var child = node.Children[i];
            var sibling = node.Children[i + 1];
            Read(child);
            Read(sibling);
            child.Keys[child.N] = node.Keys[i];
            if (!child.IsLeaf)
                child.Children[child.N + 1] = sibling.Children[0];
            node.Keys[i] = sibling.Keys[0];
            for (var j = 1; j < sibling.N; j++)
                sibling.Keys[j - 1] = sibling.Keys[j];
            if (!sibling.IsLeaf)
            {
                for (var j = 1; j <= sibling.N; j++)
                    sibling.Children[j - 1] = sibling.Children[j];
            }
            child.N++;
            sibling.N--;
            Write(child);
            Write(sibling);
        }

        protected class BTreeNode
        {
            public bool IsLeaf { get; set; }
            public int N { get; set; }
            public T[] Keys { get; set; }
            public BTreeNode[] Children { get; set; }
        }
    }
}
