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
        public void AcceptedOrderTrackingDisable_ShouldNotTrack_AcceptedOrders()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.0000_0001m, 2);
            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, OpenQuantity = 0.5m, Price = 675800M };
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
            Assert.Contains(order2, matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
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
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x));
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