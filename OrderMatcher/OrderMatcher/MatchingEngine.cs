using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher
{
    public class MatchingEngine
    {
        private readonly Book _book;
        private readonly Dictionary<ulong, Order> _currentOrders;
        private readonly Dictionary<ulong, Order> _currentIcebergOrders;
        private readonly HashSet<ulong> _acceptedOrders;
        private readonly ITradeListener _tradeListener;
        private readonly SortedDictionary<long, HashSet<ulong>> _goodTillDateOrders;
        private readonly Quantity _stepSize;
        private readonly ITimeProvider _timeProvider;
        private Price _marketPrice;
        private KeyValuePair<long, HashSet<ulong>>? _firstGoodTillDate;

        public IEnumerable<KeyValuePair<ulong, Order>> CurrentOrders => _currentOrders;
        public IEnumerable<KeyValuePair<ulong, Order>> CurrentIcebergOrders => _currentIcebergOrders;
        public IEnumerable<KeyValuePair<long, HashSet<ulong>>> GoodTillDateOrders => _goodTillDateOrders;
        public IEnumerable<ulong> AcceptedOrders => _acceptedOrders;
        public Price MarketPrice => _marketPrice;
        public Book Book => _book;

        public MatchingEngine(ITradeListener tradeListener, ITimeProvider timeProvider)
        {
            _book = new Book();
            _currentOrders = new Dictionary<ulong, Order>();
            _currentIcebergOrders = new Dictionary<ulong, Order>();
            _goodTillDateOrders = new SortedDictionary<long, HashSet<ulong>>();
            _acceptedOrders = new HashSet<ulong>();
            _tradeListener = tradeListener;
            _timeProvider = timeProvider;
            _stepSize = 1;
        }

        public OrderMatchingResult AddOrder(Order incomingOrder)
        {
            incomingOrder.OpenQuantity = incomingOrder.Quantity;
            incomingOrder.IsTip = false;

            if (incomingOrder.Price < 0 || incomingOrder.Quantity <= 0 || incomingOrder.StopPrice < 0)
            {
                return OrderMatchingResult.InvalidPriceQuantityOrStopPrice;
            }

            if (incomingOrder.OrderCondition == OrderCondition.BookOrCancel && (incomingOrder.Price == 0 || incomingOrder.StopPrice != 0))
            {
                return OrderMatchingResult.BookOrCancelCannotBeMarketOrStopOrder;
            }

            if (incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel && (incomingOrder.StopPrice != 0))
            {
                return OrderMatchingResult.ImmediateOrCancelCannotBeStopOrder;
            }

            if (incomingOrder.OrderCondition == OrderCondition.FillOrKill && (incomingOrder.StopPrice != 0))
            {
                return OrderMatchingResult.FillOrKillCannotBeStopOrder;
            }

            if (incomingOrder.CancelOn < 0)
            {
                return OrderMatchingResult.InvalidCancelOnForGTD;
            }

            if (incomingOrder.CancelOn > 0 && (incomingOrder.OrderCondition == OrderCondition.FillOrKill || incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel))
            {
                return OrderMatchingResult.GoodTillDateCannotBeIOCorFOK;
            }

            if (incomingOrder.TotalQuantity > 0)
            {
                incomingOrder.OpenQuantity = incomingOrder.TotalQuantity;
                if (incomingOrder.OrderCondition == OrderCondition.FillOrKill || incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel)
                {
                    return OrderMatchingResult.IcebergOrderCannotBeFOKorIOC;
                }
                if (incomingOrder.StopPrice != 0 || incomingOrder.Price == 0)
                {
                    return OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder;
                }
                if (incomingOrder.TotalQuantity <= incomingOrder.Quantity)
                {
                    return OrderMatchingResult.InvalidIcebergOrderTotalQuantity;
                }
            }

            if (_acceptedOrders.Contains(incomingOrder.OrderId))
            {
                return OrderMatchingResult.DuplicateOrder;
            }
            _acceptedOrders.Add(incomingOrder.OrderId);
            var timeNow = _timeProvider.GetUpochMilliseconds();
            CancelExpiredOrders(timeNow);
            if (incomingOrder.OrderCondition == OrderCondition.BookOrCancel && ((incomingOrder.IsBuy && _book.BestAskPrice <= incomingOrder.Price) || (!incomingOrder.IsBuy && incomingOrder.Price <= _book.BestBidPrice)))
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.LockedAmount, CancelReason.BookOrCancel);
            }
            else if (incomingOrder.OrderCondition == OrderCondition.FillOrKill && !_book.CheckCanFillOrder(incomingOrder.IsBuy, incomingOrder.OpenQuantity, incomingOrder.Price))
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.LockedAmount, CancelReason.FillOrKill);
            }
            else if (incomingOrder.CancelOn > 0 && incomingOrder.CancelOn <= timeNow)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.LockedAmount, CancelReason.ValidityExpired);
            }
            else
            {
                if (incomingOrder.TotalQuantity > 0)
                {
                    _currentIcebergOrders.Add(incomingOrder.OrderId, incomingOrder);
                    incomingOrder = GetTip(incomingOrder);
                }
                if (incomingOrder.CancelOn > 0)
                {
                    AddGoodTillDateOrder(incomingOrder.CancelOn, incomingOrder.OrderId);
                }
                _currentOrders.Add(incomingOrder.OrderId, incomingOrder);

                if (incomingOrder.StopPrice != 0 && ((incomingOrder.IsBuy && incomingOrder.StopPrice > _marketPrice) || (!incomingOrder.IsBuy && (incomingOrder.StopPrice < _marketPrice || _marketPrice == 0))))
                {
                    _book.AddStopOrder(incomingOrder);
                }
                else
                {
                    MatchAndAddOrder(incomingOrder);
                }
            }

            return OrderMatchingResult.OrderAccepted;
        }

        public OrderMatchingResult CancelOrder(ulong orderId)
        {
            return CancelOrder(orderId, CancelReason.UserRequested);
        }

        public void CancelExpiredOrder()
        {
            var timeNow = _timeProvider.GetUpochMilliseconds();
            CancelExpiredOrders(timeNow);
        }

        private OrderMatchingResult CancelOrder(ulong orderId, CancelReason cancelReason)
        {
            if (_currentOrders.TryGetValue(orderId, out Order order))
            {
                var quantityCancel = order.OpenQuantity;
                var remainingLockedAmount = order.LockedAmount;
                _book.RemoveOrder(order);
                _currentOrders.Remove(orderId);
                if (order.IsTip)
                {
                    if (_currentIcebergOrders.TryGetValue(orderId, out Order iceBergOrder))
                    {
                        quantityCancel += iceBergOrder.OpenQuantity;
                        remainingLockedAmount += iceBergOrder.LockedAmount;
                        _currentIcebergOrders.Remove(orderId);
                    }
                }

                if (order.CancelOn > 0)
                {
                    RemoveGoodTillDateOrder(order.CancelOn, order.OrderId);
                }

                _tradeListener?.OnCancel(orderId, quantityCancel, remainingLockedAmount, cancelReason);
                return OrderMatchingResult.CancelAcepted;
            }
            return OrderMatchingResult.OrderDoesNotExists;
        }

        private void MatchAndAddOrder(Order incomingOrder)
        {
            Price previousMarketPrice = _marketPrice;
            var anyMatch = MatchWithOpenOrders(incomingOrder);
            if (incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel && !incomingOrder.IsFilled)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.LockedAmount, CancelReason.ImmediateOrCancel);
                _currentOrders.Remove(incomingOrder.OrderId);
            }
            else if (!incomingOrder.IsFilled)
            {
                if (incomingOrder.Price == 0)
                {
                    if (anyMatch)
                    {
                        incomingOrder.Price = _marketPrice;
                        _book.AddOrderOpenBook(incomingOrder);
                    }
                    else
                    {
                        _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.LockedAmount, CancelReason.MarketOrderNoLiquidity);
                        _currentOrders.Remove(incomingOrder.OrderId);
                        if (incomingOrder.CancelOn > 0)
                        {
                            RemoveGoodTillDateOrder(incomingOrder.CancelOn, incomingOrder.OrderId);
                        }
                    }
                }
                else
                {
                    _book.AddOrderOpenBook(incomingOrder);
                }
            }
            else
            {
                _currentOrders.Remove(incomingOrder.OrderId);
                if (incomingOrder.IsTip)
                {
                    AddTip(incomingOrder.OrderId);
                }

                if (incomingOrder.CancelOn > 0)
                {
                    RemoveGoodTillDateOrder(incomingOrder.CancelOn, incomingOrder.OrderId);
                }
            }

            if (_marketPrice > previousMarketPrice)
            {
                var priceLevels = _book.RemoveStopBids(_marketPrice);
                AddStopToOrderBook(priceLevels);
            }
            else if (_marketPrice < previousMarketPrice)
            {
                var priceLevels = _book.RemoveStopAsks(_marketPrice);
                AddStopToOrderBook(priceLevels);
            }
        }

        private void AddStopToOrderBook(List<PriceLevel> priceLevels)
        {
            for (int i = 0; i <  priceLevels.Count; i++)
            {
                foreach (var order in priceLevels[i])
                {
                    _tradeListener?.OnOrderTriggered(order.OrderId);
                    MatchAndAddOrder(order);
                }
            }
        }
        
        private bool MatchWithOpenOrders(Order incomingOrder)
        {
            bool anyMatchHappend = false;
            while (true)
            {
                Order restingOrder = _book.GetBestBuyOrderToMatch(!incomingOrder.IsBuy);
                if (restingOrder == null)
                {
                    break;
                }

                if ((incomingOrder.IsBuy && (restingOrder.Price <= incomingOrder.Price || incomingOrder.Price == 0)) || (!incomingOrder.IsBuy && (restingOrder.Price >= incomingOrder.Price)))
                {
                    Price matchPrice = restingOrder.Price;
                    Quantity maxQuantity = 0;
                    if (incomingOrder.Price == 0 && incomingOrder.IsBuy == true && incomingOrder.OpenQuantity == 0 && incomingOrder.LockedAmount != 0)
                    {
                        Quantity quantity = incomingOrder.LockedAmount / matchPrice;
                        quantity = quantity - (quantity % _stepSize);
                        if (quantity == 0)
                        {
                            break;
                        }
                        maxQuantity = quantity >= restingOrder.OpenQuantity ? restingOrder.OpenQuantity : quantity;
                        incomingOrder.LockedAmount -= maxQuantity * matchPrice;
                    }
                    else if (incomingOrder.OpenQuantity != 0)
                    {
                        maxQuantity = incomingOrder.OpenQuantity >= restingOrder.OpenQuantity ? restingOrder.OpenQuantity : incomingOrder.OpenQuantity;
                        incomingOrder.OpenQuantity -= maxQuantity;
                    }
                    else
                    {
                        throw new Exception("not expected");
                    }
                    bool orderFilled = _book.FillOrder(restingOrder, maxQuantity);
                    if (orderFilled)
                    {
                        _currentOrders.Remove(restingOrder.OrderId);
                        if (restingOrder.IsTip)
                        {
                            AddTip(restingOrder.OrderId);
                        }

                        if (restingOrder.CancelOn > 0)
                        {
                            RemoveGoodTillDateOrder(restingOrder.CancelOn, restingOrder.OrderId);
                        }
                    }

                    _tradeListener?.OnTrade(incomingOrder.OrderId, restingOrder.OrderId, matchPrice, maxQuantity);
                    _marketPrice = matchPrice;
                    anyMatchHappend = true;
                }
                else
                {
                    break;
                }

                if (incomingOrder.IsFilled)
                {
                    break;
                }
            }
            return anyMatchHappend;
        }

        private void AddTip(ulong orderId)
        {
            if (_currentIcebergOrders.TryGetValue(orderId, out Order order))
            {
                var tip = GetTip(order);
                _currentOrders.Add(tip.OrderId, tip);

                if (order.CancelOn > 0)
                {
                    AddGoodTillDateOrder(order.CancelOn, order.OrderId);
                }

                MatchAndAddOrder(tip);
            }
        }

        private void CancelExpiredOrders(long timeNow)
        {
            if (_firstGoodTillDate != null && _firstGoodTillDate.Value.Key <= timeNow)
            {
                List<HashSet<ulong>> expiredOrderIds = new List<HashSet<ulong>>();
                List<long> timeCollection = new List<long>();
                foreach (var time in _goodTillDateOrders)
                {
                    if (time.Key <= timeNow)
                    {
                        timeCollection.Add(time.Key);
                        expiredOrderIds.Add(time.Value);
                    }
                    else
                    {
                        break;
                    }
                }

                for (var i = 0; i < timeCollection.Count; i++)
                {
                    _goodTillDateOrders.Remove(timeCollection[i]);
                }

                for (var i = 0; i < expiredOrderIds.Count; i++)
                {
                    foreach (var orderId in expiredOrderIds[i])
                    {
                        CancelOrder(orderId, CancelReason.ValidityExpired);
                    }
                }

                _firstGoodTillDate = _goodTillDateOrders.Count > 0 ? _goodTillDateOrders.First() : (KeyValuePair<long, HashSet<ulong>>?)null;
            }
        }

        private void AddGoodTillDateOrder(long time, ulong orderId)
        {
            if (!_goodTillDateOrders.TryGetValue(time, out HashSet<ulong> orderIds))
            {
                orderIds = new HashSet<ulong>();
                _goodTillDateOrders.Add(time, orderIds);
            }
            orderIds.Add(orderId);

            if (_firstGoodTillDate == null || time < _firstGoodTillDate.Value.Key)
            {
                _firstGoodTillDate = _goodTillDateOrders.First();
            }
        }

        private void RemoveGoodTillDateOrder(long time, ulong orderId)
        {
            if (_goodTillDateOrders.TryGetValue(time, out var orderIds))
            {
                orderIds.Remove(orderId);
                if (orderIds.Count == 0)
                {
                    _goodTillDateOrders.Remove(time);

                    if (time == _firstGoodTillDate.Value.Key)
                    {
                        _firstGoodTillDate = _goodTillDateOrders.Count > 0 ? _goodTillDateOrders.First() : (KeyValuePair<long, HashSet<ulong>>?)null;
                    }
                }
            }
        }

        private Order GetTip(Order order)
        {
            var quantity = order.OpenQuantity < order.Quantity ? order.OpenQuantity : order.Quantity;
            order.OpenQuantity -= quantity;
            if (order.IsFilled)
            {
                _currentIcebergOrders.Remove(order.OrderId);
            }

            return new Order { IsBuy = order.IsBuy, Price = order.Price, OrderId = order.OrderId, IsTip = true, OpenQuantity = quantity, Quantity = quantity };
        }
    }
}
//TODO add decimal places for quote currency
//TODO : Trade Logger unit test + Trade Deserializer + Unit test