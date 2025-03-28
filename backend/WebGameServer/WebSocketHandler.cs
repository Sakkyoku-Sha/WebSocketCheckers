using System.Collections.Concurrent;
using System.Net.WebSockets;
using WebGameServer.WebSocketEncoding;
using WebGameServer.WebSocketEncoding.ToClientMessages;

namespace WebGameServer;

public static class WebSocketHandler
{
    // Use a thread-safe dictionary to store all active WebSocket connections
    private static readonly ConcurrentDictionary<Guid, WebSocket> Sockets = new();
    
    // Add a new WebSocket connection
    public static async Task AddSocketAsync(WebSocket socket, Guid userId)
    {
        Sockets[userId] = socket;
        
        var messageToSend = WebSocketEncoder.Encode(new ClientConnectedMessage(userId));
        await socket.SendAsync(messageToSend, WebSocketMessageType.Binary, true, CancellationToken.None);
        
        //!!Spawns a task that will live until the connection is closed. 
        await StartReceiving(socket, userId);
    }

    // Remove a WebSocket connection
    private static async Task RemoveSocketAsync(Guid socketId)
    {
        if (Sockets.TryRemove(socketId, out var socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
    }

    // Broadcast a message to all connected WebSocket clients
    public static async Task SendMessageToAll(ArraySegment<byte> message)
    {
        // Send the message to each connected WebSocket
        List<Guid> disconnectedSocketIds = [];
        
        foreach (var (id, socket) in Sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                disconnectedSocketIds.Add(id);
                continue;
            }
            try
            {
                await socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception)
            {
                // Handle potential exceptions, such as closed connections
                // Optionally log the error or remove the socket from the dictionary
            }
        }
        foreach (var socketId in disconnectedSocketIds)
        {
            await RemoveSocketAsync(socketId);
        }
    }
    
    // Handle receiving messages from a WebSocket
    public const int WebSocketBufferSize = 1024 * 4; 
    private static async Task StartReceiving(WebSocket socket, Guid socketId)
    {
        var buffer = new byte[WebSocketBufferSize];
        while (socket.State == WebSocketState.Open)
        {
            try
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the client", CancellationToken.None);
                    RemoveSocketAsync(socketId);
                }
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var fromClient = WebSocketDecoder.DecodeToWrapper(buffer);
                    switch (fromClient.Type)
                    {
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                
                
            }
            catch (Exception)
            {
                // Handle exception (e.g., connection closed)
                await RemoveSocketAsync(socketId);
            }
        }
    }
}
