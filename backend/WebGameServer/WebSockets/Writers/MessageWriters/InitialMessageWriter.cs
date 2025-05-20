using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct InitialMessageWriter(GameMetaData[] gameMetaData, GameInfo? currentGame) : IMessageWriter
{
    public static ToClientMessageType ResponseType => ToClientMessageType.InitialStateMessage;
    public static ushort Version => 1;
    
    private readonly GameMetaDataWriter _gameMetaDataWriter = new(gameMetaData);
    private readonly GameInfoWriter _gameInfoWriter = new(currentGame);
    
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        _gameMetaDataWriter.WriteBytes(ref byteWriter);
        if (currentGame != null)
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