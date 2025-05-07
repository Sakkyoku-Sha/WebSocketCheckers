using System.Net.WebSockets;

namespace WebGameServer.WebSocketEncoding.FromClientMessages;

public enum FromClientMessageType : ushort
{
    IdentifyUser = 0,
    TryMakeMoveRequest = 1, 
    TryJoinGameRequest = 2, 
    TryCreateGameRequest = 3,
    GetActiveGamesRequest = 4,
}
public static class FromClientDecode
{
    public static void HandleFromClient(byte[] message, UserSession sourceSession)
    {
        var reader = new ByteReader(message);
        var messageVersion = reader.ReadUShort();
        var messageType = (FromClientMessageType)reader.ReadUShort();
        
        switch (messageType)
        {
            case FromClientMessageType.IdentifyUser:
                var playerId = reader.ReadFixedSizeStruct<IdentifyUserMessage>();
                _ = FromClientMessageHandler.OnIdentifyUser(sourceSession, playerId);
                break;
            case FromClientMessageType.TryMakeMoveRequest:
                var tryMakeMoveRequest = reader.ReadFixedSizeStruct<TryMakeMoveRequest>();
                _ = FromClientMessageHandler.OnTryMakeMoveRequest(tryMakeMoveRequest);
                break;
            case FromClientMessageType.TryJoinGameRequest:
                var tryJoinGameRequest = reader.ReadFixedSizeStruct<TryJoinGameRequest>();
                _ = FromClientMessageHandler.OnTryJoinGameRequest(sourceSession, tryJoinGameRequest);
                break;
            case FromClientMessageType.TryCreateGameRequest:
                var tryCreateGameRequest = reader.ReadFixedSizeStruct<TryCreateGameRequest>();
                _ = FromClientMessageHandler.OnTryCreateGameRequest(sourceSession, tryCreateGameRequest);
                break;
            case FromClientMessageType.GetActiveGamesRequest:
                _ = FromClientMessageHandler.OnGetActiveGamesRequest(sourceSession);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}