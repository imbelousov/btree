using System;
using System.Linq;
using NUnit.Framework;

namespace BTree.Test
{
    public class RemoveTests : TestsBase<int>
    {
        [Test]
        public void RemoveFromEmptyTree()
        {
            Assert.IsFalse(Tree.Remove(1));
        }

        [Test]
        public void RemoveSingleItemFromTree()
        {
            const int value = 1;
            Tree.Add(value);
            Assert.IsTrue(Tree.Remove(value));
            Assert.IsFalse(Tree.Contains(value));
        }

        [Test]
        public void RemoveItemFromTreeContainingManyItems()
        {
            foreach (var value in Enumerable.Range(1, 10000))
                Tree.Add(value);
            const int removedValue = 2;
            Assert.IsTrue(Tree.Remove(removedValue));
            Assert.IsFalse(Tree.Contains(removedValue));
            Assert.IsTrue(Tree.Contains(removedValue - 1));
            Assert.IsTrue(Tree.Contains(removedValue + 1));
        }

        [Test]
        public void RemoveAllItems()
        {
            var values = Enumerable.Range(1, 10000).ToList();
            foreach (var value in values)
                Tree.Add(value);
            foreach (var value in values)
                Tree.Remove(value);
            CollectionAssert.IsEmpty(Tree.Enumerate(false));
        }

        [Test]
        public void RemoveDuplicate()
        {
            const int value = 1;
            Tree.Add(value);
            Tree.Add(value);
            Assert.IsTrue(Tree.Remove(value));
            Assert.IsTrue(Tree.Contains(value));
        }

        [Test]
        public void RemoveDuplicateTwice()
        {
            const int value = 1;
            Tree.Add(value);
            Tree.Add(value);
            Assert.IsTrue(Tree.Remove(value));
            Assert.IsTrue(Tree.Remove(value));
            Assert.IsFalse(Tree.Contains(value));
        }

        [Test]
        public void RemoveManyDuplicates()
        {
            var values = Enumerable.Range(1, 10000).ToList();
            foreach (var value in values)
            {
                Tree.Add(value);
                Tree.Add(value);
            }
            foreach (var value in values)
            {
                Assert.IsTrue(Tree.Remove(value));
                Assert.IsTrue(Tree.Contains(value));
            }
        }

        [Test]
        public void RemoveManyDuplicatesTwice()
        {
            var values = Enumerable.Range(1, 10000).ToList();
            foreach (var value in values)
            {
                Tree.Add(value);
                Tree.Add(value);
            }
            foreach (var value in values)
            {
                Assert.IsTrue(Tree.Remove(value));
                Assert.IsTrue(Tree.Remove(value));
            }
            CollectionAssert.IsEmpty(Tree.Enumerate(false));
        }

        public RemoveTests(Type type, int t)
            : base(type, t)
        {
        }
    }
}
