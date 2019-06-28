using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public class OrderSequenceComparer : IComparer<Order>
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Compare(Order x, Order y)
        {
            if (x.Sequnce < y.Sequnce)
            {
                return -1;
            }
            else if (x.Sequnce > y.Sequnce)
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
