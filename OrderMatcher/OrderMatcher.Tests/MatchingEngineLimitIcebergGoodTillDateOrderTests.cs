using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineLimitIcebergGoodTillDateOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineLimitIcebergGoodTillDateOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Cancels_AlreadyPassedCancelledOn()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 1, UserId = 1, CancelOn = 1, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 5100, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(100, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Cancels_PendingGTDIcebergOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10, TotalQuantity = 5000, TipQuantity = 500 };
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 50, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 50, null, null, 500, 2.5m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(450, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
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
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 10);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 4950, 500, 1, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(200, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);
        }


        [Fact]
        public void AddOrder_Cancels_PendingStopGTDIcebergOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 100, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10, TotalQuantity = 5000, TipQuantity = 500, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.StopAskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.StopAskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(5000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            matchingEngine.CancelExpiredOrder(11);
            mockTradeListener.Verify(x => x.OnCancel(order1.OrderId, order1.UserId, 5100, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }
    }
}
