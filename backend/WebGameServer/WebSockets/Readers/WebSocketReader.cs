namespace WebGameServer.WebSockets.Readers;

public enum FromClientMessageType : ushort
{
    //Initial Message From User 
    IdentifyUser = 0,
    
    //Server Status Queries 
    GetActiveGamesRequest = 1,
    
    //Try Act on Game State, Either Game State is Updated or Fail Response is Returned
    TryJoinGameRequest = 2, 
    TryCreateGameRequest = 3,
    TryMakeMoveRequest = 4,
    
    //Draw Request / Responses  
    DrawRequest = 5,
    DrawRequestResponse = 6,
    
    //Game Status Changes
    Surrender = 7,
}

public static class WebSocketReader
{
    public static void HandleFromClient(byte[] message, UserSession sourceSession)
    {
        var reader = new ByteReader(message);
        var messageVersion = reader.ReadUShort();
        var messageType = (FromClientMessageType)reader.ReadUShort();
        
        if(sourceSession.Identified == false && messageType != FromClientMessageType.IdentifyUser)
        {
            //If the user is not identified, we should not process any other messages
            return;
        }
        
        switch (messageType)
        {
            //Initial Message From User
            case FromClientMessageType.IdentifyUser:
                var playerId = reader.ReadFixedSizeStruct<IdentifyUserMessage>();
                _ = FromClientMessageHandler.OnIdentifyUser(sourceSession, playerId);
                break;
            
            //Server Status Queries
            case FromClientMessageType.GetActiveGamesRequest:
                _ = FromClientMessageHandler.OnGetActiveGamesRequest(sourceSession);
                break;
            
            //Try Act on Game State 
            case FromClientMessageType.TryJoinGameRequest:
                var tryJoinGameRequest = reader.ReadFixedSizeStruct<TryJoinGameRequest>();
                FromClientMessageHandler.OnTryJoinGameRequest(sourceSession, tryJoinGameRequest);
                break;
            case FromClientMessageType.TryCreateGameRequest:
                _ = FromClientMessageHandler.OnTryCreateGameRequest(sourceSession);
                break;
            case FromClientMessageType.TryMakeMoveRequest:
                var tryMakeMoveRequest = reader.ReadFixedSizeStruct<TryMakeMoveRequest>();
                FromClientMessageHandler.OnTryMakeMoveRequest(sourceSession, tryMakeMoveRequest);
                break; 
            
            
            //Draw Request / Responses
            case FromClientMessageType.DrawRequest:
                FromClientMessageHandler.OnDrawRequest(sourceSession);
                break; 
            case FromClientMessageType.DrawRequestResponse:
                var drawResponse = reader.ReadFixedSizeStruct<DrawRequestResponse>();
                FromClientMessageHandler.OnDrawResponse(sourceSession, drawResponse);
                break; 
            
            //Game Status Changes
            case FromClientMessageType.Surrender:
                FromClientMessageHandler.OnSurrenderGame(sourceSession);
                break; 
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

