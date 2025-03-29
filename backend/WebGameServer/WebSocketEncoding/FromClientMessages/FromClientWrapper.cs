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
    IdentifyUser = 0,
}