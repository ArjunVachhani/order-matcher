namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy);
        void OnCancel(ulong orderId, Quantity remainingQuantity, CancelReason cancelReason);
        void OnOrderTriggered(ulong orderId);
    }
}
