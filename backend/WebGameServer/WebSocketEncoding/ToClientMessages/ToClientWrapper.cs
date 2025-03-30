using System.Runtime.InteropServices;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;
public ref struct ToClientWrapper : IByteSerializable<ToClientWrapper>
{
    public ushort VersionId;
    public ToClientMessageType Type;
    public ushort PayLoadSize; 
    public Span<byte> Payload;
    
    public byte[] ToBytes()
    {
        var byteCount = 6 + PayLoadSize; // ushort (2) + ushort (2) + ushort (2) + payload
        var result = new byte[byteCount];

        var span = new Span<byte>(result);

        // Writing fields using MemoryMarshal
        MemoryMarshal.Write(span, in VersionId);
        MemoryMarshal.Write(span.Slice(2), in Type);
        MemoryMarshal.Write(span.Slice(4), in PayLoadSize);
        
        // Copy Payload
        Payload.CopyTo(span.Slice(6));

        return result;
    }

    public static ToClientWrapper FromByteSpan(Span<byte> data)
    {
        var versionId = MemoryMarshal.Cast<byte, ushort>(data.Slice(0, 2))[0];
        var type = MemoryMarshal.Cast<byte, ToClientMessageType>(data.Slice(2, 2))[0];
        var payloadSize = MemoryMarshal.Cast<byte, ushort>(data.Slice(4, 2))[0];

        return new ToClientWrapper()
        {
            VersionId = versionId,
            Type = type,
            PayLoadSize = payloadSize,
            Payload = data.Slice(6, payloadSize)
        };
    }
}
public enum ToClientMessageType : ushort
{
    SessionStartMessage = 0,
    PlayerJoined = 1,
    GameHistoryUpdate = 2, 
    GameInfoMessage = 3,
}