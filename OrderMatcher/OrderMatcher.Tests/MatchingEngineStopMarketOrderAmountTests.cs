﻿namespace OrderMatcher.Tests;

public class MatchingEngineStopMarketOrderAmountTests
{
    private readonly Mock<ITradeListener> mockTradeListener;
    private readonly Mock<IFeeProvider> mockFeeProcider;
    private MatchingEngine matchingEngine;
    public MatchingEngineStopMarketOrderAmountTests()
    {
        mockTradeListener = new Mock<ITradeListener>();
        mockFeeProcider = new Mock<IFeeProvider>();
        mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
        matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
    }

    [Fact]
    public void CancelOrder_Stop_Market_Order_Amount_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
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

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, UserId = 3, OrderAmount = 500, StopPrice = 11 };
        OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);

        OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
        mockTradeListener.Verify(x => x.OnCancel(3, 3, 0, 0, 0, CancelReason.UserRequested));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
        Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);
    }

    [Fact]
    public void AddOrder_Stop_Market_Order_Amount_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Buy()
    {
        Order order1 = new Order { IsBuy = true, Price = 0, OrderId = 1, UserId = 1, StopPrice = 9, OrderAmount = 1000 };
        OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

        mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
        Assert.Contains(order1, matchingEngine.CurrentOrders);
        Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal((ulong)1, order1.Sequence);
    }

    [Fact]
    public void AddOrder_Stop_Market_Order_Amount_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
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

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, UserId = 3, OrderAmount = 5000, StopPrice = 11 };
        OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order3.OpenQuantity);
        Assert.Equal((ulong)2, order3.Sequence);
    }

    [Fact]
    public void AddOrder_Stop_Market_Order_Amount_Trigger_And_Matches_As_Taker_Buy()
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

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, UserId = 3, StopPrice = 11, OrderAmount = 5500 };
        OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal((ulong)2, order3.Sequence);

        Order order4 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 4, UserId = 4 };
        OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

        mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(1000, order4.OpenQuantity);
        Assert.Equal((ulong)3, order4.Sequence);

        Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 5, UserId = 5 };
        OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

        mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
        mockTradeListener.Verify(x => x.OnTrade(5, 4, 5, 4, true, 11, 500, null, null, 5500, 27.5m));
        mockTradeListener.Verify(x => x.OnTrade(3, 4, 3, 4, true, 11, 500, 0, 22, 5500, 27.5m));
        mockTradeListener.Verify(x => x.OnOrderTriggered(3, 3));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
        Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
        Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order5.OpenQuantity);
        Assert.Equal((ulong)0, order5.Sequence);
        Assert.Equal((ulong)2, order3.Sequence);
    }

    [Fact]
    public void AddOrder_Stop_Market_Order_Amount_Trigger_And_Matches_As_Taker_Buy2()
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

        Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
        OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

        mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
        mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, 5000, 25));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
        Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
        Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order2.OpenQuantity);
        Assert.Equal((ulong)0, order2.Sequence);

        Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, UserId = 3, StopPrice = 11, OrderAmount = 4400 };
        OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

        mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
        Assert.Contains(order3, matchingEngine.CurrentOrders);
        Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Empty(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal((ulong)2, order3.Sequence);

        Order order4 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 4, UserId = 4 };
        OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

        mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
        Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Single(matchingEngine.Book.StopBidSide);
        Assert.Equal(1000, order4.OpenQuantity);
        Assert.Equal((ulong)3, order4.Sequence);

        Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 5, UserId = 5 };
        OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

        mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
        mockTradeListener.Verify(x => x.OnTrade(5, 4, 5, 4, true, 11, 500, null, null, 5500, 27.5m));
        mockTradeListener.Verify(x => x.OnTrade(3, 4, 3, 4, true, 11, 400, null, null, 4400, 22));
        mockTradeListener.Verify(x => x.OnOrderTriggered(3, 3));
        mockTradeListener.VerifyNoOtherCalls();
        Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
        Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
        Assert.Contains(order4, matchingEngine.CurrentOrders);
        Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
        Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x));
        Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x));
        Assert.Empty(matchingEngine.Book.BidSide);
        Assert.Single(matchingEngine.Book.AskSide);
        Assert.Empty(matchingEngine.Book.StopAskSide);
        Assert.Empty(matchingEngine.Book.StopBidSide);
        Assert.Equal(0, order5.OpenQuantity);
        Assert.Equal((ulong)0, order5.Sequence);
        Assert.Equal((ulong)2, order3.Sequence);
    }
}
