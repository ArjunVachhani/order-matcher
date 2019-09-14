using System;

namespace OrderMatcher
{
    public class CancelledOrderSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int orderIdOffset;
        private static readonly int remainingQuantityOffset;
        private static readonly int cancelReasonOffset;
        private static readonly int timestampOffset;

        private static readonly int sizeOfMessageLenght;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfRemainingQuantity;
        private static readonly int sizeOfCancelReason;
        private static readonly int sizeOfTimestamp;

        static CancelledOrderSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = sizeof(ulong);
            sizeOfRemainingQuantity = Quantity.SizeOfQuantity;
            sizeOfCancelReason = sizeof(CancelReason);
            sizeOfTimestamp = sizeof(ulong);

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
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
            return Serialize(cancelledOrder.OrderId, cancelledOrder.RemainingQuantity, cancelledOrder.CancelReason, cancelledOrder.Timestamp);
        }

        public static byte[] Serialize(ulong orderId, Quantity remainingQuantity, CancelReason cancelReason, long timeStamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.Cancel;
            Write(msg, versionOffset, version);
            Write(msg, orderIdOffset, orderId);
            Write(msg, remainingQuantityOffset, remainingQuantity);
            msg[cancelReasonOffset] = (byte)cancelReason;
            Write(msg, timestampOffset, timeStamp);
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
            cancelledOrder.RemainingQuantity = ReadQuantity(bytes, remainingQuantityOffset);
            cancelledOrder.CancelReason = (CancelReason)bytes[cancelReasonOffset];
            cancelledOrder.Timestamp = BitConverter.ToInt64(bytes, timestampOffset);

            return cancelledOrder;
        }
    }
}
