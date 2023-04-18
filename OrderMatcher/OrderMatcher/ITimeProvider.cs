using System.Runtime.CompilerServices;

namespace OrderMatcher;

public interface ITimeProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetSecondsFromEpoch();
}
