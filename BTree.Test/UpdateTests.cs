using System;
using System.Text;

namespace BTree.Test
{
    public class UpdateTests : TestsBase<ComplexStructure>
    {
        protected override IItemSerializer<ComplexStructure> Serializer => new ComplexStructureSerializer();

        public UpdateTests(Type type, int t)
            : base(type, t)
        {
        }
    }

    public struct ComplexStructure : IComparable<ComplexStructure>
    {
        public int Key { get; set; }
        public string Value { get; set; }

        public int CompareTo(ComplexStructure other) => Key.CompareTo(other.Key);
    }

    public class ComplexStructureSerializer : IItemSerializer<ComplexStructure>
    {
        public int MaxSerializedItemLength => 64;

        public void SerializeItem(ComplexStructure item, Span<byte> buffer)
        {
            BitConverter.TryWriteBytes(buffer, item.Key);
            buffer[4] = item.Value != null ? (byte) Encoding.UTF8.GetBytes(item.Value, buffer.Slice(5)) : byte.MaxValue;
        }

        public ComplexStructure DeserializeItem(ReadOnlySpan<byte> buffer)
        {
            return new ComplexStructure
            {
                Key = BitConverter.ToInt32(buffer),
                Value = buffer[4] != byte.MaxValue ? Encoding.UTF8.GetString(buffer.Slice(5, buffer[4])) : null
            };
        }
    }
}
