using System.Runtime.InteropServices;
namespace WebGameServer.State;

//Required for Efficient Byte Serialization DO NOT DELETE 
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct CheckersMove(
    byte fromIndex,
    byte toIndex,
    bool promoted,
    ulong capturedPawns,
    ulong capturedKings)
{
    [FieldOffset(0)] public readonly byte FromIndex = fromIndex;
    [FieldOffset(1)] public readonly byte ToIndex = toIndex;
    [FieldOffset(2)] public readonly bool Promoted = promoted;
    [FieldOffset(3)] public readonly ulong CapturedPawns = capturedPawns;
    [FieldOffset(11)] public readonly ulong CapturedKings = capturedKings;
    
    public const int ByteSize = 19; // 1 + 1 + 1 + 8 + 8
}