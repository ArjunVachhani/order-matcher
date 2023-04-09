namespace OrderMatcher.Tests;

public class MatchingEngineOrderAmountGoodTillDateOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineOrderAmountGoodTillDateOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForGTD()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 0, CancelOn = 1, OrderAmount = 1 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.GoodTillDateCannotBeMarketOrIOCorFOK, accepted);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        mockTradeListener.VerifyNoOtherCalls();
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
    }

    [Fact]
    public void AddOrder_Accepts_OrderAmountOnlySupportedForStopMarketBuyOrder_ForGTD()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 0, StopPrice = 10, CancelOn = 10, OrderAmount = 1 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
        mockTradeListener.Verify(x => x.OnAccept(1, 1));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);

        matchingEngine.CancelExpiredOrder(10);
        mockTradeListener.Verify(x => x.OnCancel(1, 1, 0, 0, 0, CancelReason.ValidityExpired));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.DoesNotContain(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
    }
}
