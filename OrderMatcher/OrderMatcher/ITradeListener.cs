using OrderMatcher.Types;

namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnAccept(OrderId orderId);
        void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Quantity? askFee, Quantity? bidCost, Quantity? bidFee);
        void OnCancel(OrderId orderId, Quantity remainingQuantity, Quantity cost, Quantity fee, CancelReason cancelReason);
        void OnOrderTriggered(OrderId orderId);
    }
}
