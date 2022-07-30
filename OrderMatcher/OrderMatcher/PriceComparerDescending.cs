using OrderMatcher.Types;
using System.Collections.Generic;

namespace OrderMatcher
{
    public class PriceComparerDescending : IComparer<Price>
    {
        private PriceComparerDescending() { }

        public int Compare(Price x, Price y)
        {
            return y.CompareTo(x);
        }

        private static readonly PriceComparerDescending _shared = new PriceComparerDescending();
        public static PriceComparerDescending Shared => _shared;
    }
}
