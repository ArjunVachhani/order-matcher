using System.Collections.Generic;

namespace OrderMatcher.Tests;

public class PriceLevelTests
{
    [Fact]
    public void AddOrder_AddsOrder()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1, priceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2 };
        priceLevel.AddOrder(order2);

        Assert.Equal(2, priceLevel.OrderCount);
        Assert.Equal(100, priceLevel.Price);
        Assert.Contains(order1, priceLevel);
        Assert.Contains(order2, priceLevel);
    }

    [Fact]
    public void RemoveOrder_RemovesOrder()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1, priceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2 };
        priceLevel.AddOrder(order2);

        Assert.Equal(2, priceLevel.OrderCount);

        Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 100, Sequence = 3 };
        priceLevel.AddOrder(order3);

        Assert.Equal(3, priceLevel.OrderCount);

        Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 100, Sequence = 4 };
        priceLevel.AddOrder(order4);

        Assert.Equal(4, priceLevel.OrderCount);

        priceLevel.RemoveOrder(order3);

        Assert.Equal(3, priceLevel.OrderCount);
        Assert.Equal(100, priceLevel.Price);
        Assert.Contains(order1, priceLevel);
        Assert.Contains(order2, priceLevel);
        Assert.Contains(order4, priceLevel);
    }

    [Fact]
    public void Fill_RemovesOrder_IfOpenQuantityIs0()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, OpenQuantity = 1000, Sequence = 1 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1, priceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, OpenQuantity = 1000, Sequence = 2 };
        priceLevel.AddOrder(order2);

        Assert.Equal(2, priceLevel.OrderCount);

        var filled = priceLevel.Fill(order1, 1000);

        Assert.True(filled);
        Assert.Equal(1, priceLevel.OrderCount);
        Assert.Equal(1000, priceLevel.Quantity);
        Assert.Equal(100, priceLevel.Price);
        Assert.Contains(order2, priceLevel);
    }

    [Fact]
    public void Fill_DoesNotRemoveOrder_IfOpenQuantityIsNot0()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 100, Sequence = 1 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1, priceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 1000, Price = 100, Sequence = 2 };
        priceLevel.AddOrder(order2);

        Assert.Equal(2, priceLevel.OrderCount);

        var filled = priceLevel.Fill(order1, 900);

        Assert.False(filled);
        Assert.Equal(2, priceLevel.OrderCount);
        Assert.Equal(1100, priceLevel.Quantity);
        Assert.Equal(100, priceLevel.Price);
        Assert.Contains(order1, priceLevel);
        Assert.Contains(order2, priceLevel);
        Assert.True(100 == order1.OpenQuantity, "Quantity should be 100");
    }

    [Fact]
    public void Fill_ThrowsException_IfOpenQuantityIsLessThanFillQuantity()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1, priceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2 };
        priceLevel.AddOrder(order2);

        Assert.Equal(2, priceLevel.OrderCount);

        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => priceLevel.Fill(order1, 1100));
        Assert.Equal("Order quantity is less then requested fill quanity", ex.Message);
        Assert.Equal(100, priceLevel.Price);
        Assert.Equal(2, priceLevel.OrderCount);
    }

    [Fact]
    public void PriceLevelSortsOrderBasedOnSequence()
    {
        Price price = new Price(1);
        PriceLevel priceLevel = new PriceLevel(price);
        Assert.Equal(1, priceLevel.Price);

        Order order1 = new Order() { Sequence = 1 };
        priceLevel.AddOrder(order1);

        Order order3 = new Order() { Sequence = 3 };
        priceLevel.AddOrder(order3);

        Order order2 = new Order() { Sequence = 2 };
        priceLevel.AddOrder(order2);

        Order order7 = new Order() { Sequence = 7 };
        priceLevel.AddOrder(order7);

        Order order6 = new Order() { Sequence = 6 };
        priceLevel.AddOrder(order6);

        Order order4 = new Order() { Sequence = 4 };
        priceLevel.AddOrder(order4);

        Order order5 = new Order() { Sequence = 5 };
        priceLevel.AddOrder(order5);

        List<Order> expectedSequence = new List<Order> { order1, order2, order3, order4, order5, order6, order7 };
        AssertHelper.SequentiallyEqual(expectedSequence, priceLevel);
        Assert.Equal(1, priceLevel.Price);
    }

    [Fact]
    public void SetPriceWorksIfNoOrder()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);
        priceLevel.SetPrice(102);//works if no orders

        Assert.Equal(102, priceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 102, Sequence = 1, OpenQuantity = 1000 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1000, priceLevel.Quantity);
        Assert.Equal(1, priceLevel.OrderCount);
        Assert.Equal(102, priceLevel.Price);

        priceLevel.RemoveOrder(order1);
        Assert.Equal(0, priceLevel.Quantity);
        Assert.Equal(0, priceLevel.OrderCount);
        Assert.Equal(102, priceLevel.Price);


        priceLevel.SetPrice(103);//works if no orders

        Assert.Equal(103, priceLevel.Price);
        Order order2 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 103, Sequence = 1, OpenQuantity = 1000 };
        priceLevel.AddOrder(order2);

        Assert.Equal(1000, priceLevel.Quantity);
        Assert.Equal(1, priceLevel.OrderCount);
        Assert.Equal(103, priceLevel.Price);
    }

    [Fact]
    public void SetPriceThrowsIfHasOrder()
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Assert.Equal(100, priceLevel.Price);

        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        priceLevel.AddOrder(order1);

        Assert.Equal(1000, priceLevel.Quantity);
        Assert.Equal(1, priceLevel.OrderCount);
        Assert.Equal(100, priceLevel.Price);


        var exception = Assert.Throws<OrderMatcherException>(() => priceLevel.SetPrice(104));//throws if has orders
        Assert.StartsWith("Cannot set price because pricelevel has", exception.Message);

        Assert.Equal(1000, priceLevel.Quantity);
        Assert.Equal(1, priceLevel.OrderCount);
        Assert.Equal(100, priceLevel.Price);
    }

    [Theory]
    [InlineData(10, 0, 0, 1, 9)]
    [InlineData(10, 10, 10, 10, 10)]
    [InlineData(10, 10, 10, 1, 10)]
    [InlineData(10, 10, 10, 11, 9)]
    [InlineData(10, 10, 10, 20, 10)]
    public void DecrementQuantityTests(int openQuantity, int tipQuanitity, int totalQuantity, int quantityToDecrement, int expectedPriceLevelQuantity)
    {
        PriceLevel priceLevel = new PriceLevel(100);
        Order order1 = new Order()
        {
            IsBuy = true,
            OrderId = 1,
            UserId = 1,
            Price = 100,
            Sequence = 1,
            OpenQuantity = openQuantity,
            TotalQuantity = totalQuantity,
            TipQuantity = tipQuanitity
        };
        priceLevel.AddOrder(order1);

        Order order2 = new Order()
        {
            IsBuy = true,
            OrderId = 2,
            UserId = 2,
            Price = 100,
            Sequence = 2,
            OpenQuantity = openQuantity,
            TotalQuantity = totalQuantity,
            TipQuantity = tipQuanitity
        };
        priceLevel.DecrementQuantity(order1, quantityToDecrement);
        Assert.Equal(expectedPriceLevelQuantity, priceLevel.Quantity);
        priceLevel.DecrementQuantity(order2, quantityToDecrement);
    }
}
