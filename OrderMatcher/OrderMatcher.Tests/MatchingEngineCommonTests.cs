using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineCommonTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;

        public MatchingEngineCommonTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void CancelOrder_Removes_Orders_If_Exists()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult result = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.CancelAcepted, result);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 1000, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_Removes_Orders_No_Cancel_Listener()
        {
            MatchingEngine matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult result = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.CancelAcepted, result);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 1000, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_If_Orders_Does_Not_Already_Cancelled_Call_OnCancel()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult cancelled = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.CancelAcepted, cancelled);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 1000, 0, 0, CancelReason.UserRequested));
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);

            OrderMatchingResult cancelled2 = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.OrderDoesNotExists, cancelled2);
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfQuantityIsNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = -1000, Price = 10, StopPrice = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfPriceIsNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = -10, StopPrice = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfStopPriceIsNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10, StopPrice = -1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Adds_Order_In_Accepted_Order_List()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();

            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Single(matchingEngine.CurrentOrders);
            Assert.Single(matchingEngine.Book.BidSide);
        }

        [Fact]
        public void AddOrder_Rejects_Duplicate_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);

            OrderMatchingResult accepted2 = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.DuplicateOrder, accepted2);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidPrice_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = -10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidQuantity_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = -1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidStopPrice_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, StopPrice = -10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsBookOrCancel_IfOrderIsPresentBuySideForSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnCancel(2, 2, 5000, 0, 0, CancelReason.BookOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2.OrderId, matchingEngine.CurrentOrders.Select(x => x.Key));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void MarketBuyOrder_MatchingMultipleSellOrder()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.0000_0001m, 2);
            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 0.5m, Price = 675800M };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((Quantity)0.5m, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);


            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 1.1m, Price = 680000M };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((Quantity)1.1m, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);


            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 0.6m, Price = 678800M };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((Quantity)0.6m, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequence);

            Order order4 = new Order { IsBuy = true, Price = 0, OrderId = 4, UserId = 4, OrderAmount = 700000m, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(order4.OrderId, order1.OrderId, order4.UserId, order1.UserId, 675800m, 0.5m, 0, 675.8m, null, null));
            mockTradeListener.Verify(x => x.OnTrade(order4.OrderId, order3.OrderId, order4.UserId, order3.UserId, 678800M, 0.53344136m, null, null, 700000, 3500));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.DoesNotContain(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
        }

        [Fact]
        public void AcceptedOrderTrackingDisable_ShouldNotTrack_AcceptedOrders()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.0000_0001m, 2);
            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 0.5m, Price = 675800M };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((Quantity)0.5m, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            matchingEngine.AcceptedOrderTrackingEnabled = false;
            Assert.False(matchingEngine.AcceptedOrderTrackingEnabled);
            Assert.Empty(matchingEngine.AcceptedOrders);

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 1.1m, Price = 680000M };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Empty(matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((Quantity)1.1m, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            matchingEngine.AcceptedOrderTrackingEnabled = true;
            Assert.True(matchingEngine.AcceptedOrderTrackingEnabled);
            Assert.Empty(matchingEngine.AcceptedOrders);

            Order order3 = new Order { IsBuy = false, OrderId = 3, UserId = 3, OpenQuantity = 0.6m, Price = 678800M };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((Quantity)0.6m, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequence);
        }
    }
}

//TODO test scenario for GetQuantity
//TODO complex scenario for good till date to test caching logic
//TODO test for remaining amount for market order
//TODO fill or kill and good till date pre calculation
//TODO test book stop loss
//TODO Stop market triggers another Stop orders