using System.Collections.Generic;

namespace OrderMatcher
{
    internal class PriceLevelComparerDescending<T> : IComparer<T> where T : IPriceLevel
    {
        private PriceLevelComparerDescending() { }

        public int Compare(T x, T y)
        {
            return y.Price.CompareTo(x.Price);
        }

        private static readonly PriceLevelComparerDescending<T> _shared = new PriceLevelComparerDescending<T>();
        public static PriceLevelComparerDescending<T> Shared => _shared;
    }
}
