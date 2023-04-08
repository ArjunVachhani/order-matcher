using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineStopBookOrCancelOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineStopBookOrCancelOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(0)]
        public void AddOrder_Rejects_BookOrCancel_If_StopOrder(int price)
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

            Order order2 = new Order { IsBuy = true, OpenQuantity = 500, Price = price, OrderId = 2, UserId = 2, OrderCondition = OrderCondition.BookOrCancel, StopPrice = 1000 };
            OrderMatchingResult result2 = matchingEngine.AddOrder(order2, 2);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.BookOrCancelCannotBeMarketOrStopOrder, result2);
            Assert.DoesNotContain(order2, matchingEngine.CurrentOrders);
            Assert.DoesNotContain(order2.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order2, matchingEngine.Book.BidSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Single(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order2.OpenQuantity);
            Assert.Equal((ulong)0, order2.Sequence);
        }
    }
}
