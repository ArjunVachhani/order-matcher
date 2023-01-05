using OrderMatcher.Types.Serializers;
using System;
using System.Runtime.CompilerServices;

namespace OrderMatcher.Types
{
    public readonly struct OrderId : IEquatable<OrderId>, IComparable<OrderId>
    {
        public const int SizeOfOrderId = sizeof(long);
        public static readonly OrderId MaxValue = long.MaxValue;
        public static readonly OrderId MinValue = long.MinValue;
        private readonly long _orderId;
        public OrderId(long orderId)
        {
            _orderId = orderId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator OrderId(long orderId)
        {
            return new OrderId(orderId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(OrderId c)
        {
            return c._orderId;
        }

        public static bool operator >(OrderId a, OrderId b)
        {
            return (a._orderId > b._orderId);
        }

        public static bool operator <(OrderId a, OrderId b)
        {
            return (a._orderId < b._orderId);
        }


        public static bool operator <=(OrderId a, OrderId b)
        {
            return (a._orderId <= b._orderId);
        }

        public static bool operator >=(OrderId a, OrderId b)
        {
            return (a._orderId >= b._orderId);
        }

        public static bool operator ==(OrderId a, OrderId b)
        {
            return (a._orderId == b._orderId);
        }

        public static bool operator !=(OrderId a, OrderId b)
        {
            return (a._orderId != b._orderId);
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is OrderId))
            {
                return false;
            }

            OrderId OrderId = (OrderId)obj;
            return _orderId == OrderId._orderId;
        }

        public override int GetHashCode()
        {
            return -5579697 + _orderId.GetHashCode();
        }

        public override string ToString()
        {
            return _orderId.ToString();
        }

        public bool Equals(OrderId other)
        {
            return _orderId == other._orderId;
        }

        public int CompareTo(OrderId other)
        {
            return _orderId.CompareTo(other._orderId);
        }

        public void WriteBytes(Span<byte> bytes)
        {
            WriteBytes(bytes, this);
        }

        public static void WriteBytes(Span<byte> bytes, OrderId orderId)
        {
            Serializer.Write(bytes, orderId._orderId);
        }

        public static OrderId ReadOrderId(ReadOnlySpan<byte> bytes)
        {
            return Serializer.ReadLong(bytes);
        }
    }
}