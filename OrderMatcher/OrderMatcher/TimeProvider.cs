using System;
using System.Runtime.CompilerServices;

namespace OrderMatcher
{
    public class TimeProvider : ITimeProvider
    {
        private readonly DateTime Jan1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetUpochMilliseconds()
        {
            return (long)DateTime.UtcNow.Subtract(Jan1970).TotalMilliseconds;
        }
    }
}
