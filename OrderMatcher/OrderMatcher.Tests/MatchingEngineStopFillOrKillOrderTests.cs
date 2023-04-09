namespace OrderMatcher.Tests;

public class MatchingEngineStopFillOrKillOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineStopFillOrKillOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(0)]
    public void AddOrder_Rejects_FillOrKill_Order_With_StopLimit_Price(int price)
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = price, OrderCondition = OrderCondition.FillOrKill, StopPrice = 9 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.FillOrKillCannotBeStopOrder, accepted);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        mockTradeListener.VerifyNoOtherCalls();
    }
}
