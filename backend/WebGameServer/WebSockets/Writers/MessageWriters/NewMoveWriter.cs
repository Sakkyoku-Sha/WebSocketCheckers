using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct NewMoveWriter(CheckersMove checkersMove, JumpPath[] jumpPaths) : IMessageWriter
{
    private readonly ForcedMovesWriter _forcedMovesWriter = new(jumpPaths);
    
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteCheckersMove(checkersMove);
        _forcedMovesWriter.WriteBytes(ref byteWriter);
    }
    public int CalculatePayLoadLength()
    {
        return CheckersMove.ByteSize + _forcedMovesWriter.CalculatePayLoadLength();
    }
    
    public static ToClientMessageType ResponseType => ToClientMessageType.NewMove;
    public static ushort Version => 1; 
}

