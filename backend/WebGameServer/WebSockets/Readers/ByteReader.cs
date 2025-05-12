using System.Runtime.InteropServices;
using WebGameServer.State;

namespace WebGameServer.WebSockets
{
    public ref struct ByteReader
    {
        private ReadOnlySpan<byte> Buffer;
        public int Offset;

        /// <summary>
        /// Constructs a reader over the given buffer.
        /// </summary>
        public ByteReader(ReadOnlySpan<byte> buffer)
        {
            Buffer = buffer;
            Offset = 0;
        }

        /// <summary>
        /// Reads a Guid (16 bytes) and advances the offset.
        /// </summary>
        public Guid ReadGuid()
        {
            var value = Buffer.Slice(Offset, 16);
            Offset += 16;
            return MemoryMarshal.Cast<byte, Guid>(value)[0];
        }
        
        /// <summary>
        /// Reads the next "encodingLength" amount of bytes as a UTF16LE String 
        /// </summary>
        public string ReadStringUTF16LE(int encodingLength)
        {
            var slice = Buffer.Slice(Offset, encodingLength);
            // reinterpret bytes as chars
            var charSpan = MemoryMarshal.Cast<byte, char>(slice);
            var result = new string(charSpan);
            Offset += encodingLength;
            return result;
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> CheckersMove structs.
        /// </summary>
        public ReadOnlySpan<CheckersMove> ReadCheckersMoves(int count)
        {
            int byteCount = count * CheckersMove.ByteSize;
            var slice = Buffer.Slice(Offset, byteCount);
            var movesSpan = MemoryMarshal.Cast<byte, CheckersMove>(slice);
            Offset += byteCount;
            return movesSpan;
        }

        /// <summary>
        /// Reads an unsigned 64‑bit integer.
        /// </summary>
        public ulong ReadULong()
        {
            var value = MemoryMarshal.Read<ulong>(Buffer[Offset..]);
            Offset += sizeof(ulong);
            return value;
        }

        /// <summary>
        /// Reads an unsigned 16‑bit integer.
        /// </summary>
        public ushort ReadUShort()
        {
            var value = MemoryMarshal.Read<ushort>(Buffer[Offset..]);
            Offset += sizeof(ushort);
            return value;
        }

        /// <summary>
        /// Reads a single byte.
        /// </summary>
        public byte ReadByte()
        {
            byte value = Buffer[Offset];
            Offset += 1;
            return value;
        }

        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            var result = Buffer.Slice(Offset, length);
            Offset += length;
            return result;
        }
        
        public T ReadFixedSizeStruct<T>() where T : struct, IFixedByteSize
        {
            var slice = Buffer.Slice(Offset, T.ByteSize);
            Offset += T.ByteSize;
            
            return MemoryMarshal.Read<T>(slice);
        }
    }
}
public interface IFixedByteSize
{
    public static abstract int ByteSize { get; }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct TryMakeMoveRequest(Guid playerId, int gameId, byte fromXy, byte toXy) : IFixedByteSize
{
    public readonly Guid PlayerId = playerId;
    public readonly int GameId = gameId;
    public readonly byte FromXy = fromXy; //Bit Board Format e.g 10 == (2, 1) 
    public readonly byte ToXy = toXy;
    public static int ByteSize => 16 + sizeof(int) + sizeof(byte) + sizeof(byte);
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct TryJoinGameRequest(Guid playerId, int gameId) : IFixedByteSize
{
    public readonly Guid PlayerId = playerId;
    public readonly int GameId = gameId;
    
    public static int ByteSize => 16 + sizeof(int);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct TryCreateGameRequest(Guid playerId) : IFixedByteSize
{
    public readonly Guid PlayerId = playerId;
    public static int ByteSize => 16; 
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct IdentifyUserMessage(Guid playerId) : IFixedByteSize
{
    public readonly Guid PlayerId = playerId;
    public static int ByteSize => 16;
}
