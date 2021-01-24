using System;
using System.Collections.Generic;
using System.IO;

namespace BTree
{
    public class CachingDiskBTree<T> : DiskBTree<T>
    {
        private readonly BTreeCache _cache;
        private long? _root;
        private long? _lastDeletedNode;
        private long? _lastNode;

        public CachingDiskBTree(Stream stream, long memoryLimit)
            : base(stream)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(string fileName, long memoryLimit)
            : base(fileName)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, bool leaveOpen, long memoryLimit)
            : base(stream, leaveOpen)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, int t, long memoryLimit)
            : base(stream, t)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, bool leaveOpen, int t, long memoryLimit)
            : base(stream, leaveOpen, t)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(string fileName, int t, long memoryLimit)
            : base(fileName, t)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, int t, IComparer<T> comparer, long memoryLimit)
            : base(stream, t, comparer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, bool leaveOpen, int t, IComparer<T> comparer, long memoryLimit)
            : base(stream, leaveOpen, t, comparer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(string fileName, int t, IComparer<T> comparer, long memoryLimit)
            : base(fileName, t, comparer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, int t, IItemSerializer<T> serializer, long memoryLimit)
            : base(stream, t, serializer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, bool leaveOpen, int t, IItemSerializer<T> serializer, long memoryLimit)
            : base(stream, leaveOpen, t, serializer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(string fileName, int t, IItemSerializer<T> serializer, long memoryLimit)
            : base(fileName, t, serializer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, int t, IComparer<T> comparer, IItemSerializer<T> serializer, long memoryLimit)
            : base(stream, t, comparer, serializer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(string fileName, int t, IComparer<T> comparer, IItemSerializer<T> serializer, long memoryLimit)
            : base(fileName, t, comparer, serializer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        public CachingDiskBTree(Stream stream, bool leaveOpen, int t, IComparer<T> comparer, IItemSerializer<T> serializer, long memoryLimit)
            : base(stream, leaveOpen, t, comparer, serializer)
        {
            _cache = new BTreeCache(memoryLimit);
        }

        protected override void WriteRoot(BTreeNode rootNode)
        {
            _root = ((DiskBTreeNode) rootNode).Id;
            base.WriteRoot(rootNode);
        }

        protected override void ReadRoot(BTreeNode rootNode)
        {
            if (_root.HasValue)
                ((DiskBTreeNode) rootNode).Id = _root.Value;
            else
                base.ReadRoot(rootNode);
        }

        protected override void WriteLastDeletedNode(long id)
        {
            _lastDeletedNode = id;
            base.WriteLastDeletedNode(id);
        }

        protected override long ReadLastDeletedNode() => _lastDeletedNode ?? base.ReadLastDeletedNode();

        protected override void WriteLastNode(long id)
        {
            _lastNode = id;
            base.WriteLastNode(id);
        }

        protected override long ReadLastNode() => _lastNode ?? base.ReadLastNode();

        protected override void WriteOnDisk(long offset, ReadOnlySpan<byte> buffer)
        {
            _cache?.Set(offset, buffer);
            base.WriteOnDisk(offset, buffer);
        }

        protected override bool ReadFromDisk(long offset, Span<byte> buffer) => _cache.TryGet(offset, buffer) || base.ReadFromDisk(offset, buffer);
    }

    internal class BTreeCache
    {
        private readonly long _memoryLimit;
        private readonly Dictionary<long, Node> _dict;
        private Node _first;
        private Node _last;
        private long _memoryUsage;

        public BTreeCache(long memoryLimit)
        {
            if (memoryLimit < 0)
                throw new ArgumentException($"'{nameof(memoryLimit)}' must be a non-negative number", nameof(memoryLimit));
            _memoryLimit = memoryLimit;
            _dict = new Dictionary<long, Node>();
        }

        public bool TryGet(long offset, Span<byte> buffer)
        {
            if (!_dict.TryGetValue(offset, out var node))
                return false;
            if (!node.TryCopyValueTo(buffer))
                return false;
            MoveToEnd(node);
            return true;
        }

        public void Set(long offset, ReadOnlySpan<byte> buffer)
        {
            if (_dict.TryGetValue(offset, out var node))
                Delete(node);
            node = new Node(offset, buffer);
            if (node.MemoryUsage > _memoryLimit)
                return;
            while (_memoryUsage + node.MemoryUsage > _memoryLimit && _first != null)
                Delete(_first);
            Add(node);
        }

        private void Add(Node node)
        {
            _dict[node.Key] = node;
            _memoryUsage += node.MemoryUsage;
            if (_last != null)
            {
                node.Previous = _last;
                _last.Next = node;
            }
            if (_first == null)
                _first = node;
            _last = node;
        }

        private void Delete(Node node)
        {
            _dict.Remove(node.Key);
            _memoryUsage -= node.MemoryUsage;
            if (node.Next != null)
            {
                if (node.Previous != null)
                {
                    node.Next.Previous = node.Previous;
                    node.Previous.Next = node.Next;
                }
                else
                {
                    node.Next.Previous = null;
                    _first = node.Next;
                }
            }
            else
            {
                if (node.Previous != null)
                {
                    node.Previous.Next = null;
                    _last = node.Previous;
                }
                else
                {
                    _first = null;
                    _last = null;
                }
            }
        }

        private void MoveToEnd(Node node)
        {
            if (node.Next == null)
                return;
            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
                node.Next.Previous = node.Previous;
            }
            else
            {
                _first = node.Next;
                node.Next.Previous = null;
            }
            node.Previous = _last;
            node.Next = null;
            _last = node;
        }

        private class Node
        {
            private readonly byte[] _value;

            public long Key { get; }
            public Node Previous { get; set; }
            public Node Next { get; set; }

            public long MemoryUsage =>
                IntPtr.Size * 3 + // Object header
                IntPtr.Size + // Reference to "Previous"
                IntPtr.Size + // Reference to "Next"
                8 + // "Key"
                IntPtr.Size * 3 // Reference to "_value"
                + (long) Math.Ceiling(_value.Length / (double) IntPtr.Size) * IntPtr.Size // Payload of "_value"
                + IntPtr.Size * 3 + 8; // Minimal dictionary overhead + stored "Key"

            public Node(long key, ReadOnlySpan<byte> value)
            {
                Key = key;
                _value = new byte[value.Length];
                value.CopyTo(_value);
            }

            public bool TryCopyValueTo(Span<byte> buffer)
            {
                if (_value.Length > buffer.Length)
                    return false;
                _value.CopyTo(buffer);
                return true;
            }
        }
    }
}
