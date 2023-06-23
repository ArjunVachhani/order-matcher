using System.Collections.Generic;

namespace OrderMatcher.Tests;

public class QuantityTrackingPriceLevelTests
{
    [Fact]
    public void AddOrder_AddsOrder()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Assert.Equal(2000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Assert.Contains(order1, quantityTrackingPriceLevel);
        Assert.Contains(order2, quantityTrackingPriceLevel);
    }

    [Fact]
    public void RemoveOrder_RemovesOrder()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Assert.Equal(2000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);

        Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 100, Sequence = 3, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order3);

        Assert.Equal(3000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(3, quantityTrackingPriceLevel.OrderCount);

        Order order4 = new Order() { IsBuy = true, OrderId = 4, UserId = 4, Price = 100, Sequence = 4, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order4);

        Assert.Equal(4000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(4, quantityTrackingPriceLevel.OrderCount);

        quantityTrackingPriceLevel.RemoveOrder(order3);

        Assert.Equal(3000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(3, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Assert.Contains(order1, quantityTrackingPriceLevel);
        Assert.Contains(order2, quantityTrackingPriceLevel);
        Assert.Contains(order4, quantityTrackingPriceLevel);
    }

    [Fact]
    public void Fill_RemovesOrder_IfOpenQuantityIs0()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, OpenQuantity = 1000, Sequence = 1 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, OpenQuantity = 1000, Sequence = 2 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Assert.Equal(2000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);

        var filled = quantityTrackingPriceLevel.Fill(order1, 1000);

        Assert.True(filled);
        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Assert.Contains(order2, quantityTrackingPriceLevel);
    }

    [Fact]
    public void Fill_DoesNotRemoveOrder_IfOpenQuantityIsNot0()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 100, Sequence = 1 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, OpenQuantity = 1000, Price = 100, Sequence = 2 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Assert.Equal(2000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);

        var filled = quantityTrackingPriceLevel.Fill(order1, 900);

        Assert.False(filled);
        Assert.Equal(1100, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Assert.Contains(order1, quantityTrackingPriceLevel);
        Assert.Contains(order2, quantityTrackingPriceLevel);
        Assert.True(100 == order1.OpenQuantity, "Quantity should be 100");
    }

    [Fact]
    public void Fill_ThrowsException_IfOpenQuantityIsLessThanFillQuantity()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Assert.Equal(2000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);

        OrderMatcherException ex = Assert.Throws<OrderMatcherException>(() => quantityTrackingPriceLevel.Fill(order1, 1100));
        Assert.Equal("Order quantity is less then requested fill quanity", ex.Message);
        Assert.Equal(2000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(2, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
    }


    [Fact]
    public void First_ReturnsFirstOrder_1()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 2, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 100, Sequence = 3, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order3);

        Assert.Equal(order2, quantityTrackingPriceLevel.First);
    }


    [Fact]
    public void First_ReturnsFirstOrder_2()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);

        Order order2 = new Order() { IsBuy = true, OrderId = 2, UserId = 2, Price = 100, Sequence = 2, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Order order3 = new Order() { IsBuy = true, OrderId = 3, UserId = 3, Price = 100, Sequence = 3, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order3);

        Assert.Equal(order1, quantityTrackingPriceLevel.First);
    }

    [Fact]
    public void SortsOrderBasedOnSequence()
    {
        Price price = new Price(1);
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(price);
        Assert.Equal(1, quantityTrackingPriceLevel.Price);

        Order order1 = new Order() { Sequence = 1 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Order order3 = new Order() { Sequence = 3 };
        quantityTrackingPriceLevel.AddOrder(order3);

        Order order2 = new Order() { Sequence = 2 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Order order7 = new Order() { Sequence = 7 };
        quantityTrackingPriceLevel.AddOrder(order7);

        Order order6 = new Order() { Sequence = 6 };
        quantityTrackingPriceLevel.AddOrder(order6);

        Order order4 = new Order() { Sequence = 4 };
        quantityTrackingPriceLevel.AddOrder(order4);

        Order order5 = new Order() { Sequence = 5 };
        quantityTrackingPriceLevel.AddOrder(order5);

        List<Order> expectedSequence = new List<Order> { order1, order2, order3, order4, order5, order6, order7 };
        AssertHelper.SequentiallyEqual(expectedSequence, quantityTrackingPriceLevel);
    }

    [Fact]
    public void SetPriceWorksIfNoOrder()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
        quantityTrackingPriceLevel.SetPrice(102);//works if no orders

        Assert.Equal(102, quantityTrackingPriceLevel.Price);
        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 102, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(102, quantityTrackingPriceLevel.Price);

        quantityTrackingPriceLevel.RemoveOrder(order1);
        Assert.Equal(0, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(0, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(102, quantityTrackingPriceLevel.Price);


        quantityTrackingPriceLevel.SetPrice(103);//works if no orders

        Assert.Equal(103, quantityTrackingPriceLevel.Price);
        Order order2 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 103, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order2);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(103, quantityTrackingPriceLevel.Price);
    }

    [Fact]
    public void SetPriceThrowsIfHasOrder()
    {
        QuantityTrackingPriceLevel quantityTrackingPriceLevel = new QuantityTrackingPriceLevel(100);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);

        Order order1 = new Order() { IsBuy = true, OrderId = 1, UserId = 1, Price = 100, Sequence = 1, OpenQuantity = 1000 };
        quantityTrackingPriceLevel.AddOrder(order1);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);


        var exception = Assert.Throws<OrderMatcherException>(() => quantityTrackingPriceLevel.SetPrice(104));//throws if has orders
        Assert.StartsWith("Cannot set price because pricelevel has", exception.Message);

        Assert.Equal(1000, quantityTrackingPriceLevel.Quantity);
        Assert.Equal(1, quantityTrackingPriceLevel.OrderCount);
        Assert.Equal(100, quantityTrackingPriceLevel.Price);
    }

    [Theory]
    [InlineData(10, 0, 0, 1, 9)]
    [InlineData(10, 10, 10, 10, 10)]
    [InlineData(10, 10, 10, 1, 10)]
    [InlineData(10, 10, 10, 11, 9)]
    [InlineData(10, 10, 10, 20, 10)]
    public void DecrementQuantityTests(int openQuantity, int tipQuanitity, int totalQuantity, int quantityToDecrement, int expectedPriceLevelQuantity)
    {
        QuantityTrackingPriceLevel priceLevel = new QuantityTrackingPriceLevel(100);
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
