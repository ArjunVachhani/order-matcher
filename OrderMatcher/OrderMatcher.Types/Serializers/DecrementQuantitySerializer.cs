namespace OrderMatcher.Types.Serializers;

public class DecrementQuantitySerializer : Serializer
{
    private static readonly short version;
    private static readonly int messageLengthOffset;
    private static readonly int messageTypeOffset;
    private static readonly int versionOffset;
    private static readonly int orderIdOffset;
    private static readonly int userIdOffset;
    private static readonly int quantityToDecrementOffset;
    private static readonly int messageSequenceOffset;
    private static readonly int sizeOfMessage;

    public static int MessageSize => sizeOfMessage;

    static DecrementQuantitySerializer()
    {
        version = 1;

        messageLengthOffset = 0;
        messageTypeOffset = messageLengthOffset + sizeof(int);
        versionOffset = messageTypeOffset + sizeof(MessageType);
        orderIdOffset = versionOffset + sizeof(short);
        userIdOffset = orderIdOffset + OrderId.SizeOfOrderId;
        quantityToDecrementOffset = userIdOffset + UserId.SizeOfUserId;
        messageSequenceOffset = quantityToDecrementOffset + Quantity.SizeOfQuantity;
        sizeOfMessage = messageSequenceOffset + sizeof(long);
    }

    public static void Serialize(DecrementQuantity decrementQuanity, Span<byte> bytes)
    {
        if (decrementQuanity == null)
            throw new ArgumentNullException(nameof(decrementQuanity));

        Serialize(decrementQuanity.OrderId, decrementQuanity.UserId, decrementQuanity.QuantityDecremented, decrementQuanity.MessageSequence, bytes);
    }

    public static void Serialize(OrderId orderId, UserId userId, Quantity quantityToDecrement, long messageSequence, Span<byte> bytes)
    {
        if (bytes.Length < sizeOfMessage)
            throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

        Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
        bytes[messageTypeOffset] = (byte)MessageType.DecrementQuantity;
        Write(bytes.Slice(versionOffset), version);
        Write(bytes.Slice(orderIdOffset), orderId);
        Write(bytes.Slice(userIdOffset), userId);
        quantityToDecrement.WriteBytes(bytes.Slice(quantityToDecrementOffset));
        Write(bytes.Slice(messageSequenceOffset), messageSequence);
    }

    public static DecrementQuantity Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != sizeOfMessage)
            throw new OrderMatcherException("Decrement quantity message must be of size : " + sizeOfMessage);

        var messageType = (MessageType)(bytes[messageTypeOffset]);

        if (messageType != MessageType.DecrementQuantity)
            throw new OrderMatcherException(Constant.INVALID_MESSAGE);

        var messageVersion = BitConverter.ToInt16(bytes.Slice(versionOffset));
        if (messageVersion != version)
            throw new OrderMatcherException(Constant.INVALID_VERSION);

        var decrementQuantity = new DecrementQuantity();
        decrementQuantity.OrderId = OrderId.ReadOrderId(bytes.Slice(orderIdOffset));
        decrementQuantity.UserId = UserId.ReadUserId(bytes.Slice(userIdOffset));
        decrementQuantity.QuantityDecremented = Quantity.ReadQuantity(bytes.Slice(quantityToDecrementOffset));
        decrementQuantity.MessageSequence = BitConverter.ToInt64(bytes.Slice(messageSequenceOffset));
        return decrementQuantity;
    }
}
