using System;

namespace OrderMatcher
{
    public class FillSerializer : Serializer
    {
        private static short version;
        private static int messageLengthOffset;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int makerOrderIdOffset;
        private static int takerOrderIdOffset;
        private static int matchRateOffset;
        private static int matchQuantityOffset;
        private static int timestampOffset;

        private static int sizeOfMessageLenght;
        private static int sizeOfMessage;
        private static int sizeOfVersion;
        private static int sizeOfMessagetType;
        private static int sizeOfMakerOrderId;
        private static int sizeOfTakerOrderId;
        private static int sizeOfMatchRate;
        private static int sizeOfMatchQuantity;
        private static int sizeOfTimestamp;

        static FillSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfMakerOrderId = sizeof(ulong);
            sizeOfTakerOrderId = sizeof(ulong);
            sizeOfMatchRate = Price.SizeOfPrice;
            sizeOfMatchQuantity = Quantity.SizeOfQuantity;
            sizeOfTimestamp = sizeof(ulong);

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            makerOrderIdOffset = versionOffset + sizeOfVersion;
            takerOrderIdOffset = makerOrderIdOffset + sizeOfMakerOrderId;
            matchRateOffset = takerOrderIdOffset + sizeOfTakerOrderId;
            matchQuantityOffset = matchRateOffset + sizeOfMatchRate;
            timestampOffset = matchQuantityOffset + sizeOfMatchQuantity;
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static byte[] Serialize(Fill fill)
        {
            if (fill == null)
            {
                throw new ArgumentNullException(nameof(fill));
            }
            return Serialize(fill.MakerOrderId, fill.TakerOrderId, fill.MatchRate, fill.MatchQuantity, fill.Timestamp);
        }

        public static byte[] Serialize(ulong makerOrderId, ulong takerOrderId, Price matchRate, Quantity matchQuantity, long timeStamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            WriteInt(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.Fill;
            WriteShort(msg, versionOffset, version);
            WriteULong(msg, makerOrderIdOffset, makerOrderId);
            WriteULong(msg, takerOrderIdOffset, takerOrderId);
            WriteInt(msg, matchRateOffset, matchRate);
            WriteInt(msg, matchQuantityOffset, matchQuantity);
            WriteLong(msg, timestampOffset, timeStamp);

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
            fill.MakerOrderId = BitConverter.ToUInt64(bytes, makerOrderIdOffset);
            fill.TakerOrderId = BitConverter.ToUInt64(bytes, takerOrderIdOffset);
            fill.MatchRate = BitConverter.ToInt32(bytes, matchRateOffset);
            fill.MatchQuantity = BitConverter.ToInt32(bytes, matchQuantityOffset);
            fill.Timestamp = BitConverter.ToInt64(bytes, timestampOffset);
            return fill;
        }
    }
}
