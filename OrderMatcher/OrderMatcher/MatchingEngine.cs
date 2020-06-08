using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher
{
    public class MatchingEngine
    {
        private readonly Book _book;
        private readonly Dictionary<OrderId, Order> _currentOrders;
        private readonly Dictionary<OrderId, Iceberg> _currentIcebergOrders;
        private readonly Dictionary<OrderId, Quantity> _orderAmount;
        private readonly HashSet<OrderId> _acceptedOrders;
        private readonly ITradeListener _tradeListener;
        private readonly SortedDictionary<int, HashSet<OrderId>> _goodTillDateOrders;
        private readonly Quantity _stepSize;
        private readonly ITimeProvider _timeProvider;
        private readonly IFeeProvider _feeProvider;
        private readonly int _quoteCurrencyDecimalPlaces;
        private readonly decimal _power;
        private Price _marketPrice;
        private KeyValuePair<int, HashSet<OrderId>>? _firstGoodTillDate;

        public IEnumerable<KeyValuePair<OrderId, Order>> CurrentOrders => _currentOrders;
        public IEnumerable<KeyValuePair<OrderId, Iceberg>> CurrentIcebergOrders => _currentIcebergOrders;
        public IEnumerable<KeyValuePair<int, HashSet<OrderId>>> GoodTillDateOrders => _goodTillDateOrders;
        public IEnumerable<OrderId> AcceptedOrders => _acceptedOrders;
        public Price MarketPrice => _marketPrice;
        public Book Book => _book;

        public MatchingEngine(ITradeListener tradeListener, ITimeProvider timeProvider, IFeeProvider feeProvider, Quantity stepSize, int quoteCurrencyDecimalPlaces = 0)
        {
            if (quoteCurrencyDecimalPlaces < 0)
                throw new NotSupportedException($"Invalid value of {nameof(quoteCurrencyDecimalPlaces)}");

            if (stepSize < 0)
                throw new NotSupportedException($"Invalid value of {nameof(stepSize)}");

            _book = new Book();
            _currentOrders = new Dictionary<OrderId, Order>();
            _currentIcebergOrders = new Dictionary<OrderId, Iceberg>();
            _orderAmount = new Dictionary<OrderId, Quantity>();
            _goodTillDateOrders = new SortedDictionary<int, HashSet<OrderId>>();
            _acceptedOrders = new HashSet<OrderId>();
            _tradeListener = tradeListener;
            _timeProvider = timeProvider;
            _feeProvider = feeProvider;
            _quoteCurrencyDecimalPlaces = quoteCurrencyDecimalPlaces;
            _power = (decimal)Math.Pow(10, _quoteCurrencyDecimalPlaces);
            _stepSize = stepSize;
        }

        public OrderMatchingResult AddOrder(OrderWrapper orderWrapper, bool isOrderTriggered = false)
        {
            var incomingOrder = orderWrapper.Order;
            if (incomingOrder == null)
                throw new ArgumentNullException(nameof(incomingOrder));

            incomingOrder.IsTip = false;

            if (incomingOrder.Price < 0 || (incomingOrder.OpenQuantity <= 0 && orderWrapper.OrderAmount == 0) || (incomingOrder.OpenQuantity == 0 && orderWrapper.OrderAmount <= 0) || orderWrapper.StopPrice < 0 || orderWrapper.TotalQuantity < 0)
            {
                return OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity;
            }

            if (orderWrapper.OrderCondition == OrderCondition.BookOrCancel && (incomingOrder.Price == 0 || orderWrapper.StopPrice != 0))
            {
                return OrderMatchingResult.BookOrCancelCannotBeMarketOrStopOrder;
            }

            if (incomingOrder.OpenQuantity % _stepSize != 0 || orderWrapper.TotalQuantity % _stepSize != 0)
            {
                return OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize;
            }

            if (orderWrapper.OrderCondition == OrderCondition.ImmediateOrCancel && (orderWrapper.StopPrice != 0))
            {
                return OrderMatchingResult.ImmediateOrCancelCannotBeStopOrder;
            }

            if (orderWrapper.OrderCondition == OrderCondition.FillOrKill && orderWrapper.StopPrice != 0)
            {
                return OrderMatchingResult.FillOrKillCannotBeStopOrder;
            }

            if (incomingOrder.CancelOn < 0)
            {
                return OrderMatchingResult.InvalidCancelOnForGTD;
            }

            if (incomingOrder.CancelOn > 0 && (orderWrapper.OrderCondition == OrderCondition.FillOrKill || orderWrapper.OrderCondition == OrderCondition.ImmediateOrCancel))
            {
                return OrderMatchingResult.GoodTillDateCannotBeIOCorFOK;
            }

            if (incomingOrder.Price == 0 && orderWrapper.OrderAmount != 0 && incomingOrder.OpenQuantity != 0)
            {
                return OrderMatchingResult.MarketOrderOnlySupportedOrderAmountOrQuantityNoBoth;
            }

            if (orderWrapper.OrderAmount != 0 && (incomingOrder.Price != 0 || !incomingOrder.IsBuy))
            {
                return OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder;
            }

            if (orderWrapper.TotalQuantity > 0)
            {
                incomingOrder.OpenQuantity = orderWrapper.TipQuantity;
                if (orderWrapper.OrderCondition == OrderCondition.FillOrKill || orderWrapper.OrderCondition == OrderCondition.ImmediateOrCancel)
                {
                    return OrderMatchingResult.IcebergOrderCannotBeFOKorIOC;
                }
                if (orderWrapper.StopPrice != 0 || incomingOrder.Price == 0)
                {
                    return OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder;
                }
                if (orderWrapper.TotalQuantity <= orderWrapper.TipQuantity)
                {
                    return OrderMatchingResult.InvalidIcebergOrderTotalQuantity;
                }
            }

            if (_acceptedOrders.Contains(incomingOrder.OrderId))
            {
                return OrderMatchingResult.DuplicateOrder;
            }
            _acceptedOrders.Add(incomingOrder.OrderId);
            _tradeListener?.OnAccept(incomingOrder.OrderId);
            if (orderWrapper.OrderAmount != 0 && orderWrapper.StopPrice != 0)
            {
                _orderAmount.Add(orderWrapper.Order.OrderId, orderWrapper.OrderAmount);
            }

            Quantity? quantity = null;
            bool canBeFilled = false;
            if (orderWrapper.Order.IsBuy && orderWrapper.Order.OpenQuantity == 0 && orderWrapper.StopPrice == 0)
            {
                var quantityAndFill = GetQuantity(orderWrapper.OrderAmount);
                if (quantityAndFill.Quantity.HasValue)
                {
                    quantity = quantityAndFill.Quantity.Value;
                    canBeFilled = quantityAndFill.CanFill;
                }
            }

            var timeNow = _timeProvider.GetSecondsFromEpoch();
            CancelExpiredOrders(timeNow);
            if (orderWrapper.OrderCondition == OrderCondition.BookOrCancel && ((incomingOrder.IsBuy && _book.BestAskPrice <= incomingOrder.Price) || (!incomingOrder.IsBuy && incomingOrder.Price <= _book.BestBidPrice)))
            {
                if (orderWrapper.TotalQuantity == 0)
                    _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.BookOrCancel);
                else
                    _tradeListener?.OnCancel(incomingOrder.OrderId, orderWrapper.TotalQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.BookOrCancel);
            }
            else if (orderWrapper.OrderCondition == OrderCondition.FillOrKill && orderWrapper.OrderAmount == 0 && !_book.CheckCanFillOrder(incomingOrder.IsBuy, incomingOrder.OpenQuantity, incomingOrder.Price))
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.FillOrKill);
            }
            else if (orderWrapper.OrderCondition == OrderCondition.FillOrKill && orderWrapper.OrderAmount != 0 && canBeFilled == false)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, 0, 0, 0, CancelReason.FillOrKill);
            }
            else if (incomingOrder.CancelOn > 0 && incomingOrder.CancelOn <= timeNow)
            {
                if (orderWrapper.TotalQuantity == 0)
                    _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.ValidityExpired);
                else
                    _tradeListener?.OnCancel(incomingOrder.OrderId, orderWrapper.TotalQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.ValidityExpired);
            }
            else
            {
                if (orderWrapper.TotalQuantity > 0)
                {
                    var iceberg = new Iceberg() { TipQuantity = orderWrapper.TipQuantity, TotalQuantity = orderWrapper.TotalQuantity };
                    _currentIcebergOrders.Add(incomingOrder.OrderId, iceberg);
                    incomingOrder = GetTip(incomingOrder, iceberg, 0, 0);
                }
                if (incomingOrder.CancelOn > 0)
                {
                    AddGoodTillDateOrder(incomingOrder.CancelOn, incomingOrder.OrderId);
                }
                _currentOrders.Add(incomingOrder.OrderId, incomingOrder);

                if (orderWrapper.StopPrice != 0 && !isOrderTriggered && ((incomingOrder.IsBuy && orderWrapper.StopPrice > _marketPrice) || (!incomingOrder.IsBuy && (orderWrapper.StopPrice < _marketPrice || _marketPrice == 0))))
                {
                    _book.AddStopOrder(incomingOrder, orderWrapper.StopPrice);
                }
                else
                {
                    if (orderWrapper.Order.IsBuy && orderWrapper.Order.OpenQuantity == 0)
                    {
                        if (quantity.HasValue)
                        {
                            orderWrapper.Order.OpenQuantity = quantity.Value;
                            MatchAndAddOrder(incomingOrder, orderWrapper.OrderCondition);
                        }
                        else
                        {
                            _currentOrders.Remove(orderWrapper.Order.OrderId);
                            _tradeListener?.OnCancel(orderWrapper.Order.OrderId, 0, 0, incomingOrder.Fee, CancelReason.MarketOrderNoLiquidity);
                        }
                    }
                    else
                    {
                        MatchAndAddOrder(incomingOrder, orderWrapper.OrderCondition);
                    }
                }
            }

            return OrderMatchingResult.OrderAccepted;
        }

        public OrderMatchingResult CancelOrder(OrderId orderId)
        {
            return CancelOrder(orderId, CancelReason.UserRequested);
        }

        public void CancelExpiredOrder()
        {
            var timeNow = _timeProvider.GetSecondsFromEpoch();
            CancelExpiredOrders(timeNow);
        }

        private OrderMatchingResult CancelOrder(OrderId orderId, CancelReason cancelReason)
        {
            if (_currentOrders.TryGetValue(orderId, out Order order))
            {
                var quantityCancel = order.OpenQuantity;
                _book.RemoveOrder(order);
                _currentOrders.Remove(orderId);
                if (order.IsTip)
                {
                    if (_currentIcebergOrders.TryGetValue(orderId, out Iceberg iceBergOrder))
                    {
                        quantityCancel += iceBergOrder.TotalQuantity;
                        _currentIcebergOrders.Remove(orderId);
                    }
                }

                if (order.CancelOn > 0)
                {
                    RemoveGoodTillDateOrder(order.CancelOn, order.OrderId);
                }

                if (order.IsStop && order.IsBuy && order.OpenQuantity == 0)
                {
                    _orderAmount.Remove(order.OrderId);
                }

                _tradeListener?.OnCancel(orderId, quantityCancel, order.Cost, order.Fee, cancelReason);
                return OrderMatchingResult.CancelAcepted;
            }
            return OrderMatchingResult.OrderDoesNotExists;
        }

        private void MatchAndAddOrder(Order incomingOrder, OrderCondition? orderCondition = null)
        {
            Price previousMarketPrice = _marketPrice;
            var matchResult = MatchWithOpenOrders(incomingOrder);
            if (orderCondition == OrderCondition.ImmediateOrCancel && !incomingOrder.IsFilled)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.ImmediateOrCancel);
                _currentOrders.Remove(incomingOrder.OrderId);
            }
            else if (!incomingOrder.IsFilled)
            {
                if (incomingOrder.Price == 0)
                {
                    if (matchResult && incomingOrder.OpenQuantity > 0)
                    {
                        incomingOrder.Price = _marketPrice;
                        _book.AddOrderOpenBook(incomingOrder);
                    }
                    else
                    {
                        _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.MarketOrderNoLiquidity);
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
                    AddTip(incomingOrder, incomingOrder.Cost, incomingOrder.Fee);
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
            for (int i = 0; i < priceLevels.Count; i++)
            {
                foreach (var order in priceLevels[i])
                {
                    _tradeListener?.OnOrderTriggered(order.OrderId);
                    if (order.IsBuy && order.OpenQuantity == 0)
                    {
                        _orderAmount.TryGetValue(order.OrderId, out Quantity orderAmount);
                        _orderAmount.Remove(order.OrderId);
                        var quantityAndFill = GetQuantity(orderAmount);
                        if (quantityAndFill.Quantity.HasValue)
                        {
                            order.OpenQuantity = quantityAndFill.Quantity.Value;
                            MatchAndAddOrder(order);
                        }
                        else
                        {
                            _currentOrders.Remove(order.OrderId);
                            _tradeListener?.OnCancel(order.OrderId, 0, 0, 0, CancelReason.MarketOrderNoLiquidity);
                        }
                    }
                    else
                    {
                        MatchAndAddOrder(order);
                    }
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
                    if (incomingOrder.OpenQuantity > 0)
                    {
                        maxQuantity = incomingOrder.OpenQuantity >= restingOrder.OpenQuantity ? restingOrder.OpenQuantity : incomingOrder.OpenQuantity;
                        incomingOrder.OpenQuantity -= maxQuantity;
                    }
                    else
                    {
                        throw new Exception("not expected");
                    }

                    var cost = Math.Round(maxQuantity * matchPrice, _quoteCurrencyDecimalPlaces);
                    restingOrder.Cost += cost;
                    incomingOrder.Cost += cost;
                    var incomingFee = _feeProvider.GetFee(incomingOrder.FeeId);
                    var restingFee = _feeProvider.GetFee(restingOrder.FeeId);
                    restingOrder.Fee += Math.Round((cost * restingFee.MakerFee) / 100, _quoteCurrencyDecimalPlaces);
                    incomingOrder.Fee += Math.Round((cost * incomingFee.TakerFee) / 100, _quoteCurrencyDecimalPlaces);
                    bool orderFilled = _book.FillOrder(restingOrder, maxQuantity);
                    bool isRestingTipAdded = false;
                    if (orderFilled)
                    {
                        _currentOrders.Remove(restingOrder.OrderId);
                        if (restingOrder.CancelOn > 0)
                        {
                            RemoveGoodTillDateOrder(restingOrder.CancelOn, restingOrder.OrderId);
                        }

                        if (restingOrder.IsTip)
                        {
                            isRestingTipAdded = AddTip(restingOrder, restingOrder.Cost, restingOrder.Fee);
                        }
                    }

                    bool isIncomingOrderFilled = incomingOrder.IsFilled;
                    if (incomingOrder.IsTip == true)
                    {
                        isIncomingOrderFilled = !_currentIcebergOrders.ContainsKey(incomingOrder.OrderId);
                    }

                    bool isRestingOrderFilled = restingOrder.IsFilled && !isRestingTipAdded;

                    Quantity? askRemainingQuanity = null;
                    Quantity? askFee = null;
                    Quantity? bidCost = null;
                    Quantity? bidFee = null;
                    if (incomingOrder.IsBuy)
                    {
                        if (isIncomingOrderFilled)
                        {
                            bidCost = incomingOrder.Cost;
                            bidFee = incomingOrder.Fee;
                        }
                        if (isRestingOrderFilled)
                        {
                            askRemainingQuanity = restingOrder.OpenQuantity;
                            askFee = restingOrder.Fee;
                        }
                    }
                    else
                    {
                        if (isRestingOrderFilled)
                        {
                            bidCost = restingOrder.Cost;
                            bidFee = restingOrder.Fee;
                        }
                        if (isIncomingOrderFilled)
                        {
                            askRemainingQuanity = incomingOrder.OpenQuantity;
                            askFee = incomingOrder.Fee;
                        }
                    }

                    _tradeListener?.OnTrade(incomingOrder.OrderId, restingOrder.OrderId, matchPrice, maxQuantity, askRemainingQuanity, askFee, bidCost, bidFee);
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

        private bool AddTip(Order order, Quantity cost, Quantity fee)
        {
            if (_currentIcebergOrders.TryGetValue(order.OrderId, out Iceberg iceberg))
            {
                var tip = GetTip(order, iceberg, cost, fee);
                _currentOrders.Add(tip.OrderId, tip);

                if (order.CancelOn > 0)
                {
                    AddGoodTillDateOrder(order.CancelOn, order.OrderId);
                }

                MatchAndAddOrder(tip);
                return true;
            }
            return false;
        }

        private void CancelExpiredOrders(int timeNow)
        {
            if (_firstGoodTillDate != null && _firstGoodTillDate.Value.Key <= timeNow)
            {
                List<HashSet<OrderId>> expiredOrderIds = new List<HashSet<OrderId>>();
                List<int> timeCollection = new List<int>();
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

                _firstGoodTillDate = _goodTillDateOrders.Count > 0 ? _goodTillDateOrders.First() : (KeyValuePair<int, HashSet<OrderId>>?)null;
            }
        }

        private void AddGoodTillDateOrder(int time, OrderId orderId)
        {
            if (!_goodTillDateOrders.TryGetValue(time, out HashSet<OrderId> orderIds))
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
            if (_goodTillDateOrders.TryGetValue(time, out var orderIds))
            {
                orderIds.Remove(orderId);
                if (orderIds.Count == 0)
                {
                    _goodTillDateOrders.Remove(time);

                    if (time == _firstGoodTillDate.Value.Key)
                    {
                        _firstGoodTillDate = _goodTillDateOrders.Count > 0 ? _goodTillDateOrders.First() : (KeyValuePair<int, HashSet<OrderId>>?)null;
                    }
                }
            }
        }

        private Order GetTip(Order order, Iceberg iceberg, Quantity cost, Quantity fee)
        {
            var quantity = iceberg.TipQuantity < iceberg.TotalQuantity ? iceberg.TipQuantity : iceberg.TotalQuantity;
            iceberg.TotalQuantity -= quantity;
            if (iceberg.TotalQuantity == 0)
            {
                _currentIcebergOrders.Remove(order.OrderId);
            }

            return new Order { IsBuy = order.IsBuy, Price = order.Price, OrderId = order.OrderId, IsTip = true, OpenQuantity = quantity, CancelOn = order.CancelOn, Cost = cost, Fee = fee };
        }

        private (Quantity? Quantity, bool CanFill) GetQuantity(Quantity orderAmount)
        {
            bool dustRemaining = false;
            Quantity quantity = 0;
            foreach (var level in Book.AskSide)
            {
                foreach (var order in level.Value)
                {
                    if (orderAmount == 0)
                        goto outOfLoop;

                    var amount = order.OpenQuantity * order.Price;
                    if (amount <= orderAmount)
                    {
                        quantity += order.OpenQuantity;
                        orderAmount -= amount;
                    }
                    else
                    {
                        dustRemaining = true;
                        var q = (orderAmount / order.Price);
                        q = q - (q % _stepSize);
                        if (q > 0)
                        {
                            quantity += q;
                        }
                        else
                        {
                            goto outOfLoop;
                        }
                    }
                }
            }

        outOfLoop:
            var fill = orderAmount == 0 || dustRemaining == true;
            if (quantity > 0)
                return (quantity, fill);
            else
                return (null, fill);
        }
    }
}