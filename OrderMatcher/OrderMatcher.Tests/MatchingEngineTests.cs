using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;

        public MatchingEngineTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void CancelOrder_Removes_Orders_If_Exists()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult result = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.CancelAcepted, result);
            mockTradeListener.Verify(x => x.OnCancel(1, 1000, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_Removes_Orders_No_Cancel_Listener()
        {
            MatchingEngine matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult result = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.CancelAcepted, result);
            mockTradeListener.Verify(x => x.OnCancel(1, 1000, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_If_Orders_Does_Not_Already_Cancelled_Call_OnCancel()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();

            OrderMatchingResult cancelled = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.CancelAcepted, cancelled);
            mockTradeListener.Verify(x => x.OnCancel(1, 1000, 0, 0, CancelReason.UserRequested));
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);

            OrderMatchingResult cancelled2 = matchingEngine.CancelOrder(order1.OrderId);
            Assert.Equal(OrderMatchingResult.OrderDoesNotExists, cancelled2);
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_Normal_Market_After_Match_Enters_Pending_With_Limit_Should_Be_Cancel_Price_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 1000, null, null, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(10, matchingEngine.Book.AskSide.First().Key);
            Assert.Equal(10, order2.Price);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            OrderMatchingResult result3 = matchingEngine.CancelOrder(2);

            mockTradeListener.Verify(x => x.OnCancel(2, 500, 10000, 50, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result3);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Equal(10, order2.Price);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void CancelOrder_Normal_Market_After_Match_Enters_Pending_With_Limit_Should_Be_Cancel_Price_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 1000, 0, 20, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Equal(10, matchingEngine.Book.BidSide.First().Key);
            Assert.Equal(10, order2.Price);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            OrderMatchingResult result3 = matchingEngine.CancelOrder(2);

            mockTradeListener.Verify(x => x.OnCancel(2, 500, 10000, 50, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result3);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Equal(10, order2.Price);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void CancelOrder_Stop_Limit_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void CancelOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void CancelOrder_Stop_Limit_Order_Entered_In_Stop_Book_With_Market_Rate_For_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void CancelOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void CancelOrder_Cancels_Triggered_Stop_Limit_Order_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, IsStop = true };
            var orderWrapper4 = new OrderWrapper { Order = order4, StopPrice = 11 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 5, IsStop = true };
            var orderWrapper5 = new OrderWrapper { Order = order5, StopPrice = 13 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.Contains(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order5, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(2, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 500, Price = 12, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.Contains(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(2, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal((ulong)5, order6.Sequnce);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 500, Price = 12, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 12, 500, 0, 12, 6000, 30));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.Verify(x => x.OnOrderTriggered(4));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order7.OpenQuantity);
            Assert.Equal((ulong)0, order7.Sequnce);
            Assert.Equal((ulong)6, order3.Sequnce);
            Assert.Equal((ulong)7, order4.Sequnce);

            OrderMatchingResult result8 = matchingEngine.CancelOrder(order4.OrderId);

            mockTradeListener.Verify(x => x.OnCancel(4, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.DoesNotContain(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order7.OpenQuantity);
            Assert.Equal((ulong)0, order7.Sequnce);
            Assert.Equal((ulong)6, order3.Sequnce);
            Assert.Equal((ulong)7, order4.Sequnce);
        }

        [Fact]
        public void TriggerWorks_IfNoTriggerListerPassed()
        {
            var matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 0);
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, IsStop = true };
            var orderWrapper4 = new OrderWrapper { Order = order4, StopPrice = 9 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 5, IsStop = true };
            var orderWrapper5 = new OrderWrapper { Order = order5, StopPrice = 7 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.Contains(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order5, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Equal(2, matchingEngine.Book.StopAskSide.Count());
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 500, Price = 8, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.Contains(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(2, matchingEngine.Book.StopAskSide.Count());
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal((ulong)5, order6.Sequnce);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 500, Price = 8, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 8, 500, 0, 8, 4000, 20));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.Verify(x => x.OnOrderTriggered(4));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(10, matchingEngine.Book.AskSide.First().Key);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Equal(7, matchingEngine.Book.StopAskSide.First().Key);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order7.OpenQuantity);
            Assert.Equal((ulong)0, order7.Sequnce);
            Assert.Equal((ulong)6, order3.Sequnce);
            Assert.Equal((ulong)7, order4.Sequnce);

            OrderMatchingResult result8 = matchingEngine.CancelOrder(order4.OrderId);

            mockTradeListener.Verify(x => x.OnCancel(4, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result8);
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(10, matchingEngine.Book.AskSide.First().Key);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Equal(7, matchingEngine.Book.StopAskSide.First().Key);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)6, order3.Sequnce);
            Assert.Equal((ulong)7, order4.Sequnce);
        }

        [Fact]
        public void CancelOrder_Cancels_Triggered_Stop_Market_Order_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 600, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 600, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 100, 0, 12, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal(10, order3.Price);
            Assert.Equal((ulong)2, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(3);
            mockTradeListener.Verify(x => x.OnCancel(3, 500, 1000, 5, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal(10, order3.Price);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void CancelOrder_Cancels_Triggered_Stop_Market_Order_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 600, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(100, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 600, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 11, 100, null, null, 6100, 27.2m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal(11, order3.Price);
            Assert.Equal((ulong)3, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(3);

            mockTradeListener.Verify(x => x.OnCancel(3, 500, 1100, 5.5m, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal(11, order3.Price);
            Assert.Equal((ulong)3, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_ResetsOpenQuantityAndIsTip()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.False(order1.IsTip);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfPriceQuantityStopPriceNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = -1000, Price = -10, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = -1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfQuantityIsNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = -1000, Price = 10, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfPriceIsNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = -10, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfStopPriceIsNegative()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = -1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Adds_Order_In_Accepted_Order_List()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();

            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Single(matchingEngine.CurrentOrders);
            Assert.Single(matchingEngine.Book.BidSide);
        }

        [Fact]
        public void AddOrder_Rejects_Duplicate_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);

            OrderMatchingResult accepted2 = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.DuplicateOrder, accepted2);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidPrice_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = -10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidQuantity_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = -1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidOrderAmount_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderAmount = -1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidStopPrice_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = -10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidTotalQuantityIceberg_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = -10, StopPrice = 10, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_TotalQuantityNotMultipleOfStepSize_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.5m, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000.1m, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_QuantityNotMultipleOfStepSize_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.5m, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 10000.1m, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_TotalQuantityNotMultipleOfStepSize2_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 3, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_QuantityNotMultipleOfStepSize2_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 3, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 10000, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Accepts_TotalQuantityMultipleOfStepSize_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Accepts_QuantityMultipleOfStepSize_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 10000, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Normal_Limit_Matches_With_Pending_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();

            Order order2 = new Order { IsBuy = false, OrderId = 2, OpenQuantity = 1000, Price = 10 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult accepted2 = matchingEngine.AddOrder(orderWrapper2, 2);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 1000, 0, 50, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();

            Assert.Equal(0, order1.OpenQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Normal_Limit_Matches_With_Pending_Order_Works_If_No_TradeListener_Passed()
        {
            matchingEngine = new MatchingEngine(null, mockFeeProcider.Object, 1, 0);

            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            Order order2 = new Order { IsBuy = false, OrderId = 2, OpenQuantity = 1000, Price = 10 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult accepted2 = matchingEngine.AddOrder(orderWrapper2, 2);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted2);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Equal(0, order1.OpenQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Normal_Limit_Deos_Not_Matches_With_Pending_Order_If_Limit_Not_Satisfy()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            Order order2 = new Order { IsBuy = false, OrderId = 2, OpenQuantity = 1000, Price = 11 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult accepted2 = matchingEngine.AddOrder(orderWrapper2, 2);
            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted2);

            mockTradeListener.VerifyNoOtherCalls();

            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal(2, matchingEngine.CurrentOrders.Count());
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Normal_Limit_Complex_Matching_Scenario()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal(500, order1.OpenQuantity);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.Contains(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);

            Order order6 = new Order { IsBuy = true, OpenQuantity = 500, Price = 12, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(6, 2, 11, 500, null, null, 5500, 27.5m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.DoesNotContain(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order6, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order6.OpenQuantity);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order6.Sequnce);

            Order order7 = new Order { IsBuy = false, OpenQuantity = 500, Price = 12, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.Contains(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order7, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order7.OpenQuantity);
            Assert.Equal((ulong)5, order7.Sequnce);

            Order order8 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 13, OrderId = 8 };
            var orderWrapper8 = new OrderWrapper { Order = order8 };
            OrderMatchingResult result8 = matchingEngine.AddOrder(orderWrapper8, 8);

            mockTradeListener.Verify(x => x.OnAccept(order8.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result8);
            Assert.Contains(order8, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order8.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order8, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Equal(3, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order8.OpenQuantity);
            Assert.Equal((ulong)6, order8.Sequnce);

            Order order9 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 14, OrderId = 9 };
            var orderWrapper9 = new OrderWrapper { Order = order9 };
            OrderMatchingResult result9 = matchingEngine.AddOrder(orderWrapper9, 9);

            mockTradeListener.Verify(x => x.OnAccept(order9.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result9);
            Assert.Contains(order9, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order9.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order9, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Equal(4, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order9.OpenQuantity);
            Assert.Equal((ulong)7, order9.Sequnce);

            Order order10 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 14, OrderId = 10 };
            var orderWrapper10 = new OrderWrapper { Order = order10 };
            OrderMatchingResult result10 = matchingEngine.AddOrder(orderWrapper10, 10);

            mockTradeListener.Verify(x => x.OnAccept(order10.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result10);
            Assert.Contains(order10, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order10.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order10, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Equal(4, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order10.OpenQuantity);
            Assert.Equal((ulong)8, order10.Sequnce);

            Order order11 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 15, OrderId = 11 };
            var orderWrapper11 = new OrderWrapper { Order = order11 };
            OrderMatchingResult result11 = matchingEngine.AddOrder(orderWrapper11, 11);

            mockTradeListener.Verify(x => x.OnAccept(order11.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result11);
            Assert.Contains(order11, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order11.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order11, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Equal(5, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order11.OpenQuantity);
            Assert.Equal((ulong)9, order11.Sequnce);

            Order order12 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 12 };
            var orderWrapper12 = new OrderWrapper { Order = order12 };
            OrderMatchingResult result12 = matchingEngine.AddOrder(orderWrapper12, 12);

            mockTradeListener.Verify(x => x.OnAccept(order12.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result12);
            Assert.Contains(order12, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order12.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order12, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Equal(5, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order12.OpenQuantity);
            Assert.Equal((ulong)10, order12.Sequnce);

            Order order13 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 8, OrderId = 13 };
            var orderWrapper13 = new OrderWrapper { Order = order13 };
            OrderMatchingResult result13 = matchingEngine.AddOrder(orderWrapper13, 13);

            mockTradeListener.Verify(x => x.OnAccept(order13.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result13);
            Assert.Contains(order13, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order13.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order13, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(3, matchingEngine.Book.BidSide.Count());
            Assert.Equal(5, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order13.OpenQuantity);
            Assert.Equal((ulong)11, order13.Sequnce);

            Order order14 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 7, OrderId = 14 };
            var orderWrapper14 = new OrderWrapper { Order = order14 };
            OrderMatchingResult result14 = matchingEngine.AddOrder(orderWrapper14, 14);

            mockTradeListener.Verify(x => x.OnAccept(order14.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result14);
            Assert.Contains(order14, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order14.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order14, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(4, matchingEngine.Book.BidSide.Count());
            Assert.Equal(5, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order14.OpenQuantity);
            Assert.Equal((ulong)12, order14.Sequnce);

            Order order15 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 7, OrderId = 15 };
            var orderWrapper15 = new OrderWrapper { Order = order15 };
            OrderMatchingResult result15 = matchingEngine.AddOrder(orderWrapper15, 15);

            mockTradeListener.Verify(x => x.OnAccept(order15.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result15);
            Assert.Contains(order15, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order15.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order15, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(4, matchingEngine.Book.BidSide.Count());
            Assert.Equal(5, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order15.OpenQuantity);
            Assert.Equal((ulong)13, order15.Sequnce);

            OrderMatchingResult result16 = matchingEngine.CancelOrder(2);

            Assert.Equal(OrderMatchingResult.CancelAcepted, result16);
            mockTradeListener.Verify(x => x.OnCancel(2, 500, 5500, 11, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(4, matchingEngine.Book.BidSide.Count());
            Assert.Equal(4, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order17 = new Order { IsBuy = true, OpenQuantity = 5000, Price = 16, OrderId = 17 };
            var orderWrapper17 = new OrderWrapper { Order = order17 };
            OrderMatchingResult result17 = matchingEngine.AddOrder(orderWrapper17, 17);

            mockTradeListener.Verify(x => x.OnAccept(order17.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(17, 7, 12, 500, 0, 12, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 8, 13, 1000, 0, 26, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 9, 14, 1000, 0, 28, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 10, 14, 1000, 0, 28, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 11, 15, 1000, 0, 30, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result17);
            Assert.Contains(order17, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order17.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order17, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(5, matchingEngine.Book.BidSide.Count());
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order17.OpenQuantity);
            Assert.Equal((ulong)14, order17.Sequnce);

            Order order18 = new Order { IsBuy = false, OpenQuantity = 4000, Price = 7, OrderId = 18 };
            var orderWrapper18 = new OrderWrapper { Order = order18 };
            OrderMatchingResult result18 = matchingEngine.AddOrder(orderWrapper18, 18);

            mockTradeListener.Verify(x => x.OnAccept(order18.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(18, 17, 16, 500, null, null, 70000, 326));
            mockTradeListener.Verify(x => x.OnTrade(18, 1, 10, 500, null, null, 10000, 20));
            mockTradeListener.Verify(x => x.OnTrade(18, 5, 10, 1000, null, null, 10000, 20));
            mockTradeListener.Verify(x => x.OnTrade(18, 3, 9, 1000, null, null, 9000, 18));
            mockTradeListener.Verify(x => x.OnTrade(18, 12, 9, 1000, 0, 205, 9000, 18));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result18);
            Assert.DoesNotContain(order18, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order18.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order18, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order18.OpenQuantity);
            Assert.Equal((ulong)0, order18.Sequnce);

            Order order19 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 7, OrderId = 19 };
            var orderWrapper19 = new OrderWrapper { Order = order19 };
            OrderMatchingResult result19 = matchingEngine.AddOrder(orderWrapper19, 19);

            mockTradeListener.Verify(x => x.OnAccept(order19.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(19, 13, 8, 1000, null, null, 8000, 16));
            mockTradeListener.Verify(x => x.OnTrade(19, 14, 7, 1000, null, null, 7000, 14));
            mockTradeListener.Verify(x => x.OnTrade(19, 15, 7, 1000, 0, 110, 7000, 14));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result19);
            Assert.DoesNotContain(order19, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order19.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order19, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order19.OpenQuantity);
            Assert.Equal((ulong)0, order19.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Matches_Order_With_Open_Orders_For_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Matches_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Multiple_Matches_Order_With_Open_Orders_For_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, 0, 25, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Multiple_Matches_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, 0, 20, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Matches_Multiple_Order_With_Open_Orders_For_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 9, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 0, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 9, 500, 0, 47.5m, 4500, 9));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Matches_Multiple_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 0, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 11, 500, 0, 11, 10500, 52.5m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Cancel_Order_If_Pending_Buy_And_No_Sell_And_No_Match()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult result1 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 500, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result1);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Cancel_Order_If_Pending_Sell_And_No_Buy_And_No_Match()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult result1 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 500, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result1);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_After_Match_Enters_Pending_With_Limit_Price_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 1000, null, null, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(10, matchingEngine.Book.AskSide.First().Key);
            Assert.Equal(10, order2.Price);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_After_Match_Enters_Pending_With_Limit_Price_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 1000, 0, 20, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Equal(10, matchingEngine.Book.BidSide.First().Key);
            Assert.Equal(10, order2.Price);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Complex_Matching_Scenario()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal(500, order1.OpenQuantity);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            mockTradeListener.Verify(x => x.OnTrade(5, 1, 10, 500, 0, 25, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            mockTradeListener.Verify(x => x.OnTrade(6, 3, 9, 1000, null, null, 9000, 18));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Contains(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal(9, order6.Price);
            Assert.Equal((ulong)4, order6.Sequnce);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 2000, Price = 12, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 9, 500, 0, 54, null, null));
            mockTradeListener.Verify(x => x.OnTrade(7, 2, 11, 1000, 0, 22, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.Contains(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order7.OpenQuantity);
            Assert.Equal((ulong)5, order7.Sequnce);

            Order order8 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 12, OrderId = 8 };
            var orderWrapper8 = new OrderWrapper { Order = order8 };
            OrderMatchingResult result8 = matchingEngine.AddOrder(orderWrapper8, 8);

            mockTradeListener.Verify(x => x.OnAccept(order8.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result8);
            Assert.Contains(order8, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order8.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order8, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order8.OpenQuantity);
            Assert.Equal((ulong)6, order8.Sequnce);

            Order order9 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 13, OrderId = 9 };
            var orderWrapper9 = new OrderWrapper { Order = order9 };
            OrderMatchingResult result9 = matchingEngine.AddOrder(orderWrapper9, 9);

            mockTradeListener.Verify(x => x.OnAccept(order9.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result9);
            Assert.Contains(order9, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order9.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order9, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order9.OpenQuantity);
            Assert.Equal((ulong)7, order9.Sequnce);

            Order order10 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 0, OrderId = 10 };
            var orderWrapper10 = new OrderWrapper { Order = order10 };
            OrderMatchingResult result10 = matchingEngine.AddOrder(orderWrapper10, 10);

            mockTradeListener.Verify(x => x.OnAccept(order10.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result10);
            mockTradeListener.Verify(x => x.OnTrade(10, 9, 13, 1000, null, null, 13000, 26));
            mockTradeListener.Verify(x => x.OnTrade(10, 7, 12, 500, null, null, 21500, 89.5m));
            mockTradeListener.Verify(x => x.OnTrade(10, 8, 12, 1500, 0, 185, 18000, 36));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order10, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order10.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order10, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order10.OpenQuantity);
            Assert.Equal((ulong)0, order10.Sequnce);

            Order order11 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 0, OrderId = 11 };
            var orderWrapper11 = new OrderWrapper { Order = order11 };
            OrderMatchingResult result11 = matchingEngine.AddOrder(orderWrapper11, 11);

            mockTradeListener.Verify(x => x.OnAccept(order11.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result11);
            mockTradeListener.Verify(x => x.OnCancel(11, 3000, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order11, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order11.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order11, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(3000, order11.OpenQuantity);
            Assert.Equal((ulong)0, order11.Sequnce);

            Order order12 = new Order { IsBuy = true, OpenQuantity = 3000, Price = 0, OrderId = 12 };
            var orderWrapper12 = new OrderWrapper { Order = order12 };
            OrderMatchingResult result12 = matchingEngine.AddOrder(orderWrapper12, 12);

            mockTradeListener.Verify(x => x.OnAccept(order12.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result12);
            mockTradeListener.Verify(x => x.OnCancel(12, 3000, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order12, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order12.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order12, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(3000, order12.OpenQuantity);
            Assert.Equal((ulong)0, order12.Sequnce);

            Order order13 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 13 };
            var orderWrapper13 = new OrderWrapper { Order = order13 };
            OrderMatchingResult result13 = matchingEngine.AddOrder(orderWrapper13, 13);

            mockTradeListener.Verify(x => x.OnAccept(order13.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result13);
            Assert.Contains(order13, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order13.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order13, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order13.OpenQuantity);
            Assert.Equal((ulong)8, order13.Sequnce);

            Order order14 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 14 };
            var orderWrapper14 = new OrderWrapper { Order = order14 };
            OrderMatchingResult result14 = matchingEngine.AddOrder(orderWrapper14, 14);

            mockTradeListener.Verify(x => x.OnAccept(order14.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result14);
            Assert.Contains(order14, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order14.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order14, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order14.OpenQuantity);
            Assert.Equal((ulong)9, order14.Sequnce);

            Order order15 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 15 };
            var orderWrapper15 = new OrderWrapper { Order = order15 };
            OrderMatchingResult result15 = matchingEngine.AddOrder(orderWrapper15, 15);

            mockTradeListener.Verify(x => x.OnAccept(order15.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result15);
            Assert.Contains(order15, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order15.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order15, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order15.OpenQuantity);
            Assert.Equal((ulong)10, order15.Sequnce);

            Order order16 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 16 };
            var orderWrapper16 = new OrderWrapper { Order = order16 };
            OrderMatchingResult result16 = matchingEngine.AddOrder(orderWrapper16, 16);

            mockTradeListener.Verify(x => x.OnAccept(order16.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result16);
            mockTradeListener.Verify(x => x.OnTrade(16, 13, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order16, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order16.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order16, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order16.OpenQuantity);
            Assert.Equal(500, order13.OpenQuantity);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Buy()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Buy()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 0, OrderId = 1, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 0, OrderId = 1, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Order_Entered_In_Stop_Book_With_Market_Rate_For_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Triggers_Stop_Limit_Order_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, IsStop = true };
            var orderWrapper4 = new OrderWrapper { Order = order4, StopPrice = 11 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 5, IsStop = true };
            var orderWrapper5 = new OrderWrapper { Order = order5, StopPrice = 13 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.Contains(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order5, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(2, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 500, Price = 12, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.Contains(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(2, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal((ulong)5, order6.Sequnce);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 500, Price = 12, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 12, 500, 0, 12, 6000, 30));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.Verify(x => x.OnOrderTriggered(4));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order7.OpenQuantity);
            Assert.Equal((ulong)0, order7.Sequnce);
            Assert.Equal((ulong)6, order3.Sequnce);
            Assert.Equal((ulong)7, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_Triggers_Stop_Limit_Order_Ask()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, IsStop = true };
            var orderWrapper4 = new OrderWrapper { Order = order4, StopPrice = 9 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 5, IsStop = true };
            var orderWrapper5 = new OrderWrapper { Order = order5, StopPrice = 7 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.Contains(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order5, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Equal(2, matchingEngine.Book.StopAskSide.Count());
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 500, Price = 8, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.Contains(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(2, matchingEngine.Book.StopAskSide.Count());
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal((ulong)5, order6.Sequnce);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 500, Price = 8, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 8, 500, 0, 8, 4000, 20));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.Verify(x => x.OnOrderTriggered(4));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.DoesNotContain(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Equal(10, matchingEngine.Book.AskSide.First().Key);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Equal(7, matchingEngine.Book.StopAskSide.First().Key);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order7.OpenQuantity);
            Assert.Equal((ulong)0, order7.Sequnce);
            Assert.Equal((ulong)6, order3.Sequnce);
            Assert.Equal((ulong)7, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Order_Added_To_Book_If_Stop_Price_Is_Less_Than_Market_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Order_Added_To_Book_If_Stop_Price_Is_More_Than_Market_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Added_To_Book_If_Stop_Price_Is_Less_Than_Market_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 600, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 600, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 100, 0, 12, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal(10, order3.Price);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Added_To_Book_If_Stop_Price_Is_More_Than_Market_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 600, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(100, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 600, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 11, 100, null, null, 6100, 27.2m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal(11, order3.Price);
            Assert.Equal((ulong)3, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Trigger_And_Matches_As_Taker_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 11, 500, null, null, 5500, 27.5m));
            mockTradeListener.Verify(x => x.OnTrade(3, 4, 11, 500, 0, 22, 5500, 27.5m));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Trigger_And_Matches_As_Taker_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 11, 500, null, null, 5500, 27.5m));
            mockTradeListener.Verify(x => x.OnTrade(3, 4, 11, 500, 0, 22, 5500, 27.5m));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Limit_Trigger_And_Matches_As_Taker_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 7, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 8, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 8, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 8, 500, 0, 8, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 5, 8, 500, 0, 20, 8000, 28));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Trigger_And_Matches_As_Taker_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 8, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 8, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 8, 500, 0, 8, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 5, 8, 500, 0, 20, 8000, 28));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_Rejects_BookOrCancel_If_Market()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

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
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_BookOrCancel_If_StopOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 1000, OrderId = 2, IsStop = true };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel, StopPrice = 1000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

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
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_BookOrCancel_If_Matching_Sell_Available()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(2, 500, 0, 0, CancelReason.BookOrCancel));
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
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_Not_Matching_Sell_Available()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 9, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
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
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_No_Sell_Available()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 9, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result1 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_BookOrCancel_If_Matching_Buy_Available()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(2, 500, 0, 0, CancelReason.BookOrCancel));
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
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_Not_Matching_Buy_Available()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
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
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_BookOrCancel_If_No_Buy_Available()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderCondition = OrderCondition.BookOrCancel };
            OrderMatchingResult result1 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_StopOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 1000, OrderId = 2, IsStop = true };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel, StopPrice = 1000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.ImmediateOrCancelCannotBeStopOrder, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.DoesNotContain(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_Pending_Limit_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.Verify(x => x.OnCancel(2, 1000, 5000, 25, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_Pending_Limit_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnCancel(2, 1000, 5000, 25, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_No_Match_Limit_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 9, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(2, 1500, 0, 0, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_No_Match_Limit_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(2, 1500, 0, 0, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_ImmediateOrCancel_If_Full_Match_Limit_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 100, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_ImmediateOrCancel_If_Full_Match_Limit_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 100, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 100, 0, 5, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_Pending_MarketOrder_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.Verify(x => x.OnCancel(2, 1000, 5000, 25, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_Pending_MarketOrder_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnCancel(2, 1000, 5000, 25, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_Full_MarketOrder_Buy()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 0, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result1 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1500, 0, 0, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result1);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_ImmediateOrCancel_If_Full_MarketOrder_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result1 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1500, 0, 0, CancelReason.ImmediateOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result1);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_ImmediateOrCancel_Full_MarketOrder_Matches_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 100, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Does_Not_Cancels_ImmediateOrCancel_If_Full_MarketOrder_Matches_Sell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 100, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 100, 0, 5, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Rejects_FillOrKill_Order_With_Stop_Price()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, OpenQuantity = 1000, Price = 10, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderCondition = OrderCondition.FillOrKill, StopPrice = 9 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.FillOrKillCannotBeStopOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_CancelsFullQuantityLimitFOKOrder_IfNotEnoughSellAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 600, Price = 10, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(5, 600, 0, 0, CancelReason.FillOrKill));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_CancelsFullQuantityFOKMarketOrder_IfNotEnoughSellAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 600, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(5, 600, 0, 0, CancelReason.FillOrKill));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_FillsFullQuantityFOKLimitOrder_IfNotEnoughSellAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 400, Price = 10, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 10, 400, null, null, 4000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_FillsFullQuantityFOKMarketOrder_IfNotEnoughSellAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 400, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 10, 400, null, null, 4000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_CancelsFullQuantityLimitFOKOrder_IfNotEnoughBuyAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 600, Price = 10, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(5, 600, 0, 0, CancelReason.FillOrKill));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_CancelsFullQuantityFOKMarketOrder_IfNotEnoughBuyAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 600, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(5, 600, 0, 0, CancelReason.FillOrKill));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_FillsFullQuantityFOKLimitOrder_IfNotEnoughBuyAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 400, Price = 10, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 10, 400, 0, 20, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_FillsFullQuantityFOKMarketOrder_IfNotEnoughBuyAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 400, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 10, 400, 0, 20, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void CancelOrder_CancelsFullOrder_IfNoFillIcebergOrderBuySide()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TipQuantity = 500, TotalQuantity = 5000 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            OrderMatchingResult acceptanceResult2 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(1, 5000, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult2);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void CancelOrder_CancelCorrectAmount_IfIcebergOrderPartiallyFilledBuy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TipQuantity = 500, TotalQuantity = 5000 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 200, null, null, 2000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(300, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            OrderMatchingResult acceptanceResult4 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(1, 4300, 7000, 14, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult4);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void CancelOrder_CancelsCorrectAmount_IfLastTipIsPartiallyRemainingBuy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TipQuantity = 500, TotalQuantity = 1200 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(700, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(200, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(200, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequnce);

            OrderMatchingResult acceptanceResult5 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(1, 100, 11000, 22, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult5);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void CancelOrder_RejectsRequest_IfIcebergOrderMatchedFully()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 500, 0, 30, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequnce);

            OrderMatchingResult acceptanceResult5 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderDoesNotExists, acceptanceResult5);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergOrderIsAddedSellSide()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsTip_IfTipIsMatchedFullyBuySide()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_AddedTipWithRemainQuantiy_IfLastTipIsRemaining()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1200, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(700, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(200, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(200, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_AddedTipWithRemainQuantiy2_IfLastTipIsRemaining()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(400, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_AddedTipNoTip_IfLastTipIsMatcheFully()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 500, 0, 30, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_AddedAllTip_IfMatchedWithHugeOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 5000, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 100, 50000, 250));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Empty(matchingEngine.CurrentIcebergOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_SetsTotalQuantityToOpenQuantity_ForIcebergOrders()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfIcebergOrderIsIOC()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeFOKorIOC, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfIcebergOrderIsFOK()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderCondition = OrderCondition.FillOrKill, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeFOKorIOC, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfIcebergOrderIsStopOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfIcebergOrderIsMarketOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsOrder_QuantityMoreThanTotalQuantity()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 409, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.InvalidIcebergOrderTotalQuantity, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsOrder_QuantityEqualToTotalQuantity()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.InvalidIcebergOrderTotalQuantity, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_RejectsBookOrCancel_IfOrderIsPresentBuySideForSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderCondition = OrderCondition.BookOrCancel, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(2, 5000, 0, 0, CancelReason.BookOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergeOrderTipIsMatchedSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order2.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 2).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order2.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsMoreTip_IfIcebergeOrderTipIsMatchedSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 400, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(400, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 10, 400, null, null, 4000, 8));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order3.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 3).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order3.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsAllTip_IfIcebergeOrderTipIsMatchedSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 400, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(400, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 600, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 2, 11, 400, null, null, 4400, 8.8m));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 100, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 400, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 10, 100, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 10, 500, 0, 77, 6000, 12));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order4.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsAllTip_IfIcebergeOrderTipIsMatchedWithSingleHugeOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 5000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(5000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 250, 50000, 100));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Empty(matchingEngine.CurrentIcebergOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void CancelOrder_CancelsProperAmount_IfIcebergeFewOrderTipIsMatchedAddOrder()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 400, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(400, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 10, 400, null, null, 4000, 8));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order3.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 3).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order3.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);

            OrderMatchingResult acceptanceResult4 = matchingEngine.CancelOrder(order3.OrderId);

            mockTradeListener.Verify(x => x.OnCancel(order3.OrderId, 600, 9000, 45, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Empty(matchingEngine.CurrentIcebergOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void AddOrder_CancelsIceberg_IfIcebergeOrderTipIsMatchedWithSingleHugeOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, TotalQuantity = 5000, OrderCondition = OrderCondition.BookOrCancel, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(2, 5000, 0, 0, CancelReason.BookOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Empty(matchingEngine.CurrentIcebergOrders);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergeOrderTipIsMatchedWithMultipleOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 10, 500, null, null, 5000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order3.OrderId, matchingEngine.CurrentOrders.Select(x => x.Value.OrderId));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(3500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order3.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergeOrderTipIsMatchedWithFullyOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, TotalQuantity = 1000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 10, 500, 0, 50, 5000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Empty(matchingEngine.CurrentIcebergOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_IcebergHasHigherPriotityThanStopOrder()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);


            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(4, 2, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnOrderTriggered(1));
            mockTradeListener.Verify(x => x.OnTrade(1, 4, 10, 500, 0, 60, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Empty(matchingEngine.CurrentIcebergOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order4.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_AlreadyPassedCancelledOn()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, CancelOn = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Accepts_CancelledOnFuture()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, CancelOn = 11 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_AlreadyEqualCancelledOn()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, CancelOn = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 10);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_CancelsPendingOrderBeforeMatch()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, CancelOn = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 0);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 10);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_CancelsPendingStopOrderBeforeMatch()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, IsStop = true, CancelOn = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 0);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 10);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 500, 0, 0, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Cancels_PendingIcebergOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, CancelOn = 10 };
            var orderWrapper1 = new OrderWrapper { Order = order1, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4500, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentIcebergOrders.Single(x => x.Key == order1.OrderId).Value.TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 10);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 4500, 5000, 10, CancelReason.ValidityExpired));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x.Value).Select(x => x.OrderId).ToList());
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentIcebergOrders.Select(x => x.Key));
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(200, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForMarketSell()
        {
            Order order1 = new Order { IsBuy = false, OrderId = 1, Price = 0 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForLimitBuy()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, Price = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(orderWrapper1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Cancels_MarketOrderAmountIfNoSellAvailable()
        {
            Order order1 = new Order { IsBuy = true, Price = 0, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderAmount = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(1, 0, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Matches_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderAmount = 5000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Multiple_Matches_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, Price = 0, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2, OrderAmount = 5000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, OrderAmount = 5000 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, 0, 20, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Matches_MarketOrderNoLiquidity()
        {
            Order order1 = new Order { IsBuy = true, Price = 0, OrderId = 3 };
            var orderWrapper1 = new OrderWrapper { Order = order1, OrderAmount = 10000 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(3, 0, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Matches_Multiple_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3, OrderAmount = 10000 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 11, 454, null, null, 9994, 49.97m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Complex_Matching_Scenario()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 3 };
            var orderWrapper3 = new OrderWrapper { Order = order3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal(500, order1.OpenQuantity);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            mockTradeListener.Verify(x => x.OnTrade(5, 1, 10, 500, 0, 25, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 6 };
            var orderWrapper6 = new OrderWrapper { Order = order6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(orderWrapper6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            mockTradeListener.Verify(x => x.OnTrade(6, 3, 9, 1000, null, null, 9000, 18));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Contains(order6, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal(9, order6.Price);
            Assert.Equal((ulong)4, order6.Sequnce);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 2000, Price = 12, OrderId = 7 };
            var orderWrapper7 = new OrderWrapper { Order = order7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(orderWrapper7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(7, 6, 9, 500, 0, 54, null, null));
            mockTradeListener.Verify(x => x.OnTrade(7, 2, 11, 1000, 0, 22, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.Contains(order7, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order7, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order7.OpenQuantity);
            Assert.Equal((ulong)5, order7.Sequnce);

            Order order8 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 12, OrderId = 8 };
            var orderWrapper8 = new OrderWrapper { Order = order8 };
            OrderMatchingResult result8 = matchingEngine.AddOrder(orderWrapper8, 8);

            mockTradeListener.Verify(x => x.OnAccept(order8.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result8);
            Assert.Contains(order8, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order8.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order8, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order8.OpenQuantity);
            Assert.Equal((ulong)6, order8.Sequnce);

            Order order9 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 13, OrderId = 9 };
            var orderWrapper9 = new OrderWrapper { Order = order9 };
            OrderMatchingResult result9 = matchingEngine.AddOrder(orderWrapper9, 9);

            mockTradeListener.Verify(x => x.OnAccept(order9.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result9);
            Assert.Contains(order9, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order9.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order9, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order9.OpenQuantity);
            Assert.Equal((ulong)7, order9.Sequnce);

            Order order10 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 0, OrderId = 10 };
            var orderWrapper10 = new OrderWrapper { Order = order10 };
            OrderMatchingResult result10 = matchingEngine.AddOrder(orderWrapper10, 10);

            mockTradeListener.Verify(x => x.OnAccept(order10.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result10);
            mockTradeListener.Verify(x => x.OnTrade(10, 9, 13, 1000, null, null, 13000, 26));
            mockTradeListener.Verify(x => x.OnTrade(10, 7, 12, 500, null, null, 21500, 89.5m));
            mockTradeListener.Verify(x => x.OnTrade(10, 8, 12, 1500, 0, 185, 18000, 36));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order10, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order10.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order10, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order10.OpenQuantity);
            Assert.Equal((ulong)0, order10.Sequnce);

            Order order11 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 0, OrderId = 11 };
            var orderWrapper11 = new OrderWrapper { Order = order11 };
            OrderMatchingResult result11 = matchingEngine.AddOrder(orderWrapper11, 11);

            mockTradeListener.Verify(x => x.OnAccept(order11.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result11);
            mockTradeListener.Verify(x => x.OnCancel(11, 3000, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order11, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order11.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order11, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(3000, order11.OpenQuantity);
            Assert.Equal((ulong)0, order11.Sequnce);

            Order order12 = new Order { IsBuy = true, Price = 0, OrderId = 12 };
            var orderWrapper12 = new OrderWrapper { Order = order12, OrderAmount = 3000 };
            OrderMatchingResult result12 = matchingEngine.AddOrder(orderWrapper12, 12);

            mockTradeListener.Verify(x => x.OnAccept(order12.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result12);
            mockTradeListener.Verify(x => x.OnCancel(12, 0, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order12, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order12.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order12, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order12.OpenQuantity);
            Assert.Equal(3000, orderWrapper12.OrderAmount);
            Assert.Equal((ulong)0, order12.Sequnce);

            Order order13 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 13 };
            var orderWrapper13 = new OrderWrapper { Order = order13 };
            OrderMatchingResult result13 = matchingEngine.AddOrder(orderWrapper13, 13);

            mockTradeListener.Verify(x => x.OnAccept(order13.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result13);
            Assert.Contains(order13, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order13.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order13, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order13.OpenQuantity);
            Assert.Equal((ulong)8, order13.Sequnce);

            Order order14 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 14 };
            var orderWrapper14 = new OrderWrapper { Order = order14 };
            OrderMatchingResult result14 = matchingEngine.AddOrder(orderWrapper14, 14);

            mockTradeListener.Verify(x => x.OnAccept(order14.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result14);
            Assert.Contains(order14, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order14.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order14, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order14.OpenQuantity);
            Assert.Equal((ulong)9, order14.Sequnce);

            Order order15 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 15 };
            var orderWrapper15 = new OrderWrapper { Order = order15 };
            OrderMatchingResult result15 = matchingEngine.AddOrder(orderWrapper15, 15);

            mockTradeListener.Verify(x => x.OnAccept(order15.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result15);
            Assert.Contains(order15, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order15.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order15, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order15.OpenQuantity);
            Assert.Equal((ulong)10, order15.Sequnce);

            Order order16 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 16 };
            var orderWrapper16 = new OrderWrapper { Order = order16 };
            OrderMatchingResult result16 = matchingEngine.AddOrder(orderWrapper16, 16);

            mockTradeListener.Verify(x => x.OnAccept(order16.OrderId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result16);
            mockTradeListener.Verify(x => x.OnTrade(16, 13, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order16, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order16.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order16, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order16.OpenQuantity);
            Assert.Equal(500, order13.OpenQuantity);
        }

        [Fact]
        public void CancelOrder_Stop_Market_Order_Amount_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, OrderAmount = 500, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 0, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Amount_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Buy()
        {
            Order order1 = new Order { IsBuy = true, Price = 0, OrderId = 1, IsStop = true };
            var orderWrapper1 = new OrderWrapper { Order = order1, StopPrice = 9, OrderAmount = 1000 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)1, order1.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Amount_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, OrderAmount = 5000, StopPrice = 11 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Amount_Trigger_And_Matches_As_Taker_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
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
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 11, OrderAmount = 5500 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Single(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 11, 500, null, null, 5500, 27.5m));
            mockTradeListener.Verify(x => x.OnTrade(3, 4, 11, 500, 0, 22, 5500, 27.5m));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
            Assert.Equal((ulong)2, order3.Sequnce);
        }

        [Fact]
        public void AddOrder_CancelsFullQuantityFOKMarketOrderAmount_IfNotEnoughSellAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill, OrderAmount = 6000 };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnCancel(5, 0, 0, 0, CancelReason.FillOrKill));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order5.Sequnce);
        }

        [Fact]
        public void AddOrder_FillsFullQuantityFOKMarketOrderAmount_IfNotEnoughSellAvailable()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1 };
            var orderWrapper1 = new OrderWrapper { Order = order1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(orderWrapper1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequnce);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2 };
            var orderWrapper2 = new OrderWrapper { Order = order2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(orderWrapper2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 10, 500, 0, 10, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequnce);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, IsStop = true };
            var orderWrapper3 = new OrderWrapper { Order = order3, StopPrice = 9 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(orderWrapper3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x.Value));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequnce);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4 };
            var orderWrapper4 = new OrderWrapper { Order = order4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(orderWrapper4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order4, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequnce);

            Order order5 = new Order { IsBuy = true, Price = 0, OrderId = 5 };
            var orderWrapper5 = new OrderWrapper { Order = order5, OrderCondition = OrderCondition.FillOrKill, OrderAmount = 4000 };
            OrderMatchingResult acceptanceResult5 = matchingEngine.AddOrder(orderWrapper5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 10, 400, null, null, 4000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x.Value));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequnce);
        }
    }
}

//TODO test scenario for GetQuantity
//TODO complex scenario for good till date to test cacheing logic
//TODO test for remaing amout for market order
//TODO fill or kill and good till date pre calculation
//TODO test book stop loss
//TODO Stop market triggers another Stop orders