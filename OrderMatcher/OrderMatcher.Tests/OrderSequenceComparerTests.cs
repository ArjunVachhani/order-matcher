using OrderMatcher.Types;
using System.Collections.Generic;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderSequenceComparerTests
    {
        [Fact]
        public void Compare_ReturnsNegativeValue_IfSecondItemIsLarger()
        {
            OrderSequenceComparer comparer = new OrderSequenceComparer();
            Order order1 = new Order() { Sequnce = 1 };
            Order order2 = new Order() { Sequnce = 2 };

            Assert.True(comparer.Compare(order1, order2) < 0, "it should return negative value");
        }

        [Fact]
        public void Compare_ReturnsPositiveValue_IfSecondItemIsSmaller()
        {
            OrderSequenceComparer comparer = new OrderSequenceComparer();
            Order order1 = new Order() { Sequnce = 2 };
            Order order2 = new Order() { Sequnce = 1 };

            Assert.True(comparer.Compare(order1, order2) > 0, "it should return positive value");
        }

        [Fact]
        public void Compare_Returns0Value_IfBothItemIsEqual()
        {
            OrderSequenceComparer comparer = new OrderSequenceComparer();
            Order order1 = new Order() { Sequnce = 2 };
            Order order2 = new Order() { Sequnce = 2 };

            Assert.True(comparer.Compare(order1, order2) == 0, "it should return 0");
        }

        [Fact]
        public void PriceLevelSortsOrderBasedOnSequence()
        {
            OrderSequenceComparer comparer = new OrderSequenceComparer();
            Price price = new Price(1);
            PriceLevel priceLevel = new PriceLevel(price);

            Order order1 = new Order() { Sequnce = 1 };
            priceLevel.AddOrder(order1);

            Order order3 = new Order() { Sequnce = 3 };
            priceLevel.AddOrder(order3);

            Order order2 = new Order() { Sequnce = 2 };
            priceLevel.AddOrder(order2);

            Order order7 = new Order() { Sequnce = 7 };
            priceLevel.AddOrder(order7);

            Order order6 = new Order() { Sequnce = 6 };
            priceLevel.AddOrder(order6);

            Order order4 = new Order() { Sequnce = 4 };
            priceLevel.AddOrder(order4);

            Order order5 = new Order() { Sequnce = 5 };
            priceLevel.AddOrder(order5);

            List<Order> expectedSequence = new List<Order> { order1, order2, order3, order4, order5, order6, order7 };
            AssertHelper.SequentiallyEqual(expectedSequence, priceLevel);
        }
    }
}
