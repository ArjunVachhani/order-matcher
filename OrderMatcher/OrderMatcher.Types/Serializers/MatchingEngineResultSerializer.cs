using System;

namespace OrderMatcher.Types.Serializers
{
    public class MatchingEngineResultSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int orderIdOffset;
        private static readonly int resultOffset;
        private static readonly int timestampOffset;

        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;
        private static readonly int sizeOfResult;
        private static readonly int sizeOfTimestamp;

        public static int MessageSize => sizeOfMessage;

        static MatchingEngineResultSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfVersion = sizeof(short);
            sizeOfOrderId = sizeof(ulong);
            sizeOfResult = sizeof(byte);
            sizeOfTimestamp = sizeof(long);
            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            resultOffset = orderIdOffset + sizeOfOrderId;
            timestampOffset = resultOffset + sizeOfResult;
            sizeOfMessage = timestampOffset + sizeOfTimestamp;
        }

        public static void Serialize(MatchingEngineResult matchingEngineResult, Span<byte> bytes)
        {
            if (matchingEngineResult == null)
                throw new ArgumentNullException(nameof(matchingEngineResult));

            Serialize(matchingEngineResult.OrderId, matchingEngineResult.Result, matchingEngineResult.Timestamp, bytes);
        }

        public static void Serialize(ulong orderId, OrderMatchingResult result, long timeStamp, Span<byte> bytes)
        {
            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.OrderMatchingResult;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(orderIdOffset), orderId);
            Write(bytes.Slice(resultOffset), (byte)result);
            Write(bytes.Slice(timestampOffset), timeStamp);
        }

        public static MatchingEngineResult Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != sizeOfMessage)
                throw new OrderMatcherException("OrderMatchingResult Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)bytes[messageTypeOffset];

            if (messageType != MessageType.OrderMatchingResult)
                throw new OrderMatcherException(Constant.INVALID_MESSAGE);

            var messageVersion = BitConverter.ToInt16(bytes.Slice(versionOffset));
            if (messageVersion != version)
                throw new OrderMatcherException(Constant.INVALID_VERSION);

            var result = new MatchingEngineResult();
            result.OrderId = BitConverter.ToUInt64(bytes.Slice(orderIdOffset));
            result.Result = (OrderMatchingResult)bytes[resultOffset];
            result.Timestamp = BitConverter.ToInt64(bytes.Slice(timestampOffset));
            return result;
        }
    }
}
