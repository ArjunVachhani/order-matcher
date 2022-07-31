namespace OrderMatcher.Types
{
    public class CancelledOrder
    {
        public OrderId OrderId { get; set; }
        public UserId UserId { get; set; }
        public Quantity RemainingQuantity { get; set; }
        public Amount Cost { get; set; }
        public Amount Fee { get; set; }
        public CancelReason CancelReason { get; set; }
        public int Timestamp { get; set; }
        public long MessageSequence { get; set; }
    }
}
