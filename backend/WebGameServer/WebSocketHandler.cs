using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebGameServer;

public static class WebSocketHandler
{
    // Use a thread-safe dictionary to store all active WebSocket connections
    private static ConcurrentDictionary<string, WebSocket> _sockets = new();

    // Add a new WebSocket connection
    public static async Task AddSocketAsync(WebSocket socket, byte[] connectionMessage)
    {
        var socketId = Guid.NewGuid().ToString();
        _sockets[socketId] = socket;
        
        var segment = new ArraySegment<byte>(connectionMessage);
        await socket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
        
        //!!Spawns a task that will live until the connection is closed. 
        await StartReceiving(socket, socketId);
    }

    // Remove a WebSocket connection
    private static void RemoveSocket(string socketId)
    {
        _sockets.TryRemove(socketId, out WebSocket _);
    }

    // Broadcast a message to all connected WebSocket clients
    public static async Task SendMessageToAll(byte[] message)
    {
        var segment = new ArraySegment<byte>(message);

        // Send the message to each connected WebSocket
        List<string> disconnectedSocketIds = [];
        
        foreach (var (id, socket) in _sockets)
        {
            if (socket.State != WebSocketState.Open)
            {
                disconnectedSocketIds.Add(id);
                continue;
            }
            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception)
            {
                // Handle potential exceptions, such as closed connections
                // Optionally log the error or remove the socket from the dictionary
            }
        }
        foreach (var socketId in disconnectedSocketIds)
        {
            RemoveSocket(socketId);
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
                    RemoveSocket(socketId);
                }
            }
            catch (Exception)
            {
                // Handle exception (e.g., connection closed)
                RemoveSocket(socketId);
            }
        }
    }
}
