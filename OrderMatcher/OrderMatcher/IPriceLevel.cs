using OrderMatcher.Types;
using System.Collections.Generic;

namespace OrderMatcher
{
    internal interface IPriceLevel : IEnumerable<Order>
    {
        public int OrderCount { get; }
        public Price Price { get; }
        public Quantity Quantity { get; }
        void AddOrder(Order order);
        bool RemoveOrder(Order order);
        void SetPrice(Price price);
        bool Fill(Order order, Quantity quantity);
    }
}
