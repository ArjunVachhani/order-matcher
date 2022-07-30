using OrderMatcher.Types;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OrderMatcher.Tests
{
    public class PriceComparerAscendingTests
    {
        [Fact]
        public void Compare_ReturnsPositive_IfSecondArguementIsSmaller()
        {
            PriceComparerAscending comparer = PriceComparerAscending.Shared;
            Price p1 = new Price(2);
            Price p2 = new Price(1);
            int result = comparer.Compare(p1, p2);
            Assert.True(result > 0, "Result should be greater than 0");
        }

        [Fact]
        public void Compare_ReturnsNegative_IfSecondArguementIsLarger()
        {
            PriceComparerAscending comparer = PriceComparerAscending.Shared;
            Price p1 = new Price(1);
            Price p2 = new Price(2);
            int result = comparer.Compare(p1, p2);
            Assert.True(result < 0, "Result should be less than 0");
        }

        [Fact]
        public void Compare_Returns0_IfBothArguementAreEqual()
        {
            PriceComparerAscending comparer = PriceComparerAscending.Shared;
            Price p1 = new Price(1);
            Price p2 = new Price(1);
            int result = comparer.Compare(p1, p2);
            Assert.True(result == 0, "Result should be equal to 0");
        }

        [Fact]
        public void SortedDictionarySortsAscendingPriceLevel()
        {
            PriceComparerAscending comparer = PriceComparerAscending.Shared;
            SortedDictionary<Price, PriceLevel> sortedDictionary = new SortedDictionary<Price, PriceLevel>(comparer);
            Price price1 = new Price(1);
            PriceLevel level1 = new PriceLevel(price1);
            sortedDictionary.Add(price1, level1);

            Price price4 = new Price(4);
            PriceLevel level4 = new PriceLevel(price4);
            sortedDictionary.Add(price4, level4);

            Price price3 = new Price(3);
            PriceLevel level3 = new PriceLevel(price3);
            sortedDictionary.Add(price3, level3);

            Price price2 = new Price(2);
            PriceLevel level2 = new PriceLevel(price2);
            sortedDictionary.Add(price2, level2);

            Price price0 = new Price(0);
            PriceLevel level0 = new PriceLevel(price0);
            sortedDictionary.Add(price0, level0);

            List<PriceLevel> expectedPriceLevelSortOrder = new List<PriceLevel>() { level0, level1, level2, level3, level4 };
            AssertHelper.SequentiallyEqual(expectedPriceLevelSortOrder, sortedDictionary.Values.ToList());

            List<Price> expectedPriceSortOrder = new List<Price>() { price0, price1, price2, price3, price4 };
            AssertHelper.SequentiallyEqual(expectedPriceSortOrder, sortedDictionary.Keys.ToList());
        }

        [Fact]
        public void SortedDictionarySortsAscendingQuantityTrackingPriceLevel()
        {
            PriceComparerAscending comparer = PriceComparerAscending.Shared;
            SortedDictionary<Price, QuantityTrackingPriceLevel> sortedDictionary = new SortedDictionary<Price, QuantityTrackingPriceLevel>(comparer);
            Price price1 = new Price(1);
            QuantityTrackingPriceLevel level1 = new QuantityTrackingPriceLevel(price1);
            sortedDictionary.Add(price1, level1);

            Price price4 = new Price(4);
            QuantityTrackingPriceLevel level4 = new QuantityTrackingPriceLevel(price4);
            sortedDictionary.Add(price4, level4);

            Price price3 = new Price(3);
            QuantityTrackingPriceLevel level3 = new QuantityTrackingPriceLevel(price3);
            sortedDictionary.Add(price3, level3);

            Price price2 = new Price(2);
            QuantityTrackingPriceLevel level2 = new QuantityTrackingPriceLevel(price2);
            sortedDictionary.Add(price2, level2);

            Price price0 = new Price(0);
            QuantityTrackingPriceLevel level0 = new QuantityTrackingPriceLevel(price0);
            sortedDictionary.Add(price0, level0);

            List<QuantityTrackingPriceLevel> expectedPriceLevelSortOrder = new List<QuantityTrackingPriceLevel>() { level0, level1, level2, level3, level4 };
            AssertHelper.SequentiallyEqual(expectedPriceLevelSortOrder, sortedDictionary.Values.ToList());

            List<Price> expectedPriceSortOrder = new List<Price>() { price0, price1, price2, price3, price4 };
            AssertHelper.SequentiallyEqual(expectedPriceSortOrder, sortedDictionary.Keys.ToList());
        }
    }
}
