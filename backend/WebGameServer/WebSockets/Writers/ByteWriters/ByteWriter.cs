using System.Diagnostics;
using System.Runtime.InteropServices;
using WebGameServer.State;

namespace WebGameServer.WebSockets.Writers.ByteWriters;

public ref struct ByteWriter
{
    private Span<byte> _buffer;
    private int _offset;
    
    /// <summary>
    /// This class takes the buffer it writes to, it will never attempt to expand or copy this buffer
    /// if you attempt to write more information to the byte writer than that exists it will throw an exception.
    ///
    /// It is the responsibility of the creator of this class to ensure this is the correct size. 
    /// </summary>
    /// <param name="buffer"></param>
    public ByteWriter(Span<byte> buffer)
    {
        _buffer = buffer;
        _offset = 0; 
    }
    
    /// <summary>
    /// Writes the next 8 bytes to the span at the current offset
    /// </summary>
    /// <param name="value"></param>
    public void WriteGuid(Guid value)
    {
        MemoryMarshal.Write(_buffer[_offset..], in value);
        _offset += 16;
    }
    
    /// <summary>
    /// Writes the next 8 bytes to the span at the current offset
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt(int value)
    {
        MemoryMarshal.Write(_buffer[_offset..], in value);
        _offset += sizeof(int);
    }
    
    public void WriteLengthPrefixedStringUTF16LE(string value)
    { 
        var stringSpan = MemoryMarshal.AsBytes(value.AsSpan());
        
        //For now all string fields prefix with the length of the string in a single byte followed by the string itself. 
        Debug.Assert(stringSpan.Length <= byte.MaxValue);
        WriteByte((byte)stringSpan.Length); 
       
       //This approach only works in .Net 5+ as it depends on the string values natively being encoded as UTF16LE 
       //This is done to avoid the cost of creating a new string each time in order to generate a UTF8 encoding.
       //The string lengths here aren't long enough to make this a problem. 
    
       stringSpan.CopyTo(_buffer[_offset..]);
       _offset += stringSpan.Length;
    }
    
    public void WriteCheckersMoves(CheckersMove[] moves, int amountToWrite)
    {
        if (moves.Length <= 0) return;
        
        //Note this works due to the sequential attribute tag on the Checkers Move. 
        var moveSpan = moves.AsSpan(0, amountToWrite);
        MemoryMarshal.AsBytes(moveSpan).CopyTo(_buffer[_offset..]);
        
        _offset += amountToWrite * CheckersMove.ByteSize;
    }
    public void WriteCheckersMove(ref readonly CheckersMove move)
    {
        MemoryMarshal.Write(_buffer[_offset..], in move);
        _offset += CheckersMove.ByteSize;
    }
    public void WriteCheckersMove(CheckersMove move)
    {
        MemoryMarshal.Write(_buffer[_offset..], in move);
        _offset += CheckersMove.ByteSize;
    }
    
    public void WriteULong(ulong value)
    {
        MemoryMarshal.Write(_buffer[_offset..], in value);
        _offset += sizeof(ulong);
    }

    public void WriteUShort(ushort value)
    {
        MemoryMarshal.Write(_buffer[_offset..], in value);
        _offset += sizeof(ushort); 
    }

    public void WriteByte(byte singleByte)
    {
        _buffer[_offset] = singleByte;
        _offset += 1;
    }
    
    public void WriteBool(bool boolean)
    {
        _buffer[_offset] = boolean ? (byte)1 : (byte)0;
        _offset += 1;
    }
    public int BytesWritten => _offset;
}
