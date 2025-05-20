using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct GameCreatedWriter(GameMetaData createdGame) : IMessageWriter
{
    private readonly GameMetaDataWriter _gameMetaDataWriter = new([createdGame]);
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        _gameMetaDataWriter.WriteBytes(ref byteWriter);
    }

    public int CalculatePayLoadLength()
    {
        return _gameMetaDataWriter.CalculatePayLoadLength();
    }

    public static ToClientMessageType ResponseType => ToClientMessageType.GameCreatedMessage;
    public static ushort Version => 1;
}