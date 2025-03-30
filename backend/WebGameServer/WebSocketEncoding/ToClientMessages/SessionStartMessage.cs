using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public readonly struct SessionStartMessage : IToClientMessage<SessionStartMessage>
{
    private readonly Guid _sessionId;
    private readonly bool _isInGame;
    private readonly GameInfoMessage? _gameInfo;
    
    public SessionStartMessage(Guid sessionId, bool isInGame = false, GameInfo? gameInfo = null)
    {
        _sessionId = sessionId;
        _isInGame = isInGame;
        _gameInfo = gameInfo == null ? null : new GameInfoMessage(gameInfo);
    }
    public byte[] ToBytes()
    {
        var sessionIdBytes= _sessionId.ToByteArray();
        var isinGameByte = (byte) (_isInGame ? 1 : 0);
        var gameInfoBytes = _gameInfo?.ToBytes() ?? Array.Empty<byte>();
        
        var totalSize = sessionIdBytes.Length + 1 + gameInfoBytes.Length;
        var buffer = new byte[totalSize];
        
        Buffer.BlockCopy(sessionIdBytes, 0, buffer, 0, sessionIdBytes.Length);
        buffer[sessionIdBytes.Length] = isinGameByte;
        Buffer.BlockCopy(gameInfoBytes, 0, buffer, sessionIdBytes.Length + 1, gameInfoBytes.Length);
        
        return buffer; 
    }
    public static SessionStartMessage FromByteSpan(Span<byte> data)
    {
        //let it fly should never be called 
        throw new NotImplementedException();
    }   
    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.SessionStartMessage;
    }
}