using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct InitialMessageWriter(GameMetaData[] gameMetaData, GameInfo? gameInfo) : IMessageWriter
{
    public static ToClientMessageType MessageType => ToClientMessageType.InitialServerMessage;
    public static ushort Version => 0;
    
    private readonly GameMetaDataWriter _gameMetaDataWriter = new(gameMetaData);
    private readonly GameInfoWriter _gameInfoWriter = new(gameInfo);
    
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        _gameMetaDataWriter.WriteBytes(ref byteWriter);
        if (gameInfo != null)
        {
            byteWriter.WriteBool(true);
            _gameInfoWriter.WriteBytes(ref byteWriter);
        }
        else
        {
            byteWriter.WriteBool(false);
        }
    }

    public int CalculatePayLoadLength()
    {
        return _gameMetaDataWriter.CalculatePayLoadLength() + 
               sizeof(bool) + 
               _gameInfoWriter.CalculatePayLoadLength(); //returns 0 if null 
    }
}