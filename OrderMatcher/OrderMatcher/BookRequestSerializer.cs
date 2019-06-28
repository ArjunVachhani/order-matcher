using System;

namespace OrderMatcher
{
    public class BookRequestSerializer : Serializer
    {
        private static short version;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int levelCountOffset;


        private static int sizeOfMessage;
        private static int sizeOfVersion;
        private static int sizeOfMessagetType;
        private static int sizeOfLevelCount;

        static BookRequestSerializer()
        {

            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfLevelCount = sizeof(int);

            version = 1;

            messageTypeOffset = 0;
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
            msg[messageTypeOffset] = (byte)MessageType.BookRequest;
            var versionByteArray = BitConverter.GetBytes(version);
            msg[versionOffset] = versionByteArray[0];
            msg[versionOffset + 1] = versionByteArray[1];

            CopyBytes(BitConverter.GetBytes(bookRequest.LevelCount), msg, levelCountOffset);
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
