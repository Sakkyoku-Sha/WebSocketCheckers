using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct DrawRequestRejectedWriter : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        // No additional data to write for this message
    }

    public int CalculatePayLoadLength()
    {
        // No additional data to write for this message
        return 0;
    }
    
    public static ToClientMessageType ResponseType => ToClientMessageType.DrawRequestRejected;
    public static ushort Version => 1;
}