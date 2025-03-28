namespace WebGameServer.WebSocketEncoding.FromClientMessages;
public ref struct FromClientWrapper
{
    public ushort VersionId;
    public FromClientMessageType Type;
    public ushort PayLoadSize; 
    public Span<byte> Payload;
}
public enum FromClientMessageType : ushort
{
    CreateGame,
    JoinGame,
}

public struct JoinGameMessage : IFromClientMessage<JoinGameMessage>
{
    public Guid GameId;
    public Guid UserId; 
    
    private const int DoubleGuidSize = 32; // 2 * 16 bytes for two Guid values
    public byte[] ToBytes()
    {
        var result = new byte[DoubleGuidSize];
        var resultSpan = new Span<byte>(result);
        
        if (!GameId.TryWriteBytes(resultSpan.Slice(0)) ||
            !UserId.TryWriteBytes(resultSpan.Slice(16)))
        {
            throw new Exception("Failed to write bytes in JoinGameMessage bytes");
        }

        return result;
    }

    public static JoinGameMessage FromBytes(Span<byte> data)
    {
        return new JoinGameMessage()
        {
            GameId = new Guid(data.Slice(0, 16)),
            UserId = new Guid(data.Slice(16, 16))
        }; 
    }

    public static FromClientMessageType GetMessageType()
    {
        return FromClientMessageType.JoinGame;
    }
}