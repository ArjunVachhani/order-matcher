namespace OrderMatcher.Types;

public class Order
{
    public bool IsBuy { get; set; }
    public OrderId OrderId { get; set; }
    public UserId UserId { get; set; }
    public ulong Sequence { get; set; }
    public Quantity OpenQuantity { get; set; }
    public Price Price { get; set; }
    public int CancelOn { get; set; }
    public Amount Cost { get; set; }
    public Amount Fee { get; set; }
    public short FeeId { get; set; }
    public Quantity TipQuantity { get; set; }
    public Quantity TotalQuantity { get; set; }
    public Amount OrderAmount { get; set; }
    public Price StopPrice { get; set; }
    public OrderCondition OrderCondition { get; set; }
    public bool IsFilled
    {
        get
        {
            return OpenQuantity == 0;
        }
    }
    public bool IsStop
    {
        get
        {
            return StopPrice > 0;
        }
    }
    public bool IsTip
    {
        get
        {
            return TipQuantity > 0 && TotalQuantity > 0;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is Order order)
        {
            return OrderId == order.OrderId;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return OrderId.GetHashCode();
    }
}
