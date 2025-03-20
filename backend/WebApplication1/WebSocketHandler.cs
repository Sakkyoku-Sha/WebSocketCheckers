using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WebApplication1;

public static class WebSocketHandler
{
    // Use a thread-safe dictionary to store all active WebSocket connections
    private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

    // Add a new WebSocket connection
    public static void AddSocket(WebSocket socket)
    {
        string socketId = Guid.NewGuid().ToString();
        _sockets[socketId] = socket;
        //StartReceiving(socket, socketId);
    }

    // Remove a WebSocket connection
    public static void RemoveSocket(string socketId)
    {
        _sockets.TryRemove(socketId, out WebSocket _);
    }

    // Broadcast a message to all connected WebSocket clients
    public static async Task SendMessageToAll(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);

        // Send the message to each connected WebSocket
        foreach (var socket in _sockets.Values)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception)
                {
                    // Handle potential exceptions, such as closed connections
                    // Optionally log the error or remove the socket from the dictionary
                }
            }
        }
    }

    // Handle receiving messages from a WebSocket
    private static async void StartReceiving(WebSocket socket, string socketId)
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
