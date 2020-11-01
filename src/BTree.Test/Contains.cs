using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Contains
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void AddValueAndCheckIfContains(int value)
        {
            _tree.Add(value);
            Assert.IsTrue(_tree.Contains(value));
        }

        [Test]
        public void CheckIfEmptyTreeContains()
        {
            Assert.IsFalse(_tree.Contains(5));
        }

        [TestCase(0, 1)]
        [TestCase(1, 0)]
        [TestCase(1000, 900)]
        [TestCase(900, 1000)]
        [TestCase(-1, 1)]
        [TestCase(1, -1)]
        [TestCase(int.MaxValue, int.MinValue)]
        public void CheckIfTreeContainsNotExistingValue(int addedValue, int value)
        {
            _tree.Add(addedValue);
            Assert.IsFalse(_tree.Contains(value));
        }

        [Test]
        public void AddManyValuesAndCheckAllOfThem()
        {
            var values = Enumerable.Range(-10000, 20000).ToList();
            foreach (var value in values)
                _tree.Add(value);
            foreach (var value in values)
                Assert.IsTrue(_tree.Contains(value));
        }

        [Test]
        public void AddManyValuesAndCheckAnother()
        {
            var values = Enumerable.Range(-10000, 20000).ToList();
            foreach (var value in values)
                _tree.Add(value * 2);
            foreach (var value in values)
                Assert.IsFalse(_tree.Contains(value * 2 + 1));
        }

        [Test]
        public void AddManyValuesButInRandomOrderAndCheckAllOfThem()
        {
            var random = new Random(896823);
            var values = Enumerable.Range(-10000, 20000).ToList();
            foreach (var value in values.OrderBy(x => random.Next()))
                _tree.Add(value);
            foreach (var value in values.OrderBy(x => random.Next()))
                Assert.IsTrue(_tree.Contains(value));
        }

        public Contains(Type type, int t)
        {
            _type = type;
            _t = t;
        }

        [SetUp]
        public void SetUp()
        {
            _tree = CreateTree(_type, _t);
        }

        [TearDown]
        public void TearDown()
        {
            (_tree as IDisposable)?.Dispose();
        }

        private BTree<int> CreateTree(Type type, int t)
        {
            return new BTree<int>(t, Comparer<int>.Default);
        }

        private readonly Type _type;
        private readonly int _t;
        private BTree<int> _tree;
    }
}
