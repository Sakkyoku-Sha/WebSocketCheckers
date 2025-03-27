using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebGameServer;

public static class WebSocketHandler
{
    // Use a thread-safe dictionary to store all active WebSocket connections
    private static readonly ConcurrentDictionary<string, WebSocket> Sockets = new();

    // Add a new WebSocket connection
    public static async Task AddSocketAsync(WebSocket socket, ArraySegment<byte> connectionMessage)
    {
        var socketId = Guid.NewGuid().ToString();
        Sockets[socketId] = socket;
        
        await socket.SendAsync(connectionMessage, WebSocketMessageType.Binary, true, CancellationToken.None);
        
        //!!Spawns a task that will live until the connection is closed. 
        await StartReceiving(socket, socketId);
    }

    // Remove a WebSocket connection
    private static void RemoveSocketAsync(string socketId)
    {
        Sockets.TryRemove(socketId, out _);
    }

    // Broadcast a message to all connected WebSocket clients
    public static async Task SendMessageToAll(ArraySegment<byte> message)
    {
        // Send the message to each connected WebSocket
        List<string> disconnectedSocketIds = [];
        
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
            RemoveSocketAsync(socketId);
        }
    }

    // Handle receiving messages from a WebSocket
    private static async Task StartReceiving(WebSocket socket, string socketId)
    {
        var buffer = new byte[1024 * 4];
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
            }
            catch (Exception)
            {
                // Handle exception (e.g., connection closed)
                RemoveSocketAsync(socketId);
            }
        }
    }
}
