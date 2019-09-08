namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy);
        void OnCancel(ulong orderId, Quantity remainingQuantity, Quantity remainingLockedAmount, CancelReason cancelReason);
        void OnOrderTriggered(ulong orderId);
    }
}
