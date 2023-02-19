using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class GoodTillDateOrdersTests
    {
        [Fact]
        void AddNonGtdOrder()
        {
            GoodTillDateOrders goodTillDateOrders = new GoodTillDateOrders();

            var expiredOrders = goodTillDateOrders.GetExpiredOrders(1);
            Assert.Null(expiredOrders);

            var order1 = new Types.Order { OrderId = 1 };
            goodTillDateOrders.Add(order1);

            expiredOrders = goodTillDateOrders.GetExpiredOrders(0);
            Assert.Null(expiredOrders);
            Assert.DoesNotContain(order1.OrderId, goodTillDateOrders.SelectMany(x => x.Value));
        }

        [Fact]
        void RemoveNonGtdOrder()
        {
            GoodTillDateOrders goodTillDateOrders = new GoodTillDateOrders();

            var expiredOrders = goodTillDateOrders.GetExpiredOrders(1);
            Assert.Null(expiredOrders);

            var order1 = new Types.Order { OrderId = 1 };
            goodTillDateOrders.Add(order1);
            goodTillDateOrders.Remove(order1);
        }

        [Fact]
        void Add()
        {
            GoodTillDateOrders goodTillDateOrders = new GoodTillDateOrders();

            var expiredOrders = goodTillDateOrders.GetExpiredOrders(1);
            Assert.Null(expiredOrders);

            var order1 = new Types.Order { CancelOn = 10, OrderId = 1 };
            goodTillDateOrders.Add(order1);

            expiredOrders = goodTillDateOrders.GetExpiredOrders(9);
            Assert.Null(expiredOrders);
            Assert.Contains(order1.OrderId, goodTillDateOrders.SelectMany(x => x.Value));

            expiredOrders = goodTillDateOrders.GetExpiredOrders(10);
            Assert.Contains(order1.OrderId, goodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order1.OrderId, expiredOrders.SelectMany(x => x));

            //just returns orders, does not removes it
            expiredOrders = goodTillDateOrders.GetExpiredOrders(10);
            Assert.Contains(order1.OrderId, goodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order1.OrderId, expiredOrders.SelectMany(x => x));

            expiredOrders = goodTillDateOrders.GetExpiredOrders(11);
            Assert.Contains(order1.OrderId, goodTillDateOrders.SelectMany(x => x.Value));
            Assert.Contains(order1.OrderId, expiredOrders.SelectMany(x => x));
        }

        [Fact]
        void Remove()
        {
            GoodTillDateOrders goodTillDateOrders = new GoodTillDateOrders();

            var expiredOrders = goodTillDateOrders.GetExpiredOrders(1);
            Assert.Null(expiredOrders);

            var order1 = new Types.Order { CancelOn = 10, OrderId = 1 };
            goodTillDateOrders.Add(order1);
            goodTillDateOrders.Remove(order1);

            expiredOrders = goodTillDateOrders.GetExpiredOrders(11);
            Assert.Null(expiredOrders);
        }

        [Fact]
        void MultipleAction()
        {
            GoodTillDateOrders goodTillDateOrders = new GoodTillDateOrders();

            var expiredOrders = goodTillDateOrders.GetExpiredOrders(1);
            Assert.Null(expiredOrders);

            var order1 = new Types.Order { CancelOn = 10, OrderId = 1 };
            goodTillDateOrders.Add(order1);

            var order2 = new Types.Order { CancelOn = 11, OrderId = 2 };
            goodTillDateOrders.Add(order2);

            var order3 = new Types.Order { CancelOn = 9, OrderId = 3 };
            goodTillDateOrders.Add(order3);

            var order4 = new Types.Order { CancelOn = 12, OrderId = 4 };
            goodTillDateOrders.Add(order4);

            var order5 = new Types.Order { CancelOn = 12, OrderId = 5 };
            goodTillDateOrders.Add(order5);

            var order6 = new Types.Order { OrderId = 6 };
            goodTillDateOrders.Add(order6);

            var order7 = new Types.Order { CancelOn = 11, OrderId = 7 };
            goodTillDateOrders.Add(order7);

            var expectedOrderIds = new[] { order1.OrderId, order1.OrderId, order2.OrderId, order3.OrderId, order4.OrderId, order5.OrderId, order7.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, goodTillDateOrders.SelectMany(x => x.Value).ToHashSet());

            expiredOrders = goodTillDateOrders.GetExpiredOrders(8);
            Assert.Null(expiredOrders);

            expiredOrders = goodTillDateOrders.GetExpiredOrders(9);
            expectedOrderIds = new[] { order3.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, expiredOrders.SelectMany(x => x).ToHashSet());

            expiredOrders = goodTillDateOrders.GetExpiredOrders(10);
            expectedOrderIds = new[] { order3.OrderId, order1.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, expiredOrders.SelectMany(x => x).ToHashSet());

            expiredOrders = goodTillDateOrders.GetExpiredOrders(11);
            expectedOrderIds = new[] { order3.OrderId, order1.OrderId, order2.OrderId, order7.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, expiredOrders.SelectMany(x => x).ToHashSet());

            expiredOrders = goodTillDateOrders.GetExpiredOrders(12);
            expectedOrderIds = new[] { order3.OrderId, order1.OrderId, order2.OrderId, order7.OrderId, order4.OrderId, order5.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, expiredOrders.SelectMany(x => x).ToHashSet());

            goodTillDateOrders.Remove(order3);
            expiredOrders = goodTillDateOrders.GetExpiredOrders(9);
            Assert.Null(expiredOrders);
            expiredOrders = goodTillDateOrders.GetExpiredOrders(10);
            expectedOrderIds = new[] { order3.OrderId, order1.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, expiredOrders.SelectMany(x => x).ToHashSet());

            goodTillDateOrders.Remove(order1);
            goodTillDateOrders.Remove(order2);
            goodTillDateOrders.Remove(order5);
            goodTillDateOrders.Remove(order7);
            expiredOrders = goodTillDateOrders.GetExpiredOrders(11);
            Assert.Null(expiredOrders);
            expiredOrders = goodTillDateOrders.GetExpiredOrders(12);
            expectedOrderIds = new[] { order4.OrderId }.ToHashSet();
            Assert.Subset(expectedOrderIds, expiredOrders.SelectMany(x => x).ToHashSet());

            goodTillDateOrders.Remove(order4);
            expiredOrders = goodTillDateOrders.GetExpiredOrders(15);
            Assert.Null(expiredOrders);
        }
    }
}
