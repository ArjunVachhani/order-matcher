namespace OrderMatcher;

public class Book
{
    private readonly GoodTillDateOrders _goodTillDateOrders;
    private readonly CurrentOrders _currentOrders;
    private readonly Side<QuantityTrackingPriceLevel> _bids;
    private readonly Side<QuantityTrackingPriceLevel> _asks;
    private readonly Side<PriceLevel> _stopBids;
    private readonly Side<PriceLevel> _stopAsks;

    private ulong _sequence;

    public IEnumerable<KeyValuePair<int, HashSet<OrderId>>> GoodTillDateOrders => _goodTillDateOrders;
    public IEnumerable<Order> CurrentOrders => _currentOrders;
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
        _currentOrders = new CurrentOrders();
        _goodTillDateOrders = new GoodTillDateOrders();
        _bids = new Side<QuantityTrackingPriceLevel>(PriceComparerDescending.Shared, PriceLevelComparerDescending<QuantityTrackingPriceLevel>.Shared);
        _asks = new Side<QuantityTrackingPriceLevel>(PriceComparerAscending.Shared, PriceLevelComparerAscending<QuantityTrackingPriceLevel>.Shared);
        _stopBids = new Side<PriceLevel>(PriceComparerAscending.Shared, PriceLevelComparerAscending<PriceLevel>.Shared);
        _stopAsks = new Side<PriceLevel>(PriceComparerDescending.Shared, PriceLevelComparerDescending<PriceLevel>.Shared);
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

        _currentOrders.Remove(order);
        _goodTillDateOrders.Remove(order);
    }

    internal void AddStopOrder(Order order)
    {
        order.Sequence = ++_sequence;
        var side = order.IsBuy ? _stopBids : _stopAsks;
        side.AddOrder(order, order.StopPrice);
        _currentOrders.Add(order);
        _goodTillDateOrders.Add(order);
    }

    internal void AddOrderOpenBook(Order order)
    {
        order.Sequence = ++_sequence;
        var side = order.IsBuy ? _bids : _asks;
        side.AddOrder(order, order.Price);
        _currentOrders.Add(order);
        _goodTillDateOrders.Add(order);
    }

    internal IReadOnlyList<PriceLevel>? RemoveStopAsks(Price price)
    {
        return RemoveFromTracking(_stopAsks.RemovePriceLevelTill(price));
    }

    internal IReadOnlyList<PriceLevel>? RemoveStopBids(Price price)
    {
        return RemoveFromTracking(_stopBids.RemovePriceLevelTill(price));
    }

    private IReadOnlyList<PriceLevel>? RemoveFromTracking(IReadOnlyList<PriceLevel>? priceLevels)
    {
        if (priceLevels != null)
        {
            for (int i = 0; i < priceLevels.Count; i++)
            {
                foreach (var order in priceLevels[i])
                {
                    _currentOrders.Remove(order);
                    _goodTillDateOrders.Remove(order);
                }
            }
        }
        return priceLevels;
    }

    internal bool TryGetOrder(OrderId orderId, out Order? order)
    {
        return _currentOrders.TryGetOrder(orderId, out order);
    }

    internal List<HashSet<OrderId>>? GetExpiredOrders(int timeNow)
    {
        return _goodTillDateOrders.GetExpiredOrders(timeNow);
    }

    internal bool FillOrder(Order order, Quantity quantity)
    {
        var side = order.IsBuy ? _bids : _asks;
        if (side.FillOrder(order, quantity))
        {
            _currentOrders.Remove(order);
            _goodTillDateOrders.Remove(order);
            return true;
        }
        return false;
    }

    internal Order? GetBestBuyOrderToMatch(bool isBuy)
    {
        return isBuy ? _bids.BestPriceLevel?.First : _asks.BestPriceLevel?.First;
    }

    internal void DecrementQuantity(Order order, Quantity quantityToDecrement)
    {
        var side = order.IsBuy ? _bids: _asks;
        side.DecrementQuantity(order, quantityToDecrement);
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
}
