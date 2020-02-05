using System;

namespace OrderMatcher
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

        static OrderTriggerSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = sizeof(ulong);
            sizeOfTimestamp = sizeof(long);
            sizeOfMessageLenght = sizeof(int);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            timestampOffset = orderIdOffset + sizeOfOrderId;
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static byte[] Serialize(OrderTrigger orderTrigger)
        {
            if (orderTrigger == null)
            {
                throw new ArgumentNullException(nameof(orderTrigger));
            }

            return Serialize(orderTrigger.OrderId, orderTrigger.Timestamp);
        }

        public static byte[] Serialize(OrderId orderId, long timestamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.OrderTrigger;
            Write(msg, versionOffset, (long)version);
            Write(msg, orderIdOffset, orderId);
            Write(msg, timestampOffset, timestamp);
            return msg;
        }

        public static OrderTrigger Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Order Trigger Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.OrderTrigger)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != OrderTriggerSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var orderTrigger = new OrderTrigger();

            orderTrigger.OrderId = BitConverter.ToInt32(bytes, orderIdOffset);
            orderTrigger.Timestamp = BitConverter.ToInt64(bytes, timestampOffset);

            return orderTrigger;
        }
    }
}
