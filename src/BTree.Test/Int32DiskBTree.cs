using System;
using System.Collections.Generic;
using System.IO;

namespace BTree.Test
{
    public class Int32DiskBTree : DiskBTree<int>
    {
        public Int32DiskBTree(Stream stream, bool leaveOpen, int t, IComparer<int> comparer)
            : base(stream, leaveOpen, t, comparer)
        {
        }

        public Int32DiskBTree(Stream stream, int t, IComparer<int> comparer)
            : base(stream, t, comparer)
        {
        }

        public Int32DiskBTree(string fileName, int t, IComparer<int> comparer)
            : base(fileName, t, comparer)
        {
        }

        protected override int ItemLength => 4;

        protected override void SerializeItem(int item, Span<byte> buffer)
        {
            BitConverter.TryWriteBytes(buffer, item);
        }

        protected override int DeserializeItem(ReadOnlySpan<byte> buffer)
        {
            return BitConverter.ToInt32(buffer);
        }
    }
}
