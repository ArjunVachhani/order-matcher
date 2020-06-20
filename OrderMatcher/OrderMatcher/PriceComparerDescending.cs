using OrderMatcher.Types;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public class PriceComparerDescending : IComparer<Price>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(Price x, Price y)
        {
            if (x > y)
            {
                return -1;
            }
            else if (x < y)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
