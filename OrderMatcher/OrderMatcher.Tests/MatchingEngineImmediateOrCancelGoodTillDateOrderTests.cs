using Moq;
using OrderMatcher.Types;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class MatchingEngineImmediateOrCancelGoodTillDateOrderTests
    {
        private readonly Mock<ITradeListener> mockTradeListener;
        private readonly Mock<IFeeProvider> mockFeeProcider;
        private MatchingEngine matchingEngine;
        public MatchingEngineImmediateOrCancelGoodTillDateOrderTests()
        {
            mockTradeListener = new Mock<ITradeListener>();
            mockFeeProcider = new Mock<IFeeProvider>();
            mockFeeProcider.Setup(x => x.GetFee(It.IsAny<short>())).Returns(new Fee { MakerFee = 0.2m, TakerFee = 0.5m });
            matchingEngine = new MatchingEngine(mockTradeListener.Object, mockFeeProcider.Object, 1, 2);
        }

        [Fact]
        public void AddOrder_Rejects_ImmediateOrCancel_If_GTDOrder()
        {
            Order order1 = new Order { IsBuy = false, OpenQuantity = 500, Price = 10, OrderId = 1, UserId = 1, CancelOn = 10, OrderCondition = OrderCondition.ImmediateOrCancel };
            OrderMatchingResult acceptanceResult = matchingEngine.AddOrder(order1, 1);

            mockTradeListener.VerifyNoOtherCalls();
            Assert.Equal(OrderMatchingResult.GoodTillDateCannotBeMarketOrIOCorFOK, acceptanceResult);
            Assert.DoesNotContain(order1, matchingEngine.CurrentOrders);
            Assert.DoesNotContain(order1.OrderId, matchingEngine.AcceptedOrders);
            Assert.DoesNotContain(order1, matchingEngine.Book.AskSide.SelectMany(x => x));
            Assert.Empty(matchingEngine.Book.BidSide);
            Assert.Empty(matchingEngine.Book.AskSide);
            Assert.Empty(matchingEngine.Book.StopAskSide);
            Assert.Empty(matchingEngine.Book.StopBidSide);
            Assert.Equal(500, order1.OpenQuantity);
            Assert.Equal((ulong)0, order1.Sequence);
        }
    }
}
