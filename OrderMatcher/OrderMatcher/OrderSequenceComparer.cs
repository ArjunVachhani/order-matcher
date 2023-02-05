using OrderMatcher.Types;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class OrderSequenceComparer : IComparer<Order>
    {
        private OrderSequenceComparer() { }

        public int Compare(Order? x, Order? y)
        {
            return x!.Sequence.CompareTo(y!.Sequence);
        }

        private static readonly OrderSequenceComparer _shared = new OrderSequenceComparer();
        public static OrderSequenceComparer Shared => _shared;
    }
}
