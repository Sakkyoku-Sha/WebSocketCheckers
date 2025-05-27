using System.Net.WebSockets;
using WebGameServer.State;

namespace WebGameServer;

public class UserSession(WebSocket socket, Guid sessionId)
{
    public readonly WebSocketChannel SocketChannel = new(socket);
    public Guid PlayerId = Guid.Empty;
    public Guid SessionId = sessionId;
    public bool Identified = false;
    public uint GameId = GameInfo.EmptyGameId;
    public bool IsInGame => GameId != GameInfo.EmptyGameId;
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
        GameId = GameInfo.EmptyGameId; 
    }
}