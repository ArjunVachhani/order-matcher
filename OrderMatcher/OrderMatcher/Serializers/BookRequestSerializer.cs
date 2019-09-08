using System;

namespace OrderMatcher
{
    public class BookRequestSerializer : Serializer
    {
        private static readonly short version;
        private static readonly int messageLengthOffset;
        private static readonly int messageTypeOffset;
        private static readonly int versionOffset;
        private static readonly int levelCountOffset;


        private static readonly int sizeOfMessageLenght;
        private static readonly int sizeOfMessage;
        private static readonly int sizeOfVersion;
        private static readonly int sizeOfMessagetType;
        private static readonly int sizeOfLevelCount;

        static BookRequestSerializer()
        {
            sizeOfMessageLenght = sizeof(int);
            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfLevelCount = sizeof(int);

            version = 1;

            messageLengthOffset = 0;
            messageTypeOffset = messageLengthOffset + sizeOfMessageLenght;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            levelCountOffset = versionOffset + sizeOfVersion;
            sizeOfMessage = levelCountOffset + sizeOfLevelCount;
        }


        public static byte[] Serialize(BookRequest bookRequest)
        {
            if (bookRequest == null)
            {
                throw new ArgumentNullException(nameof(bookRequest));
            }

            byte[] msg = new byte[sizeOfMessage];
            Write(msg, messageLengthOffset, sizeOfMessage);
            msg[messageTypeOffset] = (byte)MessageType.BookRequest;
            Write(msg, versionOffset, (int)version);
            Write(msg, levelCountOffset, bookRequest.LevelCount);
            return msg;
        }

        public static BookRequest Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Book Request Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.BookRequest)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != BookRequestSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var bookRequest = new BookRequest();

            bookRequest.LevelCount = BitConverter.ToInt32(bytes, levelCountOffset);

            return bookRequest;
        }
    }
}
