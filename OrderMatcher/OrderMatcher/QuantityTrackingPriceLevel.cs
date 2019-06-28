using System;
using System.Collections;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class QuantityTrackingPriceLevel : IEnumerable<Order>
    {
        private readonly SortedSet<Order> _orders;
        private readonly Price _price;
        private Quantity _quantity;
        public int OrderCount => _orders.Count;
        public Quantity Quantity => _quantity;
        public Price Price => _price;

        private static readonly OrderSequenceComparer _orderSequenceComparer;
        static QuantityTrackingPriceLevel()
        {
            _orderSequenceComparer = new OrderSequenceComparer();
        }

        public QuantityTrackingPriceLevel(Price price)
        {
            _price = price;
            _quantity = 0;
            _orders = new SortedSet<Order>(_orderSequenceComparer);
        }

        public void AddOrder(Order order)
        {
            _quantity += order.OpenQuantity;
            _orders.Add(order);
        }

        public bool RemoveOrder(Order order)
        {
            _quantity -= order.OpenQuantity;
            return _orders.Remove(order);
        }

        public bool Fill(Order order, Quantity quantity)
        {
            if (order.OpenQuantity >= quantity)
            {
                _quantity -= quantity;
                order.OpenQuantity = order.OpenQuantity - quantity;
                if (order.OpenQuantity == 0)
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
