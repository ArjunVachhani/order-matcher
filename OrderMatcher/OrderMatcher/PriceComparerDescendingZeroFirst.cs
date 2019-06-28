using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public class PriceComparerDescendingZeroFirst : IComparer<Price>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(Price x, Price y)
        {
            if (((x > y) && (x != 0 && y != 0)) || (x == 0 && y != 0))
            {
                return -1;
            }
            else if (((x < y) && (x != 0 && y != 0)) || (y == 0 && x != 0))
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
