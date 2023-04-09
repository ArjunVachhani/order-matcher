using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineStopLimitIcebergOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineStopLimitIcebergOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_AddsToStopBook_IfIcebergOrderIsStopOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(1, 1));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Contains(order1, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);

            var cancelResult = matchingEngine.CancelOrder(1);
            Assert.Equal(OrderMatchingResult.CancelAcepted, cancelResult);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 1500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void StopLimitIcebergMatchAndCancel_Works_Fine()
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

            Order order3 = new Order { IsBuy = true, OpenQuantity = 0, Price = 12, OrderId = 3, UserId = 3, StopPrice = 11, TotalQuantity = 1200, TipQuantity = 500 };
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

            Order order4 = new Order { IsBuy = true, OpenQuantity = 0, Price = 12, OrderId = 4, UserId = 4, StopPrice = 11, TotalQuantity = 1200, TipQuantity = 500 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 12, OrderId = 6, UserId = 6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(order6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId, order6.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.Contains(order6, matchingEngine.CurrentOrders);
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order6.OpenQuantity);
            Assert.Equal((ulong)4, order6.Sequence);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 500, Price = 12, OrderId = 7, UserId = 7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(order7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId, order7.UserId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 7, 6, true, 12, 500, null, null, 6000, 30));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3, 3));
            mockTradeListener.Verify(x => x.OnOrderTriggered(4, 4));
            mockTradeListener.Verify(x => x.OnTrade(3, 6, 3, 6, true, 12, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 6, 3, 6, true, 12, 500, 0, 36, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order7, matchingEngine.CurrentOrders);
            Assert.DoesNotContain(order6, matchingEngine.CurrentOrders);
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order7, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.DoesNotContain(order6, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);

            OrderMatchingResult result8 = matchingEngine.CancelOrder(order4.OrderId);

            mockTradeListener.Verify(x => x.OnCancel(4, 4, 1200, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult result9 = matchingEngine.CancelOrder(order3.OrderId);

            mockTradeListener.Verify(x => x.OnCancel(3, 3, 200, 12000, 60, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void AddOrder_IcebergHasHigherPriotityThanStopOrder()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, StopPrice = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);


            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequence);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 0, Price = 10, OrderId = 4, UserId = 4, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 2, 4, 2, false, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 4, 3, false, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnOrderTriggered(1, 1));
            mockTradeListener.Verify(x => x.OnTrade(1, 4, 1, 4, true, 10, 500, 0, 60, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order4.Sequence);
        }
    }
}
