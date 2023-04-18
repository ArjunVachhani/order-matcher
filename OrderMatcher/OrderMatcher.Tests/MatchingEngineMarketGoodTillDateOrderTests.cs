namespace OrderMatcher.Tests;

public class MatchingEngineMarketGoodTillDateOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineMarketGoodTillDateOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_RejectsOrder_MarketGoodTillDate()
    {
        Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 1, UserId = 1, CancelOn = 10 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.GoodTillDateCannotBeMarketOrIOCorFOK, acceptanceResult);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)0, order1.Sequence);
    }
}
