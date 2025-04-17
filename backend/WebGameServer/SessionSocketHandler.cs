using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebGameServer.State;
using WebGameServer.WebSocketEncoding.FromClientMessages;
using WebGameServer.WebSocketEncoding.Writers;

namespace WebGameServer;

public static class SessionSocketHandler
{
    private const int UserTimeout = 30; //30s timeouts. 
    private static readonly ConcurrentDictionary<Guid, UserSession> SessionsMap = new();
    private static readonly ConcurrentDictionary<Guid, UserSession> IdentifiedPlayerSessions = new();
    public static async Task AddSocketAsync(WebSocket socket)
    {
        var sessionId = Guid.NewGuid();
        var session = new UserSession(socket, sessionId);
        SessionsMap[sessionId] = session;
        
        //Notify the client of the sessionId
        await ToClientEncode.WriteSessionStartAsync(session, sessionId);
        
        //!!Spawns a task that will live until the connection is closed. 
        await StartReceiving(session, sessionId);
    }
    
    private static async Task CloseAsync(Guid sessionId)
    {
        if (SessionsMap.TryGetValue(sessionId, out var userSession))
        {
            await userSession.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
      
        //Todo cleanup sessions with timeouts.  e.g remove session. 
    }
    
    // Handle receiving messages from a WebSocket, runs forever 
    private const int WebSocketBufferSize = 1024; 
    private static async Task StartReceiving(UserSession session, Guid sessionId)
    {
        var buffer = new byte[WebSocketBufferSize];
        while (session.State == WebSocketState.Open)
        {
            try
            {
                var result = await session.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                await HandleClientMessage(session, sessionId, result, buffer);
            }
            catch (Exception)
            {
                // Handle exception (e.g., connection closed)
                await CloseAsync(sessionId);
            }
        }
    }

    private static async Task HandleClientMessage(UserSession session, Guid sessionId, WebSocketReceiveResult result,
        byte[] buffer)
    {
        switch (result.MessageType)
        {
            case WebSocketMessageType.Close:
                await CloseAsync(sessionId);
                break;
                    
            case WebSocketMessageType.Binary:
            {
                FromClientDecode.HandleFromClient(buffer, session);
                break;
            }
            case WebSocketMessageType.Text:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static UserSession[] GetSessionsForPlayers(Guid[] playerIds)
    {
        var sessions = new UserSession[playerIds.Length];
        for (var i = 0; i < playerIds.Length; i++)
        {
            if (IdentifiedPlayerSessions.TryGetValue(playerIds[i], out var session))
            {
                sessions[i] = session;
            }
            else
            {
                throw new Exception("FAILED TO RETRIEVE SESSION FOR USER " + playerIds[i]);
            }
        }
        
        return sessions;
    }
    
    public static UserSession GetSessionForUserId(PlayerInfo resultOpponentInfo)
    {
        if(IdentifiedPlayerSessions.TryGetValue(resultOpponentInfo.PlayerId, out var session))
        {
            return session;
        }
        else
        {
            throw new Exception("FAILED TO RETRIEVE SESSION FOR USER " + resultOpponentInfo.PlayerId);
        }
    }

    public static void IdentifyPlayer(UserSession session, Guid messagePlayerId)
    {
        session.PlayerId = messagePlayerId;
        session.Identified = true;

        //If we already exist, we should remove the old session as it must have been dropped. 
        if (IdentifiedPlayerSessions.TryGetValue(messagePlayerId, out var previousSession))
        {
            SessionsMap.Remove(previousSession.SessionId, out _);
            IdentifiedPlayerSessions.TryUpdate(messagePlayerId, session, previousSession);
        }
        else
        {
            IdentifiedPlayerSessions.TryAdd(messagePlayerId, session);
        }
    }
}
