namespace OrderMatcher.Tests;

public class MatchingEngineMarketIcebergOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineMarketIcebergOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_RejectsOrder_IfIcebergOrderIsMarketOrder()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 0, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeMarketOrStopMarketOrder, acceptanceResult);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal((ulong)0, order1.Sequence);
    }
}
