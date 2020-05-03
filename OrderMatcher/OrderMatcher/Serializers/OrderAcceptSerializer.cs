using System;

namespace OrderMatcher
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

        public static byte[] Serialize(OrderAccept orderAccept)
        {
            if (orderAccept == null)
            {
                throw new ArgumentNullException(nameof(orderAccept));
            }

            return Serialize(orderAccept.OrderId, orderAccept.Timestamp);
        }

        public static byte[] Serialize(OrderId orderId, int timestamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.OrderAccept;
            Write(msg, versionOffset, (long)version);
            Write(msg, orderIdOffset, orderId);
            Write(msg, timestampOffset, timestamp);
            return msg;
        }

        public static OrderAccept Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Order accept message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.OrderAccept)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != OrderAcceptSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var orderAccept = new OrderAccept();

            orderAccept.OrderId = BitConverter.ToInt32(bytes, orderIdOffset);
            orderAccept.Timestamp = BitConverter.ToInt32(bytes, timestampOffset);

            return orderAccept;
        }
    }
}
