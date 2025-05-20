using System.Net.WebSockets;

namespace WebGameServer;

public class UserSession(WebSocket socket, Guid sessionId)
{
    private const int NoGame = -1;
    
    public readonly WebSocketChannel SocketChannel = new(socket);
    public Guid PlayerId = Guid.Empty;
    public Guid SessionId = sessionId;
    public bool Identified = false;
    public int GameId = NoGame;
    public bool IsInGame => GameId >= 0;
    public CancellationToken TimeOutToken = CancellationToken.None;
    public WebSocketState State => socket.State;

    public async Task CloseAsync(WebSocketCloseStatus normalClosure, string empty, CancellationToken none)
    {
        await socket.CloseAsync(normalClosure, empty, none);
    }

    public async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> arraySegment, CancellationToken none)
    {
        return await socket.ReceiveAsync(arraySegment, none);
    }

    public void ResetGameId()
    {
        GameId = NoGame; 
    }
}