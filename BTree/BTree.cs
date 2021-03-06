﻿using System;
using System.Collections.Generic;

namespace BTree
{
    public class BTree<T>
    {
        public const int DefaultT = 20;

        private readonly int _t;
        private readonly IComparer<T> _comparer;

        protected BTreeNode Root { get; set; }
        protected int MaxItemsCount { get; }
        protected int MaxChildrenCount { get; }

        public BTree()
            : this(DefaultT, Comparer<T>.Default)
        {
        }

        public BTree(int t)
            : this(t, Comparer<T>.Default)
        {
        }

        public BTree(IComparer<T> comparer)
            : this(DefaultT, comparer)
        {
        }

        public BTree(int t, IComparer<T> comparer)
        {
            if (t <= 1)
                throw new ArgumentException($"'{nameof(t)}' must be a positive number and greater than 1", nameof(t));
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            _t = t;
            _comparer = comparer;
            MaxItemsCount = 2 * _t - 1;
            MaxChildrenCount = 2 * _t;
        }

        public virtual void Add(T item)
        {
            InitIfNeeded();
            AddInternal(item);
        }

        public virtual bool Remove(T item)
        {
            InitIfNeeded();
            return RemoveInternal(item);
        }

        public virtual bool Update(T item, Func<T, T> updater)
        {
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));
            InitIfNeeded();
            return UpdateInternal(item, updater);
        }

        public virtual bool Contains(T item)
        {
            InitIfNeeded();
            return ContainsInternal(item);
        }

        public virtual IEnumerable<T> Enumerate(bool reverse)
        {
            InitIfNeeded();
            if (reverse)
            {
                var stack = new Stack<(BTreeNode, int)>();
                stack.Push((Root, Root.N));
                return EnumerateReverse(stack);
            }
            else
            {
                var stack = new Stack<(BTreeNode, int)>();
                stack.Push((Root, 0));
                return Enumerate(stack);
            }
        }

        public virtual IEnumerable<T> EnumerateFrom(T item, bool reverse)
        {
            InitIfNeeded();
            if (!reverse)
            {
                var stack = SearchFirst(Root, item);
                return Enumerate(stack);
            }
            else
            {
                var stack = SearchLast(Root, item);
                return EnumerateReverse(stack);
            }
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

        protected virtual BTreeNode AllocateNode()
        {
            return new BTreeNode
            {
                Items = new T[MaxItemsCount],
                Children = new BTreeNode[MaxChildrenCount],
                IsLeaf = true
            };
        }

        protected virtual void FreeNode(BTreeNode node)
        {
        }

        private void AddInternal(T item)
        {
            var r = Root;
            if (r.N == 2 * _t - 1)
            {
                var s = AllocateNode();
                Root = s;
                s.IsLeaf = false;
                s.Children[0] = r;
                SplitChild(s, 0);
                AddNonFull(s, item);
                WriteRoot(Root);
            }
            else
                AddNonFull(r, item);
        }

        private bool RemoveInternal(T item)
        {
            if (!Remove(Root, item))
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

        private bool UpdateInternal(T item, Func<T, T> updater)
        {
            var (node, i) = SearchAny(Root, item);
            if (i < 0)
                return false;
            var oldItem = node.Items[i];
            var newItem = updater(oldItem);
            if (_comparer.Compare(oldItem, newItem) == 0)
            {
                node.Items[i] = newItem;
                Write(node);
            }
            else
            {
                AddInternal(newItem);
                RemoveInternal(oldItem);
            }
            return true;
        }

        private bool ContainsInternal(T item)
        {
            var (_, i) = SearchAny(Root, item);
            return i >= 0;
        }

        private void InitIfNeeded()
        {
            if (Root == null)
            {
                Root = AllocateNode();
                ReadRoot(Root);
            }
            Read(Root);
        }

        private IEnumerable<T> Enumerate(Stack<(BTreeNode, int)> stack)
        {
            while (stack.TryPop(out var tuple))
            {
                var (node, i) = tuple;
                if (i < 0)
                    i = ~i;
                if (node.IsLeaf)
                {
                    for (var j = i; j < node.N; j++)
                        yield return node.Items[j];
                    FreeNode(node);
                }
                else
                {
                    if (i > 0)
                        yield return node.Items[i - 1];
                    if (i < node.N)
                        stack.Push((node, i + 1));
                    var child = node.Children[i];
                    Read(child);
                    stack.Push((child, 0));
                    if (i == node.N)
                        FreeNode(node);
                }
            }
        }

        private IEnumerable<T> EnumerateReverse(Stack<(BTreeNode, int)> stack)
        {
            while (stack.TryPop(out var tuple))
            {
                var (node, i) = tuple;
                if (i < -1)
                    i = ~i;
                if (node.IsLeaf)
                {
                    for (var j = i; j > 0; j--)
                        yield return node.Items[j - 1];
                    FreeNode(node);
                }
                else
                {
                    if (i < node.N)
                        yield return node.Items[i];
                    if (i > 0)
                        stack.Push((node, i - 1));
                    var child = node.Children[i];
                    Read(child);
                    stack.Push((child, child.N));
                    if (i == 0)
                        FreeNode(node);
                }
            }
        }

        private Stack<(BTreeNode, int)> SearchFirst(BTreeNode node, T item)
        {
            var stack = new Stack<(BTreeNode, int)>();
            while (true)
            {
                var i = BinarySearch(node, item);
                i = FindLeftBorder(node, item, i);
                if (node.IsLeaf)
                {
                    stack.Push((node, i));
                    break;
                }
                if (i < 0)
                {
                    i = ~i;
                    if (i < node.N)
                        stack.Push((node, i + 1));
                }
                else
                    stack.Push((node, i + 1));
                node = node.Children[i];
                Read(node);
            }
            return stack;
        }

        private Stack<(BTreeNode, int)> SearchLast(BTreeNode node, T item)
        {
            var stack = new Stack<(BTreeNode, int)>();
            while (true)
            {
                var i = BinarySearch(node, item);
                i = FindRightBorder(node, item, i);
                if (node.IsLeaf)
                {
                    stack.Push((node, i));
                    break;
                }
                if (i < 0)
                {
                    i = ~i;
                    if (i > 0)
                        stack.Push((node, i - 1));
                }
                else
                    stack.Push((node, i - 1));
                node = node.Children[i];
                Read(node);
            }
            return stack;
        }

        private (BTreeNode, int) SearchAny(BTreeNode node, T item)
        {
            while (true)
            {
                var i = BinarySearch(node, item);
                i = FindLeftBorder(node, item, i);
                if (node.IsLeaf)
                    return (node, i);
                if (i < 0)
                    i = ~i;
                else
                    return (node, i);
                node = node.Children[i];
                Read(node);
            }
        }

        private int BinarySearch(BTreeNode node, T item)
        {
            var l = 0;
            var r = node.N - 1;
            var i = 0;
            while (r >= l)
            {
                i = (l + r) / 2;
                var cmp = _comparer.Compare(item, node.Items[i]);
                if (cmp < 0)
                    r = i - 1;
                else if (cmp > 0)
                    l = i + 1;
                else
                    return i;
            }
            while (i < node.N && _comparer.Compare(item, node.Items[i]) > 0)
                i++;
            return ~i;
        }

        private int FindLeftBorder(BTreeNode node, T item, int startIndex)
        {
            var i = startIndex;
            var inverse = i < 0;
            if (inverse)
                i = ~i;
            while (i > 0 && _comparer.Compare(item, node.Items[i - 1]) == 0)
                i--;
            if (inverse)
                i = ~i;
            return i;
        }

        private int FindRightBorder(BTreeNode node, T item, int startIndex)
        {
            var i = startIndex;
            var inverse = i < 0;
            if (inverse)
                i = ~i;
            while (i < node.N && _comparer.Compare(item, node.Items[i]) == 0)
                i++;
            if (inverse)
                i = ~i;
            return i;
        }

        private void SplitChild(BTreeNode node, int i)
        {
            var rightChild = AllocateNode();
            var leftChild = node.Children[i];
            rightChild.IsLeaf = leftChild.IsLeaf;
            rightChild.N = _t - 1;
            for (var j = 0; j < _t - 1; j++)
                rightChild.Items[j] = leftChild.Items[j + _t];
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
                node.Items[j] = node.Items[j - 1];
            node.Items[i] = leftChild.Items[_t - 1];
            node.N++;
            Write(leftChild);
            Write(rightChild);
            Write(node);
        }

        private void AddNonFull(BTreeNode node, T item)
        {
            var i = node.N;
            if (node.IsLeaf)
            {
                while (i > 0 && _comparer.Compare(item, node.Items[i - 1]) < 0)
                {
                    node.Items[i] = node.Items[i - 1];
                    i--;
                }
                node.Items[i] = item;
                node.N++;
                Write(node);
            }
            else
            {
                while (i > 0 && _comparer.Compare(item, node.Items[i - 1]) < 0)
                    i--;
                i++;
                Read(node.Children[i - 1]);
                if (node.Children[i - 1].N == 2 * _t - 1)
                {
                    SplitChild(node, i - 1);
                    if (_comparer.Compare(item, node.Items[i - 1]) > 0)
                        i++;
                }
                AddNonFull(node.Children[i - 1], item);
            }
        }

        private bool Remove(BTreeNode node, T item)
        {
            var i = BinarySearch(node, item);
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
                return Remove(node.Children[i], item);
            }
        }

        private void RemoveFromLeaf(BTreeNode node, int i)
        {
            for (var j = i + 1; j < node.N; j++)
                node.Items[j - 1] = node.Items[j];
            node.N--;
            Write(node);
        }

        private void RemoveFromNonLeaf(BTreeNode node, int i)
        {
            Read(node.Children[i]);
            if (node.Children[i].N >= _t)
            {
                var pred = GetPred(node, i);
                node.Items[i] = pred;
                Remove(node.Children[i], pred);
                Write(node);
                return;
            }
            Read(node.Children[i + 1]);
            if (node.Children[i + 1].N >= _t)
            {
                var succ = GetSucc(node, i);
                node.Items[i] = succ;
                Remove(node.Children[i + 1], succ);
                Write(node);
                return;
            }
            var item = node.Items[i];
            Merge(node, i);
            Remove(node.Children[i], item);
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
            return cur.Items[cur.N - 1];
        }

        private T GetSucc(BTreeNode node, int i)
        {
            var cur = node.Children[i + 1];
            while (!cur.IsLeaf)
            {
                cur = cur.Children[0];
                Read(cur);
            }
            return cur.Items[0];
        }

        private void Merge(BTreeNode node, int i)
        {
            var child = node.Children[i];
            var sibling = node.Children[i + 1];
            Read(child);
            Read(sibling);
            child.Items[_t - 1] = node.Items[i];
            for (var j = 0; j < sibling.N; j++)
                child.Items[j + _t] = sibling.Items[j];
            if (!child.IsLeaf)
            {
                for (var j = 0; j <= sibling.N; j++)
                    child.Children[j + _t] = sibling.Children[j];
            }
            for (var j = i + 1; j < node.N; j++)
                node.Items[j - 1] = node.Items[j];
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
                child.Items[j + 1] = child.Items[j];
            if (!child.IsLeaf)
            {
                for (var j = child.N; j >= 0; j--)
                    child.Children[j + 1] = child.Children[j];
            }
            child.Items[0] = node.Items[i - 1];
            if (!child.IsLeaf)
                child.Children[0] = sibling.Children[sibling.N];
            node.Items[i - 1] = sibling.Items[sibling.N - 1];
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
            child.Items[child.N] = node.Items[i];
            if (!child.IsLeaf)
                child.Children[child.N + 1] = sibling.Children[0];
            node.Items[i] = sibling.Items[0];
            for (var j = 1; j < sibling.N; j++)
                sibling.Items[j - 1] = sibling.Items[j];
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
            public T[] Items { get; set; }
            public BTreeNode[] Children { get; set; }
        }
    }
}
