using System;
using System.Linq;
using NUnit.Framework;

namespace BTree.Test
{
    public class ContainsTests : TestsBase<int>
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(int.MaxValue)]
        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void AddValueAndCheckIfContains(int value)
        {
            Tree.Add(value);
            Assert.IsTrue(Tree.Contains(value));
        }

        [Test]
        public void CheckIfEmptyTreeContains()
        {
            Assert.IsFalse(Tree.Contains(5));
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
            Tree.Add(addedValue);
            Assert.IsFalse(Tree.Contains(value));
        }

        [Test]
        public void AddManyValuesAndCheckAllOfThem()
        {
            var values = Enumerable.Range(-10000, 20000).ToList();
            foreach (var value in values)
                Tree.Add(value);
            foreach (var value in values)
                Assert.IsTrue(Tree.Contains(value));
        }

        [Test]
        public void AddManyValuesAndCheckAnother()
        {
            var values = Enumerable.Range(-10000, 20000).ToList();
            foreach (var value in values)
                Tree.Add(value * 2);
            foreach (var value in values)
                Assert.IsFalse(Tree.Contains(value * 2 + 1));
        }

        [Test]
        public void AddManyValuesButInRandomOrderAndCheckAllOfThem()
        {
            var random = new Random(896823);
            var values = Enumerable.Range(-10000, 20000).ToList();
            foreach (var value in values.OrderBy(x => random.Next()))
                Tree.Add(value);
            foreach (var value in values.OrderBy(x => random.Next()))
                Assert.IsTrue(Tree.Contains(value));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        [TestCase(10000)]
        public void AddDuplicatesAndCheck(int count)
        {
            const int value = 5;
            for (var i = 0; i < count; i++)
                Tree.Add(value);
            Assert.IsTrue(Tree.Contains(value));
        }

        [Test]
        public void AddManyDuplicatesToNonEmptyTreeAndCheck()
        {
            const int value = 5;
            for (var i = 0; i < 10000; i++)
                Tree.Add(i);
            for (var i = 0; i < 10000; i++)
                Tree.Add(value);
            Assert.IsTrue(Tree.Contains(value));
        }

        public ContainsTests(Type type, int t)
            : base(type, t)
        {
        }
    }
}
