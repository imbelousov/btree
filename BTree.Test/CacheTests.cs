using System;
using System.Linq;
using NUnit.Framework;

namespace BTree.Test
{
    [TestFixture]
    public class CacheTests
    {
        private BTreeCache _cache;
        private Random _rand;

        [Test]
        public void ReadNotCachedValue()
        {
            Assert.IsFalse(_cache.TryGet(0, new byte[1]));
        }

        [Test]
        public void ReadCachedValue()
        {
            var expected = new byte[] {1};
            var actual = new byte[1];
            _cache.Set(0, expected);
            Assert.IsTrue(_cache.TryGet(0, actual));
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void CacheManyValues()
        {
            var arrays = Enumerable.Range(0, 5).ToDictionary(x => x, x => GenerateArray(16));
            foreach (var (offset, array) in arrays)
                _cache.Set(offset, array);
            foreach (var (offset, expected) in arrays)
            {
                var actual = new byte[expected.Length];
                Assert.IsTrue(_cache.TryGet(offset, actual));
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void FirstEntryIsRemoved()
        {
            _cache.Set(0, GenerateArray(600));
            _cache.Set(1, GenerateArray(600));
            Assert.IsFalse(_cache.TryGet(0, new byte[600]));
            Assert.IsTrue(_cache.TryGet(1, new byte[600]));
        }

        [Test]
        public void FirstAndSecondEntryIsRemovedButNotThird()
        {
            _cache.Set(0, GenerateArray(200));
            _cache.Set(1, GenerateArray(200));
            _cache.Set(2, GenerateArray(200));
            _cache.Set(3, GenerateArray(600));
            Assert.IsFalse(_cache.TryGet(0, new byte[200]));
            Assert.IsFalse(_cache.TryGet(1, new byte[200]));
            Assert.IsTrue(_cache.TryGet(2, new byte[200]));
            Assert.IsTrue(_cache.TryGet(3, new byte[700]));
        }

        [Test]
        public void TrySetTooLargeValue()
        {
            _cache.Set(0, GenerateArray(2000));
            Assert.IsFalse(_cache.TryGet(0, new byte[2000]));
        }

        [Test]
        public void Overwrite()
        {
            var expected = GenerateArray(200);
            _cache.Set(0, GenerateArray(150));
            _cache.Set(0, expected);
            var actual = new byte[expected.Length];
            _cache.TryGet(0, actual);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void FirstAndThirdEntryIsRemovedButNotSecond()
        {
            _cache.Set(0, GenerateArray(200));
            _cache.Set(1, GenerateArray(200));
            _cache.Set(2, GenerateArray(200));
            _cache.TryGet(1, new byte[200]);
            _cache.Set(3, GenerateArray(600));
            Assert.IsFalse(_cache.TryGet(0, new byte[200]));
            Assert.IsTrue(_cache.TryGet(1, new byte[200]));
            Assert.IsFalse(_cache.TryGet(2, new byte[200]));
            Assert.IsTrue(_cache.TryGet(3, new byte[700]));
        }

        [SetUp]
        public void SetUp()
        {
            _cache = new BTreeCache(1024);
            _rand = new Random();
        }

        private byte[] GenerateArray(int length)
        {
            var result = new byte[length];
            _rand.NextBytes(result);
            return result;
        }
    }
}
