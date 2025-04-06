using System.Runtime.InteropServices;

namespace WebGameServer.WebSocketEncoding;

public ref struct ByteWriter
{
    private Span<byte> _buffer;
    public int Offset;

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
        Offset = 0; 
    }
    
    /// <summary>
    /// Writes the next 8 bytes to the span at the current offset
    /// </summary>
    /// <param name="value"></param>
    public void WriteGuid(ref Guid value)
    {
        MemoryMarshal.Write(_buffer[Offset..], in value);
    }
    
    
}
