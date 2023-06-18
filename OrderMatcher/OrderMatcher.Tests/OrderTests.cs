namespace OrderMatcher.Tests;

public class OrderTests
{
    [Fact]
    public void Filled_ReturnsTrue_MarketOrderFilled()
    {
        Order order = new Order() { Price = 0, OpenQuantity = 0, IsBuy = true };
        Assert.True(order.IsFilled);
    }

    [Fact]
    public void Filled_ReturnsFalse_MarketOrderFilled_2()
    {
        Order order = new Order() { Price = 0, OpenQuantity = 1, IsBuy = true };
        Assert.False(order.IsFilled);
    }

    [Fact]
    public void Filled_ReturnsTrue_MarketOrderFilled_3()
    {
        Order order = new Order() { Price = 0, OpenQuantity = 0, IsBuy = false };
        Assert.True(order.IsFilled);
    }

    [Fact]
    public void Filled_ReturnsFalse_MarketOrderFilled_4()
    {
        Order order = new Order() { Price = 0, OpenQuantity = 1, IsBuy = false };
        Assert.False(order.IsFilled);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void EqualsChecksOrderId(int orderId1, int orderId2, bool expectedResult)
    {
        var order1 = new Order() { OrderId = orderId1 };
        var order2 = new Order() { OrderId = orderId2 };
        var actualResult = order1.Equals(order2);
        Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void GetHashCodeResultsOrderIdHashCode(int orderId)
    {
        var order = new Order() { OrderId = orderId };
        Assert.Equal(new OrderId(orderId).GetHashCode(), order.GetHashCode());
    }

    [Theory]
    [InlineData(0, 1, 1, true)]
    [InlineData(0, 1, 0, false)]
    [InlineData(1, 1, 0, false)]
    [InlineData(1, 1, 1, true)]
    public void IsTipTest(int openQuantity, int tipQuantity, int totalQuantity, bool expectedResult)
    {
        var order = new Order() { OpenQuantity = openQuantity, TipQuantity = tipQuantity, TotalQuantity = totalQuantity };
        Assert.Equal(expectedResult, order.IsTip);
    }


    [Theory]
    [InlineData(10, 0, 0, 9, 1, 0, true)]
    [InlineData(10, 0, 0, 10, 10, 0, false)]
    [InlineData(10, 0, 0, 11, 10, 0, false)]
    [InlineData(10, 1, 10, 9, 10, 1, true)]
    [InlineData(10, 1, 10, 8, 10, 2, true)]
    [InlineData(10, 1, 10, 10, 10, 0, true)]
    [InlineData(10, 1, 10, 11, 9, 0, true)]
    [InlineData(10, 1, 10, 19, 1, 0, true)]
    [InlineData(10, 1, 10, 20, 10, 10, false)]
    public void DecrementQuantityTest(int openQuantity, int tipQuantity, int totalQuantity, int quantityToDecremenent, int remainingOpenQuantity, int remainingTotalQuantity, bool expectedResult)
    {
        var order = new Order() { OpenQuantity = openQuantity, TipQuantity = tipQuantity, TotalQuantity = totalQuantity };
        var result = order.DecrementQuantity(quantityToDecremenent);
        Assert.Equal(expectedResult, result);
        Assert.Equal(remainingOpenQuantity, order.OpenQuantity);
        Assert.Equal(remainingTotalQuantity, order.TotalQuantity);
    }
}
