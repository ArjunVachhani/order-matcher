using Moq;
using OrderMatcher.Types;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineGoodTillDateOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineGoodTillDateOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Cancels_AlreadyPassedCancelledOn()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Cancels_AlreadyEqualCancelledOn()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 500, 0, 0, CancelReason.ValidityExpired));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Accepts_CancelledOnFuture()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 11 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);
        }

        [Fact]
        public void AddOrder_CancelsPendingOrderBeforeMatch()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 0);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 10);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);
        }
        
        [Fact]
        public void AddOrder_MatchesWithPendingOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 0);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 9);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(X => X.OnTrade(2, 1, 2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void AddOrder_CancelsMultiplePendingOrderBeforeMatch()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 0);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, CancelOn = 10 };
            OrderMatchingResult result = matchingEngine.AddOrder(order2, 0);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result);
            Assert.Contains(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, CancelOn = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 0);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 4, UserId = 4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 10);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.Verify(x => x.OnCancel(2, 2, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.Verify(x => x.OnCancel(3, 3, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.DoesNotContain(order2.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.DoesNotContain(order3.OrderId, matchingEngine.GoodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)4, order4.Sequence);
        }

        [Fact]
        public void HashSet_EnumeratorModifiedDuringIterating_DoesNotThrowException()
        {
            HashSet<int> hashset = new HashSet<int>();
            var count = 10000;
            for (int i = 0; i < count; i++)
                hashset.Add(i);

            foreach (var item in hashset)
            {
                hashset.Remove(item);
            }
            Assert.Empty(hashset);
        }

        [Fact]
        public void Dictionary_EnumeratorModifiedDuringIterating_DoesNotThrowException()
        {
            Dictionary<int, int> dictionary = new Dictionary<int, int>();
            var count = 10000;
            for (int i = 0; i < count; i++)
                dictionary.Add(i, i);

            foreach (var item in dictionary)
            {
                dictionary.Remove(item.Key);
            }
            Assert.Empty(dictionary);
        }
    }
}
