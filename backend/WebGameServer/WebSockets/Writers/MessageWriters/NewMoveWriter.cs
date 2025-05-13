using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public struct NewMoveWriter(ref CheckersMove checkersMove, JumpPath[] jumpPaths) : IMessageWriter
{
    private CheckersMove _checkersMove = checkersMove;
    private readonly ForcedMovesWriter _forcedMovesWriter = new(jumpPaths);
    
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteCheckersMove(ref _checkersMove);
        _forcedMovesWriter.WriteBytes(ref byteWriter);
    }
    public int CalculatePayLoadLength()
    {
        return CheckersMove.ByteSize + _forcedMovesWriter.CalculatePayLoadLength();
    }

    public static ToClientMessageType MessageType => ToClientMessageType.NewMoveMessage;
    public static ushort Version => 1; 
}