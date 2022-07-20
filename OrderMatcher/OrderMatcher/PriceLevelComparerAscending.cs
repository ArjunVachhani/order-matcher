using System.Collections.Generic;

namespace OrderMatcher
{
    internal class PriceLevelComparerAscending<T> : IComparer<T> where T : IPriceLevel
    {
        public int Compare(T x, T y)
        {
            if (x.Price < y.Price)
                return -1;
            else if (x.Price > y.Price)
                return 1;
            else
                return 0;
        }
    }
}
