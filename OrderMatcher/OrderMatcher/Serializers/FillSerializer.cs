using System;

namespace OrderMatcher
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

        public static int MessageSize => sizeOfMessage;

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
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static byte[] Serialize(Fill fill)
        {
            if (fill == null)
            {
                throw new ArgumentNullException(nameof(fill));
            }
            return Serialize(fill.MakerOrderId, fill.TakerOrderId, fill.MatchRate, fill.MatchQuantity, fill.AskRemainingQuantity, fill.AskFee, fill.BidCost, fill.BidFee, fill.Timestamp);
        }

        public static byte[] Serialize(OrderId makerOrderId, OrderId takerOrderId, Price matchRate, Quantity matchQuantity, Quantity? remainingAskQuantiy, Quantity? askFee, Quantity? bidCost, Quantity? bidFee, int timeStamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.Fill;
            Write(msg, versionOffset, version);
            Write(msg, makerOrderIdOffset, makerOrderId);
            Write(msg, takerOrderIdOffset, takerOrderId);
            Write(msg, matchRateOffset, matchRate);
            Write(msg, matchQuantityOffset, matchQuantity);
            msg[isAskRemainingNullOffset] = Convert.ToByte(remainingAskQuantiy.HasValue ? true : false);

            if (remainingAskQuantiy.HasValue)
                Write(msg, askRemainingQuantityOffset, remainingAskQuantiy.Value);

            msg[isAskFeeNullOffset] = Convert.ToByte(askFee.HasValue ? true : false);

            if (askFee.HasValue)
                Write(msg, askFeeOffset, askFee.Value);

            msg[isBidCostNullOffset] = Convert.ToByte(bidCost.HasValue ? true : false);

            if (bidCost.HasValue)
                Write(msg, bidCostOffset, bidCost.Value);

            msg[isBidFeeNullOffset] = Convert.ToByte(bidFee.HasValue ? true : false);

            if (bidFee.HasValue)
                Write(msg, bidFeeOffset, bidFee.Value);

            Write(msg, timestampOffset, timeStamp);
            return msg;
        }

        public static Fill Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Fill Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.Fill)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != FillSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var fill = new Fill();
            fill.MakerOrderId = BitConverter.ToInt32(bytes, makerOrderIdOffset);
            fill.TakerOrderId = BitConverter.ToInt32(bytes, takerOrderIdOffset);
            fill.MatchRate = ReadPrice(bytes, matchRateOffset);
            fill.MatchQuantity = ReadQuantity(bytes, matchQuantityOffset);
            fill.Timestamp = BitConverter.ToInt32(bytes, timestampOffset);

            if (Convert.ToBoolean(bytes[isAskRemainingNullOffset]))
                fill.AskRemainingQuantity = ReadQuantity(bytes, askRemainingQuantityOffset);

            if (Convert.ToBoolean(bytes[isAskFeeNullOffset]))
                fill.AskFee = ReadQuantity(bytes, askFeeOffset);

            if (Convert.ToBoolean(bytes[isBidCostNullOffset]))
                fill.BidCost = ReadQuantity(bytes, bidCostOffset);

            if (Convert.ToBoolean(bytes[isBidFeeNullOffset]))
                fill.BidFee = ReadQuantity(bytes, bidFeeOffset);

            return fill;
        }
    }
}
