namespace OrderMatcher
{
    public interface ITradeFeeProvider
    {
        TradeFee GetTradeFee(short tradeFeeId);
    }
}
