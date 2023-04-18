namespace OrderMatcher.Tests;

public class MatchingEngineMarketFillOrKillOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineMarketFillOrKillOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_CancelsFullQuantityFOKMarketOrder_IfNotEnoughSellAvailable()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, StopPrice = 11 };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);

        Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
        OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

        mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order4.OpenQuantity);
        Assert.Equal((ulong)3, order4.Sequence);

        Order order5 = new Order { IsBuy = false, OpenQuantity = 600, Price = 0, OrderId = 5, UserId = 5, OrderCondition = OrderCondition.FillOrKill };
        OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(order5, 5);

        mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
        mockTradeListener.Verify(x => x.OnCancel(5, 5, 600, 0, 0, CancelReason.FillOrKill));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
        Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
        Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(600, order5.OpenQuantity);
        Assert.Equal((ulong)0, order5.Sequence);
    }

    [Fact]
    public void AddOrder_FillsFullQuantityFOKMarketOrder_IfNotEnoughSellAvailable()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, StopPrice = 9 };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);

        Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
        OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

        mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order4.OpenQuantity);
        Assert.Equal((ulong)3, order4.Sequence);

        Order order5 = new Order { IsBuy = true, OpenQuantity = 400, Price = 0, OrderId = 5, UserId = 5, OrderCondition = OrderCondition.FillOrKill };
        OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(order5, 5);

        mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
        mockTradeListener.Verify(x => x.OnTrade(5, 4, 5, 4, true, 10, 400, null, null, 4000, 20));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
        Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
        Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order5.OpenQuantity);
        Assert.Equal((ulong)0, order5.Sequence);
    }

    [Fact]
    public void AddOrder_CancelsFullQuantityFOKMarketOrder_IfNotEnoughBuyAvailable()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, StopPrice = 11 };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);

        Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
        OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

        mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order4.OpenQuantity);
        Assert.Equal((ulong)3, order4.Sequence);

        Order order5 = new Order { IsBuy = false, OpenQuantity = 600, Price = 0, OrderId = 5, UserId = 5, OrderCondition = OrderCondition.FillOrKill };
        OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(order5, 5);

        mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
        mockTradeListener.Verify(x => x.OnCancel(5, 5, 600, 0, 0, CancelReason.FillOrKill));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
        Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
        Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order5, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(600, order5.OpenQuantity);
        Assert.Equal((ulong)0, order5.Sequence);
    }

    [Fact]
    public void AddOrder_FillsFullQuantityFOKMarketOrder_IfNotEnoughBuyAvailable()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, StopPrice = 11 };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);

        Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
        OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

        mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order4.OpenQuantity);
        Assert.Equal((ulong)3, order4.Sequence);

        Order order5 = new Order { IsBuy = false, OpenQuantity = 400, Price = 0, OrderId = 5, UserId = 5, OrderCondition = OrderCondition.FillOrKill };
        OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(order5, 5);

        mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
        mockTradeListener.Verify(x => x.OnTrade(5, 4, 5, 4, false, 10, 400, 0, 20, null, null));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
        Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
        Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order5, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order5.OpenQuantity);
        Assert.Equal((ulong)0, order5.Sequence);
    }
}
