using System;
using System.Collections.Generic;
using System.Linq;

namespace BTree
{
    public interface IItemSerializer<T>
    {
        int MaxSerializedItemLength { get; }

        void SerializeItem(T item, Span<byte> buffer);

        T DeserializeItem(ReadOnlySpan<byte> buffer);
    }

    internal static class TypedSerializers
    {
        private static readonly Dictionary<Type, object> Instances;

        static TypedSerializers()
        {
            Instances = FindTypedSerializers().ToDictionary(GetSerializedType, Activator.CreateInstance);
        }

        public static bool TryGet<T>(out IItemSerializer<T> serializer)
        {
            if (!Instances.TryGetValue(typeof(T), out var obj))
            {
                serializer = default;
                return false;
            }
            serializer = (IItemSerializer<T>) obj;
            return true;
        }

        private static IEnumerable<Type> FindTypedSerializers() => typeof(IItemSerializer<>).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && !x.IsGenericType)
            .Where(x => x.GetImplementedInterface() != null);

        private static Type GetSerializedType(Type serializerType) => serializerType
            .GetImplementedInterface()
            .GetGenericArguments()[0];

        private static Type GetImplementedInterface(this Type type) => type.GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IItemSerializer<>));
    }

    public sealed class ItemSerializer<T> : IItemSerializer<T>
    {
        public static readonly ItemSerializer<T> Default = new ItemSerializer<T>();

        private readonly IItemSerializer<T> _typedSerializer;

        public int MaxSerializedItemLength => _typedSerializer.MaxSerializedItemLength;

        private ItemSerializer()
        {
            if (!TypedSerializers.TryGet<T>(out _typedSerializer))
                throw new NotSupportedException($"Type '{typeof(T)}' is not supported. You have to implement '{nameof(IItemSerializer<T>)}' manually.");
        }

        public void SerializeItem(T item, Span<byte> buffer) => _typedSerializer.SerializeItem(item, buffer);

        public T DeserializeItem(ReadOnlySpan<byte> buffer) => _typedSerializer.DeserializeItem(buffer);
    }

    internal class ByteSerializer : IItemSerializer<byte>
    {
        public int MaxSerializedItemLength => 1;

        public void SerializeItem(byte item, Span<byte> buffer) => buffer[0] = item;

        public byte DeserializeItem(ReadOnlySpan<byte> buffer) => buffer[0];
    }

    internal class SByteSerializer : IItemSerializer<sbyte>
    {
        public int MaxSerializedItemLength => 1;

        public void SerializeItem(sbyte item, Span<byte> buffer) => buffer[0] = (byte) item;

        public sbyte DeserializeItem(ReadOnlySpan<byte> buffer) => (sbyte) buffer[0];
    }

    internal class BooleanSerializer : IItemSerializer<bool>
    {
        public int MaxSerializedItemLength => 1;

        public void SerializeItem(bool item, Span<byte> buffer) => buffer[0] = (byte) (item ? 1 : 0);

        public bool DeserializeItem(ReadOnlySpan<byte> buffer) => buffer[0] != 0;
    }

    internal class Int16Serializer : IItemSerializer<short>
    {
        public int MaxSerializedItemLength => sizeof(short);

        public void SerializeItem(short item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public short DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToInt16(buffer);
    }

    internal class UInt16Serializer : IItemSerializer<ushort>
    {
        public int MaxSerializedItemLength => sizeof(ushort);

        public void SerializeItem(ushort item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public ushort DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToUInt16(buffer);
    }

    internal class CharSerializer : IItemSerializer<char>
    {
        public int MaxSerializedItemLength => sizeof(char);

        public void SerializeItem(char item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public char DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToChar(buffer);
    }

    internal class Int32Serializer : IItemSerializer<int>
    {
        public int MaxSerializedItemLength => sizeof(int);

        public void SerializeItem(int item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public int DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToInt32(buffer);
    }

    internal class UInt32Serializer : IItemSerializer<uint>
    {
        public int MaxSerializedItemLength => sizeof(uint);

        public void SerializeItem(uint item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public uint DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToUInt32(buffer);
    }

    internal class Int64Serializer : IItemSerializer<long>
    {
        public int MaxSerializedItemLength => sizeof(long);

        public void SerializeItem(long item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public long DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToInt64(buffer);
    }

    internal class UInt64Serializer : IItemSerializer<ulong>
    {
        public int MaxSerializedItemLength => sizeof(ulong);

        public void SerializeItem(ulong item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public ulong DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToUInt64(buffer);
    }

    internal class SingleSerializer : IItemSerializer<float>
    {
        public int MaxSerializedItemLength => sizeof(float);

        public void SerializeItem(float item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public float DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToSingle(buffer);
    }

    internal class DoubleSerializer : IItemSerializer<double>
    {
        public int MaxSerializedItemLength => sizeof(double);

        public void SerializeItem(double item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

        public double DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToDouble(buffer);
    }

    internal class DecimalSerializer : IItemSerializer<decimal>
    {
        public int MaxSerializedItemLength => sizeof(decimal);

        public unsafe void SerializeItem(decimal item, Span<byte> buffer)
        {
            var ptr = &item;
            var l1 = *(long*) ptr;
            var l2 = *((long*) ptr + 1);
            BitConverter.TryWriteBytes(buffer, l1);
            BitConverter.TryWriteBytes(buffer.Slice(sizeof(long)), l2);
        }

        public unsafe decimal DeserializeItem(ReadOnlySpan<byte> buffer)
        {
            var result = new decimal();
            var l1 = BitConverter.ToInt64(buffer);
            var l2 = BitConverter.ToInt64(buffer.Slice(sizeof(long)));
            var ptr = &result;
            *(long*) ptr = l1;
            *((long*) ptr + 1) = l2;
            return result;
        }
    }

    internal class DateTimeSerializer : IItemSerializer<DateTime>
    {
        public int MaxSerializedItemLength => sizeof(long);

        public void SerializeItem(DateTime item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item.ToBinary());

        public DateTime DeserializeItem(ReadOnlySpan<byte> buffer) => DateTime.FromBinary(BitConverter.ToInt64(buffer));
    }

    internal class GuidSerializer : IItemSerializer<Guid>
    {
        public int MaxSerializedItemLength => 16;

        public void SerializeItem(Guid item, Span<byte> buffer) => item.TryWriteBytes(buffer);

        public Guid DeserializeItem(ReadOnlySpan<byte> buffer) => new Guid(buffer.Slice(0, 16));
    }
}
