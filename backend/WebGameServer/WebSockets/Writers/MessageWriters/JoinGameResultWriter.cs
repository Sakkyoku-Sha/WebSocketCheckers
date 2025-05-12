using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct JoinGameResultWriter(bool tryJoinGameResult, GameInfo? gameInfo) : IMessageWriter
{
    private readonly GameInfoWriter _gameInfoWriter = new GameInfoWriter(gameInfo);   
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteBool(tryJoinGameResult);
        _gameInfoWriter.WriteBytes(ref byteWriter);
    }
    public int CalculatePayLoadLength()
    {
        return sizeof(bool) + _gameInfoWriter.CalculatePayLoadLength();
    }

    public static ToClientMessageType MessageType => ToClientMessageType.TryJoinGameResultMessage;
    public static ushort Version => 1; 
}