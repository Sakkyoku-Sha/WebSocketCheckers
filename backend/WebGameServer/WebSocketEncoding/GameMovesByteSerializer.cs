using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding;

public static class GameMovesByteSerializer
{
    const int CheckersMoveSerializedSize = sizeof(byte) * 2 + sizeof(bool) + sizeof(ulong); // 2 + 1 + 8 = 11 bytes

    // Serialize methods (as you already have them)
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
            moveSpan[0] = move.FromIndex;
            moveSpan[1] = move.ToIndex;
            moveSpan[2] = (byte)(move.Promoted ? 1 : 0);
            BitConverter.TryWriteBytes(moveSpan.Slice(3), move.CapturedPieces);
        }

        return new ArraySegment<byte>(buffer);
    }

    public static ArraySegment<byte> SerializeMostRecentHistory(GameState state)
    {
        if (state.MoveHistoryCount == 0)
        {
            return new ArraySegment<byte>(Array.Empty<byte>());
        }

        return Serialize(state.MoveHistory[state.MoveHistoryCount - 1]);
    }

    private static byte[] Serialize(CheckersMove move)
    {
        var buffer = new byte[CheckersMoveSerializedSize];
        Span<byte> span = buffer.AsSpan(0, CheckersMoveSerializedSize);
        span[0] = move.FromIndex;
        span[1] = move.ToIndex;
        span[2] = (byte)(move.Promoted ? 1 : 0);
        BitConverter.TryWriteBytes(span.Slice(3), move.CapturedPieces);
        return buffer;
    }

    // ----------------- DESERIALIZATION METHODS -----------------

    /// <summary>
    /// Deserialize a single CheckersMove from a span of bytes.
    /// Assumes that the span is exactly CheckersMoveSerializedSize bytes long.
    /// </summary>
    public static CheckersMove DeserializeMove(ReadOnlySpan<byte> span)
    {
        if (span.Length != CheckersMoveSerializedSize)
            throw new ArgumentException($"Span length must be {CheckersMoveSerializedSize} bytes.");

        byte fromIndex = span[0];
        byte toIndex = span[1];
        bool promoted = span[2] != 0;
        ulong capturedPieces = BitConverter.ToUInt64(span.Slice(3));
        return new CheckersMove(fromIndex, toIndex, promoted, capturedPieces);
    }

    /// <summary>
    /// Deserialize an entire history of moves from an ArraySegment<byte>.
    /// </summary>
    public static CheckersMove[] DeserializeEntireHistory(ArraySegment<byte> data)
    {
        if (data.Count % CheckersMoveSerializedSize != 0)
            throw new ArgumentException("Data length is not a multiple of the serialized move size.");

        int moveCount = data.Count / CheckersMoveSerializedSize;
        CheckersMove[] moves = new CheckersMove[moveCount];
        ReadOnlySpan<byte> span = data.AsSpan();

        for (int i = 0; i < moveCount; i++)
        {
            ReadOnlySpan<byte> moveSpan = span.Slice(i * CheckersMoveSerializedSize, CheckersMoveSerializedSize);
            moves[i] = DeserializeMove(moveSpan);
        }

        return moves;
    }
}