using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebGameServer.WebSocketEncoding;
using WebGameServer.WebSocketEncoding.FromClientMessages;
using WebGameServer.WebSocketEncoding.ToClientMessages;

namespace WebGameServer;

public static class SessionSocketHandler
{
    private static readonly ConcurrentDictionary<Guid, WebSocket> SessionIdToSockets = new();
    private static readonly Dictionary<Guid, Guid> UserIdToSessionId = new();
    
    public static async Task AddSocketAsync(WebSocket socket)
    {
        var sessionId = Guid.NewGuid();
        SessionIdToSockets[sessionId] = socket;
        
        //Notify the client of the sessionId
        var message = WebSocketEncoder.Encode(new SessionStartMessage(sessionId));
        await socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
        
        //!!Spawns a task that will live until the connection is closed. 
        await StartReceiving(socket, sessionId);
    }
    
    private static async Task CloseAsync(Guid sessionId)
    {
        if (SessionIdToSockets.TryRemove(sessionId, out var socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        
        //!! Remove the sessionId from the UserIdToSessionId dictionary
        var userId = UserIdToSessionId.FirstOrDefault(x => x.Value == sessionId).Key;
        if (userId != Guid.Empty)
        {
            UserIdToSessionId.Remove(userId);
        }
    }
    public static bool HasClient(Guid userId)
    {
        return UserIdToSessionId.ContainsKey(userId);
    }
    
    public static async Task SendMessageToUsersAsync(IEnumerable<Guid> userIds, ArraySegment<byte> message)
    {
        // Send the message to each connected WebSocket
        var sessionsToCleanUp = new List<(Guid sessionId, Guid userId)>();
        foreach (var userId in userIds)
        {
            if (!UserIdToSessionId.TryGetValue(userId, out var sessionId))
            {
                throw new Exception("UserId should always exist in this dictionary, if it doesn't something is wrong");
            }
            if (!SessionIdToSockets.TryGetValue(sessionId, out var socket))
            {
                throw new Exception("UserId should never be correlated to a sessionId that does not exist in the socket dictionary");
            }
            if (socket.State != WebSocketState.Open)
            {
                sessionsToCleanUp.Add((sessionId, userId));
                continue;
            }
            try
            {
                await socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception)
            {
                await CloseAsync(sessionId);
            }
        }
        foreach (var (sessionId, userId) in sessionsToCleanUp)
        {
            await CloseAsync(sessionId);
            UserIdToSessionId.Remove(userId);
        }
    }
    
    // Handle receiving messages from a WebSocket, runs forever 
    public const int WebSocketBufferSize = 1024 * 4; 
    private static async Task StartReceiving(WebSocket socket, Guid sessionId)
    {
        var buffer = new byte[WebSocketBufferSize];
        while (socket.State == WebSocketState.Open)
        {
            try
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await CloseAsync(sessionId);
                        break;
                    
                    case WebSocketMessageType.Binary:
                    {
                        var fromClient = WebSocketDecoder.DecodeToWrapper(buffer);
                        switch (fromClient.Type)
                        {
                            case FromClientMessageType.IdentifyUser:
                                var userIdMessage = IdentifyUserMessage.FromBytes(fromClient.Payload);
                                HandleIdentifyUserMessage(userIdMessage, sessionId);
                                Console.WriteLine("UserId: " + userIdMessage.UserId);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    }
                    case WebSocketMessageType.Text:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                // Handle exception (e.g., connection closed)
                await CloseAsync(sessionId);
            }
        }
    }

    private static void HandleIdentifyUserMessage(IdentifyUserMessage identifyUserIdMessage, Guid sessionId)
    {
        UserIdToSessionId[identifyUserIdMessage.UserId] = sessionId;
    }
}
