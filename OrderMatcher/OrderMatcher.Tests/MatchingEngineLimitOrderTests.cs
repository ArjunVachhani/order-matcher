using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineLimitOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineLimitOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Normal_Limit_Matches_With_Pending_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 1000, 0, 50, 10000, 20));
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

            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
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
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);

            Order order2 = new Order { IsBuy = false, OrderId = 2, UserId = 2, OpenQuantity = 1000, Price = 11 };
            OrderMatchingResult accepted2 = matchingEngine.AddOrder(order2, 2);
            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
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
            Order order1 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 1, UserId = 1 };
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
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 11, OrderId = 2, UserId = 2 };
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
            Assert.Equal(1000, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 3, UserId = 3 };
            OrderMatchingResult result3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
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
            Assert.Equal((ulong)3, order3.Sequence);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult result4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            Assert.Equal(OrderMatchingResult.OrderAccepted, result4);
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 500, 0, 25, null, null));
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

            Order order5 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 10, OrderId = 5, UserId = 5 };
            OrderMatchingResult result5 = matchingEngine.AddOrder(order5, 5);

            mockTradeListener.Verify(x => x.OnAccept(order5.OrderId, order5.UserId));
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
            Assert.Equal((ulong)4, order5.Sequence);

            Order order6 = new Order { IsBuy = true, OpenQuantity = 500, Price = 12, OrderId = 6, UserId = 6 };
            OrderMatchingResult result6 = matchingEngine.AddOrder(order6, 6);

            mockTradeListener.Verify(x => x.OnAccept(order6.OrderId, order6.UserId));
            mockTradeListener.Verify(x => x.OnTrade(6, 2, 6, 2, 11, 500, null, null, 5500, 27.5m));
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
            Assert.Equal((ulong)0, order6.Sequence);

            Order order7 = new Order { IsBuy = false, OpenQuantity = 500, Price = 12, OrderId = 7, UserId = 7 };
            OrderMatchingResult result7 = matchingEngine.AddOrder(order7, 7);

            mockTradeListener.Verify(x => x.OnAccept(order7.OrderId, order7.UserId));
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
            Assert.Equal((ulong)5, order7.Sequence);

            Order order8 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 13, OrderId = 8, UserId = 8 };
            OrderMatchingResult result8 = matchingEngine.AddOrder(order8, 8);

            mockTradeListener.Verify(x => x.OnAccept(order8.OrderId, order8.UserId));
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
            Assert.Equal((ulong)6, order8.Sequence);

            Order order9 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 14, OrderId = 9, UserId = 9 };
            OrderMatchingResult result9 = matchingEngine.AddOrder(order9, 9);

            mockTradeListener.Verify(x => x.OnAccept(order9.OrderId, order9.UserId));
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
            Assert.Equal((ulong)7, order9.Sequence);

            Order order10 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 14, OrderId = 10, UserId = 10 };
            OrderMatchingResult result10 = matchingEngine.AddOrder(order10, 10);

            mockTradeListener.Verify(x => x.OnAccept(order10.OrderId, order10.UserId));
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
            Assert.Equal((ulong)8, order10.Sequence);

            Order order11 = new Order { IsBuy = false, OpenQuantity = 1000, Price = 15, OrderId = 11, UserId = 11 };
            OrderMatchingResult result11 = matchingEngine.AddOrder(order11, 11);

            mockTradeListener.Verify(x => x.OnAccept(order11.OrderId, order11.UserId));
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
            Assert.Equal((ulong)9, order11.Sequence);

            Order order12 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 9, OrderId = 12, UserId = 12 };
            OrderMatchingResult result12 = matchingEngine.AddOrder(order12, 12);

            mockTradeListener.Verify(x => x.OnAccept(order12.OrderId, order12.UserId));
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
            Assert.Equal((ulong)10, order12.Sequence);

            Order order13 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 8, OrderId = 13, UserId = 13 };
            OrderMatchingResult result13 = matchingEngine.AddOrder(order13, 13);

            mockTradeListener.Verify(x => x.OnAccept(order13.OrderId, order13.UserId));
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
            Assert.Equal((ulong)11, order13.Sequence);

            Order order14 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 7, OrderId = 14, UserId = 14 };
            OrderMatchingResult result14 = matchingEngine.AddOrder(order14, 14);

            mockTradeListener.Verify(x => x.OnAccept(order14.OrderId, order14.UserId));
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
            Assert.Equal((ulong)12, order14.Sequence);

            Order order15 = new Order { IsBuy = true, OpenQuantity = 1000, Price = 7, OrderId = 15, UserId = 15 };
            OrderMatchingResult result15 = matchingEngine.AddOrder(order15, 15);

            mockTradeListener.Verify(x => x.OnAccept(order15.OrderId, order15.UserId));
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
            Assert.Equal((ulong)13, order15.Sequence);

            OrderMatchingResult result16 = matchingEngine.CancelOrder(2);

            Assert.Equal(OrderMatchingResult.CancelAcepted, result16);
            mockTradeListener.Verify(x => x.OnCancel(2, 2, 500, 5500, 11, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders.Select(x => x.Value));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x.Value));
            Assert.Equal(4, matchingEngine.Book.BidSide.Count());
            Assert.Equal(4, matchingEngine.Book.AskSide.Count());
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order17 = new Order { IsBuy = true, OpenQuantity = 5000, Price = 16, OrderId = 17, UserId = 17 };
            OrderMatchingResult result17 = matchingEngine.AddOrder(order17, 17);

            mockTradeListener.Verify(x => x.OnAccept(order17.OrderId, order17.UserId));
            mockTradeListener.Verify(x => x.OnTrade(17, 7, 17, 7, 12, 500, 0, 12, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 8, 17, 8, 13, 1000, 0, 26, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 9, 17, 9, 14, 1000, 0, 28, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 10, 17, 10, 14, 1000, 0, 28, null, null));
            mockTradeListener.Verify(x => x.OnTrade(17, 11, 17, 11, 15, 1000, 0, 30, null, null));
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
            Assert.Equal((ulong)14, order17.Sequence);

            Order order18 = new Order { IsBuy = false, OpenQuantity = 4000, Price = 7, OrderId = 18, UserId = 18 };
            OrderMatchingResult result18 = matchingEngine.AddOrder(order18, 18);

            mockTradeListener.Verify(x => x.OnAccept(order18.OrderId, order18.UserId));
            mockTradeListener.Verify(x => x.OnTrade(18, 17, 18, 17, 16, 500, null, null, 70000, 326));
            mockTradeListener.Verify(x => x.OnTrade(18, 1, 18, 1, 10, 500, null, null, 10000, 20));
            mockTradeListener.Verify(x => x.OnTrade(18, 5, 18, 5, 10, 1000, null, null, 10000, 20));
            mockTradeListener.Verify(x => x.OnTrade(18, 3, 18, 3, 9, 1000, null, null, 9000, 18));
            mockTradeListener.Verify(x => x.OnTrade(18, 12, 18, 12, 9, 1000, 0, 205, 9000, 18));
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
            Assert.Equal((ulong)0, order18.Sequence);

            Order order19 = new Order { IsBuy = false, OpenQuantity = 3000, Price = 7, OrderId = 19, UserId = 19 };
            OrderMatchingResult result19 = matchingEngine.AddOrder(order19, 19);

            mockTradeListener.Verify(x => x.OnAccept(order19.OrderId, order19.UserId));
            mockTradeListener.Verify(x => x.OnTrade(19, 13, 19, 13, 8, 1000, null, null, 8000, 16));
            mockTradeListener.Verify(x => x.OnTrade(19, 14, 19, 14, 7, 1000, null, null, 7000, 14));
            mockTradeListener.Verify(x => x.OnTrade(19, 15, 19, 15, 7, 1000, 0, 110, 7000, 14));
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
            Assert.Equal((ulong)0, order19.Sequence);
        }

    }
}
