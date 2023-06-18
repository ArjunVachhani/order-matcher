namespace OrderMatcher.Types.Serializers;

public class SelfMatchSerializer : Serializer
{
    private static readonly short version;
    private static readonly int messageLengthOffset;
    private static readonly int messageTypeOffset;
    private static readonly int versionOffset;
    private static readonly int incomingOrderIdOffset;
    private static readonly int restingOrderIdOffset;
    private static readonly int userIdOffset;
    private static readonly int messageSequenceOffset;
    private static readonly int sizeOfMessage;

    public static int MessageSize => sizeOfMessage;

    static SelfMatchSerializer()
    {
        version = 1;

        messageLengthOffset = 0;
        messageTypeOffset = messageLengthOffset + sizeof(int);
        versionOffset = messageTypeOffset + sizeof(MessageType);
        incomingOrderIdOffset = versionOffset + sizeof(short);
        restingOrderIdOffset = incomingOrderIdOffset + OrderId.SizeOfOrderId;
        userIdOffset = restingOrderIdOffset + OrderId.SizeOfOrderId;
        messageSequenceOffset = userIdOffset + Quantity.SizeOfQuantity;
        sizeOfMessage = messageSequenceOffset + sizeof(long);
    }

    public static void Serialize(SelfMatch selfMatch, Span<byte> bytes)
    {
        if (selfMatch == null)
            throw new ArgumentNullException(nameof(selfMatch));
        
        Serialize(selfMatch.IncomingOrderId, selfMatch.RestingOrderId, selfMatch.UserId, selfMatch.MessageSequence, bytes);
    }

    public static void Serialize(OrderId incomingOrderId, OrderId restingOrderId, UserId userId, long messageSequence, Span<byte> bytes)
    {
        if (bytes.Length < sizeOfMessage)
            throw new ArgumentException(Constant.INVALID_SIZE, nameof(bytes));

        Write(bytes.Slice(messageLengthOffset), sizeOfMessage);
        bytes[messageTypeOffset] = (byte)MessageType.SelfMatch;
        Write(bytes.Slice(versionOffset), version);
        Write(bytes.Slice(incomingOrderIdOffset), incomingOrderId);
        Write(bytes.Slice(restingOrderIdOffset), restingOrderId);
        Write(bytes.Slice(userIdOffset), userId);
        Write(bytes.Slice(messageSequenceOffset), messageSequence);
    }

    public static SelfMatch Deserialize(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != sizeOfMessage)
            throw new OrderMatcherException("Self match message must be of size : " + sizeOfMessage);

        var messageType = (MessageType)(bytes[messageTypeOffset]);

        if (messageType != MessageType.SelfMatch)
            throw new OrderMatcherException(Constant.INVALID_MESSAGE);

        var messageVersion = BitConverter.ToInt16(bytes.Slice(versionOffset));
        if (messageVersion != version)
            throw new OrderMatcherException(Constant.INVALID_VERSION);

        var selfMatch = new SelfMatch();
        selfMatch.IncomingOrderId = OrderId.ReadOrderId(bytes.Slice(incomingOrderIdOffset));
        selfMatch.RestingOrderId = OrderId.ReadOrderId(bytes.Slice(restingOrderIdOffset));
        selfMatch.UserId = UserId.ReadUserId(bytes.Slice(userIdOffset));
        selfMatch.MessageSequence = BitConverter.ToInt64(bytes.Slice(messageSequenceOffset));
        return selfMatch;
    }
}