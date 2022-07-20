using OrderMatcher.Types;
using System.Collections;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class PriceLevel : IPriceLevel
    {
        private readonly SortedSet<Order> _orders;

        private Price _price;

        public int OrderCount => _orders.Count;
        public Price Price => _price;
        public Quantity Quantity
        {
            get
            {
                Quantity totalQuantity = 0;
                foreach (var order in _orders)
                {
                    totalQuantity += order.OpenQuantity;
                }
                return totalQuantity;
            }
        }

        private static readonly OrderSequenceComparer _orderSequenceComparer = new OrderSequenceComparer();

        public PriceLevel()
        {
            _orders = new SortedSet<Order>(_orderSequenceComparer);
        }

        public PriceLevel(Price price)
        {
            _price = price;
            _orders = new SortedSet<Order>(_orderSequenceComparer);
        }

        public void SetPrice(Price price)
        {
            if (_orders.Count > 0)
                throw new OrderMatcherException($"Cannot set price because pricelevel has {_orders.Count} orders.");

            _price = price;
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
                throw new OrderMatcherException(Constant.ORDER_QUANTITY_IS_LESS_THEN_REQUESTED_FILL_QUANTITY);
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
