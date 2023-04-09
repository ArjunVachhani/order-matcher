namespace OrderMatcher.Types;

public class OrderTrigger
{
    public OrderId OrderId { get; set; }
    public UserId UserId { get; set; }
    public int Timestamp { get; set; }
    public long MessageSequence { get; set; }
}
