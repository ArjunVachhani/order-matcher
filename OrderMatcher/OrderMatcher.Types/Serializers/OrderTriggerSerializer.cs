using System;
using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher.Types.Serializers
{
    public class OrderTriggerSerializer : Serializer
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
        static OrderTriggerSerializer()
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

        public static void Serialize(OrderTrigger orderTrigger, Span<byte> bytes)
        {
            if (orderTrigger == null)
            {
                throw new ArgumentNullException(nameof(orderTrigger));
            }

            Serialize(orderTrigger.OrderId, orderTrigger.Timestamp, bytes);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static void Serialize(OrderId orderId, int timestamp, Span<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < MessageSize)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset, 4), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.OrderTrigger;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(orderIdOffset), orderId);
            Write(bytes.Slice(timestampOffset), timestamp);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static OrderTrigger Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != MessageSize)
                throw new Exception("Order Trigger Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.OrderTrigger)
                throw new Exception(Constant.INVALID_MESSAGE);

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));
            if (version != OrderTriggerSerializer.version)
                throw new Exception(Constant.INVALID_VERSION);

            var orderTrigger = new OrderTrigger();

            orderTrigger.OrderId = BitConverter.ToInt32(bytes.Slice(orderIdOffset));
            orderTrigger.Timestamp = BitConverter.ToInt32(bytes.Slice(timestampOffset));

            return orderTrigger;
        }
    }
}
