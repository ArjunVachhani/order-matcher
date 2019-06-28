namespace OrderMatcher
{
    public class CancelledOrder
    {
        public ulong OrderId { get; set; }
        public Quantity RemainingQuantity { get; set; }
        public CancelReason CancelReason { get; set; }
        public long Timestamp { get; set; }
    }
}
