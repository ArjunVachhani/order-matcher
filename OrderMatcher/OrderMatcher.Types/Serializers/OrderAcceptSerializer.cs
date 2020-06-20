using System;
using System.Diagnostics.CodeAnalysis;

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

        private static readonly int sizeOfMessageLenght;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfTimestamp;

        public static int MessageSize => sizeOfMessage;

        [SuppressMessage("Microsoft.Performance", "CA1810")]
        static OrderAcceptSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = OrderId.SizeOfOrderId;
            sizeOfTimestamp = sizeof(int);
            sizeOfMessageLenght = sizeof(int);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            timestampOffset = orderIdOffset + sizeOfOrderId;
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static void Serialize(OrderAccept orderAccept, Span<byte> bytes)
        {
            if (orderAccept == null)
                throw new ArgumentNullException(nameof(orderAccept));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            Serialize(orderAccept.OrderId, orderAccept.Timestamp, bytes);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static void Serialize(OrderId orderId, int timestamp, Span<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.OrderAccept;
            Write(bytes.Slice(versionOffset), (long)version);
            Write(bytes.Slice(orderIdOffset), orderId);
            Write(bytes.Slice(timestampOffset), timestamp);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
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

            orderAccept.OrderId = BitConverter.ToInt32(bytes.Slice(orderIdOffset));
            orderAccept.Timestamp = BitConverter.ToInt32(bytes.Slice(timestampOffset));

            return orderAccept;
        }
    }
}
