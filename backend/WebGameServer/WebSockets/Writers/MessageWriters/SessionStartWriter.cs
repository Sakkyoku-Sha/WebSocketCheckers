using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public struct SessionStartWriter(ref Guid sessionId) : IMessageWriter
{
    private Guid _sessionId = sessionId;
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteGuid(ref _sessionId);
    }

    public int CalculatePayLoadLength()
    {
        return ByteWriterCommon.GuidByteLength;
    }

    public static ToClientMessageType MessageType => ToClientMessageType.SessionStartMessage;
    public static ushort Version => 1;
}