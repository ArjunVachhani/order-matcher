using System;
using System.Collections;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class PriceLevel : IEnumerable<Order>
    {
        private readonly SortedSet<Order> _orders;
        private readonly Price _price;

        public int OrderCount => _orders.Count;
        public Price Price => _price;

        private static readonly OrderSequenceComparer _orderSequenceComparer;
        static PriceLevel()
        {
            _orderSequenceComparer = new OrderSequenceComparer();
        }

        public PriceLevel(Price price)
        {
            _price = price;
            _orders = new SortedSet<Order>(_orderSequenceComparer);
        }

        public void AddOrder(Order order)
        {
            _orders.Add(order);
        }

        public bool RemoveOrder(Order order)
        {
            return _orders.Remove(order);
        }

        public bool Fill(Order order, Quantity quantity)
        {
            if (order.OpenQuantity >= quantity)
            {
                order.OpenQuantity = order.OpenQuantity - quantity;
                if (order.IsFilled)
                {
                    return _orders.Remove(order);
                }
                return false;
            }
            else
            {
                throw new Exception("Order quantity is less then requested fill quanity");
            }
        }

        public IEnumerator<Order> GetEnumerator()
        {
            return _orders.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _orders.GetEnumerator();
        }
    }
}
