﻿namespace OrderMatcher;

public class PriceLevel : IPriceLevel
{
    private readonly SortedSet<Order> _orders;

    private Price _price;

    public int OrderCount => _orders.Count;
    public Price Price => _price;
    public Quantity Quantity
    {
        get
        {
            Quantity totalQuantity = 0;
            foreach (var order in _orders)
            {
                totalQuantity += order.OpenQuantity;
            }
            return totalQuantity;
        }
    }

    public PriceLevel()
    {
        _orders = new SortedSet<Order>(OrderSequenceComparer.Shared);
    }

    public PriceLevel(Price price)
    {
        _price = price;
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
        _orders.Add(order);
    }

    public bool RemoveOrder(Order order)
    {
        return _orders.Remove(order);
    }

    public bool Fill(Order order, Quantity quantity)
    {
        if (order.OpenQuantity >= quantity)
        {
            order.OpenQuantity = order.OpenQuantity - quantity;
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
            orderToUpdate.DecrementQuantity(quantityToDecrement);
        }
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
