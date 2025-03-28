using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public struct ClientConnectedMessage : IToClientMessage<ClientConnectedMessage>
{
    public Guid UserId;
    
    public ClientConnectedMessage(Guid userId)
    {
        UserId = userId;
    }
    public byte[] ToBytes()
    {
        return UserId.ToByteArray();
    }
    public static ClientConnectedMessage FromBytes(Span<byte> data)
    {
        return new ClientConnectedMessage()
        {
            UserId = new Guid(data),
        };
    }
    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.Connected;
    }
}