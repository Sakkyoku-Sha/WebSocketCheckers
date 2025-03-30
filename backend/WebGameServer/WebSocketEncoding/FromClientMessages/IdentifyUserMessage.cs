namespace WebGameServer.WebSocketEncoding.FromClientMessages;

public record IdentifyUserMessage(Guid UserId) : IFromClientMessage<IdentifyUserMessage>
{
    public Guid UserId = UserId;

    public byte[] ToBytes()
    {
        return UserId.ToByteArray();
    }
    public static IdentifyUserMessage FromByteSpan(Span<byte> data)
    {
        return new IdentifyUserMessage(new Guid(data));
    }
    public static FromClientMessageType GetMessageType()
    {
        return FromClientMessageType.IdentifyUser;
    }
}