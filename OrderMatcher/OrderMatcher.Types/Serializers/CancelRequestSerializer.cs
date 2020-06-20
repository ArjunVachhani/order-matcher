using System;
using System.Diagnostics.CodeAnalysis;

namespace OrderMatcher.Types.Serializers
{
    public class CancelRequestSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int orderIdOffset;


        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfOrderId;

        public static int MessageSize => sizeOfMessage;

        [SuppressMessage("Microsoft.Performance", "CA1810")]
        static CancelRequestSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = OrderId.SizeOfOrderId;

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            sizeOfMessage = orderIdOffset + sizeOfOrderId;
        }


        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static void Serialize(CancelRequest cancelRequest, Span<byte> bytes)
        {
            if (cancelRequest == null)
                throw new ArgumentNullException(nameof(cancelRequest));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < sizeOfMessage)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.CancelRequest;
            Write(bytes.Slice(versionOffset), version);
            Write(bytes.Slice(orderIdOffset), cancelRequest.OrderId);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303")]
        public static CancelRequest Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != sizeOfMessage)
                throw new Exception("Cancel Request Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.CancelRequest)
                throw new Exception(Constant.INVALID_MESSAGE);

            var version = BitConverter.ToInt16(bytes.Slice(versionOffset));

            if (version != CancelRequestSerializer.version)
                throw new Exception(Constant.INVALID_VERSION);

            var cancelRequest = new CancelRequest();

            cancelRequest.OrderId = BitConverter.ToInt32(bytes.Slice(orderIdOffset));

            return cancelRequest;
        }
    }
}
