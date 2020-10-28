using System;
using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher.Types.Serializers
{
    public class FillSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int makerOrderIdOffset;
        private static readonly int takerOrderIdOffset;
        private static readonly int matchRateOffset;
        private static readonly int matchQuantityOffset;
        private static readonly int timestampOffset;
        private static readonly int isAskRemainingNullOffset;
        private static readonly int askRemainingQuantityOffset;
        private static readonly int isBidCostNullOffset;
        private static readonly int bidCostOffset;
        private static readonly int isBidFeeNullOffset;
        private static readonly int bidFeeOffset;
        private static readonly int isAskFeeNullOffset;
        private static readonly int askFeeOffset;
        private static readonly int messageSequenceOffset;

        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfMakerOrderId;
        private static readonly int sizeOfTakerOrderId;
        private static readonly int sizeOfMatchRate;
        private static readonly int sizeOfMatchQuantity;
        private static readonly int sizeOfTimestamp;
        private static readonly int sizeOfAskRemainingQuantity;
        private static readonly int sizeOfAskFee;
        private static readonly int sizeOfBidCost;
        private static readonly int sizeOfBidFee;
        private static readonly int sizeOfIsAskRemainingNull;
        private static readonly int sizeOfIsAskFeeNull;
        private static readonly int sizeOfIsBidCostNull;
        private static readonly int sizeOfIsBidFeeNull;
        private static readonly int sizeOfMessageSequence;

        public static int MessageSize => sizeOfMessage;

        [SuppressMessage("Microsoft.Performance", "CA1810")]
        static FillSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfMakerOrderId = sizeof(ulong);
            sizeOfTakerOrderId = sizeof(ulong);
            sizeOfMatchRate = Price.SizeOfPrice;
            sizeOfMatchQuantity = Quantity.SizeOfQuantity;
            sizeOfAskRemainingQuantity = Quantity.SizeOfQuantity;
            sizeOfBidCost = Quantity.SizeOfQuantity;
            sizeOfTimestamp = sizeof(int);
            sizeOfIsAskRemainingNull = sizeof(bool);
            sizeOfIsAskFeeNull = sizeof(bool);
            sizeOfAskFee = Quantity.SizeOfQuantity;
            sizeOfIsBidFeeNull = sizeof(bool);
            sizeOfBidFee = Quantity.SizeOfQuantity;
            sizeOfIsBidCostNull = sizeof(bool);
            sizeOfMessageSequence = sizeof(long);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            makerOrderIdOffset = versionOffset + sizeOfVersion;
            takerOrderIdOffset = makerOrderIdOffset + sizeOfMakerOrderId;
            matchRateOffset = takerOrderIdOffset + sizeOfTakerOrderId;
            matchQuantityOffset = matchRateOffset + sizeOfMatchRate;
            isAskRemainingNullOffset = matchQuantityOffset + sizeOfMatchQuantity;
            askRemainingQuantityOffset = isAskRemainingNullOffset + sizeOfIsAskRemainingNull;
            isAskFeeNullOffset = askRemainingQuantityOffset + sizeOfAskRemainingQuantity;
            askFeeOffset = isAskFeeNullOffset + sizeOfIsAskFeeNull;
            isBidCostNullOffset = askFeeOffset + sizeOfAskFee;
            bidCostOffset = isBidCostNullOffset + sizeOfIsBidCostNull;
            isBidFeeNullOffset = bidCostOffset + sizeOfBidCost;
            bidFeeOffset = isBidFeeNullOffset + sizeOfIsBidFeeNull;
            timestampOffset = bidFeeOffset + sizeOfBidFee;
            messageSequenceOffset = timestampOffset + sizeOfTimestamp;
            sizeOfMessage = messageSequenceOffset + sizeOfMessageSequence;
        }

        public static void Serialize(Fill fill, Span<byte> bytes)
        {
            if (fill == null)
                throw new ArgumentNullException(nameof(fill));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            Serialize(fill.MessageSequence, fill.MakerOrderId, fill.TakerOrderId, fill.MatchRate, fill.MatchQuantity, fill.AskRemainingQuantity, fill.AskFee, fill.BidCost, fill.BidFee, fill.Timestamp, bytes);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static void Serialize(long messageSequence, OrderId makerOrderId, OrderId takerOrderId, Price matchRate, Quantity matchQuantity, Quantity? remainingAskQuantiy, Quantity? askFee, Quantity? bidCost, Quantity? bidFee, int timeStamp, Span<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.Fill;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(makerOrderIdOffset), makerOrderId);
            Write(bytes.Slice(takerOrderIdOffset), takerOrderId);
            Write(bytes.Slice(matchRateOffset), matchRate);
            Write(bytes.Slice(matchQuantityOffset), matchQuantity);
            bytes[isAskRemainingNullOffset] = Convert.ToByte(remainingAskQuantiy.HasValue ? true : false);

            if (remainingAskQuantiy.HasValue)
                Write(bytes.Slice(askRemainingQuantityOffset), remainingAskQuantiy.Value);

            bytes[isAskFeeNullOffset] = Convert.ToByte(askFee.HasValue ? true : false);

            if (askFee.HasValue)
                Write(bytes.Slice(askFeeOffset), askFee.Value);

            bytes[isBidCostNullOffset] = Convert.ToByte(bidCost.HasValue ? true : false);

            if (bidCost.HasValue)
                Write(bytes.Slice(bidCostOffset), bidCost.Value);

            bytes[isBidFeeNullOffset] = Convert.ToByte(bidFee.HasValue ? true : false);

            if (bidFee.HasValue)
                Write(bytes.Slice(bidFeeOffset), bidFee.Value);

            Write(bytes.Slice(timestampOffset), timeStamp);
            Write(bytes.Slice(messageSequenceOffset), messageSequence);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static Fill Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != sizeOfMessage)
                throw new Exception("Fill Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.Fill)
                throw new Exception(Constant.INVALID_MESSAGE);

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));

            if (version != FillSerializer.version)
                throw new Exception(Constant.INVALID_VERSION);

            var fill = new Fill();
            fill.MakerOrderId = BitConverter.ToInt32(bytes.Slice(makerOrderIdOffset));
            fill.TakerOrderId = BitConverter.ToInt32(bytes.Slice(takerOrderIdOffset));
            fill.MatchRate = ReadPrice(bytes.Slice(matchRateOffset));
            fill.MatchQuantity = ReadQuantity(bytes.Slice(matchQuantityOffset));
            fill.Timestamp = BitConverter.ToInt32(bytes.Slice(timestampOffset));
            fill.MessageSequence = BitConverter.ToInt64(bytes.Slice(messageSequenceOffset));

            if (Convert.ToBoolean(bytes[isAskRemainingNullOffset]))
                fill.AskRemainingQuantity = ReadQuantity(bytes.Slice(askRemainingQuantityOffset));

            if (Convert.ToBoolean(bytes[isAskFeeNullOffset]))
                fill.AskFee = ReadQuantity(bytes.Slice(askFeeOffset));

            if (Convert.ToBoolean(bytes[isBidCostNullOffset]))
                fill.BidCost = ReadQuantity(bytes.Slice(bidCostOffset));

            if (Convert.ToBoolean(bytes[isBidFeeNullOffset]))
                fill.BidFee = ReadQuantity(bytes.Slice(bidFeeOffset));

            return fill;
        }
    }
}
