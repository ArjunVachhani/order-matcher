using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineBookOrCancelOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineBookOrCancelOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Rejects_BookOrCancel_If_Market()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.BookOrCancelCannotBeMarketOrStopOrder, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.DoesNotContain(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_Cancels_BookOrCancel_If_StopOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 1000, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel, StopPrice = 1000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.BookOrCancelCannotBeMarketOrStopOrder, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.DoesNotContain(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_Cancels_BookOrCancel_If_Matching_Sell_Available()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnCancel(2, 2, 500, 0, 0, CancelReason.BookOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_Not_Matching_Sell_Available()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 9, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_No_Sell_Available()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 9, OrderId = 1, UserId = 1, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result1 = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result1);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Cancels_BookOrCancel_If_Matching_Buy_Available()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnCancel(2, 2, 500, 0, 0, CancelReason.BookOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_Not_Matching_Buy_Available()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_No_Buy_Available()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 1, UserId = 1, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result1 = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result1);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);
        }

    }
}
