namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public record SessionStartMessage : IToClientMessage<SessionStartMessage>
{
    Guid SessionId;

    public SessionStartMessage(Guid sessionId)
    {
        SessionId = sessionId;
    }
    public byte[] ToBytes()
    {
        return SessionId.ToByteArray();
    }
    public static SessionStartMessage FromBytes(Span<byte> data)
    {
        return new SessionStartMessage(new Guid(data));
    }   
    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.SessionStartMessage;
    }
}