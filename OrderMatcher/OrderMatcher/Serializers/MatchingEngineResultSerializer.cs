using System;

namespace OrderMatcher.Serializers
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

        public static byte[] Serialize(MatchingEngineResult matchingEngineResult)
        {
            if (matchingEngineResult == null)
            {
                throw new ArgumentNullException(nameof(matchingEngineResult));
            }
            return Serialize(matchingEngineResult.OrderId, matchingEngineResult.Result, matchingEngineResult.Timestamp);
        }

        public static byte[] Serialize(ulong orderId, OrderMatchingResult result, long timeStamp)
        {
            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.OrderMatchingResult;
            Write(msg, versionOffset, version);
            Write(msg, orderIdOffset, orderId);
            Write(msg, resultOffset, (byte)result);
            Write(msg, timestampOffset, timeStamp);
            return msg;
        }

        public static MatchingEngineResult Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("OrderMatchingResult Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.OrderMatchingResult)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != MatchingEngineResultSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var result = new MatchingEngineResult();
            result.OrderId = BitConverter.ToUInt64(bytes, orderIdOffset);
            result.Result = (OrderMatchingResult)bytes[resultOffset];
            result.Timestamp = BitConverter.ToInt64(bytes, timestampOffset);
            return result;
        }
    }
}
