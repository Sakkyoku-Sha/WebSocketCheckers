using System.Net.WebSockets;

namespace WebGameServer;

public class UserSession
{
    private readonly WebSocket _socket;
    public UserSession(WebSocket socket, Guid sessionId)
    {
        _socket = socket;
        SocketWriter = new WebSocketWriter(socket); 
        SessionId = sessionId;
        PlayerId = null;
        GameId = -1; 
        Identified = false;
        TimeOutToken = new CancellationToken();
    }

    public readonly WebSocketWriter SocketWriter;
    public Guid? PlayerId;
    public Guid SessionId;
    public bool Identified;
    public int GameId;
    public CancellationToken TimeOutToken;
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