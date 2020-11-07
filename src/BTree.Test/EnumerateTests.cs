using System;
using System.Linq;
using NUnit.Framework;

namespace BTree.Test
{
    public class EnumerateTests : TestsBase<int>
    {
        [Test]
        public void EnumerateEmptyTree()
        {
            Assert.IsEmpty(Tree.Enumerate(false));
        }

        [Test]
        public void EnumerateEmptyTree_Reverse()
        {
            Assert.IsEmpty(Tree.Enumerate(true));
        }

        [Test]
        public void EnumerateTreeContainingSingleValue()
        {
            const int value = 1;
            Tree.Add(value);
            CollectionAssert.AreEqual(new[] {value}, Tree.Enumerate(false));
        }

        [Test]
        public void EnumerateTreeContainingSingleValue_Reverse()
        {
            const int value = 1;
            Tree.Add(value);
            CollectionAssert.AreEqual(new[] {value}, Tree.Enumerate(true));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(20000)]
        public void EnumerateTreeContainingMultipleValues(int valuesCount)
        {
            var rand = new Random(1245645);
            var values = Enumerable.Range(-10000, 20000).OrderBy(x => rand.Next()).Take(valuesCount).ToList();
            foreach (var value in values)
                Tree.Add(value);
            CollectionAssert.AreEqual(values.OrderBy(x => x), Tree.Enumerate(false));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(20000)]
        public void EnumerateTreeContainingMultipleValues_Reverse(int valuesCount)
        {
            var rand = new Random(1245645);
            var values = Enumerable.Range(-10000, 20000).OrderBy(x => rand.Next()).Take(valuesCount).ToList();
            foreach (var value in values)
                Tree.Add(value);
            CollectionAssert.AreEqual(values.OrderByDescending(x => x), Tree.Enumerate(true));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(20000)]
        public void EnumerateDuplicates(int duplicatesCount)
        {
            const int value = 1;
            for (var i = 0; i < duplicatesCount; i++)
                Tree.Add(value);
            CollectionAssert.AreEqual(Enumerable.Repeat(value, duplicatesCount), Tree.Enumerate(false));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(20000)]
        public void EnumerateDuplicates_Reverse(int duplicatesCount)
        {
            const int value = 1;
            for (var i = 0; i < duplicatesCount; i++)
                Tree.Add(value);
            CollectionAssert.AreEqual(Enumerable.Repeat(value, duplicatesCount), Tree.Enumerate(true));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EnumerateDuplicatesButTreeAlsoContainsOtherValues(int duplicatesCount)
        {
            var rand = new Random(1245645);
            const int duplicatedValue = 1;
            var values = Enumerable.Range(-10000, 20000).Concat(Enumerable.Repeat(duplicatedValue, duplicatesCount)).OrderBy(x => rand.Next()).ToList();
            foreach (var value in values)
                Tree.Add(value);
            CollectionAssert.AreEqual(values.OrderBy(x => x), Tree.Enumerate(false));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EnumerateDuplicatesButTreeAlsoContainsOtherValues_Reverse(int duplicatesCount)
        {
            var rand = new Random(1245645);
            const int duplicatedValue = 1;
            var values = Enumerable.Range(-10000, 20000).Concat(Enumerable.Repeat(duplicatedValue, duplicatesCount)).OrderBy(x => rand.Next()).ToList();
            foreach (var value in values)
                Tree.Add(value);
            CollectionAssert.AreEqual(values.OrderByDescending(x => x), Tree.Enumerate(true));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(20000)]
        public void EnumerateTwice(int valuesCount)
        {
            var values = Enumerable.Range(-10000, 20000).Take(valuesCount).ToList();
            foreach (var value in values)
                Tree.Add(value);
            CollectionAssert.AreEqual(values, Tree.Enumerate(false));
            CollectionAssert.AreEqual(values, Tree.Enumerate(false));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(20000)]
        public void EnumerateTwice_Reverse(int valuesCount)
        {
            var values = Enumerable.Range(-10000, 20000).Take(valuesCount).Reverse().ToList();
            foreach (var value in values)
                Tree.Add(value);
            CollectionAssert.AreEqual(values, Tree.Enumerate(true));
            CollectionAssert.AreEqual(values, Tree.Enumerate(true));
        }

        public EnumerateTests(Type type, int t)
            : base(type, t)
        {
        }
    }
}
