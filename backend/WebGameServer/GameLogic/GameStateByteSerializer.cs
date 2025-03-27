using System.Runtime.InteropServices;

namespace WebGameServer.GameLogic;

public static class GameStateByteSerializer
{
    public static ArraySegment<byte> SerializeEntireHistory(GameState state)
    {
        var span = MemoryMarshal.AsBytes(state.MoveHistory.AsSpan(0, state.MoveHistoryCount));
        return new ArraySegment<byte>(span.ToArray());
    }

    public static ArraySegment<byte> SerializeMostRecentHistory(GameState state)
    {
        var buffer = new byte[4];
        var mostRecentHistory = state.MoveHistory[state.MoveHistoryCount - 1]; 
        buffer[0] = mostRecentHistory.FromX;
        buffer[1] = mostRecentHistory.FromY;
        buffer[2] = mostRecentHistory.ToX;
        buffer[3] = mostRecentHistory.ToY;
        return buffer;
    }
}