using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineStopMarketOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineStopMarketOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void CancelOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
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

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 11 };
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
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 3, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);
        }

        [Fact]
        public void CancelOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Sell()
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

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);

            OrderMatchingResult result4 = matchingEngine.CancelOrder(order3.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(3, 3, 500, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, result4);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Buy()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 0, OrderId = 1, UserId = 1, StopPrice = 9 };
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
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_Empty_Book_No_Market_Rate_Sell()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 0, OrderId = 1, UserId = 1, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Buy()
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

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 11 };
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
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Entered_In_Stop_Book_With_Market_Rate_For_Sell()
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

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Added_To_Book_If_Stop_Price_Is_Less_Than_Market_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 600, Price = 10, OrderId = 1, UserId = 1 };
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
            Assert.Equal(600, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, null, null, 5000, 25));
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

            Order order3 = new Order { IsBuy = true, OpenQuantity = 600, Price = 0, OrderId = 3, UserId = 3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, true, 10, 100, 0, 12, null, null));
            mockTradeListener.Verify(x => x.OnCancel(3, 3, 500, 1000, 5, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Order_Added_To_Book_If_Stop_Price_Is_More_Than_Market_Sell()
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 600, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, true, 10, 500, 0, 10, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(100, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 600, Price = 0, OrderId = 3, UserId = 3, StopPrice = 10 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 3, 2, false, 11, 100, null, null, 6100, 27.2m));
            mockTradeListener.Verify(x => x.OnCancel(3, 3, 500, 1100, 5.5m, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Trigger_And_Matches_As_Taker_Buy()
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

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 11 };
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
            Assert.Equal(500, order3.OpenQuantity);
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
        public void AddOrder_Stop_Market_Trigger_And_Matches_As_Taker_Sell()
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

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 8, OrderId = 4, UserId = 4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequence);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 8, OrderId = 5, UserId = 5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 5, 4, true, 8, 500, 0, 8, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 5, 3, 5, false, 8, 500, 0, 20, 8000, 28));
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
            Assert.Equal((ulong)4, order5.Sequence);
        }

        [Fact]
        public void AddOrder_Stop_Market_Trigger_And_Cancelled()
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

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 9 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.StopAskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 8, OrderId = 4, UserId = 4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            Assert.Contains(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order4, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequence);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 8, OrderId = 5, UserId = 5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
            mockTradeListener.Verify(x => x.OnTrade(5, 4, 5, 4, true, 8, 500, 0, 8, 4000, 20));
            mockTradeListener.Verify(x => x.OnOrderTriggered(3, 3));
            mockTradeListener.Verify(x => x.OnCancel(3, 3, 500, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);
            Assert.Equal((ulong)0, order5.Sequence);
        }

        [Fact]
        public void AddOrder_Triggers_Stop_Limit_Order_Buy_Time_Priority()
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

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 3, UserId = 3, StopPrice = 11 };
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
            Assert.Equal(500, order3.OpenQuantity);
            Assert.Equal((ulong)2, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 4, UserId = 4, StopPrice = 11 };
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
            Assert.Equal(500, order4.OpenQuantity);
            Assert.Equal((ulong)3, order4.Sequence);

            Order order5 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 5, UserId = 5, StopPrice = 12 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            Assert.Contains(order5, matchingEngine.CurrentOrders);
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order5, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(2, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order5.OpenQuantity);
            Assert.Equal((ulong)4, order5.Sequence);

            Order order6 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 6, UserId = 6, StopPrice = 12 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(order6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId, order6.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            Assert.Contains(order6, matchingEngine.CurrentOrders);
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order6, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(2, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal((ulong)5, order6.Sequence);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 7, UserId = 7, StopPrice = 13 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(order7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId, order7.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.Contains(order7, matchingEngine.CurrentOrders);
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order7, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(3, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order7.OpenQuantity);
            Assert.Equal((ulong)6, order7.Sequence);

            Order order8 = new Order { IsBuy = true, OpenQuantity = 500, Price = 0, OrderId = 8, UserId = 8, StopPrice = 13 };
            OrderMatchingResult result8 = matchingEngine.AddOrder(order8, 8);

            mockTradeListener.Verify(x => x.OnAccept(order8.OrderId, order8.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result8);
            Assert.Contains(order8, matchingEngine.CurrentOrders);
            Assert.Contains(order8.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order8, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(3, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(500, order8.OpenQuantity);
            Assert.Equal((ulong)7, order8.Sequence);

            Order order9 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 12, OrderId = 9, UserId = 9 };
            OrderMatchingResult result9 = matchingEngine.AddOrder(order9, 9);

            mockTradeListener.Verify(x => x.OnAccept(order9.OrderId, order9.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result9);
            Assert.Contains(order9, matchingEngine.CurrentOrders);
            Assert.Contains(order9.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order9, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(3, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(1000, order9.OpenQuantity);
            Assert.Equal((ulong)8, order9.Sequence);

            Order order10 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 13, OrderId = 10, UserId = 10 };
            OrderMatchingResult result10 = matchingEngine.AddOrder(order10, 10);

            mockTradeListener.Verify(x => x.OnAccept(order10.OrderId, order10.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result10);
            Assert.Contains(order10, matchingEngine.CurrentOrders);
            Assert.Contains(order10.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order10, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(3, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(1000, order10.OpenQuantity);
            Assert.Equal((ulong)9, order10.Sequence);

            Order order11 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 14, OrderId = 11, UserId = 11 };
            OrderMatchingResult result11 = matchingEngine.AddOrder(order11, 11);

            mockTradeListener.Verify(x => x.OnAccept(order11.OrderId, order11.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result11);
            Assert.Contains(order11, matchingEngine.CurrentOrders);
            Assert.Contains(order11.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order11, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Equal(3, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Equal(3, matchingEngine.Book.StopBidSide.Count());
            Assert.Equal(1000, order11.OpenQuantity);
            Assert.Equal((ulong)10, order11.Sequence);

            Order order12 = new Order { IsBuy = true, OpenQuantity = 500, Price = 11, OrderId = 12, UserId = 12 };
            OrderMatchingResult result12 = matchingEngine.AddOrder(order12, 12);
            Assert.Equal(OrderMatchingResult.OrderAccepted, result12);
            mockTradeListener.Verify(x => x.OnAccept(order12.OrderId, order12.UserId));
            Assert.Equal(500, order12.OpenQuantity);
            Assert.Equal((ulong)11, order12.Sequence);

            Order order13 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 13, UserId = 13 };
            OrderMatchingResult result13 = matchingEngine.AddOrder(order13, 13);
            Assert.Equal(OrderMatchingResult.OrderAccepted, result13);
            mockTradeListener.Verify(x => x.OnAccept(order13.OrderId, order13.UserId));
            Assert.Equal(0, order13.OpenQuantity);
            Assert.Equal((ulong)0, order13.Sequence);

            mockTradeListener.Verify(x => x.OnTrade(13, 12, 13, 12, false, 11, 500, 0, 27.5m, 5500, 11m));

            mockTradeListener.Verify(x => x.OnOrderTriggered(3, 3));
            mockTradeListener.Verify(x => x.OnTrade(3, 9, 3, 9, true, 12, 500, null, null, 6000, 30m));
            mockTradeListener.Verify(x => x.OnOrderTriggered(4, 4));
            mockTradeListener.Verify(x => x.OnTrade(4, 9, 4, 9, true, 12, 500, 0, 24, 6000, 30m));

            mockTradeListener.Verify(x => x.OnOrderTriggered(5, 5));
            mockTradeListener.Verify(x => x.OnTrade(5, 10, 5, 10, true, 13, 500, null, null, 6500, 32.5m));
            mockTradeListener.Verify(x => x.OnOrderTriggered(6, 6));
            mockTradeListener.Verify(x => x.OnTrade(6, 10, 6, 10, true, 13, 500, 0, 26, 6500, 32.5m));

            mockTradeListener.Verify(x => x.OnOrderTriggered(7, 7));
            mockTradeListener.Verify(x => x.OnTrade(7, 11, 7, 11, true, 14, 500, null, null, 7000, 35m));
            mockTradeListener.Verify(x => x.OnOrderTriggered(8, 8));
            mockTradeListener.Verify(x => x.OnTrade(8, 11, 8, 11, true, 14, 500, 0, 28, 7000, 35m));

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result12);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order12.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }
    }
}
