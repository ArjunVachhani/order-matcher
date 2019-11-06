namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy, bool incomingOrderCompleted);
        void OnCancel(ulong orderId, Quantity remainingQuantity, Quantity remainingOrderAmount, CancelReason cancelReason);
        void OnOrderTriggered(ulong orderId);
    }
}
