﻿namespace OrderMatcher.Tests;

public class MatchingEngineImmediateOrCancelOrderTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineImmediateOrCancelOrderTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void AddOrder_Cancels_ImmediateOrCancel_If_Pending_Limit_Buy()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 10, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.ImmediateOrCancel };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, null, null));
        mockTradeListener.Verify(x => x.OnCancel(2, 2, 1000, 5000, 25, CancelReason.ImmediateOrCancel));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(1000, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);
    }

    [Fact]
    public void AddOrder_Cancels_ImmediateOrCancel_If_Pending_Limit_Sell()
    {
        Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 10, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.ImmediateOrCancel };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, false, 10, 500, null, null, 5000, 10));
        mockTradeListener.Verify(x => x.OnCancel(2, 2, 1000, 5000, 25, CancelReason.ImmediateOrCancel));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(1000, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);
    }

    [Fact]
    public void AddOrder_Cancels_ImmediateOrCancel_If_No_Match_Limit_Buy()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 9, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.ImmediateOrCancel };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnCancel(2, 2, 1500, 0, 0, CancelReason.ImmediateOrCancel));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(1500, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);
    }

    [Fact]
    public void AddOrder_Cancels_ImmediateOrCancel_If_No_Match_Limit_Sell()
    {
        Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 11, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.ImmediateOrCancel };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnCancel(2, 2, 1500, 0, 0, CancelReason.ImmediateOrCancel));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(1500, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);
    }

    [Fact]
    public void AddOrder_Does_Not_Cancels_ImmediateOrCancel_If_Full_Match_Limit_Buy()
    {
        Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = true, OpenQuantity = 100, Price = 11, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.ImmediateOrCancel };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 100, null, null, 1000, 5));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);
    }

    [Fact]
    public void AddOrder_Does_Not_Cancels_ImmediateOrCancel_If_Full_Match_Limit_Sell()
    {
        Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(500, order1.OpenQuantity);
        Assert.Equal((ulong)1, order1.Sequence);

        Order order2 = new Order { IsBuy = false, OpenQuantity = 100, Price = 10, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.ImmediateOrCancel };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, false, 10, 100, 0, 5, null, null));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Single(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);
    }
}
