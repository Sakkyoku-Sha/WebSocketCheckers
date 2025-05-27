using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct NewMoveWriter(TimedCheckersMove checkersMove, JumpPath[] jumpPaths) : IMessageWriter
{
    private readonly ForcedMovesWriter _forcedMovesWriter = new(jumpPaths);
    
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteTimedCheckersMove(checkersMove);
        _forcedMovesWriter.WriteBytes(ref byteWriter);
    }
    public int CalculatePayLoadLength()
    {
        return TimedCheckersMove.ByteSize + _forcedMovesWriter.CalculatePayLoadLength();
    }
    
    public static ToClientMessageType ResponseType => ToClientMessageType.NewMove;
    public static ushort Version => 1; 
}

