namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnAccept(OrderId orderId);
        void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Quantity? bidCost);
        void OnCancel(OrderId orderId, Quantity remainingQuantity, Quantity cost, CancelReason cancelReason);
        void OnOrderTriggered(OrderId orderId);
    }
}
