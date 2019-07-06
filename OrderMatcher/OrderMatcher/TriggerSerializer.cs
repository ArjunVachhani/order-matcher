using System;

namespace OrderMatcher
{
    public class TriggerSerializer : Serializer
    {
        private static short version;
        private static int messageLengthOffset;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int orderIdOffset;
        private static int timestampOffset;

        private static int sizeOfMessageLenght;
        private static int sizeOfMessage;
        private static int sizeOfVersion;
        private static int sizeOfMessagetType;
        private static int sizeOfOrderId;
        private static int sizeOfTimestamp;

        static TriggerSerializer()
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

        public static byte[] Serialize(ulong orderId, long timestamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            WriteInt(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.OrderTrigger;
            WriteLong(msg, versionOffset, version);
            WriteULong(msg, orderIdOffset, orderId);
            WriteLong(msg, timestampOffset, timestamp);
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
            if (version != TriggerSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var orderTrigger = new OrderTrigger();

            orderTrigger.OrderId = BitConverter.ToUInt64(bytes, orderIdOffset);
            orderTrigger.Timestamp = BitConverter.ToInt64(bytes, timestampOffset);

            return orderTrigger;
        }
    }
}
