using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct GameStatusWriter(GameStatus gameStatus) : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteByte((byte)gameStatus);
    }
    public int CalculatePayLoadLength()
    {
        return sizeof(byte);
    }

    public static ToClientMessageType ResponseType => ToClientMessageType.GameStatusChanged;
    public static ushort Version => 1; 
}