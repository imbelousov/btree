using System;
using System.Collections.Generic;
using System.IO;

namespace BTree
{
    public abstract class DiskBTree<T> : BTree<T>, IDisposable
    {
        private const int HeaderSize = 24;
        private const int ExpansionSize = 1024;
        private readonly Stream _stream;
        private readonly bool _leaveOpen;

        protected abstract int ItemLength { get; }

        protected int NodeSize => 1 + 4 + ItemLength * MaxItemsCount + 8 * MaxChildrenCount;

        protected DiskBTree(Stream stream, bool leaveOpen, int t, IComparer<T> comparer)
            : base(t, comparer)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("Expected a readable stream", nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a writable stream", nameof(stream));
            if (!stream.CanSeek)
                throw new ArgumentException("Expected a seekable stream", nameof(stream));
            _stream = stream;
            _leaveOpen = leaveOpen;
            if (_stream.Length < HeaderSize)
                Init();
        }

        protected DiskBTree(Stream stream, int t, IComparer<T> comparer)
            : this(stream, false, t, comparer)
        {
        }

        protected DiskBTree(string fileName, int t, IComparer<T> comparer)
            : this(File.Open(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite), t, comparer)
        {
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

        protected abstract void SerializeItem(T item, Span<byte> buffer);

        protected abstract T DeserializeItem(ReadOnlySpan<byte> buffer);

        protected override void Write(BTreeNode node)
        {
            var diskNode = (DiskBTreeNode) node;
            var buffer = (Span<byte>) stackalloc byte[1 + 4 + node.N * ItemLength + (!node.IsLeaf ? (node.N + 1) * 8 : 0)];
            if (diskNode.Id < 0)
                diskNode.Id = FindNewNodeId();

            buffer[0] = (byte) (node.IsLeaf ? NodeType.Leaf : NodeType.NonLeaf);
            BitConverter.TryWriteBytes(buffer.Slice(1, 4), node.N);
            for (var i = 0; i < node.N; i++)
                SerializeItem(node.Items[i], buffer.Slice(1 + 4 + i * ItemLength, ItemLength));
            if (!node.IsLeaf)
            {
                for (var i = 0; i < node.N + 1; i++)
                {
                    var childNode = (DiskBTreeNode) node.Children[i];
                    BitConverter.TryWriteBytes(buffer.Slice(1 + 4 + node.N * ItemLength + i * 8, 8), childNode?.Id ?? -1);
                }
            }

            WriteAt(GetOffset(diskNode.Id), buffer);
        }

        protected override void WriteRoot(BTreeNode rootNode)
        {
            WriteAt(0, ((DiskBTreeNode) rootNode).Id);
        }

        protected override void Read(BTreeNode node)
        {
            var diskNode = (DiskBTreeNode) node;
            if (diskNode.Synchronized)
                return;

            if (diskNode.Id == 0 && _stream.Length == HeaderSize)
                return;

            var buffer = (Span<byte>) stackalloc byte[1 + 4];
            if (!ReadAt(GetOffset(diskNode.Id), buffer))
                ThrowCorruptedNode(diskNode.Id);
            node.IsLeaf = (NodeType) buffer[0] switch
            {
                NodeType.NonLeaf => false,
                NodeType.Leaf => true,
                _ => ThrowCorruptedNode<bool>(diskNode.Id)
            };
            node.N = BitConverter.ToInt32(buffer.Slice(1, 4));
            if (node.N < 0 || node.N > MaxItemsCount)
                ThrowCorruptedNode<bool>(diskNode.Id);

            buffer = stackalloc byte[node.N * ItemLength + (!node.IsLeaf ? (node.N + 1) * 8 : 0)];
            if (!ReadAt(GetOffset(diskNode.Id) + 5, buffer))
                ThrowCorruptedNode(diskNode.Id);
            for (var i = 0; i < node.N; i++)
                node.Items[i] = DeserializeItem(buffer.Slice(i * ItemLength, ItemLength));
            if (!node.IsLeaf)
            {
                for (var i = 0; i < node.N + 1; i++)
                {
                    var id = BitConverter.ToInt64(buffer.Slice(node.N * ItemLength + i * 8, 8));
                    if (id >= 0)
                    {
                        if (id >= 0)
                        {
                            var childNode = (DiskBTreeNode) AllocateNode();
                            node.Children[i] = childNode;
                            childNode.Id = id;
                        }
                    }
                }
            }

            diskNode.Synchronized = true;
        }

        private void ThrowCorruptedNode(long nodeId) => ThrowCorruptedNode<object>(nodeId);
        private TReturnType ThrowCorruptedNode<TReturnType>(long nodeId) => throw new DiskBTreeException($"Node {nodeId} is corrupted");
        private void ThrowCorruptedHeader() => throw new DiskBTreeException("Header is corrupted");

        protected override void ReadRoot(BTreeNode rootNode)
        {
            ((DiskBTreeNode) rootNode).Id = ReadInt64At(0, 0);
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
            return ReadInt64At(8, -1);
        }

        protected virtual void WriteLastDeletedNode(long id)
        {
            WriteAt(8, id);
        }

        protected virtual long ReadLastNode()
        {
            return ReadInt64At(16, -1);
        }

        protected virtual void WriteLastNode(long id)
        {
            WriteAt(16, id);
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
	        var diskNode = (DiskBTreeNode)node;
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
            WriteAt(0, 0);
            WriteAt(8, -1);
            WriteAt(16, 0);
        }

        private long FindNewNodeId()
        {
            var lastDeletedNode = ReadLastDeletedNode();
            if (lastDeletedNode < 0)
            {
                var lastNode = ReadLastNode();
                var id = lastNode + 1;
                if (lastNode < 0)
                    ThrowCorruptedHeader();
                if (GetOffset(id) > _stream.Length)
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
            _stream.Seek(GetOffset(lastId) + NodeSize - 1, SeekOrigin.Begin);
            _stream.WriteByte(0);
            _stream.Flush();
        }

        private long GetOffset(long id) => NodeSize * id + HeaderSize;

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
