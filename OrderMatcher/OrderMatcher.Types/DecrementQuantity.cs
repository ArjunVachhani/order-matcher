namespace OrderMatcher.Types;

public class DecrementQuantity
{
    public OrderId OrderId { get; set; }
    public UserId UserId { get; set; }
    public Quantity QuantityDecremented { get; set; }
    public long MessageSequence { get; set; }
}
