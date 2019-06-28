namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy);
    }
}
