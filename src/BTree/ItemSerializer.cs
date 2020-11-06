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
			if(!Instances.TryGetValue(typeof(T), out var obj))
			{
				serializer = default;
				return false;
			}
			serializer = (IItemSerializer<T>)obj;
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
			if(!TypedSerializers.TryGet<T>(out _typedSerializer))
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

	internal class Int32Serializer : IItemSerializer<int>
	{
		public int MaxSerializedItemLength => sizeof(int);

		public void SerializeItem(int item, Span<byte> buffer) => BitConverter.TryWriteBytes(buffer, item);

		public int DeserializeItem(ReadOnlySpan<byte> buffer) => BitConverter.ToInt32(buffer);
	}
}
