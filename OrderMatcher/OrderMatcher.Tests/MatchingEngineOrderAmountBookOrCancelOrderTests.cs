namespace OrderMatcher.Tests;

public class MatchingEngineOrderAmountBookOrCancelOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineOrderAmountBookOrCancelOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForBookOrCancelBuy()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 1, OrderCondition = OrderCondition.BookOrCancel, OrderAmount = 1 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        mockTradeListener.VerifyNoOtherCalls();
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
    }
}
