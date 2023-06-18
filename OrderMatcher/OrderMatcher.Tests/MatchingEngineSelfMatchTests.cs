namespace OrderMatcher.Tests;

public class MatchingEngineSelfMatchTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineSelfMatchTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_Normal_Limit_SelfMatch_And_Cancel()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        var cancelResult = matchingEngine.CancelOrder(1);

        mockTradeListener.Verify(x => x.OnCancel(1, 1, 1000, 0, 0, CancelReason.UserRequested));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.CancelAcepted, cancelResult);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide); ;
    }

    [Fact]
    public void AddOrder_Normal_Limit_Matches_With_Pending_Order()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 1000, Price = 10, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, false, 10, 1000, 0, 50, 10000, 20));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(0, order1.OpenQuantity);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Empty(matchingEngine.CurrentOrders);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
    }

    [Fact]
    public void AddOrder_Normal_Limit_Matches_With_Same_User_MatchAction_Pending_Order()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 1000, Price = 10, SelfMatchAction = SelfMatchAction.Match };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 1, 1, false, 10, 1000, 0, 50, 10000, 20));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(0, order1.OpenQuantity);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Empty(matchingEngine.CurrentOrders);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
    }

    [Fact]
    public void AddOrder_Normal_Limit_Matches_With_Same_User_CancelNewest()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 900, Price = 10, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order2.OrderId, order2.UserId, 900, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(1000, order1.OpenQuantity);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Equal(1000, matchingEngine.Book.BidSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_Normal_Limit_Matches_With_Same_User_CancelOldest()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 900, Price = 10, SelfMatchAction = SelfMatchAction.CancelOldest };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 1000, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(900, order2.OpenQuantity);
        Assert.Contains(order2, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Equal(900, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_Normal_Limit_Matches_With_Same_User_Decrement_IncomingLargerQuantity()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 1100, Price = 10, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order2.OrderId, order2.UserId, 1000));
        mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 1000, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(100, order2.OpenQuantity);
        Assert.Contains(order2, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Equal(100, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_Normal_Limit_Matches_With_Same_User_Decrement_IncomingSmallerQuantity()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 900, Price = 10, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order1.OrderId, order1.UserId, 900));
        mockTradeListener.Verify(x => x.OnCancel(order2.OrderId, order2.UserId, 900, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(100, order1.OpenQuantity);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Equal(100, matchingEngine.Book.BidSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }
}
