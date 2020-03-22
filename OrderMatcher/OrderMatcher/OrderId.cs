using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public readonly struct OrderId : System.IEquatable<OrderId>
    {
        public const int SizeOfOrderId = sizeof(int);
        public static readonly OrderId MaxValue = int.MaxValue;
        public static readonly OrderId MinValue = int.MinValue;
        private readonly int _orderId;
        public OrderId(int orderId)
        {
            _orderId = orderId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator OrderId(int orderId)
        {
            return new OrderId(orderId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(OrderId c)
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

        public override bool Equals(object obj)
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
    }
}