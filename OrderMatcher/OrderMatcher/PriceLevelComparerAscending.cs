using System.Collections.Generic;

namespace OrderMatcher
{
    internal class PriceLevelComparerAscending<T> : IComparer<T> where T : class, IPriceLevel
    {
        private PriceLevelComparerAscending() { }

        public int Compare(T? x, T? y)
        {
            return x!.Price.CompareTo(y!.Price);
        }

        private static readonly PriceLevelComparerAscending<T> _shared = new PriceLevelComparerAscending<T>();
        public static PriceLevelComparerAscending<T> Shared => _shared;
    }
}
