namespace OrderMatcher.Tests;

public class OrderSequenceComparerTests
{
    [Fact]
    public void Compare_ReturnsNegativeValue_IfSecondItemIsLarger()
    {
        OrderSequenceComparer comparer = OrderSequenceComparer.Shared;
        Order order1 = new Order() { Sequence = 1 };
        Order order2 = new Order() { Sequence = 2 };

        Assert.True(comparer.Compare(order1, order2) < 0, "it should return negative value");
    }

    [Fact]
    public void Compare_ReturnsPositiveValue_IfSecondItemIsSmaller()
    {
        OrderSequenceComparer comparer = OrderSequenceComparer.Shared;
        Order order1 = new Order() { Sequence = 2 };
        Order order2 = new Order() { Sequence = 1 };

        Assert.True(comparer.Compare(order1, order2) > 0, "it should return positive value");
    }

    [Fact]
    public void Compare_Returns0Value_IfBothItemIsEqual()
    {
        OrderSequenceComparer comparer = OrderSequenceComparer.Shared;
        Order order1 = new Order() { Sequence = 2 };
        Order order2 = new Order() { Sequence = 2 };

        Assert.True(comparer.Compare(order1, order2) == 0, "it should return 0");
    }
}
