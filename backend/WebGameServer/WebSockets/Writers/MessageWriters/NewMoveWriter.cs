using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public struct NewMoveWriter(ref CheckersMove checkersMove, int[] forcedMovesInPosition) : IMessageWriter
{
    private CheckersMove _checkersMove = checkersMove;
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteCheckersMove(ref _checkersMove);
        byteWriter.WriteByte((byte)forcedMovesInPosition.Length);
        for(var i = 0; i < forcedMovesInPosition.Length; i++)
        {
            byteWriter.WriteInt(forcedMovesInPosition[i]);
        }
    }
    public int CalculatePayLoadLength()
    {
        var totalBytes = CheckersMove.ByteSize + sizeof(byte);
        for(var i = 0; i < forcedMovesInPosition.Length; i++)
        {
            totalBytes += sizeof(int);
        }
        return totalBytes;
    }

    public static ToClientMessageType MessageType => ToClientMessageType.NewMoveMessage;
    public static ushort Version => 1; 
}