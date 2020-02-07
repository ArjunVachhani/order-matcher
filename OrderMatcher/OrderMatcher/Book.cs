using System;
using System.Collections.Generic;
using System.Linq;
namespace OrderMatcher
{
    public class Book
    {
        private readonly SortedDictionary<Price, QuantityTrackingPriceLevel> _bidSide;
        private readonly SortedDictionary<Price, QuantityTrackingPriceLevel> _askSide;
        private readonly SortedDictionary<Price, PriceLevel> _stopBid;
        private readonly SortedDictionary<Price, PriceLevel> _stopAsk;
        private readonly PriceComparerAscending _priceComparerAscending;
        private readonly PriceComparerDescending _priceComparerDescending;
        private readonly PriceComparerDescendingZeroFirst _priceComparerDescendingZeroFirst;
        private readonly Dictionary<OrderId, Price> _stopDictionary;

        private ulong _sequence;
        private PriceLevel _bestStopBidPriceLevel;
        private PriceLevel _bestStopAskPriceLevel;
        private QuantityTrackingPriceLevel _bestBidPriceLevel;
        private QuantityTrackingPriceLevel _bestAskPriceLevel;
        
        public IEnumerable<KeyValuePair<Price, QuantityTrackingPriceLevel>> BidSide => _bidSide;
        public IEnumerable<KeyValuePair<Price, QuantityTrackingPriceLevel>> AskSide => _askSide;
        internal int AskPriceLevelCount => _askSide.Count;
        internal int BidPriceLevelCount => _bidSide.Count;
        public IEnumerable<KeyValuePair<Price, PriceLevel>> StopBidSide => _stopBid;
        public IEnumerable<KeyValuePair<Price, PriceLevel>> StopAskSide => _stopAsk;
        public Price? BestBidPrice => _bestBidPriceLevel?.Price;
        public Price? BestAskPrice => _bestAskPriceLevel?.Price;
        public Quantity? BestBidQuantity => _bestBidPriceLevel?.Quantity;
        public Quantity? BestAskQuantity => _bestAskPriceLevel?.Quantity;
        public Price? BestStopBidPrice => _bestStopBidPriceLevel?.Price;
        public Price? BestStopAskPrice => _bestStopAskPriceLevel?.Price;

        public Book()
        {
            _priceComparerAscending = new PriceComparerAscending();
            _priceComparerDescendingZeroFirst = new PriceComparerDescendingZeroFirst();
            _priceComparerDescending = new PriceComparerDescending();
            _bidSide = new SortedDictionary<Price, QuantityTrackingPriceLevel>(_priceComparerDescendingZeroFirst);
            _askSide = new SortedDictionary<Price, QuantityTrackingPriceLevel>(_priceComparerAscending);
            _stopBid = new SortedDictionary<Price, PriceLevel>(_priceComparerAscending);
            _stopAsk = new SortedDictionary<Price, PriceLevel>(_priceComparerDescending);
            _stopDictionary = new Dictionary<OrderId, Price>();
            _sequence = 0;
        }

        public void RemoveOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (order.IsBuy)
            {
                bool removed = false;
                if (_bidSide.TryGetValue(order.Price, out QuantityTrackingPriceLevel priceLevel))
                {
                    removed = priceLevel.RemoveOrder(order);
                    RemoveEmptyPriceLevel(priceLevel, _bidSide, true);
                }
                if (!removed && order.IsStop && _stopDictionary.TryGetValue(order.OrderId,out var stopPrice) && _stopBid.TryGetValue(stopPrice, out PriceLevel stopPriceLevel))
                {
                    _stopDictionary.Remove(order.OrderId);
                    stopPriceLevel.RemoveOrder(order);
                    RemoveEmptyPriceLevel(stopPriceLevel, _stopBid);
                }
            }
            else
            {
                bool removed = false;
                if (_askSide.TryGetValue(order.Price, out QuantityTrackingPriceLevel priceLevel))
                {
                    removed = priceLevel.RemoveOrder(order);
                    RemoveEmptyPriceLevel(priceLevel, _askSide, false);
                }
                if (!removed && order.IsStop && _stopDictionary.TryGetValue(order.OrderId,out var stopPrice) && _stopAsk.TryGetValue(stopPrice, out PriceLevel stopPriceLevel))
                {
                    _stopDictionary.Remove(order.OrderId);
                    stopPriceLevel.RemoveOrder(order);
                    RemoveEmptyPriceLevel(stopPriceLevel, _stopAsk);
                }
            }
        }

        public void AddStopOrder(Order order,Price stopPrice)
        {
            order.Sequnce = ++_sequence;
            _stopDictionary.Add(order.OrderId, stopPrice);
            if (order.IsBuy)
            {
                PriceLevel priceLevel = GetPriceLevel(stopPrice, _stopBid);
                priceLevel.AddOrder(order);
                if (_bestStopBidPriceLevel == null || stopPrice < _bestStopBidPriceLevel.Price)
                {
                    _bestStopBidPriceLevel = priceLevel;
                }
            }
            else
            {
                PriceLevel priceLevel = GetPriceLevel(stopPrice, _stopAsk);
                priceLevel.AddOrder(order);
                if (_bestStopAskPriceLevel == null || stopPrice > _bestStopAskPriceLevel.Price)
                {
                    _bestStopAskPriceLevel = priceLevel;
                }
            }
        }

        public void AddOrderOpenBook(Order order)
        {
            order.Sequnce = ++_sequence;
            if (order.IsBuy)
            {
                QuantityTrackingPriceLevel priceLevel = GetPriceLevel(order.Price, _bidSide);
                priceLevel.AddOrder(order);
                if (_bestBidPriceLevel == null || order.Price > _bestBidPriceLevel.Price)
                {
                    _bestBidPriceLevel = priceLevel;
                }
            }
            else
            {
                QuantityTrackingPriceLevel priceLevel = GetPriceLevel(order.Price, _askSide);
                priceLevel.AddOrder(order);
                if (_bestAskPriceLevel == null || order.Price < _bestAskPriceLevel.Price)
                {
                    _bestAskPriceLevel = priceLevel;
                }
            }
        }

        public List<PriceLevel> RemoveStopAsks(Price price)
        {
            List<PriceLevel> priceLevels = new List<PriceLevel>();
            if (_bestStopAskPriceLevel != null && price <= _bestStopAskPriceLevel.Price)
            {
                _bestStopAskPriceLevel = null;
                foreach (KeyValuePair<Price, PriceLevel> stopPriceLevel in _stopAsk)
                {
                    if (stopPriceLevel.Key >= price)
                    {
                        priceLevels.Add(stopPriceLevel.Value);
                    }
                    else
                    {
                        _bestStopAskPriceLevel = stopPriceLevel.Value;
                        break;
                    }
                }
                for (var i = 0; i < priceLevels.Count; i++)
                {
                    _stopAsk.Remove(priceLevels[i].Price);
                    foreach (var order in priceLevels[i])
                    {
                        _stopDictionary.Remove(order.OrderId);
                    }
                }
            }
            return priceLevels;
        }

        public List<PriceLevel> RemoveStopBids(Price price)
        {
            List<PriceLevel> priceLevels = new List<PriceLevel>();
            if (_bestStopBidPriceLevel != null && price >= _bestStopBidPriceLevel.Price)
            {
                _bestStopBidPriceLevel = null;
                foreach (KeyValuePair<Price, PriceLevel> stopPriceLevel in _stopBid)
                {
                    if (stopPriceLevel.Key <= price)
                    {
                        priceLevels.Add(stopPriceLevel.Value);
                    }
                    else
                    {
                        _bestStopBidPriceLevel = stopPriceLevel.Value;
                        break;
                    }
                }
                for (var i = 0; i < priceLevels.Count; i++)
                {
                    _stopBid.Remove(priceLevels[i].Price);
                    foreach (var order in priceLevels[i])
                    {
                        _stopDictionary.Remove(order.OrderId);
                    }
                }
            }
            return priceLevels;
        }

        public bool FillOrder(Order order, Quantity quantity)
        {
            SortedDictionary<Price, QuantityTrackingPriceLevel> side = order.IsBuy ? _bidSide : _askSide;
            QuantityTrackingPriceLevel priceLevel = side[order.Price];
            bool orderFilled = priceLevel.Fill(order, quantity);
            RemoveEmptyPriceLevel(priceLevel, side, order.IsBuy);
            return orderFilled;
        }

        public Order GetBestBuyOrderToMatch(bool isBuy)
        {
            QuantityTrackingPriceLevel bestPriceLevel = isBuy ? _bestBidPriceLevel : _bestAskPriceLevel;

            if (bestPriceLevel != null)
            {
                return bestPriceLevel.First;
            }
            return null;
        }

        public bool CheckCanFillOrder(bool isBuy, Quantity requestedQuantity, Price limitPrice)
        {
            return isBuy ? CheckBuyOrderCanBeFilled(requestedQuantity, limitPrice) : CheckSellOrderCanBeFilled(requestedQuantity, limitPrice);
        }

        public bool CheckCanFillMarketOrderAmount(bool isBuy, Quantity orderAmount)
        {
            return isBuy ? CheckMarketOrderAmountCanBeFilled(orderAmount, _askSide) : CheckMarketOrderAmountCanBeFilled(orderAmount, _bidSide);
        }

        private bool CheckBuyOrderCanBeFilled(Quantity requestedQuantity, Price limitPrice)
        {
            Quantity cummulativeQuantity = 0;
            foreach (var priceLevel in _askSide)
            {
                if ((limitPrice >= priceLevel.Key || limitPrice == 0) && cummulativeQuantity <= requestedQuantity)
                {
                    cummulativeQuantity += priceLevel.Value.Quantity;
                }
                else
                {
                    break;
                }
            }
            if (cummulativeQuantity >= requestedQuantity)
            {
                return true;
            }
            return false;
        }

        private bool CheckSellOrderCanBeFilled(Quantity requestedQuantity, Price limitPrice)
        {
            Quantity cummulativeQuantity = 0;
            foreach (var priceLevel in _bidSide)
            {
                if (limitPrice <= priceLevel.Key && cummulativeQuantity <= requestedQuantity)
                {
                    cummulativeQuantity += priceLevel.Value.Quantity;
                }
                else
                {
                    break;
                }
            }
            if (cummulativeQuantity >= requestedQuantity)
            {
                return true;
            }
            return false;
        }

        private bool CheckMarketOrderAmountCanBeFilled(Quantity orderAmount, SortedDictionary<Price, QuantityTrackingPriceLevel> side)
        {
            Quantity cummulativeOrderAmount = 0;
            foreach (var priceLevel in side)
            {
                if (cummulativeOrderAmount <= orderAmount)
                {
                    cummulativeOrderAmount += (priceLevel.Value.Quantity * priceLevel.Key);
                }
                else
                {
                    break;
                }
            }

            if (cummulativeOrderAmount >= orderAmount)
            {
                return true;
            }
            return false;
        }

        private static bool RemoveEmptyPriceLevel(PriceLevel priceLevel, SortedDictionary<Price, PriceLevel> side)
        {
            if (priceLevel.OrderCount == 0)
            {
                return side.Remove(priceLevel.Price);
            }
            return false;
        }

        private void RemoveEmptyPriceLevel(QuantityTrackingPriceLevel priceLevel, SortedDictionary<Price, QuantityTrackingPriceLevel> side, bool isBuy)
        {
            if (priceLevel.OrderCount == 0)
            {
                side.Remove(priceLevel.Price);
                if (isBuy && _bestBidPriceLevel.Price == priceLevel.Price)
                {
                    _bestBidPriceLevel = null;
                    if (side.Count > 0)
                    {
                        var keyval = side.FirstOrDefault();
                        _bestBidPriceLevel = keyval.Value;
                    }
                }
                else if (!isBuy && _bestAskPriceLevel.Price == priceLevel.Price)
                {
                    _bestAskPriceLevel = null;
                    if (side.Count > 0)
                    {
                        var keyval = side.FirstOrDefault();
                        _bestAskPriceLevel = keyval.Value;
                    }
                }
            }
        }

        private static PriceLevel GetPriceLevel(Price price, SortedDictionary<Price, PriceLevel> side)
        {
            if (!side.TryGetValue(price, out PriceLevel priceLevel))
            {
                priceLevel = new PriceLevel(price);
                side.Add(price, priceLevel);
            }
            return priceLevel;
        }

        private static QuantityTrackingPriceLevel GetPriceLevel(Price price, SortedDictionary<Price, QuantityTrackingPriceLevel> side)
        {
            if (!side.TryGetValue(price, out QuantityTrackingPriceLevel priceLevel))
            {
                priceLevel = new QuantityTrackingPriceLevel(price);
                side.Add(price, priceLevel);
            }
            return priceLevel;
        }
    }
}
