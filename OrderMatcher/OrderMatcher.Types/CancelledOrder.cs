namespace OrderMatcher.Types
{
    public class CancelledOrder
    {
        public OrderId OrderId { get; set; }
        public UserId UserId { get; set; }
        public Quantity RemainingQuantity { get; set; }
        public Quantity Cost { get; set; }
        public Quantity Fee { get; set; }
        public CancelReason CancelReason { get; set; }
        public int Timestamp { get; set; }
        public long MessageSequence { get; set; }
    }
}
