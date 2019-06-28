using System;

namespace OrderMatcher
{
    public class CancelledOrderSerializer : Serializer
    {
        private static short version;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int orderIdOffset;
        private static int remainingQuantityOffset;
        private static int cancelReasonOffset;
        private static int timestampOffset;

        private static int sizeOfMessage;
        private static int sizeOfVersion;
        private static int sizeOfMessagetType;
        private static int sizeOfOrderId;
        private static int sizeOfRemainingQuantity;
        private static int sizeOfCancelReason;
        private static int sizeOfTimestamp;

        static CancelledOrderSerializer()
        {
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = sizeof(ulong);
            sizeOfRemainingQuantity = Quantity.SizeOfQuantity;
            sizeOfCancelReason = sizeof(CancelReason);
            sizeOfTimestamp = sizeof(ulong);

            version = 1;

            messageTypeOffset = 0;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            remainingQuantityOffset = orderIdOffset + sizeOfOrderId;
            cancelReasonOffset = remainingQuantityOffset + sizeOfRemainingQuantity;
            timestampOffset = cancelReasonOffset + sizeOfCancelReason;
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static byte[] Serialize(CancelledOrder cancelledOrder)
        {
            if (cancelledOrder == null)
            {
                throw new ArgumentNullException(nameof(cancelledOrder));
            }

            byte[] msg = new byte[sizeOfMessage];
            msg[messageTypeOffset] = (byte)MessageType.Cancel;
            var versionByteArray = BitConverter.GetBytes(version);
            msg[versionOffset] = versionByteArray[0];
            msg[versionOffset + 1] = versionByteArray[1];

            CopyBytes(BitConverter.GetBytes(cancelledOrder.OrderId), msg, orderIdOffset);
            CopyBytes(BitConverter.GetBytes(cancelledOrder.RemainingQuantity), msg, remainingQuantityOffset);
            msg[cancelReasonOffset] = (byte)cancelledOrder.CancelReason;
            CopyBytes(BitConverter.GetBytes(cancelledOrder.Timestamp), msg, timestampOffset);
            return msg;
        }

        public static CancelledOrder Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Canceled Order Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.Cancel)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != CancelledOrderSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var cancelledOrder = new CancelledOrder();

            cancelledOrder.OrderId = BitConverter.ToUInt64(bytes, orderIdOffset);
            cancelledOrder.RemainingQuantity = BitConverter.ToInt32(bytes, remainingQuantityOffset);
            cancelledOrder.CancelReason = (CancelReason)bytes[cancelReasonOffset];
            cancelledOrder.Timestamp = BitConverter.ToInt64(bytes, timestampOffset);

            return cancelledOrder;
        }
    }
}
