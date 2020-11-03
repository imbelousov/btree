using System;
using NUnit.Framework;

namespace BTree.Test
{
    public class Enumerate : TestBase
    {
        [Test]
        public void EnumerateEmptyTree()
        {
            Assert.IsEmpty(Tree.Enumerate());
        }

        public Enumerate(Type type, int t)
            : base(type, t)
        {
        }
    }
}
