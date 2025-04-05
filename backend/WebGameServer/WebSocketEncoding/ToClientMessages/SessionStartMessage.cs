using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public readonly struct SessionStartMessage : IToClientMessage<SessionStartMessage>
{
    private readonly Guid _sessionId;
    
    public SessionStartMessage(Guid sessionId)
    {
        _sessionId = sessionId;
    }
    public byte[] ToBytes()
    {
        return _sessionId.ToByteArray();
    }
    public static SessionStartMessage FromByteSpan(Span<byte> data)
    {
        return new SessionStartMessage(new Guid(data));
    }   
    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.SessionStartMessage;
    }
}