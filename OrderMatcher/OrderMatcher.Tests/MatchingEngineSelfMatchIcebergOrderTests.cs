namespace OrderMatcher.Tests;

public class MatchingEngineSelfMatchIcebergOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineSelfMatchIcebergOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void CancelOrder_CancelsFullOrder_IfNoFillIcebergOrderBuySide()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)0, order1.Sequence);

        OrderMatchingResult acceptanceResult2 = matchingEngine.CancelOrder(order1.OrderId);
        mockTradeListener.Verify(x => x.OnCancel(1, 1, 5000, 0, 0, CancelReason.UserRequested));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult2);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal((ulong)0, order1.Sequence);
    }

    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrderPartiallyFilledBuy()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)0, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 3 };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, true, 10, 200, null, null, 2000, 10));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(300, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order3.OpenQuantity);
        Assert.Equal((ulong)0, order3.Sequence);

        OrderMatchingResult acceptanceResult4 = matchingEngine.CancelOrder(order1.OrderId);
        mockTradeListener.Verify(x => x.OnCancel(1, 1, 4300, 7000, 14, CancelReason.UserRequested));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult4);
        Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal((ulong)0, order1.Sequence);
    }

    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrder_CancelOldest()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)0, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 1, SelfMatchAction = SelfMatchAction.CancelOldest };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order3.OrderId, order1.OrderId, order1.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 4500, 5000, 10, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Equal(200, order3.OpenQuantity);
        Assert.Equal(200, matchingEngine.Book.BidSide.First().Quantity);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrder_CancelOldest_WithOpenQuantity()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Equal(500, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(5000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 1, SelfMatchAction = SelfMatchAction.CancelOldest };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order3.OrderId, order1.OrderId, order1.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 5000, 5000, 10, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Equal(200, order3.OpenQuantity);
        Assert.Equal(200, matchingEngine.Book.BidSide.First().Quantity);
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrder_CancelNewest()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)0, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 1, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order3.OrderId, order1.OrderId, order1.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order3.OrderId, order3.UserId, 200, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
    }

    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrder_CancelNewest_WithOpenQuantity()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Equal(500, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(5000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 1, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order3.OrderId, order1.OrderId, order1.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order3.OrderId, order3.UserId, 200, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }
    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrder_Decrement()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)0, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 1, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order3.OrderId, order1.OrderId, order1.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order1.OrderId, order1.UserId, 200));
        mockTradeListener.Verify(x => x.OnCancel(order3.OrderId, order3.UserId, 200, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(3800, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
    }

    [Fact]
    public void CancelOrder_CancelCorrectAmount_IfIcebergOrder_CancelNewest_Decrement()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Equal(500, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(5000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
        OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(4500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
        Assert.Equal(0, order2.OpenQuantity);

        Order order3 = new Order { IsBuy = true, OpenQuantity = 4700, Price = 10, OrderId = 3, UserId = 1, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order3.OrderId, order1.OrderId, order1.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order1.OrderId, order1.UserId, 4700));
        mockTradeListener.Verify(x => x.OnCancel(order3.OrderId, order3.UserId, 4700, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
        Assert.Equal(300, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
        Assert.Equal(0, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().TotalQuantity);
        Assert.Equal(300, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_IcebergOrder_Matches_With_Same_User_CancelNewest()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 0, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order2.OrderId, order2.UserId, 5000, 0, 0, CancelReason.SelfMatch));
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
    public void AddOrder_IcebergOrder_Matches_With_Same_User_CancelNewest_WithOpenQuantity()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 200, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.CancelNewest };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnCancel(order2.OrderId, order2.UserId, 5200, 0, 0, CancelReason.SelfMatch));
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
    public void AddOrder_Iceberg_Matches_With_Same_User_CancelOldest()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 900, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.CancelOldest };
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
    public void AddOrder_Iceberg_Matches_With_Same_User_CancelOldest_WithPendingQuantity()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 900, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.CancelOldest };
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
    public void AddOrder_IcebergOrder_Matches_With_Same_User_Decrement()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 0, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order2.OrderId, order2.UserId, 1000));
        mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 1000, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(1000, order1.OpenQuantity);
        Assert.Contains(order2, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Equal(500, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Equal(3500, matchingEngine.Book.AskSide.First().First().TotalQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_IcebergOrder_Matches_With_Same_User_Decrement_WithOpenQuantity()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 200, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order2.OrderId, order2.UserId, 1000));
        mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 1000, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(200, order2.OpenQuantity);
        Assert.Contains(order2, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Equal(200, matchingEngine.Book.AskSide.First().Quantity);
        Assert.Equal(4000, matchingEngine.Book.AskSide.First().First().TotalQuantity);
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_Iceberg_Matches_With_Same_User_Decrement()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 900, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order1.OrderId, order1.UserId, 5900));
        mockTradeListener.Verify(x => x.OnCancel(order2.OrderId, order2.UserId, 5900, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(4100, order1.OpenQuantity);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Equal(4100, matchingEngine.Book.BidSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }

    [Fact]
    public void AddOrder_Iceberg_Matches_With_Same_User_Decrement_WithPendingQuantity()
    {
        Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 10000, Price = 10 };
        OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();

        Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 1, OpenQuantity = 0, Price = 10, TotalQuantity = 5000, TipQuantity = 500, SelfMatchAction = SelfMatchAction.Decrement };
        OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
        Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnSelfMatch(order2.OrderId, order1.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnDecrement(order1.OrderId, order1.UserId, 5000));
        mockTradeListener.Verify(x => x.OnCancel(order2.OrderId, order2.UserId, 5000, 0, 0, CancelReason.SelfMatch));
        mockTradeListener.VerifyNoOtherCalls();

        Assert.Equal(5000, order1.OpenQuantity);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Equal(5000, matchingEngine.Book.BidSide.First().Quantity);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
    }
}
