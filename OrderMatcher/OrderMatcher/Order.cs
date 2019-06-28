using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public class Order
    {
        public bool IsBuy { get; set; }
        public ulong OrderId { get; set; }
        public ulong Sequnce
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }
        public Quantity Quantity { get; set; }
        public Quantity OpenQuantity { get; set; }
        public Price Price { get; set; }
        public Price StopPrice { get; set; }
        public OrderCondition OrderCondition { get; set; }
        public Quantity TotalQuantity { get; set; }
        public bool IsTip { get; set; }
        public long CancelOn { get; set; }
    }
}
