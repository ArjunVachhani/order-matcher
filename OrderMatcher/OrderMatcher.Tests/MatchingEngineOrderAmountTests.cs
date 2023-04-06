using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineOrderAmountTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineOrderAmountTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidOrderAmount_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 10, OrderAmount = -1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForMarketSell()
        {
            Order order1 = new Order { IsBuy = false, OrderId = 1, UserId = 1, Price = 0, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForLimitBuy()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 1, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForStopLimitBuy()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 1, StopPrice = 2, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForBookOrCancelBuy()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 1, OrderCondition = OrderCondition.BookOrCancel, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForGTD()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 1, CancelOn = 1, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Rejects_OrderAmountOnlySupportedForMarketBuyOrder_ForIceberg()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, Price = 1, OpenQuantity = 1, TipQuantity = 1, TotalQuantity = 10, OrderAmount = 1 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAmountOnlySupportedForMarketBuyOrder, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            mockTradeListener.VerifyNoOtherCalls();
        }

        [Fact]
        public void AddOrder_Cancels_MarketOrderAmountIfNoSellAvailable()
        {
            Order order1 = new Order { IsBuy = true, Price = 0, OrderId = 1, UserId = 1, OrderAmount = 10 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 0, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Matches_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1, UserId = 1 };
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
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, Price = 0, OrderId = 2, UserId = 2, OrderAmount = 5000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
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
        public void AddOrder_Normal_Market_Order_Amount_Multiple_Matches_Order_With_Open_Orders_For_Buy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 10, OrderId = 1, UserId = 1 };
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
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, Price = 0, OrderId = 2, UserId = 2, OrderAmount = 5000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
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

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, UserId = 3, OrderAmount = 5000 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, 0, 20, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Matches_Multiple_Order_With_Open_Orders_For_Buy()
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

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1, matchingEngine.CurrentOrders);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Equal(2, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = true, Price = 0, OrderId = 3, UserId = 3, OrderAmount = 10000 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, 0, 10, null, null));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 3, 2, 11, 454, null, null, 9994, 49.97m));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);
        }

        [Fact]
        public void AddOrder_Normal_Market_Order_Amount_Complex_Matching_Scenario()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1, UserId = 1 };
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
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result2);
            Assert.Contains(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 3, UserId = 3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result3);
            Assert.Contains(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequence);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal(500, order1.OpenQuantity);

            Order order5 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 5, UserId = 5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result5);
            mockTradeListener.Verify(x => x.OnTrade(5, 1, 5, 1, 10, 500, 0, 25, 10000, 20));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order5, matchingEngine.CurrentOrders);
            Assert.Contains(order5.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order5, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order5.OpenQuantity);

            Order order6 = new Order { IsBuy = false, OpenQuantity = 1500, Price = 0, OrderId = 6, UserId = 6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(order6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId, order6.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result6);
            mockTradeListener.Verify(x => x.OnTrade(6, 3, 6, 3, 9, 1000, null, null, 9000, 18));
            mockTradeListener.Verify(x => x.OnCancel(6, 6, 500, 9000, 45, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order6, matchingEngine.CurrentOrders);
            Assert.Contains(order6.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order6, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order6.OpenQuantity);
            Assert.Equal((ulong)0, order6.Sequence);

            Order order7 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 12, OrderId = 7, UserId = 7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(order7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId, order7.UserId));
            mockTradeListener.Verify(x => x.OnTrade(7, 2, 7, 2, 11, 1000, 0, 22, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result7);
            Assert.Contains(order7, matchingEngine.CurrentOrders);
            Assert.Contains(order7.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order7, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order7.OpenQuantity);
            Assert.Equal((ulong)4, order7.Sequence);

            Order order8 = new Order { IsBuy = true, OpenQuantity = 1500, Price = 12, OrderId = 8, UserId = 8 };
            OrderMatchingResult result8 = matchingEngine.AddOrder(order8, 8);

            mockTradeListener.Verify(x => x.OnAccept(order8.OrderId, order8.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result8);
            Assert.Contains(order8, matchingEngine.CurrentOrders);
            Assert.Contains(order8.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order8, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1500, order8.OpenQuantity);
            Assert.Equal((ulong)5, order8.Sequence);

            Order order9 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 13, OrderId = 9, UserId = 9 };
            OrderMatchingResult result9 = matchingEngine.AddOrder(order9, 9);

            mockTradeListener.Verify(x => x.OnAccept(order9.OrderId, order9.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result9);
            Assert.Contains(order9, matchingEngine.CurrentOrders);
            Assert.Contains(order9.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order9, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order9.OpenQuantity);
            Assert.Equal((ulong)6, order9.Sequence);

            Order order10 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 0, OrderId = 10, UserId = 10 };
            OrderMatchingResult result10 = matchingEngine.AddOrder(order10, 10);

            mockTradeListener.Verify(x => x.OnAccept(order10.OrderId, order10.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result10);
            mockTradeListener.Verify(x => x.OnTrade(10, 9, 10, 9, 13, 1000, null, null, 13000, 26));
            mockTradeListener.Verify(x => x.OnTrade(10, 7, 10, 7, 12, 500, null, null, 17000, 67));
            mockTradeListener.Verify(x => x.OnTrade(10, 8, 10, 8, 12, 1500, 0, 185, 18000, 36));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order10, matchingEngine.CurrentOrders);
            Assert.Contains(order10.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order10, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order10.OpenQuantity);
            Assert.Equal((ulong)0, order10.Sequence);

            Order order11 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 0, OrderId = 11, UserId = 11 };
            OrderMatchingResult result11 = matchingEngine.AddOrder(order11, 11);

            mockTradeListener.Verify(x => x.OnAccept(order11.OrderId, order11.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result11);
            mockTradeListener.Verify(x => x.OnCancel(11, 11, 3000, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order11, matchingEngine.CurrentOrders);
            Assert.Contains(order11.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order11, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(3000, order11.OpenQuantity);
            Assert.Equal((ulong)0, order11.Sequence);

            Order order12 = new Order { IsBuy = true, Price = 0, OrderId = 12, UserId = 12, OrderAmount = 3000 };
            OrderMatchingResult result12 = matchingEngine.AddOrder(order12, 12);

            mockTradeListener.Verify(x => x.OnAccept(order12.OrderId, order12.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result12);
            mockTradeListener.Verify(x => x.OnCancel(12, 12, 0, 0, 0, CancelReason.MarketOrderNoLiquidity));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order12, matchingEngine.CurrentOrders);
            Assert.Contains(order12.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order12, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order12.OpenQuantity);
            Assert.Equal(3000, order12.OrderAmount);
            Assert.Equal((ulong)0, order12.Sequence);

            Order order13 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 13, UserId = 13 };
            OrderMatchingResult result13 = matchingEngine.AddOrder(order13, 13);

            mockTradeListener.Verify(x => x.OnAccept(order13.OrderId, order13.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result13);
            Assert.Contains(order13, matchingEngine.CurrentOrders);
            Assert.Contains(order13.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order13, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order13.OpenQuantity);
            Assert.Equal((ulong)7, order13.Sequence);

            Order order14 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 14, UserId = 14 };
            OrderMatchingResult result14 = matchingEngine.AddOrder(order14, 14);

            mockTradeListener.Verify(x => x.OnAccept(order14.OrderId, order14.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result14);
            Assert.Contains(order14, matchingEngine.CurrentOrders);
            Assert.Contains(order14.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order14, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order14.OpenQuantity);
            Assert.Equal((ulong)8, order14.Sequence);

            Order order15 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 15, UserId = 15 };
            OrderMatchingResult result15 = matchingEngine.AddOrder(order15, 15);

            mockTradeListener.Verify(x => x.OnAccept(order15.OrderId, order15.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, result15);
            Assert.Contains(order15, matchingEngine.CurrentOrders);
            Assert.Contains(order15.OrderId, matchingEngine.AcceptedOrders);
            Assert.Contains(order15, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(1000, order15.OpenQuantity);
            Assert.Equal((ulong)9, order15.Sequence);

            Order order16 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 16, UserId = 16 };
            OrderMatchingResult result16 = matchingEngine.AddOrder(order16, 16);

            mockTradeListener.Verify(x => x.OnAccept(order16.OrderId, order16.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result16);
            mockTradeListener.Verify(x => x.OnTrade(16, 13, 16, 13, 10, 500, 0, 25, null, null));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order16, matchingEngine.CurrentOrders);
            Assert.Contains(order16.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order16, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order16.OpenQuantity);
            Assert.Equal(500, order13.OpenQuantity);
        }

    }
}
