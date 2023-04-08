using OrderMatcher.Types;
using System;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class MatchingEngine
    {
        private readonly Book _book;
        private readonly HashSet<OrderId> _acceptedOrders;
        private readonly Queue<List<PriceLevel>> _stopOrderQueue;
        private readonly ITradeListener _tradeListener;
        private readonly Quantity _stepSize;
        private readonly IFeeProvider _feeProvider;
        private readonly int _quoteCurrencyDecimalPlaces;
        private Price _marketPrice;
        private bool _acceptedOrderTrackingEnabled = true;

        public IEnumerable<Order> CurrentOrders => _book.CurrentOrders;
        public IEnumerable<KeyValuePair<int, HashSet<OrderId>>> GoodTillDateOrders => _book.GoodTillDateOrders;
        public IEnumerable<OrderId> AcceptedOrders => _acceptedOrders;
        public Price MarketPrice => _marketPrice;
        public Book Book => _book;

        public bool AcceptedOrderTrackingEnabled
        {
            get => _acceptedOrderTrackingEnabled;
            set
            {
                if (!value)
                {
                    _acceptedOrders.Clear();
                    _acceptedOrders.TrimExcess();
                }

                _acceptedOrderTrackingEnabled = value;
            }
        }

        public MatchingEngine(ITradeListener tradeListener, IFeeProvider feeProvider, Quantity stepSize, int quoteCurrencyDecimalPlaces = 0)
        {
            if (quoteCurrencyDecimalPlaces < 0)
                throw new NotSupportedException($"Invalid value of {nameof(quoteCurrencyDecimalPlaces)}");

            if (stepSize < 0)
                throw new NotSupportedException($"Invalid value of {nameof(stepSize)}");

            _book = new Book();
            _stopOrderQueue = new Queue<List<PriceLevel>>();
            _acceptedOrders = new HashSet<OrderId>();
            _tradeListener = tradeListener;
            _feeProvider = feeProvider;
            _quoteCurrencyDecimalPlaces = quoteCurrencyDecimalPlaces;
            _stepSize = stepSize;
        }

        public void InitializeMarketPrice(Price marketPrice)
        {
            _marketPrice = marketPrice;
        }

        public OrderMatchingResult AddOrder(Order incomingOrder, int timestamp, bool isOrderTriggered = false)
        {
            if (incomingOrder == null)
                throw new ArgumentNullException(nameof(incomingOrder));

            if (incomingOrder.Price < 0 || incomingOrder.StopPrice < 0 || incomingOrder.OpenQuantity < 0 || incomingOrder.TipQuantity < 0 || incomingOrder.TotalQuantity < 0 || incomingOrder.OrderAmount < 0)
            {
                return OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity;
            }

            if (incomingOrder.Price < 0 || (incomingOrder.OpenQuantity <= 0 && incomingOrder.OrderAmount == 0 && incomingOrder.TotalQuantity == 0) || (incomingOrder.OpenQuantity == 0 && incomingOrder.OrderAmount <= 0 && incomingOrder.TotalQuantity == 0) || incomingOrder.StopPrice < 0 || incomingOrder.TotalQuantity < 0)
            {
                return OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity;
            }

            if (incomingOrder.OrderCondition == OrderCondition.BookOrCancel && (incomingOrder.Price == 0 || incomingOrder.StopPrice != 0))
            {
                return OrderMatchingResult.BookOrCancelCannotBeMarketOrStopOrder;
            }

            if (incomingOrder.OpenQuantity % _stepSize != 0 || incomingOrder.TotalQuantity % _stepSize != 0 || incomingOrder.TipQuantity % _stepSize != 0)
            {
                return OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize;
            }

            if (incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel && (incomingOrder.StopPrice != 0 || incomingOrder.Price == 0))
            {
                return OrderMatchingResult.ImmediateOrCancelCannotBeMarketOrStopOrder;
            }

            if (incomingOrder.OrderCondition == OrderCondition.FillOrKill && incomingOrder.StopPrice != 0)
            {
                return OrderMatchingResult.FillOrKillCannotBeStopOrder;
            }

            if (incomingOrder.CancelOn < 0)
            {
                return OrderMatchingResult.InvalidCancelOnForGTD;
            }

            if (incomingOrder.CancelOn > 0 && ((incomingOrder.Price == 0 && !(incomingOrder.Price == 0 && incomingOrder.StopPrice != 0)) || incomingOrder.OrderCondition == OrderCondition.FillOrKill || incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel))
            {
                return OrderMatchingResult.GoodTillDateCannotBeMarketOrIOCorFOK;
            }

            if (incomingOrder.Price == 0 && incomingOrder.OrderAmount != 0 && incomingOrder.OpenQuantity != 0)
            {
                return OrderMatchingResult.MarketOrderOnlySupportedOrderAmountOrQuantityNoBoth;
            }

            if (incomingOrder.OrderAmount != 0 && (incomingOrder.Price != 0 || !incomingOrder.IsBuy))
            {
                return OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder;
            }

            if (incomingOrder.TotalQuantity > 0)
            {
                if (incomingOrder.OrderCondition == OrderCondition.FillOrKill || incomingOrder.OrderCondition == OrderCondition.ImmediateOrCancel)
                {
                    return OrderMatchingResult.IcebergOrderCannotBeFOKorIOC;
                }
                if (incomingOrder.Price == 0 || (incomingOrder.StopPrice != 0 && incomingOrder.Price == 0))
                {
                    return OrderMatchingResult.IcebergOrderCannotBeMarketOrStopMarketOrder;
                }
                if (incomingOrder.TotalQuantity <= incomingOrder.TipQuantity)
                {
                    return OrderMatchingResult.InvalidIcebergOrderTotalQuantity;
                }
            }

            if (_acceptedOrderTrackingEnabled)
            {
                if (_acceptedOrders.Contains(incomingOrder.OrderId))
                {
                    return OrderMatchingResult.DuplicateOrder;
                }
                _acceptedOrders.Add(incomingOrder.OrderId);
            }

            _tradeListener?.OnAccept(incomingOrder.OrderId, incomingOrder.UserId);

            Quantity? quantity = null;
            bool canBeFilled = false;
            if (incomingOrder.IsBuy && incomingOrder.OpenQuantity == 0 && incomingOrder.StopPrice == 0 && incomingOrder.OrderAmount > 0)
            {
                var quantityAndFill = GetQuantity(incomingOrder.OrderAmount);
                if (quantityAndFill.Quantity.HasValue)
                {
                    quantity = quantityAndFill.Quantity.Value;
                    canBeFilled = quantityAndFill.CanFill;
                }
            }

            CancelExpiredOrders(timestamp);
            if (incomingOrder.OrderCondition == OrderCondition.BookOrCancel && ((incomingOrder.IsBuy && _book.BestAskPrice <= incomingOrder.Price) || (!incomingOrder.IsBuy && incomingOrder.Price <= _book.BestBidPrice)))
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, incomingOrder.TotalQuantity + incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.BookOrCancel);
            }
            else if (incomingOrder.OrderCondition == OrderCondition.FillOrKill && incomingOrder.OrderAmount == 0 && !_book.CheckCanFillOrder(incomingOrder.IsBuy, incomingOrder.OpenQuantity, incomingOrder.Price))
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.FillOrKill);
            }
            else if (incomingOrder.OrderCondition == OrderCondition.FillOrKill && incomingOrder.OrderAmount > 0 && canBeFilled == false)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, 0, 0, 0, CancelReason.FillOrKill);
            }
            else if (incomingOrder.CancelOn > 0 && incomingOrder.CancelOn <= timestamp)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, incomingOrder.TotalQuantity + incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.ValidityExpired);
            }
            else
            {
                if (incomingOrder.TotalQuantity > 0)
                {
                    incomingOrder = GetTip(incomingOrder);
                }

                if (incomingOrder.StopPrice != 0 && !isOrderTriggered && ((incomingOrder.IsBuy && incomingOrder.StopPrice > _marketPrice) || (!incomingOrder.IsBuy && (incomingOrder.StopPrice < _marketPrice || _marketPrice == 0))))
                {
                    _book.AddStopOrder(incomingOrder);
                }
                else
                {
                    if (incomingOrder.IsBuy && incomingOrder.OpenQuantity == 0 && incomingOrder.OrderAmount > 0)
                    {
                        if (quantity.HasValue)
                        {
                            incomingOrder.OpenQuantity = quantity.Value;
                            MatchAndAddOrder(incomingOrder, incomingOrder.OrderCondition);
                            MatchAndAddTriggeredStopOrders();
                        }
                        else
                        {
                            _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, 0, 0, incomingOrder.Fee, CancelReason.MarketOrderNoLiquidity);
                        }
                    }
                    else
                    {
                        MatchAndAddOrder(incomingOrder, incomingOrder.OrderCondition);
                        MatchAndAddTriggeredStopOrders();
                    }
                }
            }

            return OrderMatchingResult.OrderAccepted;
        }

        public OrderMatchingResult CancelOrder(OrderId orderId)
        {
            return CancelOrder(orderId, CancelReason.UserRequested);
        }

        public void CancelExpiredOrder(int timestamp)
        {
            CancelExpiredOrders(timestamp);
        }

        private OrderMatchingResult CancelOrder(OrderId orderId, CancelReason cancelReason)
        {
            if (_book.TryGetOrder(orderId, out Order? order))
            {
                var quantityCancel = order!.OpenQuantity;
                _book.RemoveOrder(order);
                if (order.IsTip)
                {
                    quantityCancel += order.TotalQuantity;
                }

                _tradeListener?.OnCancel(orderId, order.UserId, quantityCancel, order.Cost, order.Fee, cancelReason);
                return OrderMatchingResult.CancelAcepted;
            }
            return OrderMatchingResult.OrderDoesNotExists;
        }

        private void MatchAndAddOrder(Order incomingOrder, OrderCondition? orderCondition = null)
        {
            Price previousMarketPrice = _marketPrice;
            MatchWithOpenOrders(incomingOrder);
            if (orderCondition == OrderCondition.ImmediateOrCancel && !incomingOrder.IsFilled)
            {
                _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.ImmediateOrCancel);
            }
            else if (!incomingOrder.IsFilled)
            {
                if (incomingOrder.Price == 0)
                {
                    _tradeListener?.OnCancel(incomingOrder.OrderId, incomingOrder.UserId, incomingOrder.OpenQuantity, incomingOrder.Cost, incomingOrder.Fee, CancelReason.MarketOrderNoLiquidity);
                }
                else
                {
                    _book.AddOrderOpenBook(incomingOrder);
                }
            }
            else if (incomingOrder.IsTip)
            {
                AddTip(incomingOrder);
            }

            if (_marketPrice > previousMarketPrice)
            {
                var priceLevels = _book.RemoveStopBids(_marketPrice);
                if (priceLevels != null)
                    _stopOrderQueue.Enqueue(priceLevels);
            }
            else if (_marketPrice < previousMarketPrice)
            {
                var priceLevels = _book.RemoveStopAsks(_marketPrice);
                if (priceLevels != null)
                    _stopOrderQueue.Enqueue(priceLevels);
            }
        }

        private void MatchAndAddTriggeredStopOrders()
        {
            while (_stopOrderQueue.TryDequeue(out var priceLevels))
            {
                for (int i = 0; i < priceLevels.Count; i++)
                {
                    foreach (var order in priceLevels[i])
                    {
                        _tradeListener?.OnOrderTriggered(order.OrderId, order.UserId);
                        if (order.IsBuy && order.OpenQuantity == 0)
                        {
                            var quantityAndFill = GetQuantity(order.OrderAmount);
                            if (quantityAndFill.Quantity.HasValue)
                            {
                                order.OpenQuantity = quantityAndFill.Quantity.Value;
                                MatchAndAddOrder(order);
                            }
                            else
                            {
                                _tradeListener?.OnCancel(order.OrderId, order.UserId, 0, 0, 0, CancelReason.MarketOrderNoLiquidity);
                            }
                        }
                        else
                        {
                            MatchAndAddOrder(order);
                        }
                    }
                }
            }
        }

        private void MatchWithOpenOrders(Order incomingOrder)
        {
            while (true)
            {
                Order? restingOrder = _book.GetBestBuyOrderToMatch(!incomingOrder.IsBuy);
                if (restingOrder == null)
                {
                    break;
                }

                if ((incomingOrder.IsBuy && (restingOrder.Price <= incomingOrder.Price || incomingOrder.Price == 0)) || (!incomingOrder.IsBuy && (restingOrder.Price >= incomingOrder.Price)))
                {
                    Price matchPrice = restingOrder.Price;
                    Quantity maxQuantity;
                    if (incomingOrder.OpenQuantity > 0)
                    {
                        maxQuantity = incomingOrder.OpenQuantity >= restingOrder.OpenQuantity ? restingOrder.OpenQuantity : incomingOrder.OpenQuantity;
                        incomingOrder.OpenQuantity -= maxQuantity;
                    }
                    else
                    {
                        throw new OrderMatcherException(Constant.NOT_EXPECTED);
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
                    if (orderFilled && restingOrder.IsTip)
                    {
                        isRestingTipAdded = AddTip(restingOrder);
                    }

                    bool isIncomingOrderFilled = incomingOrder.IsFilled;
                    if (incomingOrder.IsTip)
                    {
                        isIncomingOrderFilled = incomingOrder.TotalQuantity == 0;
                    }

                    bool isRestingOrderFilled = restingOrder.IsFilled && !isRestingTipAdded;

                    Quantity? askRemainingQuanity = null;
                    Amount? askFee = null;
                    Amount? bidCost = null;
                    Amount? bidFee = null;
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

                    _tradeListener?.OnTrade(incomingOrder.OrderId, restingOrder.OrderId, incomingOrder.UserId, restingOrder.UserId, matchPrice, maxQuantity, askRemainingQuanity, askFee, bidCost, bidFee);
                    _marketPrice = matchPrice;
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
        }

        private bool AddTip(Order order)
        {
            if (order.TotalQuantity > 0)
            {
                var tip = GetTip(order);

                MatchAndAddOrder(tip);
                return true;
            }
            return false;
        }

        private void CancelExpiredOrders(int timeNow)
        {
            var expiredOrderIds = _book.GetExpiredOrders(timeNow);
            if (expiredOrderIds != null)
            {
                for (var i = 0; i < expiredOrderIds.Count; i++)
                {
                    foreach (var orderId in expiredOrderIds[i])
                    {
                        CancelOrder(orderId, CancelReason.ValidityExpired);
                    }
                }
            }
        }

        private static Order GetTip(Order order)
        {
            if (order.OpenQuantity > 0)
                return order;

            var quantity = order.TipQuantity < order.TotalQuantity ? order.TipQuantity : order.TotalQuantity;
            var remainigTotalQuantity = order.TotalQuantity - quantity;
            return new Order { IsBuy = order.IsBuy, Price = order.Price, OrderId = order.OrderId, OpenQuantity = quantity, CancelOn = order.CancelOn, Cost = order.Cost, Fee = order.Fee, TipQuantity = order.TipQuantity, TotalQuantity = remainigTotalQuantity, UserId = order.UserId, FeeId = order.FeeId, OrderCondition = order.OrderCondition, StopPrice = order.StopPrice };
        }

        private (Quantity? Quantity, bool CanFill) GetQuantity(Amount orderAmount)
        {
            bool dustRemaining = false;
            Quantity quantity = 0;
            foreach (var level in _book.AskSide)
            {
                foreach (var order in level)
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
                            orderAmount -= (q * order.Price);
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