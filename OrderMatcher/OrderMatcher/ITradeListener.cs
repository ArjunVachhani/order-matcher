using OrderMatcher.Types;

namespace OrderMatcher
{
    public interface ITradeListener
    {
        void OnAccept(OrderId orderId, UserId userId);
        void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, UserId incomingUserId, UserId restingUserId, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Quantity? askFee, Quantity? bidCost, Quantity? bidFee);
        void OnCancel(OrderId orderId, UserId userId, Quantity remainingQuantity, Quantity cost, Quantity fee, CancelReason cancelReason);
        void OnOrderTriggered(OrderId orderId, UserId userId);
    }
}
