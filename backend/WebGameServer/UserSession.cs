using System.Net.WebSockets;

namespace WebGameServer;

public class UserSession(WebSocket socket, Guid sessionId)
{
    private readonly WebSocket _socket = socket;

    public readonly WebSocketChannel SocketChannel = new(socket);
    public Guid? PlayerId = null;
    public Guid SessionId = sessionId;
    public bool Identified = false;
    public int GameId = -1;
    public bool IsInGame => GameId >= 0;
    public CancellationToken TimeOutToken = CancellationToken.None;
    public WebSocketState State => _socket.State;

    public async Task CloseAsync(WebSocketCloseStatus normalClosure, string empty, CancellationToken none)
    {
        await _socket.CloseAsync(normalClosure, empty, none);
    }

    public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> arraySegment, CancellationToken none)
    {
        return await _socket.ReceiveAsync(arraySegment, none);
    }
}