using OrderMatcher.Types;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class PriceComparerAscending : IComparer<Price>
    {
        private PriceComparerAscending() { }

        public int Compare(Price x, Price y)
        {
            return x.CompareTo(y);
        }

        private static readonly PriceComparerAscending _shared = new PriceComparerAscending();
        public static PriceComparerAscending Shared => _shared;
    }
}
