using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace BTree.Test
{
    [TestFixture(typeof(BTree<>), 2)]
    [TestFixture(typeof(BTree<>), 3)]
    [TestFixture(typeof(BTree<>), 10)]
    [TestFixture(typeof(BTree<>), 100)]
    [TestFixture(typeof(DiskBTree<>), 2)]
    [TestFixture(typeof(DiskBTree<>), 3)]
    [TestFixture(typeof(DiskBTree<>), 10)]
    [TestFixture(typeof(DiskBTree<>), 100)]
    public abstract class TestsBase<T>
    {
        private readonly Type _type;
        private readonly int _t;

        protected BTree<T> Tree { get; private set; }

        protected virtual IComparer<T> Comparer => Comparer<T>.Default;
        protected virtual IItemSerializer<T> Serializer => ItemSerializer<T>.Default;

        public TestsBase(Type type, int t)
        {
            _type = type;
            _t = t;
        }

        [SetUp]
        public void SetUp()
        {
            Tree = CreateTree(_type, _t);
        }

        [TearDown]
        public void TearDown()
        {
            (Tree as IDisposable)?.Dispose();
        }

        private BTree<T> CreateTree(Type type, int t)
        {
            if (type == typeof(BTree<>))
                return new BTree<T>(t);
            if (type == typeof(DiskBTree<>))
                return new DiskBTree<T>(new MemoryStream(), t);
            throw new NotSupportedException();
        }
    }
}
