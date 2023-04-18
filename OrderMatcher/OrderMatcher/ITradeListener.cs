namespace OrderMatcher;

public interface ITradeListener
{
    void OnAccept(OrderId orderId, UserId userId);
    void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, UserId incomingUserId, UserId restingUserId, bool incomingOrderSide, Price matchPrice, Quantity matchQuantiy, Quantity? askRemainingQuantity, Amount? askFee, Amount? bidCost, Amount? bidFee);
    void OnCancel(OrderId orderId, UserId userId, Quantity remainingQuantity, Amount cost, Amount fee, CancelReason cancelReason);
    void OnOrderTriggered(OrderId orderId, UserId userId);
}
