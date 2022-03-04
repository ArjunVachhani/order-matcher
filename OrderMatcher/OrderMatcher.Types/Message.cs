using System;

namespace OrderMatcher.Types
{
    public readonly struct Message
    {
        private readonly MessageType _type;
        private readonly ReadOnlyMemory<byte> _body;
        private readonly object _object;
        private readonly ulong _deliveryTag;

        public Message(MessageType type, ReadOnlyMemory<byte> body, object obj, ulong deliveryTag)
        {
            _type = type;
            _body = body;
            _object = obj;
            _deliveryTag = deliveryTag;
        }

        public MessageType MessageType => _type;
        public ReadOnlyMemory<byte> Bytes => _body;
        public object Object => _object;
        public ulong DeliveryTag => _deliveryTag;
    }
}
