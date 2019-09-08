namespace OrderMatcher
{
    public class TradeFee
    {
        public short TradeFeeId { get; set; }
        public decimal TakerFee { get; set; }
        public decimal MakerFee { get; set; }
        public FeeCurreny FeeCurreny { get; set; }
    }

    public enum FeeCurreny
    {
        AlwaysQuoteCurrency,
        RecievingCurrency
    }

}
