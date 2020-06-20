using OrderMatcher.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        private static readonly OrderSequenceComparer _orderSequenceComparer = new OrderSequenceComparer();
        
        public QuantityTrackingPriceLevel(Price price)
        {
            _price = price;
            _quantity = 0;
            _orders = new SortedSet<Order>(_orderSequenceComparer);
        }

        internal void AddOrder(Order order)
        {
            _quantity += order.OpenQuantity;
            _orders.Add(order);
        }

        internal bool RemoveOrder(Order order)
        {
            _quantity -= order.OpenQuantity;
            return _orders.Remove(order);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        internal bool Fill(Order order, Quantity quantity)
        {
            if (order.OpenQuantity >= quantity)
            {
                _quantity -= quantity;
                order.OpenQuantity -= quantity;
                if (order.IsFilled)
                {
                    return _orders.Remove(order);
                }
                return false;
            }
            else
            {
                throw new Exception(Constant.ORDER_QUANTITY_IS_LESS_THEN_REQUESTED_FILL_QUANTITY);
            }
        }

        public Order? First
        {
            get { return _orders.Min; }
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