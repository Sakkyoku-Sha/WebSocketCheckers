namespace WebGameServer.WebSocketEncoding.FromClientMessages;

public struct CreateGameMessage : IFromClientMessage<CreateGameMessage>
{
    public Guid UserId; 
    
    public byte[] ToBytes()
    {
        return UserId.ToByteArray();
    }

    public static CreateGameMessage FromBytes(Span<byte> data)
    {
        return new CreateGameMessage()
        {
            UserId = new Guid(data),
        };
    }

    public static FromClientMessageType GetMessageType()
    {
        return FromClientMessageType.CreateGame; 
    }
}