using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public record GameHistoryUpdateMessage(CheckersMove[] Moves) : IToClientMessage<GameHistoryUpdateMessage>
{
    const int CheckersMoveSerializedSize = sizeof(byte) * 2 + sizeof(bool) + sizeof(ulong); // 2 + 1 + 8 = 11 bytes
    public byte[] ToBytes()
    {
        var totalSize = Moves.Length * CheckersMoveSerializedSize;
        var buffer = new byte[totalSize];
        Span<byte> span = buffer;

        // Serialize each move directly into the buffer
        for (var i = 0; i < Moves.Length; i++)
        {
            var move = Moves[i];
            Span<byte> moveSpan = span.Slice(i * CheckersMoveSerializedSize, CheckersMoveSerializedSize);
            moveSpan[0] = move.FromIndex;
            moveSpan[1] = move.ToIndex;
            moveSpan[2] = (byte)(move.Promoted ? 1 : 0);
            BitConverter.TryWriteBytes(moveSpan.Slice(3), move.CapturedPieces);
        }

        return buffer;
    }

    public static GameHistoryUpdateMessage FromBytes(Span<byte> data)
    {
        if (data.Length % CheckersMoveSerializedSize != 0)
            throw new ArgumentException("Data length is not a multiple of the serialized move size.");

        int moveCount = data.Length / CheckersMoveSerializedSize;
        CheckersMove[] moves = new CheckersMove[moveCount];
        ReadOnlySpan<byte> span = data;

        for (int i = 0; i < moveCount; i++)
        {
            ReadOnlySpan<byte> moveSpan = span.Slice(i * CheckersMoveSerializedSize, CheckersMoveSerializedSize);
            moves[i] = DeserializeMove(moveSpan);
        }

        return new GameHistoryUpdateMessage(moves);
    }
    
    private static CheckersMove DeserializeMove(ReadOnlySpan<byte> span)
    {
        if (span.Length != CheckersMoveSerializedSize)
            throw new ArgumentException($"Span length must be {CheckersMoveSerializedSize} bytes.");

        byte fromIndex = span[0];
        byte toIndex = span[1];
        bool promoted = span[2] != 0;
        ulong capturedPieces = BitConverter.ToUInt64(span.Slice(3));
        return new CheckersMove(fromIndex, toIndex, promoted, capturedPieces);
    }

    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.GameHistoryUpdate;
    }
}