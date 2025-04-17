using System.Net.WebSockets;

namespace WebGameServer.WebSocketEncoding.FromClientMessages;

public enum FromClientMessageType : ushort
{
    IdentifyUser = 0,
    TryMakeMoveMessage = 1, 
    TryJoinGameMessage = 2, 
    TryCreateGameMessage = 3,
}
public static class FromClientDecode
{
    public static void HandleFromClient(byte[] message, UserSession sourceSession)
    {
        var reader = new ByteReader(message);
        var messageType = (FromClientMessageType)reader.ReadUShort();
        var messageVersion = reader.ReadUShort();
        
        switch (messageType)
        {
            case FromClientMessageType.IdentifyUser:
                var playerId = reader.ReadFixedSizeStruct<IdentifyUserMessage>();
                _ = FromClientMessageHandler.OnIdentifyUser(sourceSession, playerId);
                break;
            case FromClientMessageType.TryMakeMoveMessage:
                var tryMakeMoveRequest = reader.ReadFixedSizeStruct<TryMakeMoveRequest>();
                _ = FromClientMessageHandler.OnTryMakeMoveRequest(tryMakeMoveRequest);
                break;
            case FromClientMessageType.TryJoinGameMessage:
                var tryJoinGameRequest = reader.ReadFixedSizeStruct<TryJoinGameRequest>();
                _ = FromClientMessageHandler.OnTryJoinGameRequest(sourceSession, tryJoinGameRequest);
                break;
            case FromClientMessageType.TryCreateGameMessage:
                var tryCreateGameRequest = reader.ReadFixedSizeStruct<TryCreateGameRequest>();
                _ = FromClientMessageHandler.OnTryCreateGameRequest(sourceSession, tryCreateGameRequest);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}