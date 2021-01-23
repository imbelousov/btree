using System;
using System.Collections.Generic;
using System.IO;

namespace BTree
{
    public class DiskBTree<T> : BTree<T>, IDisposable
    {
        private const int HeaderSize = 24;
        private const int RootIdOffset = 0;
        private const int LastDeletedNodeOffset = 8;
        private const int LastNodeOffset = 16;
        private const int ExpansionSize = 1024;
        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly IItemSerializer<T> _serializer;

        protected int PageSize => 1 + 4 + _serializer.MaxSerializedItemLength * MaxItemsCount + 8 * MaxChildrenCount;

        public DiskBTree(Stream stream)
            : this(stream, false, DefaultT, Comparer<T>.Default, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(string fileName)
            : this(fileName, DefaultT, Comparer<T>.Default, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(Stream stream, bool leaveOpen)
            : this(stream, leaveOpen, DefaultT, Comparer<T>.Default, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(Stream stream, int t)
            : this(stream, false, t, Comparer<T>.Default, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(Stream stream, bool leaveOpen, int t)
            : this(stream, leaveOpen, t, Comparer<T>.Default, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(string fileName, int t)
            : this(fileName, t, Comparer<T>.Default, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(Stream stream, int t, IComparer<T> comparer)
            : this(stream, false, t, comparer, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(Stream stream, bool leaveOpen, int t, IComparer<T> comparer)
            : this(stream, leaveOpen, t, comparer, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(string fileName, int t, IComparer<T> comparer)
            : this(fileName, t, comparer, ItemSerializer<T>.Default)
        {
        }

        public DiskBTree(Stream stream, int t, IItemSerializer<T> serializer)
            : this(stream, false, t, Comparer<T>.Default, serializer)
        {
        }

        public DiskBTree(Stream stream, bool leaveOpen, int t, IItemSerializer<T> serializer)
            : this(stream, leaveOpen, t, Comparer<T>.Default, serializer)
        {
        }

        public DiskBTree(string fileName, int t, IItemSerializer<T> serializer)
            : this(fileName, t, Comparer<T>.Default, serializer)
        {
        }

        public DiskBTree(Stream stream, int t, IComparer<T> comparer, IItemSerializer<T> serializer)
            : this(stream, false, t, comparer, serializer)
        {
        }

        public DiskBTree(string fileName, int t, IComparer<T> comparer, IItemSerializer<T> serializer)
            : this(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite), t, comparer, serializer)
        {
        }

        public DiskBTree(Stream stream, bool leaveOpen, int t, IComparer<T> comparer, IItemSerializer<T> serializer)
            : base(t, comparer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));
            if (!stream.CanRead)
                throw new ArgumentException("Expected a readable stream", nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a writable stream", nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("Expected a seekable stream", nameof(stream));
            _stream = stream;
            _leaveOpen = leaveOpen;
            _serializer = serializer;
            if (_stream.Length < HeaderSize)
                Init();
        }

        ~DiskBTree()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (!_leaveOpen)
                _stream.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void Add(T item)
        {
            try
            {
                base.Add(item);
            }
            finally
            {
                FreeAllNodes();
            }
        }

        public override bool Remove(T item)
        {
            try
            {
                return base.Remove(item);
            }
            finally
            {
                FreeAllNodes();
            }
        }

        public override bool Update(T item, Func<T, T> updater)
        {
            try
            {
                return base.Update(item, updater);
            }
            finally
            {
                FreeAllNodes();
            }
        }

        public override bool Contains(T item)
        {
            try
            {
                return base.Contains(item);
            }
            finally
            {
                FreeAllNodes();
            }
        }

        public override IEnumerable<T> Enumerate(bool reverse)
        {
            try
            {
                foreach (var x in base.Enumerate(reverse))
                    yield return x;
            }
            finally
            {
                FreeAllNodes();
            }
        }

        public override IEnumerable<T> EnumerateFrom(T item, bool reverse)
        {
            try
            {
                foreach (var x in base.EnumerateFrom(item, reverse))
                    yield return x;
            }
            finally
            {
                FreeAllNodes();
            }
        }

        protected override void Write(BTreeNode node)
        {
            var diskNode = (DiskBTreeNode) node;
            var buffer = (Span<byte>) stackalloc byte[PageSize];
            if (diskNode.Id < 0)
                diskNode.Id = FindNewNodeId();

            var offset = WriteHeader(diskNode, buffer);
            offset += WriteChildren(diskNode, buffer.Slice(offset));
            offset += WriteItems(diskNode, buffer.Slice(offset));

            WriteAt(GetOffset(diskNode.Id), buffer.Slice(0, offset));
        }

        private int WriteHeader(DiskBTreeNode node, Span<byte> buffer)
        {
            buffer[0] = (byte) (node.IsLeaf ? NodeType.Leaf : NodeType.NonLeaf);
            BitConverter.TryWriteBytes(buffer.Slice(1, 4), node.N);
            return 5;
        }

        private int WriteChildren(DiskBTreeNode node, Span<byte> buffer)
        {
            if (node.IsLeaf)
                return 0;
            for (var i = 0; i < node.N + 1; i++)
            {
                var childNode = (DiskBTreeNode) node.Children[i];
                BitConverter.TryWriteBytes(buffer.Slice(i * 8, 8), childNode?.Id ?? -1);
            }
            return (node.N + 1) * 8;
        }

        private int WriteItems(DiskBTreeNode node, Span<byte> buffer)
        {
            var itemLength = _serializer.MaxSerializedItemLength;
            for (var i = 0; i < node.N; i++)
                _serializer.SerializeItem(node.Items[i], buffer.Slice(i * itemLength, itemLength));
            return node.N * itemLength;
        }

        protected override void WriteRoot(BTreeNode rootNode)
        {
            WriteRootId(((DiskBTreeNode) rootNode).Id);
        }

        private void WriteRootId(long id)
        {
            WriteAt(RootIdOffset, id);
        }

        protected override void Read(BTreeNode node)
        {
            var diskNode = (DiskBTreeNode) node;
            if (diskNode.Synchronized)
                return;

            var buffer = (Span<byte>)stackalloc byte[PageSize];
            if (!ReadAt(GetOffset(diskNode.Id), buffer))
                ThrowCorruptedNode(diskNode.Id);
            var offset = ReadHeader(diskNode, buffer);
            offset += ReadChildren(diskNode, buffer.Slice(offset));
            ReadItems(diskNode, buffer.Slice(offset));

            diskNode.Synchronized = true;
        }

        private int ReadHeader(DiskBTreeNode node, Span<byte> buffer)
        {
            node.IsLeaf = (NodeType) buffer[0] switch
            {
                NodeType.NonLeaf => false,
                NodeType.Leaf => true,
                _ => ThrowCorruptedNode<bool>(node.Id)
            };
            node.N = BitConverter.ToInt32(buffer.Slice(1, 4));
            if (node.N < 0 || node.N > MaxItemsCount)
                ThrowCorruptedNode<bool>(node.Id);
            return 5;
        }

        private int ReadChildren(DiskBTreeNode node, Span<byte> buffer)
        {
            if (node.IsLeaf)
                return 0;
            for (var i = 0; i < node.N + 1; i++)
            {
                var id = BitConverter.ToInt64(buffer.Slice(i * 8, 8));
                if (id >= 0)
                {
                    var childNode = (DiskBTreeNode) AllocateNode();
                    node.Children[i] = childNode;
                    childNode.Id = id;
                }
            }
            return (node.N + 1) * 8;
        }

        private int ReadItems(DiskBTreeNode node, Span<byte> buffer)
        {
            var itemLength = _serializer.MaxSerializedItemLength;
            for (var i = 0; i < node.N; i++)
                node.Items[i] = _serializer.DeserializeItem(buffer.Slice(i * itemLength, itemLength));
            return node.N * itemLength;
        }

        private void ThrowCorruptedNode(long nodeId) => ThrowCorruptedNode<object>(nodeId);
        private TReturnType ThrowCorruptedNode<TReturnType>(long nodeId) => throw new DiskBTreeException($"Node {nodeId} is corrupted");
        private void ThrowCorruptedHeader() => throw new DiskBTreeException("Header is corrupted");

        protected override void ReadRoot(BTreeNode rootNode)
        {
            ((DiskBTreeNode) rootNode).Id = ReadInt64At(RootIdOffset, 0);
        }

        protected override void Delete(BTreeNode node)
        {
            var diskNode = (DiskBTreeNode) node;
            var lastDeletedNode = ReadLastDeletedNode();
            var buffer = (Span<byte>) stackalloc byte[9];
            buffer[0] = (byte) NodeType.Deleted;
            BitConverter.TryWriteBytes(buffer.Slice(1), lastDeletedNode);
            WriteAt(GetOffset(diskNode.Id), buffer);
            WriteLastDeletedNode(diskNode.Id);
        }

        protected virtual long ReadLastDeletedNode()
        {
            return ReadInt64At(LastDeletedNodeOffset, -1);
        }

        protected virtual void WriteLastDeletedNode(long id)
        {
            WriteAt(LastDeletedNodeOffset, id);
        }

        protected virtual long ReadLastNode()
        {
            return ReadInt64At(LastNodeOffset, -1);
        }

        protected virtual void WriteLastNode(long id)
        {
            WriteAt(LastNodeOffset, id);
        }

        protected override BTreeNode AllocateNode()
        {
            return new DiskBTreeNode
            {
                Id = -1,
                Items = new T[MaxItemsCount],
                Children = new BTreeNode[MaxChildrenCount],
                IsLeaf = true
            };
        }

        protected override void FreeNode(BTreeNode node)
        {
            var diskNode = (DiskBTreeNode) node;
            diskNode.Synchronized = false;
            diskNode.N = 0;
            diskNode.IsLeaf = false;
            Array.Fill(diskNode.Items, default);
            Array.Fill(diskNode.Children, null);
        }

        private void FreeAllNodes()
        {
            Root = null;
        }

        private void Init()
        {
            WriteRootId(0);
            WriteLastDeletedNode(-1);
            WriteLastNode(-1);
            Write(AllocateNode());
        }

        private long FindNewNodeId()
        {
            var lastDeletedNode = ReadLastDeletedNode();
            if (lastDeletedNode < 0)
            {
                var lastNode = ReadLastNode();
                var id = lastNode + 1;
                if (lastNode < -1)
                    ThrowCorruptedHeader();
                if (GetOffset(id) + PageSize > _stream.Length)
                {
                    ExpandFile(id + ExpansionSize);
                    if (GetOffset(id) > _stream.Length)
                        ThrowCorruptedHeader();
                }
                WriteLastNode(id);
                return id;
            }
            var buffer = (Span<byte>) stackalloc byte[9];
            if (!ReadAt(GetOffset(lastDeletedNode), buffer) || buffer[0] != (byte) NodeType.Deleted)
                ThrowCorruptedNode(lastDeletedNode);
            var newLastDeletedNode = BitConverter.ToInt64(buffer.Slice(1));
            WriteLastDeletedNode(newLastDeletedNode);
            return lastDeletedNode;
        }

        private void ExpandFile(long lastId)
        {
            _stream.Seek(GetOffset(lastId) + PageSize - 1, SeekOrigin.Begin);
            _stream.WriteByte(0);
            _stream.Flush();
        }

        private long GetOffset(long id) => PageSize * id + HeaderSize;

        private bool ReadAt(long position, Span<byte> buffer)
        {
            _stream.Seek(position, SeekOrigin.Begin);
            return _stream.Read(buffer) == buffer.Length;
        }

        private long ReadInt64At(long position, long defaultValue)
        {
            var buffer = (Span<byte>) stackalloc byte[8];
            return ReadAt(position, buffer) ? BitConverter.ToInt64(buffer) : defaultValue;
        }

        private void WriteAt(long position, ReadOnlySpan<byte> buffer)
        {
            _stream.Seek(position, SeekOrigin.Begin);
            _stream.Write(buffer);
            _stream.Flush();
        }

        private void WriteAt(long position, long value)
        {
            var buffer = (Span<byte>) stackalloc byte[8];
            BitConverter.TryWriteBytes(buffer, value);
            WriteAt(position, buffer);
        }

        protected class DiskBTreeNode : BTreeNode
        {
            public long Id { get; set; }
            public bool Synchronized { get; set; }
        }

        private enum NodeType : byte
        {
            NonLeaf,
            Leaf,
            Deleted
        }
    }

    public class DiskBTreeException : Exception
    {
        public DiskBTreeException(string message)
            : base(message)
        {
        }
    }
}
