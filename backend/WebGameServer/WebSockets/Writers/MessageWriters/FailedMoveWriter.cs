using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct FailedMoveWriter(byte requestFromXy, byte requestToXy) : IMessageWriter
{

    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteByte(requestFromXy);
        byteWriter.WriteByte(requestToXy);
    }

    private const int PayLoadSize = sizeof(byte) + sizeof(byte);
    public int CalculatePayLoadLength() => PayLoadSize;

    public static ToClientMessageType ResponseType => ToClientMessageType.FailedMove;
    public static ushort Version => 1;
}