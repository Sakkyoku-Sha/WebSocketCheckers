using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebGameServer.GameStateManagement;
using WebGameServer.WebSocketEncoding;
using WebGameServer.WebSocketEncoding.FromClientMessages;
using WebGameServer.WebSocketEncoding.ToClientMessages;

namespace WebGameServer;

public class SessionSocketHandler
{
    private const int UserTimeout = 30; //30s timeouts. 
    
    private static readonly ConcurrentDictionary<Guid, WebSocket> SessionIdToSockets = new();
    private static readonly ConcurrentDictionary<Guid, Guid> UserIdToSessionId = new();
    private static readonly ConcurrentDictionary<Guid, CancellationToken> ExecuteDisconnectedUserCleanupTokens = new();
    
    
    private readonly GameStateManager _gameStateManager;
    public SessionSocketHandler(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }
    
    public async Task AddSocketAsync(WebSocket socket)
    {
        var sessionId = Guid.NewGuid();
        SessionIdToSockets[sessionId] = socket;
        
        //Notify the client of the sessionId
        var message = WebSocketEncoder.Encode(new SessionStartMessage(sessionId));
        await socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
        
        //!!Spawns a task that will live until the connection is closed. 
        await StartReceiving(socket, sessionId);
    }
    
    private async Task CloseAsync(Guid sessionId)
    {
        if (SessionIdToSockets.TryRemove(sessionId, out var socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        
        //!! Remove the sessionId from the UserIdToSessionId dictionary
        var userId = UserIdToSessionId.FirstOrDefault(x => x.Value == sessionId).Key;
        if (userId != Guid.Empty)
        {
            UserIdToSessionId.Remove(userId, out _);
        }

        var cancellationToken = new CancellationToken();
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(UserTimeout), cancellationToken);
            _gameStateManager.RemoveUsersFromGames(userId);
            
        }, cancellationToken);
    }
    
    public bool HasClient(Guid userId)
    {
        return UserIdToSessionId.ContainsKey(userId);
    }
    
    public async Task SendMessageToUsersAsync(IEnumerable<Guid> userIds, ArraySegment<byte> message)
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
            if (socket.State != WebSocketState.Open && socket.State != WebSocketState.Connecting)
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
            UserIdToSessionId.Remove(userId, out _);
        }
    }
    
    // Handle receiving messages from a WebSocket, runs forever 
    public const int WebSocketBufferSize = 1024 * 4; 
    private async Task StartReceiving(WebSocket socket, Guid sessionId)
    {
        var buffer = new byte[WebSocketBufferSize];
        while (socket.State == WebSocketState.Open)
        {
            try
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                await HandleClientMessage(socket, sessionId, result, buffer);
            }
            catch (Exception)
            {
                // Handle exception (e.g., connection closed)
                await CloseAsync(sessionId);
            }
        }
    }

    private async Task HandleClientMessage(WebSocket socket, Guid sessionId, WebSocketReceiveResult result,
        byte[] buffer)
    {
        switch (result.MessageType)
        {
            case WebSocketMessageType.Close:
                await CloseAsync(sessionId);
                break;
                    
            case WebSocketMessageType.Binary:
            {
                var fromClient = WebSocketDecoder.DecodeToWrapper(buffer);
                ProcessClientMessage(socket, sessionId, fromClient);
                break;
            }
            case WebSocketMessageType.Text:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ProcessClientMessage(WebSocket socket, Guid sessionId, FromClientWrapper fromClient)
    {
        switch (fromClient.Type)
        {
            case FromClientMessageType.IdentifyUser:
                HandleIdentifyUserMessage(socket, sessionId, fromClient);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleIdentifyUserMessage(WebSocket socket, Guid sessionId, FromClientWrapper fromClient)
    {
        //Handle Actual Message From Client 
        var userIdMessage = IdentifyUserMessage.FromByteSpan(fromClient.Payload);
        UserIdToSessionId[userIdMessage.UserId] = sessionId;
        Console.WriteLine("UserId: " + userIdMessage.UserId);
        
        //If the user is actually in a game, and just lost their socket connection, we need to send them their current games info
        if (_gameStateManager.PlayerInGame(userIdMessage.UserId))
        {
            Console.WriteLine($"UserId: {userIdMessage.UserId} reconnected, and is in a game");
            if (_gameStateManager.TryGetGameByUserId(userIdMessage.UserId, out var gameInfo) || gameInfo == null)
            {
                throw new Exception("This should never happen, if the user is in a game, they should be able to reconnect to it");
            }
            
            var gameInfoMessage = WebSocketEncoder.Encode(new ActiveGameInfoMessage(gameInfo));
            _ = socket.SendAsync(gameInfoMessage, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }
}
