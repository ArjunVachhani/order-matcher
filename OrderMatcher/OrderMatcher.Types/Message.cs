using System;

namespace OrderMatcher.Types
{
    public readonly struct Message
    {
        private readonly MessageType _type;
        private readonly object _object;
        private readonly ulong _deliveryTag;

        public Message(MessageType type, object obj, ulong deliveryTag)
        {
            _type = type;
            _object = obj;
            _deliveryTag = deliveryTag;
        }

        public MessageType MessageType => _type;
        public object Object => _object;
        public ulong DeliveryTag => _deliveryTag;
    }
}
