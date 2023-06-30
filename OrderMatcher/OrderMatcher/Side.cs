namespace OrderMatcher;

internal class Side<T> where T : class, IPriceLevel, new()
{
    private readonly SortedSet<T> _priceLevels;
    private readonly IComparer<Price> _priceComparer;
    private readonly T _priceLevelForSearch = new T();

    private T? _bestPriceLevel;

    public T? BestPriceLevel => _bestPriceLevel;
    public int PriceLevelCount => _priceLevels.Count;
    public IEnumerable<T> PriceLevels => _priceLevels;

    public Side(IComparer<Price> priceComparer, IComparer<T> priceLevelComparer)
    {
        _priceComparer = priceComparer;
        _priceLevels = new SortedSet<T>(priceLevelComparer);
    }

    public void AddOrder(Order order, Price price)
    {
        T priceLevel = GetOrAddPriceLevel(price);
        priceLevel.AddOrder(order);
    }

    public bool RemoveOrder(Order order, Price price)
    {
        bool removed = false;
        _priceLevelForSearch.SetPrice(price);
        if (_priceLevels.TryGetValue(_priceLevelForSearch, out T? priceLevel))
        {
            removed = priceLevel.RemoveOrder(order);
            RemovePriceLevelIfEmpty(priceLevel);
        }
        return removed;
    }

    public void DecrementQuantity(Order order, Quantity quantityToDecrement)
    {
        _priceLevelForSearch.SetPrice(order.Price);
        if (_priceLevels.TryGetValue(_priceLevelForSearch, out T? priceLevel))
        {
            priceLevel.DecrementQuantity(order, quantityToDecrement);
        }
    }

    public IReadOnlyList<T>? RemovePriceLevelTill(Price price)
    {
        List<T>? priceLevels = null;
        if (_bestPriceLevel != null && _priceComparer.Compare(_bestPriceLevel.Price, price) <= 0)
        {
            priceLevels = new List<T>();
            _bestPriceLevel = default;
            foreach (T stopPriceLevel in _priceLevels)
            {
                if (_priceComparer.Compare(stopPriceLevel.Price, price) <= 0)
                {
                    priceLevels.Add(stopPriceLevel);
                }
                else
                {
                    _bestPriceLevel = stopPriceLevel;
                    break;
                }
            }
            for (var i = 0; i < priceLevels.Count; i++)
            {
                _priceLevels.Remove(priceLevels[i]);
            }
        }
        return priceLevels;
    }

    public bool FillOrder(Order order, Quantity quantity)
    {
        _priceLevelForSearch.SetPrice(order.Price);
        _priceLevels.TryGetValue(_priceLevelForSearch, out T? priceLevel);
        bool orderFilled = priceLevel!.Fill(order, quantity);
        RemovePriceLevelIfEmpty(priceLevel);
        return orderFilled;
    }

    public bool CheckCanBeFilled(Quantity requestedQuantity, Price limitPrice)
    {
        Quantity cummulativeQuantity = 0;
        foreach (var priceLevel in _priceLevels)
        {
            if ((_priceComparer.Compare(limitPrice, priceLevel.Price) >= 0 || limitPrice == 0) && cummulativeQuantity <= requestedQuantity)
            {
                cummulativeQuantity += priceLevel.Quantity;
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

    public bool CheckMarketOrderAmountCanBeFilled(Amount orderAmount)
    {
        Amount cummulativeOrderAmount = 0;
        foreach (var priceLevel in _priceLevels)
        {
            if (cummulativeOrderAmount <= orderAmount)
            {
                cummulativeOrderAmount += (priceLevel.Quantity * priceLevel.Price);
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
        _priceLevelForSearch.SetPrice(price);
        if (!_priceLevels.TryGetValue(_priceLevelForSearch, out T? priceLevel))
        {
            priceLevel = new T();
            priceLevel.SetPrice(price);
            _priceLevels.Add(priceLevel);
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
            _priceLevels.Remove(priceLevel);
            if (_bestPriceLevel!.Price == priceLevel.Price)
            {
                _bestPriceLevel = _priceLevels.Min;
            }
        }
    }
}
