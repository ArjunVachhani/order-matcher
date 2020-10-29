using System;
using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher.Types.Serializers
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
        private static readonly int feeOffset;
        private static readonly int cancelReasonOffset;
        private static readonly int timestampOffset;
        private static readonly int messageSequenceOffset;

        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfRemainingQuantity;
        private static readonly int sizeOfCost;
        private static readonly int sizeOfFee;
        private static readonly int sizeOfCancelReason;
        private static readonly int sizeOfTimestamp;
        private static readonly int sizeOfMessageSequence;

        public static int MessageSize => sizeOfMessage;

        [SuppressMessage("Microsoft.Performance", "CA1810")]
        static CancelledOrderSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = sizeof(ulong);
            sizeOfRemainingQuantity = Quantity.SizeOfQuantity;
            sizeOfCost = Quantity.SizeOfQuantity;
            sizeOfFee = Quantity.SizeOfQuantity;
            sizeOfCancelReason = sizeof(CancelReason);
            sizeOfTimestamp = sizeof(int);
            sizeOfMessageSequence = sizeof(long);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            remainingQuantityOffset = orderIdOffset + sizeOfOrderId;
            costOffset = remainingQuantityOffset + sizeOfRemainingQuantity;
            feeOffset = costOffset + sizeOfCost;
            cancelReasonOffset = feeOffset + sizeOfFee;
            timestampOffset = cancelReasonOffset + sizeOfCancelReason;
            messageSequenceOffset = timestampOffset + sizeOfTimestamp;
            sizeOfMessage = messageSequenceOffset + sizeOfMessageSequence;
        }

        public static void Serialize(CancelledOrder cancelledOrder, Span<byte> bytes)
        {
            if (cancelledOrder == null)
                throw new ArgumentNullException(nameof(cancelledOrder));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            Serialize(cancelledOrder.MessageSequence, cancelledOrder.OrderId, cancelledOrder.RemainingQuantity, cancelledOrder.Cost, cancelledOrder.Fee, cancelledOrder.CancelReason, cancelledOrder.Timestamp, bytes);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static void Serialize(long messageSequence, OrderId orderId, Quantity remainingQuantity, Quantity cost, Quantity fee, CancelReason cancelReason, int timeStamp, Span<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.Cancel;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(orderIdOffset), orderId);
            Write(bytes.Slice(remainingQuantityOffset), remainingQuantity);
            bytes[cancelReasonOffset] = (byte)cancelReason;
            Write(bytes.Slice(timestampOffset), timeStamp);
            Write(bytes.Slice(costOffset), cost);
            Write(bytes.Slice(feeOffset), fee);
            Write(bytes.Slice(messageSequenceOffset), messageSequence);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static CancelledOrder Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != sizeOfMessage)
                throw new Exception("Canceled Order Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.Cancel)
                throw new Exception(Constant.INVALID_MESSAGE);

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));

            if (version != CancelledOrderSerializer.version)
                throw new Exception(Constant.INVALID_VERSION);

            var cancelledOrder = new CancelledOrder();

            cancelledOrder.OrderId = BitConverter.ToInt32(bytes.Slice(orderIdOffset));
            cancelledOrder.RemainingQuantity = ReadQuantity(bytes.Slice(remainingQuantityOffset));
            cancelledOrder.CancelReason = (CancelReason)bytes[cancelReasonOffset];
            cancelledOrder.Timestamp = BitConverter.ToInt32(bytes.Slice(timestampOffset));
            cancelledOrder.Cost = ReadQuantity(bytes.Slice(costOffset));
            cancelledOrder.Fee = ReadQuantity(bytes.Slice(feeOffset));
            cancelledOrder.MessageSequence = BitConverter.ToInt64(bytes.Slice(messageSequenceOffset));

            return cancelledOrder;
        }
    }
}
