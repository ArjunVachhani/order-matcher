using System;
using System.Collections.Generic;
using System.Text;

namespace OrderMatcher
{
    class EventListener : ITradeListener, ICancelListener, IOrderTriggerListener
    {
        private readonly ITradeLogger _tradeLogger;
        private readonly ITimeProvider _timeProvider;
        public EventListener(ITradeLogger tradeLogger, ITimeProvider timeProvider)
        {
            _tradeLogger = tradeLogger;
            _timeProvider = timeProvider;
        }

        public void OnCancel(ulong orderId, Quantity remainingQuantity, CancelReason cancelReason)
        {
            var msg = CancelledOrderSerializer.Serialize(orderId, remainingQuantity, cancelReason, _timeProvider.GetUpochMilliseconds());
            _tradeLogger.Log(msg);
            //TODO add to queue;
        }

        public void OnOrderTriggered(ulong orderId)
        {
            var msg = TriggerSerializer.Serialize(orderId, _timeProvider.GetUpochMilliseconds());
            _tradeLogger.Log(msg);
            //TODO add to queue;
        }

        public void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy)
        {
            var msg = FillSerializer.Serialize(restingOrderId, incomingOrderId, matchPrice, matchQuantiy, _timeProvider.GetUpochMilliseconds());
            _tradeLogger.Log(msg);
            //TODO add to queue;
        }
    }
}
