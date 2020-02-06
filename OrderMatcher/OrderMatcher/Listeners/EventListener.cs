using System.Collections.Concurrent;

namespace OrderMatcher
{
    class EventListener : ITradeListener
    {
        private readonly ITimeProvider _timeProvider;
        private readonly BlockingCollection<byte[]> _outputActionBlock;
        private readonly BlockingCollection<byte[]> _fileActionBlock;
        public EventListener(ITimeProvider timeProvider, BlockingCollection<byte[]> outputActionBlock, BlockingCollection<byte[]> fileActionBlock)
        {
            _timeProvider = timeProvider;
            _outputActionBlock = outputActionBlock;
            _fileActionBlock = fileActionBlock;
        }

        public void OnCancel(OrderId orderId, Quantity remainingQuantity, Quantity remainingOrderAmount, CancelReason cancelReason)
        {
            var msg = CancelledOrderSerializer.Serialize(orderId, remainingQuantity, remainingOrderAmount, cancelReason, _timeProvider.GetSecondsFromEpoch());
            _outputActionBlock.Add(msg);
            _fileActionBlock.Add(msg);
        }

        public void OnOrderTriggered(OrderId orderId)
        {
            var msg = OrderTriggerSerializer.Serialize(orderId, _timeProvider.GetSecondsFromEpoch());
            _outputActionBlock.Add(msg);
            _fileActionBlock.Add(msg);
        }

        public void OnTrade(OrderId incomingOrderId, OrderId restingOrderId, Price matchPrice, Quantity matchQuantiy, bool incomingOrderFilled)
        {
            var msg = FillSerializer.Serialize(restingOrderId, incomingOrderId, matchPrice, matchQuantiy, _timeProvider.GetSecondsFromEpoch(), incomingOrderFilled);
            _outputActionBlock.Add(msg);
            _fileActionBlock.Add(msg);
        }
    }
}
