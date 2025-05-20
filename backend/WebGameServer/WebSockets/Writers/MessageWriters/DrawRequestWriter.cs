using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct DrawRequestWriter : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        // No data to write for DrawRequest
        // This is just a signal to the client that a draw request has been made
        // and they should display the draw request UI.
    }

    public int CalculatePayLoadLength()
    {
        // No data to write for DrawRequest
        // This is just a signal to the client that a draw request has been made
        // and they should display the draw request UI.
        return 0;
    }
    
    public static ToClientMessageType ResponseType => ToClientMessageType.DrawRequest;
    public static ushort Version => 1; 
}