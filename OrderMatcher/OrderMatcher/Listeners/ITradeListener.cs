namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, Price matchPrice, Quantity matchQuantiy, bool incomingOrderCompleted);
        void OnCancel(OrderId orderId, Quantity remainingQuantity, Quantity remainingOrderAmount, CancelReason cancelReason);
        void OnOrderTriggered(OrderId orderId);
    }
}
