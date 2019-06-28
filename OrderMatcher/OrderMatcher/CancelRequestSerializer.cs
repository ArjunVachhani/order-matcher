using System;

namespace OrderMatcher
{
    public class CancelRequestSerializer : Serializer
    {
        private static short version;
        private static int messageTypeOffset;
        private static int versionOffset;
        private static int orderIdOffset;


        private static int sizeOfMessage;
        private static int sizeOfVersion;
        private static int sizeOfMessagetType;
        private static int sizeOfOrderId;

        static CancelRequestSerializer()
        {

            sizeOfVersion = sizeof(short);
            sizeOfMessagetType = sizeof(MessageType);
            sizeOfOrderId = sizeof(ulong);

            version = 1;

            messageTypeOffset = 0;
            versionOffset = messageTypeOffset + sizeOfMessagetType;
            orderIdOffset = versionOffset + sizeOfVersion;
            sizeOfMessage = orderIdOffset + sizeOfOrderId;
        }


        public static byte[] Serialize(CancelRequest cancelRequest)
        {
            if (cancelRequest == null)
            {
                throw new ArgumentNullException(nameof(cancelRequest));
            }

            byte[] msg = new byte[sizeOfMessage];
            msg[messageTypeOffset] = (byte)MessageType.CancelRequest;
            var versionByteArray = BitConverter.GetBytes(version);
            msg[versionOffset] = versionByteArray[0];
            msg[versionOffset + 1] = versionByteArray[1];

            CopyBytes(BitConverter.GetBytes(cancelRequest.OrderId), msg, orderIdOffset);
            return msg;
        }

        public static CancelRequest Deserialize(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length != sizeOfMessage)
            {
                throw new Exception("Cancel Request Message must be of Size : " + sizeOfMessage);
            }

            var messageType = (MessageType)(bytes[messageTypeOffset]);
            if (messageType != MessageType.CancelRequest)
            {
                throw new Exception("Invalid Message");
            }

            var version = BitConverter.ToInt16(bytes, versionOffset);
            if (version != CancelRequestSerializer.version)
            {
                throw new Exception("version mismatch");
            }

            var cancelRequest = new CancelRequest();

            cancelRequest.OrderId = BitConverter.ToUInt64(bytes, orderIdOffset);

            return cancelRequest;
        }

        public static object Deserialize(object cancelRequestBinary)
        {
            throw new NotImplementedException();
        }
    }
}
