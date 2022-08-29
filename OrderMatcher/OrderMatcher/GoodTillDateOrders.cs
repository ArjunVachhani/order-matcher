using OrderMatcher.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher
{
    internal class GoodTillDateOrders : IEnumerable<KeyValuePair<int, HashSet<OrderId>>>
    {
        private int? _firstGtdTime;
        private readonly SortedDictionary<int, HashSet<OrderId>> _goodTillDateOrders;

        public GoodTillDateOrders()
        {
            _goodTillDateOrders = new SortedDictionary<int, HashSet<OrderId>>();
        }

        public List<HashSet<OrderId>>? GetExpiredOrders(int timeNow)
        {
            List<HashSet<OrderId>>? expiredOrderIds = null;
            if (_firstGtdTime != null && _firstGtdTime <= timeNow)
            {
                expiredOrderIds = new List<HashSet<OrderId>>();
                foreach (var time in _goodTillDateOrders)
                {
                    if (time.Key <= timeNow)
                    {
                        expiredOrderIds.Add(time.Value);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return expiredOrderIds;
        }

        public void Add(Order order)
        {
            if (order.CancelOn > 0)
            {
                Add(order.CancelOn, order.OrderId);
            }
        }

        public void Remove(Order order)
        {
            if (order.CancelOn > 0)
            {
                Remove(order.CancelOn, order.OrderId);
            }
        }

        void Add(int time, OrderId orderId)
        {
            if (!_goodTillDateOrders.TryGetValue(time, out HashSet<OrderId>? orderIds))
            {
                orderIds = new HashSet<OrderId>();
                _goodTillDateOrders.Add(time, orderIds);

                if (_firstGtdTime == null || time < _firstGtdTime)
                {
                    _firstGtdTime = time;
                }
            }
            orderIds.Add(orderId);
        }

        void Remove(int time, OrderId orderId)
        {
            _goodTillDateOrders.TryGetValue(time, out var orderIds);
            orderIds.Remove(orderId);
            if (orderIds.Count == 0)
            {
                _goodTillDateOrders.Remove(time);

                if (time == _firstGtdTime)
                {
                    _firstGtdTime = _goodTillDateOrders.Count > 0 ? _goodTillDateOrders.First().Key : (int?)null;
                }
            }
        }

        public IEnumerator<KeyValuePair<int, HashSet<OrderId>>> GetEnumerator()
        {
            return _goodTillDateOrders.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _goodTillDateOrders.GetEnumerator();
        }
    }
}
