using System;

namespace OrderMatcher.Types.Serializers
{
    public class BookRequestSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int levelCountOffset;


        private static readonly int sizeOfMessageLength;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfLevelCount;

        public static int MessageSize => sizeOfMessage;

        static BookRequestSerializer()
        {
            sizeOfMessageLength = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfLevelCount = sizeof(int);

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLength;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            levelCountOffset = versionOffset + sizeOfVersion;
            sizeOfMessage = levelCountOffset + sizeOfLevelCount;
        }

        public static void Serialize(BookRequest bookRequest, Span<byte> bytes)
        {
            if (bookRequest == null)
                throw new ArgumentNullException(nameof(bookRequest));

            if (bytes.Length < MessageSize)
                throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

            Write(bytes.Slice(messageLengthOffset, 4), sizeOfMessage);
            bytes[messageTypeOffset] = (byte)MessageType.BookRequest;
            Write(bytes.Slice(versionOffset, 4), (int)version);
            Write(bytes.Slice(levelCountOffset, 4), bookRequest.LevelCount);
        }

        public static BookRequest Deserialize(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != sizeOfMessage)
                throw new OrderMatcherException("Book Request Message must be of Size : " + sizeOfMessage);

            var messageType = (MessageType)(bytes[messageTypeOffset]);

            if (messageType != MessageType.BookRequest)
                throw new OrderMatcherException(Constant.INVALID_MESSAGE);

            var messageVersion = BitConverter.ToInt16(bytes.Slice(versionOffset));
            if (messageVersion != version)
                throw new OrderMatcherException(Constant.INVALID_VERSION);

            var bookRequest = new BookRequest();

            bookRequest.LevelCount = BitConverter.ToInt32(bytes.Slice(levelCountOffset));

            return bookRequest;
        }
    }
}
