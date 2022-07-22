using OrderMatcher.Types;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher
{
    public class Book
    {
        private KeyValuePair<int, HashSet<OrderId>>? _firstGoodTillDate;
        private readonly SortedDictionary<int, HashSet<OrderId>> _goodTillDateOrders;
        private readonly Dictionary<OrderId, Order> _currentOrders;
        private readonly Side<QuantityTrackingPriceLevel> _bids;
        private readonly Side<QuantityTrackingPriceLevel> _asks;
        private readonly Side<PriceLevel> _stopBids;
        private readonly Side<PriceLevel> _stopAsks;

        private ulong _sequence;

        public IEnumerable<KeyValuePair<int, HashSet<OrderId>>> GoodTillDateOrders => _goodTillDateOrders;
        public IEnumerable<KeyValuePair<OrderId, Order>> CurrentOrders => _currentOrders;
        public IEnumerable<QuantityTrackingPriceLevel> BidSide => _bids.PriceLevels;
        public IEnumerable<QuantityTrackingPriceLevel> AskSide => _asks.PriceLevels;
        internal int AskPriceLevelCount => _asks.PriceLevelCount;
        internal int BidPriceLevelCount => _bids.PriceLevelCount;
        public IEnumerable<PriceLevel> StopBidSide => _stopBids.PriceLevels;
        public IEnumerable<PriceLevel> StopAskSide => _stopAsks.PriceLevels;
        public Price? BestBidPrice => _bids.BestPriceLevel?.Price;
        public Price? BestAskPrice => _asks.BestPriceLevel?.Price;
        public Quantity? BestBidQuantity => _bids.BestPriceLevel?.Quantity;
        public Quantity? BestAskQuantity => _asks.BestPriceLevel?.Quantity;
        public Price? BestStopBidPrice => _stopBids.BestPriceLevel?.Price;
        public Price? BestStopAskPrice => _stopAsks.BestPriceLevel?.Price;

        public Book()
        {
            var _priceComparerAscending = new PriceComparerAscending();
            var _priceComparerDescending = new PriceComparerDescending();

            _currentOrders = new Dictionary<OrderId, Order>();
            _goodTillDateOrders = new SortedDictionary<int, HashSet<OrderId>>();
            _bids = new Side<QuantityTrackingPriceLevel>(_priceComparerDescending, new PriceLevelComparerDescending<QuantityTrackingPriceLevel>());
            _asks = new Side<QuantityTrackingPriceLevel>(_priceComparerAscending, new PriceLevelComparerAscending<QuantityTrackingPriceLevel>());
            _stopBids = new Side<PriceLevel>(_priceComparerAscending, new PriceLevelComparerAscending<PriceLevel>());
            _stopAsks = new Side<PriceLevel>(_priceComparerDescending, new PriceLevelComparerDescending<PriceLevel>());
            _sequence = 0;
        }

        internal void RemoveOrder(Order order)
        {
            if (order.IsBuy)
            {
                bool removed = _bids.RemoveOrder(order, order.Price);
                if (!removed && order.IsStop)
                    _stopBids.RemoveOrder(order, order.StopPrice);
            }
            else
            {
                bool removed = _asks.RemoveOrder(order, order.Price);
                if (!removed && order.IsStop)
                    _stopAsks.RemoveOrder(order, order.StopPrice);
            }

            _currentOrders.Remove(order.OrderId);
            if (order.CancelOn > 0)
            {
                RemoveGoodTillDateOrder(order.CancelOn, order.OrderId);
            }
        }

        internal void AddStopOrder(Order order)
        {
            order.Sequence = ++_sequence;
            var side = order.IsBuy ? _stopBids : _stopAsks;
            side.AddOrder(order, order.StopPrice);
            _currentOrders.Add(order.OrderId, order);
            if (order.CancelOn > 0)
            {
                AddGoodTillDateOrder(order.CancelOn, order.OrderId);
            }
        }

        internal void AddOrderOpenBook(Order order)
        {
            order.Sequence = ++_sequence;
            var side = order.IsBuy ? _bids : _asks;
            side.AddOrder(order, order.Price);
            _currentOrders.Add(order.OrderId, order);
            if (order.CancelOn > 0)
            {
                AddGoodTillDateOrder(order.CancelOn, order.OrderId);
            }
        }

        internal List<PriceLevel> RemoveStopAsks(Price price)
        {
            return RemoveFromTracking(_stopAsks.RemovePriceLevelTill(price));
        }

        internal List<PriceLevel> RemoveStopBids(Price price)
        {
            return RemoveFromTracking(_stopBids.RemovePriceLevelTill(price));
        }

        private List<PriceLevel> RemoveFromTracking(List<PriceLevel> priceLevels)
        {
            foreach (var priceLevel in priceLevels)
            {
                foreach (var order in priceLevel)
                {
                    _currentOrders.Remove(order.OrderId);
                    if (order.CancelOn > 0)
                    {
                        AddGoodTillDateOrder(order.CancelOn, order.OrderId);
                    }
                }
            }
            return priceLevels;
        }

        internal bool TryGetOrder(OrderId orderId, out Order order)
        {
            return _currentOrders.TryGetValue(orderId, out order);
        }

        internal List<HashSet<OrderId>> GetExpiredOrders(int timeNow)
        {
            List<HashSet<OrderId>> expiredOrderIds = new List<HashSet<OrderId>>();
            if (_firstGoodTillDate != null && _firstGoodTillDate.Value.Key <= timeNow)
            {
                foreach (var time in GoodTillDateOrders)
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

        internal bool FillOrder(Order order, Quantity quantity)
        {
            var side = order.IsBuy ? _bids : _asks;
            if (side.FillOrder(order, quantity))
            {
                _currentOrders.Remove(order.OrderId);
                if (order.CancelOn > 0)
                {
                    RemoveGoodTillDateOrder(order.CancelOn, order.OrderId);
                }
                return true;
            }
            return false;
        }

        internal Order? GetBestBuyOrderToMatch(bool isBuy)
        {
            return isBuy ? _bids.BestPriceLevel?.First : _asks.BestPriceLevel?.First;
        }

        internal bool CheckCanFillOrder(bool isBuy, Quantity requestedQuantity, Price limitPrice)
        {
            var side = isBuy ? _asks : _bids;
            return side.CheckCanBeFilled(requestedQuantity, limitPrice);
        }

        internal bool CheckCanFillMarketOrderAmount(bool isBuy, Quantity orderAmount)
        {
            var side = isBuy ? _asks : _bids;
            return side.CheckMarketOrderAmountCanBeFilled(orderAmount);
        }

        private void AddGoodTillDateOrder(int time, OrderId orderId)
        {
            if (!_goodTillDateOrders.TryGetValue(time, out HashSet<OrderId>? orderIds))
            {
                orderIds = new HashSet<OrderId>();
                _goodTillDateOrders.Add(time, orderIds);
            }
            orderIds.Add(orderId);

            if (_firstGoodTillDate == null || time < _firstGoodTillDate.Value.Key)
            {
                _firstGoodTillDate = _goodTillDateOrders.First();
            }
        }

        private void RemoveGoodTillDateOrder(int time, OrderId orderId)
        {
            _goodTillDateOrders.TryGetValue(time, out var orderIds);
            orderIds.Remove(orderId);
            if (orderIds.Count == 0)
            {
                _goodTillDateOrders.Remove(time);

                if (time == _firstGoodTillDate!.Value.Key)
                {
                    _firstGoodTillDate = _goodTillDateOrders.Count > 0 ? _goodTillDateOrders.First() : (KeyValuePair<int, HashSet<OrderId>>?)null;
                }
            }
        }
    }
}
