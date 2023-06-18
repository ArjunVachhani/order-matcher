namespace OrderMatcher.Types;

public class SelfMatch
{
    public OrderId IncomingOrderId { get; set; }
    public OrderId RestingOrderId { get; set; }
    public UserId UserId { get; set; }
    public long MessageSequence { get; set; }
}
