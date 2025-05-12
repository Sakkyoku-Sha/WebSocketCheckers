using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct CreateGameResultWriter(int createdGameId) : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteInt(createdGameId);
    }

    public int CalculatePayLoadLength()
    {
        return sizeof(int);
    }

    public static ToClientMessageType MessageType => ToClientMessageType.CreateGameResultMessage;
    public static ushort Version => 1;
}