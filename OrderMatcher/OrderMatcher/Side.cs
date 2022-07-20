using OrderMatcher.Types;
using System.Collections.Generic;
using System.Linq;

namespace OrderMatcher
{
    internal class Side<T> where T : IPriceLevel, new()
    {
        private readonly SortedDictionary<Price, T> _priceLevels;
        private readonly IComparer<Price> _priceComparer;

        private T _bestPriceLevel;

        public T BestPriceLevel => _bestPriceLevel;
        public int PriceLevelCount => _priceLevels.Count;
        public IEnumerable<KeyValuePair<Price, T>> PriceLevels => _priceLevels;

        public Side(IComparer<Price> comparer)
        {
            _priceComparer = comparer;
            _priceLevels = new SortedDictionary<Price, T>(comparer);
        }

        public void AddOrder(Order order, Price price)
        {
            T priceLevel = GetOrAddPriceLevel(price);
            priceLevel.AddOrder(order);
        }

        public bool RemoveOrder(Order order, Price price)
        {
            bool removed = false;
            if (_priceLevels.TryGetValue(price, out T priceLevel))
            {
                removed = priceLevel.RemoveOrder(order);
                RemovePriceLevelIfEmpty(priceLevel);
            }
            return removed;
        }

        public List<T> RemovePriceLevelTill(Price price)
        {
            List<T> priceLevels = new List<T>();
            if (_bestPriceLevel != null && _priceComparer.Compare(_bestPriceLevel.Price, price) <= 0)
            {
                _bestPriceLevel = default;
                foreach (KeyValuePair<Price, T> stopPriceLevel in _priceLevels)
                {
                    if (_priceComparer.Compare(stopPriceLevel.Key, price) <= 0)
                    {
                        priceLevels.Add(stopPriceLevel.Value);
                    }
                    else
                    {
                        _bestPriceLevel = stopPriceLevel.Value;
                        break;
                    }
                }
                for (var i = 0; i < priceLevels.Count; i++)
                {
                    _priceLevels.Remove(priceLevels[i].Price);
                }
            }
            return priceLevels;
        }

        public bool FillOrder(Order order, Quantity quantity)
        {
            T priceLevel = _priceLevels[order.Price];
            bool orderFilled = priceLevel.Fill(order, quantity);
            RemovePriceLevelIfEmpty(priceLevel);
            return orderFilled;
        }

        public bool CheckCanBeFilled(Quantity requestedQuantity, Price limitPrice)
        {
            Quantity cummulativeQuantity = 0;
            foreach (var priceLevel in _priceLevels)
            {
                if ((_priceComparer.Compare(limitPrice, priceLevel.Key) >= 0 || limitPrice == 0) && cummulativeQuantity <= requestedQuantity)
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

        public bool CheckMarketOrderAmountCanBeFilled(Quantity orderAmount)
        {
            Quantity cummulativeOrderAmount = 0;
            foreach (var priceLevel in _priceLevels)
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

        private T GetOrAddPriceLevel(Price price)
        {
            if (!_priceLevels.TryGetValue(price, out T priceLevel))
            {
                priceLevel = new T();
                priceLevel.SetPrice(price);
                _priceLevels.Add(price, priceLevel);
                if (_bestPriceLevel == null || _priceComparer.Compare(price, _bestPriceLevel.Price) < 0)
                {
                    _bestPriceLevel = priceLevel;
                }
            }
            return priceLevel;
        }

        private void RemovePriceLevelIfEmpty(T priceLevel)
        {
            if (priceLevel.OrderCount == 0)
            {
                _priceLevels.Remove(priceLevel.Price);
                if (_bestPriceLevel!.Price == priceLevel.Price)
                {
                    _bestPriceLevel = _priceLevels.FirstOrDefault().Value;
                }
            }
        }
    }
}
