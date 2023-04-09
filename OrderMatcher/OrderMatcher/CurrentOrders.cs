namespace OrderMatcher;

internal class CurrentOrders : IEnumerable<Order>
{
    private readonly HashSet<Order> _orders = new HashSet<Order>();
    private readonly Order _currentOrderLookup = new Order();

    public bool TryGetOrder(OrderId orderId, out Order? order)
    {
        _currentOrderLookup.OrderId = orderId;
        return _orders.TryGetValue(_currentOrderLookup, out order);
    }

    public bool Remove(Order order)
    {
        return _orders.Remove(order);
    }

    public bool Add(Order order)
    {
        return _orders.Add(order);
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
