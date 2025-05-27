using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct TryCreateGameResultWriter(uint gameId) : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteUInt(gameId);       
    }

    public int CalculatePayLoadLength()
    {
        return sizeof(uint);
    }

    public static ToClientMessageType ResponseType => ToClientMessageType.TryCreateGameResponse;
    public static ushort Version => 1;
}