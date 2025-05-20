using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct SessionStartWriter(Guid sessionId) : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteGuid(sessionId);
    }

    public int CalculatePayLoadLength()
    {
        return ByteWriterCommon.GuidByteLength;
    }

    public static ToClientMessageType ResponseType => ToClientMessageType.SessionStartMessage;
    public static ushort Version => 1;
}