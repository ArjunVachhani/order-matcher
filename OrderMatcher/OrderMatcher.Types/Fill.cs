namespace OrderMatcher.Types
{
    public class Fill
    {
        public OrderId MakerOrderId { get; set; }
        public UserId MakerUserId { get; set; }
        public OrderId TakerOrderId { get; set; }
        public UserId TakerUserId { get; set; }
        public bool TakerSide { get; set; }
        public Price MatchRate { get; set; }
        public Quantity MatchQuantity { get; set; }
        public Quantity? AskRemainingQuantity { get; set; }
        public Amount? AskFee { get; set; }
        public Amount? BidCost { get; set; }
        public Amount? BidFee { get; set; }
        public int Timestamp { get; set; }
        public long MessageSequence { get; set; }
    }
}
