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
        private static readonly int costOffset;
        private static readonly int cancelReasonOffset;
        private static readonly int timestampOffset;

        private static readonly int sizeOfMessageLenght;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfRemainingQuantity;
        private static readonly int sizeOfCost;
        private static readonly int sizeOfCancelReason;
        private static readonly int sizeOfTimestamp;

        public static int MessageSize => sizeOfMessage;

        static CancelledOrderSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = sizeof(ulong);
            sizeOfRemainingQuantity = Quantity.SizeOfQuantity;
            sizeOfCost = Quantity.SizeOfQuantity;
            sizeOfCancelReason = sizeof(CancelReason);
            sizeOfTimestamp = sizeof(int);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            remainingQuantityOffset = orderIdOffset + sizeOfOrderId;
            costOffset = remainingQuantityOffset + sizeOfRemainingQuantity;
            cancelReasonOffset = costOffset + sizeOfCost;
            timestampOffset = cancelReasonOffset + sizeOfCancelReason;
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static byte[] Serialize(CancelledOrder cancelledOrder)
        {
            if (cancelledOrder == null)
            {
                throw new ArgumentNullException(nameof(cancelledOrder));
            }
            return Serialize(cancelledOrder.OrderId, cancelledOrder.RemainingQuantity, cancelledOrder.Cost, cancelledOrder.CancelReason, cancelledOrder.Timestamp);
        }

        public static byte[] Serialize(OrderId orderId, Quantity remainingQuantity, Quantity cost, CancelReason cancelReason, int timeStamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.Cancel;
            Write(msg, versionOffset, version);
            Write(msg, orderIdOffset, orderId);
            Write(msg, remainingQuantityOffset, remainingQuantity);
            msg[cancelReasonOffset] = (byte)cancelReason;
            Write(msg, timestampOffset, timeStamp);
            Write(msg, costOffset, cost);
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

            cancelledOrder.OrderId = BitConverter.ToInt32(bytes, orderIdOffset);
            cancelledOrder.RemainingQuantity = ReadQuantity(bytes, remainingQuantityOffset);
            cancelledOrder.CancelReason = (CancelReason)bytes[cancelReasonOffset];
            cancelledOrder.Timestamp = BitConverter.ToInt32(bytes, timestampOffset);
            cancelledOrder.Cost = ReadQuantity(bytes, costOffset);

            return cancelledOrder;
        }
    }
}
