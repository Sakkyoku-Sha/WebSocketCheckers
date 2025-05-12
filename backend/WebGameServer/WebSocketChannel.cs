using System.Net.WebSockets;
using System.Threading.Channels;

namespace WebGameServer;

public class WebSocketChannel
{
    private readonly Channel<(ArraySegment<byte>, TaskCompletionSource<bool>)> _channel;
    private readonly WebSocket _webSocket;
    
    public WebSocketChannel(WebSocket webSocket, int capacity = 100)
    {
        _webSocket = webSocket;
        _channel = Channel.CreateBounded<(ArraySegment<byte>, TaskCompletionSource<bool>)>(new BoundedChannelOptions(capacity));

        _ = ConsumeSendQueueAsync();
    }

    public async Task SendAsync(byte[] segment)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _channel.Writer.WriteAsync((segment, tcs));
        await tcs.Task;
    }
    
    private async Task ConsumeSendQueueAsync()
    {
        try
        {
            await foreach (var (segment, tcs) in _channel.Reader.ReadAllAsync())
            {
                try
                {
                    await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }
        }
        catch (Exception ex) //might want to introduce recovery logic here. 
        {
            Console.WriteLine(ex);
        }
    }
}