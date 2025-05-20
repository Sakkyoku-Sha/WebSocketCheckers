using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct TryCreateGameResultWriter(int gameId) : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteInt(gameId);       
    }

    public int CalculatePayLoadLength()
    {
        return sizeof(int);
    }

    public static ToClientMessageType ResponseType => ToClientMessageType.TryCreateGameResponse;
    public static ushort Version => 1;
}