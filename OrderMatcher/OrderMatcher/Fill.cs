namespace OrderMatcher
{
    public class Fill
    {
        public ulong MakerOrderId { get; set; }
        public ulong TakerOrderId { get; set; }
        public Price MatchRate { get; set; }
        public Quantity MatchQuantity { get; set; }
        public long Timestamp { get; set; }
    }
}
