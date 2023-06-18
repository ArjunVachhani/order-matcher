﻿namespace OrderMatcher;

public class QuantityTrackingPriceLevel : IPriceLevel
{
    private readonly SortedSet<Order> _orders;

    private Price _price;
    private Quantity _quantity;

    public int OrderCount => _orders.Count;
    public Quantity Quantity => _quantity;
    public Price Price => _price;

    public QuantityTrackingPriceLevel()
    {
        _quantity = 0;
        _orders = new SortedSet<Order>(OrderSequenceComparer.Shared);
    }

    public QuantityTrackingPriceLevel(Price price)
    {
        _price = price;
        _quantity = 0;
        _orders = new SortedSet<Order>(OrderSequenceComparer.Shared);
    }

    public void SetPrice(Price price)
    {
        if (_orders.Count > 0)
            throw new OrderMatcherException($"Cannot set price because pricelevel has {_orders.Count} orders.");

        _price = price;
    }

    public void AddOrder(Order order)
    {
        _quantity += order.OpenQuantity;
        _orders.Add(order);
    }

    public bool RemoveOrder(Order order)
    {
        _quantity -= order.OpenQuantity;
        return _orders.Remove(order);
    }

    public bool Fill(Order order, Quantity quantity)
    {
        if (order.OpenQuantity >= quantity)
        {
            _quantity -= quantity;
            order.OpenQuantity -= quantity;
            if (order.IsFilled)
            {
                return _orders.Remove(order);
            }
            return false;
        }
        else
        {
            throw new OrderMatcherException(Constant.ORDER_QUANTITY_IS_LESS_THEN_REQUESTED_FILL_QUANTITY);
        }
    }

    public void DecrementQuantity(Order order, Quantity quantityToDecrement)
    {
        if (_orders.TryGetValue(order, out var orderToUpdate))
        {
            var previousOpenQuantity = orderToUpdate.OpenQuantity;
            if (orderToUpdate.DecrementQuantity(quantityToDecrement))
            {
                _quantity -= previousOpenQuantity;
                _quantity += orderToUpdate.OpenQuantity;
            }
        }
    }

    public Order? First
    {
        get { return _orders.Min; }
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