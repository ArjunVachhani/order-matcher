namespace OrderMatcher
{
    public interface IFeeProvider
    {
        Fee GetFee(short feeId);
    }
}
