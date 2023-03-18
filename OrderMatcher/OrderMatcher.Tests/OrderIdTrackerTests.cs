using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class OrderIdTrackerTests
    {
        [Fact]
        public void RangeOne()
        {
            OrderIdTracker orderIdTracker = new OrderIdTracker(5);
            for (int i = 0; i < 5; i++)
            {
                orderIdTracker.TryMark(i);
            }

            Assert.Equal(1, orderIdTracker.RangesCount);
            Assert.Equal(0, orderIdTracker.Ranges.First().FromOrderId);
            Assert.Equal(4, orderIdTracker.Ranges.First().ToOrderId);
        }

        [Fact]
        public void RangeMultiple()
        {
            OrderIdTracker orderIdTracker = new OrderIdTracker(5);
            for (int i = 0; i < 8; i++)
            {
                orderIdTracker.TryMark(i);
            }

            Assert.Equal(2, orderIdTracker.RangesCount);
            Assert.Equal(0, orderIdTracker.Ranges.First().FromOrderId);
            Assert.Equal(9, orderIdTracker.Ranges.Last().ToOrderId);
        }

        [Fact]
        public void CheckMarked()
        {
            OrderIdTracker orderIdTracker = new OrderIdTracker(5);
            for (int i = 0; i < 10; i++)
            {
                orderIdTracker.TryMark(i);
            }
            for (int i = 10; i < 20; i = i + 2)
            {
                orderIdTracker.TryMark(i);
            }
            for (int i = 20; i < 60; i = i + 3)
            {
                orderIdTracker.TryMark(i);
            }
            for (int i = 60; i < 100; i++)
            {
                orderIdTracker.TryMark(i);
            }

            Assert.Equal(13, orderIdTracker.RangesCount);
            Assert.Equal(0, orderIdTracker.Ranges.First().FromOrderId);
            Assert.Equal(99, orderIdTracker.Ranges.Last().ToOrderId);

            for (int i = 0; i < 10; i++)
            {
                Assert.True(orderIdTracker.IsMarked(i));
            }
            for (int i = 10; i < 20; i++)
            {
                if ((i - 10) % 2 == 0)
                    Assert.True(orderIdTracker.IsMarked(i));
                else
                    Assert.False(orderIdTracker.IsMarked(i));
            }
            for (int i = 20; i < 60; i++)
            {
                if ((i - 20) % 3 == 0)
                    Assert.True(orderIdTracker.IsMarked(i));
                else
                    Assert.False(orderIdTracker.IsMarked(i));
            }
            for (int i = 60; i < 100; i++)
            {
                Assert.True(orderIdTracker.IsMarked(i));
            }

            Assert.Equal(13, orderIdTracker.RangesCount);
            orderIdTracker.Compact();
            Assert.Equal(11, orderIdTracker.RangesCount);
            Assert.Equal(0, orderIdTracker.Ranges.First().FromOrderId);
            Assert.Equal(99, orderIdTracker.Ranges.Last().ToOrderId);

            for (int i = 0; i < 10; i++)
            {
                Assert.False(orderIdTracker.TryMark(i));
            }
            for (int i = 10; i < 20; i++)
            {
                if ((i - 10) % 2 == 0)
                    Assert.False(orderIdTracker.TryMark(i));
                else
                    Assert.True(orderIdTracker.TryMark(i));
            }
            for (int i = 20; i < 60; i++)
            {
                if ((i - 20) % 3 == 0)
                    Assert.False(orderIdTracker.TryMark(i));
                else
                    Assert.True(orderIdTracker.TryMark(i));
            }
            for (int i = 60; i < 100; i++)
            {
                Assert.False(orderIdTracker.TryMark(i));
            }


            Assert.Equal(1, orderIdTracker.RangesCount);
            Assert.Equal(0, orderIdTracker.Ranges.First().FromOrderId);
            Assert.Equal(99, orderIdTracker.Ranges.Last().ToOrderId);
            orderIdTracker.Compact();
            Assert.Equal(1, orderIdTracker.RangesCount);
            Assert.Equal(0, orderIdTracker.Ranges.First().FromOrderId);
            Assert.Equal(99, orderIdTracker.Ranges.Last().ToOrderId);
        }
    }
}
