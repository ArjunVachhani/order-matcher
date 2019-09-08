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

        public void OnCancel(ulong orderId, Quantity remainingQuantity, Quantity remainingLockedAmount, CancelReason cancelReason)
        {
            var msg = CancelledOrderSerializer.Serialize(orderId, remainingQuantity, cancelReason, _timeProvider.GetUpochMilliseconds());
            _outputActionBlock.Add(msg);
            _fileActionBlock.Add(msg);
        }

        public void OnOrderTriggered(ulong orderId)
        {
            var msg = TriggerSerializer.Serialize(orderId, _timeProvider.GetUpochMilliseconds());
            _outputActionBlock.Add(msg);
            _fileActionBlock.Add(msg);
        }

        public void OnTrade(ulong incomingOrderId, ulong restingOrderId, Price matchPrice, Quantity matchQuantiy)
        {
            var msg = FillSerializer.Serialize(restingOrderId, incomingOrderId, matchPrice, matchQuantiy, _timeProvider.GetUpochMilliseconds());
            _outputActionBlock.Add(msg);
            _fileActionBlock.Add(msg);
        }
    }
}
