namespace OrderMatcher
{
    public class CancelledOrder
    {
        public OrderId OrderId { get; set; }
        public Quantity RemainingQuantity { get; set; }
        public Quantity RemainingOrderAmount { get; set; }
        public CancelReason CancelReason { get; set; }
        public int Timestamp { get; set; }
    }
}
