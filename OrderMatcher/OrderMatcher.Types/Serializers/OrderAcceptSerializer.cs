using System;

namespace OrderMatcher.Types.Serializers
{
    public class OrderAcceptSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int orderIdOffset;
        private static readonly int timestampOffset;
        private static readonly int messageSequenceOffset;

        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfTimestamp;
        private static readonly int sizeOfMessageSequence;

        public static int MessageSize => sizeOfMessage;

        static OrderAcceptSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = OrderId.SizeOfOrderId;
            sizeOfTimestamp = sizeof(int);
            sizeOfMessageLength = sizeof(int);
            sizeOfMessageSequence = sizeof(long);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            timestampOffset = orderIdOffset + sizeOfOrderId;
            messageSequenceOffset = timestampOffset + sizeOfTimestamp;
            sizeOfMessage = messageSequenceOffset + sizeOfMessageSequence;
        }

        public static void Serialize(OrderAccept orderAccept, Span<byte> bytes)
        {
            if (orderAccept == null)
                throw new ArgumentNullException(nameof(orderAccept));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            Serialize(orderAccept.MessageSequence, orderAccept.OrderId, orderAccept.Timestamp, bytes);
        }

        public static void Serialize(long messageSequence, OrderId orderId, int timestamp, Span<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.OrderAccept;
            Write(bytes.Slice(versionOffset), (long)version);
            OrderId.WriteBytes(bytes.Slice(orderIdOffset), orderId);
            Write(bytes.Slice(timestampOffset), timestamp);
            Write(bytes.Slice(messageSequenceOffset), messageSequence);
        }

        public static OrderAccept Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != sizeOfMessage)
                throw new Exception("Order accept message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.OrderAccept)
                throw new Exception(Constant.INVALID_MESSAGE);

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));

            if (version != OrderAcceptSerializer.version)
                throw new Exception(Constant.INVALID_VERSION);

            var orderAccept = new OrderAccept();

            orderAccept.OrderId = OrderId.ReadOrderId(bytes.Slice(orderIdOffset));
            orderAccept.Timestamp = BitConverter.ToInt32(bytes.Slice(timestampOffset));
            orderAccept.MessageSequence = BitConverter.ToInt64(bytes.Slice(messageSequenceOffset));

            return orderAccept;
        }
    }
}
