using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public struct  OrderId
    {
        public const int SizeOfInt = sizeof(int);
        public static readonly OrderId MaxValue = (OrderId)int.MaxValue;
        public static readonly OrderId MinValue = (OrderId)int.MinValue;
        private int _orderId;
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
    }
}