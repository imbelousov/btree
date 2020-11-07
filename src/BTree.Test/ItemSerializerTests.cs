using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace BTree.Test
{
    [TestFixture]
    public class ItemSerializerTests
    {
        [TestCaseSource(nameof(SerializeAndDeserializeSource))]
        public void SerializeAndDeserialize<T>(T expected)
        {
            var buffer = new byte[256];
            ItemSerializer<T>.Default.SerializeItem(expected, buffer);
            var actual = ItemSerializer<T>.Default.DeserializeItem(buffer);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void UsingUnsupportedTypeThrowsException()
        {
            Assert.IsInstanceOf<NotSupportedException>(
                Assert.Throws<TypeInitializationException>(() => ItemSerializer<object>.Default.SerializeItem(new object(), new byte[256])).InnerException
            );
        }

        private static IEnumerable<object> SerializeAndDeserializeSource()
        {
            yield return (byte) 93;
            yield return (byte) 255;
            yield return (sbyte) -42;
            yield return (sbyte) 23;
            yield return true;
            yield return false;
            yield return (short) 11975;
            yield return (short) -16311;
            yield return (ushort) 20785;
            yield return (ushort) 61222;
            yield return 'x';
            yield return (char) 39139;
            yield return 99;
            yield return -1531429988;
            yield return 2068268084u;
            yield return 4182199093u;
            yield return -119171807922997110;
            yield return 864863214328862513;
            yield return 941424405846660913u;
            yield return 17858953433487742769u;
            yield return 84216.931874733f;
            yield return -1999025147.299922f;
            yield return 9124257776122279341.117364;
            yield return -719753815812233316275.9123814141;
            yield return 5421356356158342.3645124m;
            yield return -199345716548517486513.8725422221237m;
            yield return new DateTime(637403541221022366, DateTimeKind.Local);
            yield return new DateTime(637403411096078249, DateTimeKind.Utc);
            yield return new Guid("5d18b41e-1126-4843-bf73-6d4352c718fc");
            yield return new Guid("6dc225e0-00d1-44bb-975f-f6f78cfb98e4");
        }
    }
}
