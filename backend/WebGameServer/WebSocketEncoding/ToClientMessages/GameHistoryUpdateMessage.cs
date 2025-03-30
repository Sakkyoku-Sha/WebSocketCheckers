using System.Runtime.InteropServices;
using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.ToClientMessages;

public readonly ref struct GameHistoryUpdateMessage(CheckersMove[] moves) : IToClientMessage<GameHistoryUpdateMessage>
{
    private readonly Span<CheckersMove> _moves = moves;
    
    const int CheckersMoveSerializedSize = sizeof(byte) * 2 + sizeof(bool) + sizeof(ulong); // 2 + 1 + 8 = 11 bytes
    
    public byte[] ToBytes()
    {
        return MemoryMarshal.AsBytes(_moves).ToArray();
    }

    public static GameHistoryUpdateMessage FromByteSpan(Span<byte> data)
    {
        if (data.Length % CheckersMoveSerializedSize != 0)
            throw new ArgumentException("Data length is not a multiple of the serialized move size.");
        
        var checkersMoveSpan = MemoryMarshal.Cast<byte, CheckersMove>(data);
        
        return new GameHistoryUpdateMessage(checkersMoveSpan.ToArray());
    }
    
    public static ToClientMessageType GetMessageType()
    {
        return ToClientMessageType.GameHistoryUpdate;
    }
}