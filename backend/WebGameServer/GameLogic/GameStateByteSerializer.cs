namespace WebGameServer.GameLogic;

public static class GameStateByteSerializer
{
    // Serialize the entire history, ensuring the exact number of bytes
    public static ArraySegment<byte> SerializeEntireHistory(GameState state)
    {
        int totalSize = state.MoveHistoryCount * CheckersMoveSerializedSize;
        
        var buffer = new byte[totalSize];
        Span<byte> span = buffer;

        // Serialize each move directly into the buffer
        for (int i = 0; i < state.MoveHistoryCount; i++)
        {
            var move = state.MoveHistory[i];
            Span<byte> moveSpan = span.Slice(i * CheckersMoveSerializedSize, CheckersMoveSerializedSize);

            // Serialize the individual move
            moveSpan[0] = move.FromIndex;
            moveSpan[1] = move.ToIndex;
            moveSpan[2] = (byte)(move.Promoted ? 1 : 0);
            BitConverter.TryWriteBytes(moveSpan.Slice(3), move.CapturedPieces);
        }

        // Return an ArraySegment for memory efficiency
        return new ArraySegment<byte>(buffer);
    }
    
    public static ArraySegment<byte> SerializeMostRecentHistory(GameState state)
    {
        if (state.MoveHistoryCount == 0)
        {
            return new ArraySegment<byte>([]); 
        }
        
        return Serialize(state.MoveHistory[state.MoveHistoryCount - 1]);
    }
    
    const int CheckersMoveSerializedSize = sizeof(byte) * 2 + sizeof(bool) + sizeof(ulong); // Total size of a CheckersMove
    private static byte[] Serialize(CheckersMove move)
    {
        var buffer = new byte[CheckersMoveSerializedSize];
        
        // Get a memory span to write the data
        Span<byte> span = buffer.AsSpan(0, CheckersMoveSerializedSize);
        
        // Write each field to the span
        span[0] = move.FromIndex;                // byte
        span[1] = move.ToIndex;                  // byte
        span[2] = (byte)(move.Promoted ? 1 : 0); // bool as byte (0 or 1)
        BitConverter.TryWriteBytes(span.Slice(3), move.CapturedPieces); // ulong
        
        return buffer;
    }
}