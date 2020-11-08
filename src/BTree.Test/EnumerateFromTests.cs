using System;
using System.Linq;
using NUnit.Framework;

namespace BTree.Test
{
    public class EnumerateFromTests : TestsBase<int>
    {
        [Test]
        public void EnumerateEmptyTree()
        {
            CollectionAssert.IsEmpty(Tree.EnumerateFrom(0));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(10000)]
        public void EnumerateFromLesserValue(int count)
        {
            var items = Enumerable.Range(1, count).ToList();
            foreach (var item in items)
                Tree.Add(item);
            CollectionAssert.AreEqual(items, Tree.EnumerateFrom(0));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(10000)]
        public void EnumerateFromMinValue(int count)
        {
            var items = Enumerable.Range(1, count).ToList();
            foreach (var item in items)
                Tree.Add(item);
            CollectionAssert.AreEqual(items, Tree.EnumerateFrom(1));
        }

        [TestCase(1, 3)]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 10000)]
        [TestCase(20, 100)]
        [TestCase(50, 100)]
        [TestCase(70, 100)]
        [TestCase(90, 100)]
        [TestCase(99, 100)]
        [TestCase(100, 10000)]
        [TestCase(4000, 10000)]
        [TestCase(8500, 10000)]
        [TestCase(9900, 10000)]
        [TestCase(9999, 10000)]
        public void EnumerateFromMiddleOfTree(int startIndex, int count)
        {
            var items = Enumerable.Range(1, count).ToList();
            foreach (var item in items)
                Tree.Add(item);
            CollectionAssert.AreEqual(items.Skip(startIndex), Tree.EnumerateFrom(items[startIndex]));
        }

        [TestCase(1, 3)]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 10000)]
        [TestCase(20, 100)]
        [TestCase(50, 100)]
        [TestCase(70, 100)]
        [TestCase(90, 100)]
        [TestCase(99, 100)]
        [TestCase(100, 10000)]
        [TestCase(4000, 10000)]
        [TestCase(8500, 10000)]
        [TestCase(9900, 10000)]
        [TestCase(9999, 10000)]
        public void EnumerateFromMiddleOfTreeButFromNotAddedValue(int startIndex, int count)
        {
            var items = Enumerable.Range(1, count).Select(x => x * 2).ToList();
            foreach (var item in items)
                Tree.Add(item);
            CollectionAssert.AreEqual(items.Skip(startIndex), Tree.EnumerateFrom(items[startIndex] - 1));
        }

        [Test]
        public void EnumerateFromEachItem()
        {
            var items = Enumerable.Range(1, 1000).ToList();
            foreach (var item in items)
                Tree.Add(item);
            for (var i = 0; i < items.Count; i++)
                CollectionAssert.AreEqual(items.Skip(i), Tree.EnumerateFrom(items[i]));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public void EnumerateFromEachItemButWithDuplicates(int duplicates)
        {
            const int count = 1000;
            var items = Enumerable.Range(1, count).SelectMany(x => Enumerable.Repeat(x, duplicates)).ToList();
            foreach (var item in items)
                Tree.Add(item);
            for (var i = 0; i < count; i++)
                CollectionAssert.AreEqual(items.Where(x => x >= i + 1), Tree.EnumerateFrom(i + 1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(10000)]
        public void EnumerateFromMaxValue(int count)
        {
            var items = Enumerable.Range(1, count).ToList();
            foreach (var item in items)
                Tree.Add(item);
            CollectionAssert.AreEqual(new[] {items[^1]}, Tree.EnumerateFrom(count));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(10000)]
        public void EnumerateFromGreaterValue(int count)
        {
            var items = Enumerable.Range(1, count).ToList();
            foreach (var item in items)
                Tree.Add(item);
            CollectionAssert.IsEmpty(Tree.EnumerateFrom(count + 1));
        }

        public EnumerateFromTests(Type type, int t)
            : base(type, t)
        {
        }
    }
}
