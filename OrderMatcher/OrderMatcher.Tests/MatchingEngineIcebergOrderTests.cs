using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineIcebergOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineIcebergOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Rejects_InvalidTotalQuantityIceberg_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, TotalQuantity = -10, StopPrice = 10, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.InvalidPriceQuantityStopPriceOrderAmountOrTotalQuantity, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_CancelsFullOrder_IfNoFillIcebergOrderBuySide()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
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

            OrderMatchingResult acceptanceResult2 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 5000, 0, 0, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult2);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void CancelOrder_CancelCorrectAmount_IfIcebergOrderPartiallyFilledBuy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 5000 };
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 200, null, null, 2000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(300, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            OrderMatchingResult acceptanceResult4 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 4300, 7000, 14, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult4);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void CancelOrder_RejectsRequest_IfIcebergOrderMatchedFully()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500 };
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
            Assert.Equal(1000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 500, 0, 30, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequence);

            OrderMatchingResult acceptanceResult5 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderDoesNotExists, acceptanceResult5);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergOrderIsAddedSellSide()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 5000, TipQuantity = 500 };
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
        }

        [Fact]
        public void AddOrder_SetsTotalQuantityToOpenQuantity_ForIcebergOrders()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500 };
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
            Assert.Equal(1000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfIcebergOrderIsStopOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500, StopPrice = 9 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_RejectsOrder_IfIcebergOrderIsMarketOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 0, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.IcebergOrderCannotBeStopOrMarketOrder, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergeOrderTipIsMatchedSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order2.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 2).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order2.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_AddsMoreTip_IfIcebergeOrderTipIsMatchedSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 400, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(400, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 3, 2, 10, 400, null, null, 4000, 8));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order3.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 3).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order3.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order3.Sequence);
        }

        [Fact]
        public void AddOrder_AddsAllTip_IfIcebergeOrderTipIsMatchedSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 400, Price = 11, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(400, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 600, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Equal(2, matchingEngine.Book.BidSide.Count());
            Assert.Contains(order3, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(600, order3.OpenQuantity);
            Assert.Equal((ulong)3, order3.Sequence);

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 2, 4, 2, 11, 400, null, null, 4400, 8.8m));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 100, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 400, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 4, 3, 10, 100, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 4, 3, 10, 500, 0, 77, 6000, 12));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order4.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order4.Sequence);
        }

        [Fact]
        public void AddOrder_AddsAllTip_IfIcebergeOrderTipIsMatchedWithSingleHugeOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 5000, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(5000, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, 0, 250, 50000, 100));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void CancelOrder_CancelsProperAmount_IfIcebergeFewOrderTipIsMatchedAddOrder()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 400, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(400, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 3, 2, 10, 400, null, null, 4000, 8));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Contains(order3.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 3).Single().OpenQuantity);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order3.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            OrderMatchingResult acceptanceResult4 = matchingEngine.CancelOrder(order3.OrderId);

            mockTradeListener.Verify(x => x.OnCancel(order3.OrderId, order3.UserId, 600, 9000, 45, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void AddOrder_CancelsIceberg_IfIcebergeOrderTipIsMatchedWithSingleHugeOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2, TotalQuantity = 5000, OrderCondition = OrderCondition.BookOrCancel, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnCancel(2, 2, 5000, 0, 0, CancelReason.BookOrCancel));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergeOrderTipIsMatchedWithMultipleOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, TotalQuantity = 5000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 3, 2, 10, 500, null, null, 5000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order3.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(3500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order3.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order3.Sequence);
        }

        [Fact]
        public void AddOrder_AddsTip_IfIcebergeOrderTipIsMatchedWithFullyOrderSell()
        {
            Order order1 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.Verify(x => x.OnAccept(order1.OrderId, order1.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order1, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)1, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Single(matchingEngine.Book.BidSide);
            Assert.Contains(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)2, order2.Sequence);

            Order order3 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3, TotalQuantity = 1000, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(3, 2, 3, 2, 10, 500, 0, 50, 5000, 10));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order3.Sequence);
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

            Order order4 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4, TotalQuantity = 1500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 2, 4, 2, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnTrade(4, 3, 4, 3, 10, 500, null, null, 5000, 10));
            mockTradeListener.Verify(x => x.OnOrderTriggered(1, 1));
            mockTradeListener.Verify(x => x.OnTrade(1, 4, 1, 4, 10, 500, 0, 60, 5000, 25));
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

        [Fact]
        public void AddOrder_Cancels_PendingIcebergOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10, TotalQuantity = 5000, TipQuantity = 500 };
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 200, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 10);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 4500, 5000, 10, CancelReason.ValidityExpired));
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
            Assert.Equal((ulong)3, order3.Sequence);
        }

        [Fact]
        public void AddOrder_RejectsOrder_QuantityMoreThanTotalQuantity()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 409, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.InvalidIcebergOrderTotalQuantity, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_RejectsOrder_QuantityEqualToTotalQuantity()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 500, TipQuantity = 500 };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.InvalidIcebergOrderTotalQuantity, acceptanceResult);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal((ulong)0, order1.Sequence);
        }

        [Fact]
        public void AddOrder_Rejects_QuantityNotMultipleOfStepSize_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.5m, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10, TotalQuantity = 10000.1m, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_QuantityNotMultipleOfStepSize2_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 3, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10, TotalQuantity = 10000, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_TotalQuantityNotMultipleOfStepSize_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 0.5m, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000.1m, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Rejects_TotalQuantityNotMultipleOfStepSize2_Order()
        {
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 3, 0);
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.QuantityAndTotalQuantityShouldBeMultipleOfStepSize, accepted);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Accepts_TotalQuantityMultipleOfStepSize_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void AddOrder_Accepts_QuantityMultipleOfStepSize_Order()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10, TotalQuantity = 10000, TipQuantity = 1000 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
        }

        [Fact]
        public void CancelOrder_CancelsCorrectAmount_IfLastTipIsPartiallyRemainingBuy()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TipQuantity = 500, TotalQuantity = 1200 };
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
            Assert.Equal(700, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(200, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(200, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequence);

            OrderMatchingResult acceptanceResult5 = matchingEngine.CancelOrder(order1.OrderId);
            mockTradeListener.Verify(x => x.OnCancel(1, 1, 100, 11000, 22, CancelReason.UserRequested));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.CancelAcepted, acceptanceResult5);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.Contains(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
        }

        [Fact]
        public void AddOrder_AddsTip_IfTipIsMatchedFullyBuySide()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 5000, TipQuantity = 500 };
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(4000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_AddedTipWithRemainQuantiy_IfLastTipIsRemaining()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1200, TipQuantity = 500 };
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
            Assert.Equal(700, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(200, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(200, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(100, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequence);
        }

        [Fact]
        public void AddOrder_AddedTipWithRemainQuantiy2_IfLastTipIsRemaining()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500 };
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
            Assert.Equal(1000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 100, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 100, null, null, 1000, 5));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(400, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequence);
        }

        [Fact]
        public void AddOrder_AddedTipNoTip_IfLastTipIsMatcheFully()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 1500, TipQuantity = 500 };
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
            Assert.Equal(1000, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal((ulong)0, order1.Sequence);

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, matchingEngine.CurrentOrders.Single(x => x.OrderId == order1.OrderId).TotalQuantity);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);

            Order order3 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 3, UserId = 3 };
            OrderMatchingResult acceptanceResult3 = matchingEngine.AddOrder(order3, 3);

            mockTradeListener.Verify(x => x.OnAccept(order3.OrderId, order3.UserId));
            mockTradeListener.Verify(x => x.OnTrade(3, 1, 3, 1, 10, 500, null, null, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult3);
            Assert.Contains(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.Equal(500, matchingEngine.Book.AskSide.SelectMany(x => x).Where(x => x.OrderId == 1).Single().OpenQuantity);
            Assert.Contains(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order3, matchingEngine.CurrentOrders);
            Assert.Contains(order3.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order3, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order3.OpenQuantity);
            Assert.Equal((ulong)0, order3.Sequence);

            Order order4 = new Order { IsBuy = true, OpenQuantity = 500, Price = 10, OrderId = 4, UserId = 4 };
            OrderMatchingResult acceptanceResult4 = matchingEngine.AddOrder(order4, 4);

            mockTradeListener.Verify(x => x.OnAccept(order4.OrderId, order4.UserId));
            mockTradeListener.Verify(x => x.OnTrade(4, 1, 4, 1, 10, 500, 0, 30, 5000, 25));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult4);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.Book.AskSide.SelectMany(x => x).Select(x => x.OrderId).ToList());
            Assert.DoesNotContain(order1.OrderId, matchingEngine.CurrentOrders.Select(x => x.OrderId));
            Assert.DoesNotContain(order4, matchingEngine.CurrentOrders);
            Assert.Contains(order4.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order4, matchingEngine.Book.StopBidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order4.OpenQuantity);
            Assert.Equal((ulong)0, order4.Sequence);
        }

        [Fact]
        public void AddOrder_AddedAllTip_IfMatchedWithHugeOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, TotalQuantity = 5000, TipQuantity = 500 };
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 5000, Price = 10, OrderId = 2, UserId = 2 };
            OrderMatchingResult acceptanceResult2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.Verify(x => x.OnAccept(order2.OrderId, order2.UserId));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, null, null, null, null));
            mockTradeListener.Verify(x => x.OnTrade(2, 1, 2, 1, 10, 500, 0, 100, 50000, 250));
            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.OrderAccepted, acceptanceResult2);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Empty(matchingEngine.CurrentOrders);
            Assert.Contains(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(0, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }

        [Fact]
        public void AddOrder_ResetsOpenQuantityAndIsTip()
        {
            Order order1 = new Order { IsBuy = true, OrderId = 1, UserId = 1, OpenQuantity = 1000, Price = 10 };
            OrderMatchingResult accepted = matchingEngine.AddOrder(order1, 1);
            Assert.Equal(OrderMatchingResult.OrderAccepted, accepted);
            Assert.Equal(1000, order1.OpenQuantity);
            Assert.False(order1.IsTip);
        }
    }
}
