namespace OrderMatcher
{
    public class Fill
    {
        public OrderId MakerOrderId { get; set; }
        public OrderId TakerOrderId { get; set; }
        public Price MatchRate { get; set; }
        public Quantity MatchQuantity { get; set; }
        public int Timestamp { get; set; }
        public bool IncomingOrderFilled { get; set; }
    }
}
