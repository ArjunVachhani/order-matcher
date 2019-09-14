using System;

namespace OrderMatcher
{
    [Obsolete("pending design implementation", true)]
    public interface ITradeFeeProvider
    {
        TradeFee GetTradeFee(short tradeFeeId);
    }
}
