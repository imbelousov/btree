using System;
using System.Collections.Generic;

namespace BTree
{
    public interface IItemSerializer<T>
    {
        int MaxSerializedItemLength { get; }

        void SerializeItem(T item, Span<byte> buffer);

        T DeserializeItem(ReadOnlySpan<byte> buffer);
    }

    public sealed class ItemSerializer<T> : IItemSerializer<T>
    {
        public static readonly ItemSerializer<T> Default = new ItemSerializer<T>();

        private static readonly Dictionary<Type, DefaultSerializerInfo<T>> SupportedTypes = new Dictionary<Type, DefaultSerializerInfo<T>>
        {
            {typeof(byte), new DefaultSerializerInfo<T>(sizeof(byte), SerializeByte,)}
        };

        private readonly SerializeItem<T> _serializeItem;
        private readonly DeserializeItem<T> _deserializeItem;

        private ItemSerializer()
        {
            if (!SupportedTypes.TryGetValue(typeof(T), out var info))
                throw new NotSupportedException($"Type '{typeof(T)}' is not supported. You have to implement '{nameof(IItemSerializer<T>)}' manually.");
            MaxSerializedItemLength = info.MaxSerializedItemLength;
            _serializeItem = info.SerializeItem;
            _deserializeItem = info.DeserializeItem;
        }

        public int MaxSerializedItemLength { get; }

        public void SerializeItem(T item, Span<byte> buffer) => _serializeItem(item, buffer);

        public T DeserializeItem(ReadOnlySpan<byte> buffer) => _deserializeItem(buffer);

        private static void SerializeByte(T item, Span<byte> buffer)
        {
#pragma warning disable CS8509
            buffer[0] = item switch {byte b => b};
#pragma warning restore CS8509
        }

        private static T DeserializeByte(Span<byte> buffer)
        {
            return (T) buffer[0];
        }
    }

    internal delegate void SerializeItem<T>(T item, Span<byte> buffer);

    internal delegate T DeserializeItem<T>(ReadOnlySpan<byte> buffer);

    internal struct DefaultSerializerInfo<T>
    {
        public int MaxSerializedItemLength { get; }
        public SerializeItem<T> SerializeItem { get; }
        public DeserializeItem<T> DeserializeItem { get; }

        public DefaultSerializerInfo(int maxSerializedItemLength, SerializeItem<T> serializeItem, DeserializeItem<T> deserializeItem)
        {
            MaxSerializedItemLength = maxSerializedItemLength;
            SerializeItem = serializeItem;
            DeserializeItem = deserializeItem;
        }
    }
}
