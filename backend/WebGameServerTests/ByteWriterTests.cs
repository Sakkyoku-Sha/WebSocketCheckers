using System.Text;
using WebGameServer.WebSockets.Writers.ByteWriters;
using WebGameServer.State; // Assumes CheckersMove is defined here

[TestFixture]
public class ByteWriterTests
{
    [Test]
    public void WriteBool_True_Writes1()
    {
        var buffer = new byte[1];
        var writer = new ByteWriter(buffer);
        
        writer.WriteBool(true);
        
        Assert.That(buffer[0], Is.EqualTo(1));
        Assert.That(writer.BytesWritten, Is.EqualTo(1));
    }

    [Test]
    public void WriteBool_False_Writes0()
    {
        var buffer = new byte[1];
        var writer = new ByteWriter(buffer);
        
        writer.WriteBool(false);
        
        Assert.That(buffer[0], Is.EqualTo(0));
        Assert.That(writer.BytesWritten, Is.EqualTo(1));
    }

    [Test]
    public void WriteByte_WritesCorrectValue()
    {
        var buffer = new byte[1];
        var writer = new ByteWriter(buffer);

        writer.WriteByte(42);

        Assert.That(buffer[0], Is.EqualTo(42));
        Assert.That(writer.BytesWritten, Is.EqualTo(1));
    }

    [Test]
    public void WriteUShort_WritesCorrectBytes()
    {
        var buffer = new byte[2];
        var writer = new ByteWriter(buffer);

        writer.WriteUShort(0x1234);

        Assert.That(buffer, Is.EqualTo(new byte[] { 0x34, 0x12 }));
        Assert.That(writer.BytesWritten, Is.EqualTo(2));
    }

    [Test]
    public void WriteInt_WritesCorrectBytes()
    {
        var buffer = new byte[4];
        var writer = new ByteWriter(buffer);

        writer.WriteInt(0x78563412);

        Assert.That(buffer, Is.EqualTo(new byte[] { 0x12, 0x34, 0x56, 0x78 }));
        Assert.That(writer.BytesWritten, Is.EqualTo(4));
    }

    [Test]
    public void WriteULong_WritesCorrectBytes()
    {
        var buffer = new byte[8];
        var writer = new ByteWriter(buffer);

        writer.WriteULong(0x1122334455667788UL);

        Assert.That(buffer, Is.EqualTo(new byte[] { 0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11 }));
        Assert.That(writer.BytesWritten, Is.EqualTo(8));
    }

    [Test]
    public void WriteGuid_WritesCorrectBytes()
    {
        var buffer = new byte[16];
        var writer = new ByteWriter(buffer);

        var guid = Guid.NewGuid();
        writer.WriteGuid(ref guid);

        var guidBytes = guid.ToByteArray();
        Assert.That(buffer[..16], Is.EqualTo(guidBytes));
        Assert.That(writer.BytesWritten, Is.EqualTo(16));
    }

    [Test]
    public void WriteLengthPrefixedStringUTF16LE_WritesCorrectly()
    {
        var buffer = new byte[256];
        var writer = new ByteWriter(buffer);

        var input = "AB";
        writer.WriteLengthPrefixedStringUTF16LE(ref input);

        // Length-prefixed with 4 bytes (2 UTF-16 chars = 4 bytes)
        Assert.That(buffer[0], Is.EqualTo(4)); // 4 bytes for "AB" in UTF16LE
        Assert.That(buffer[1], Is.EqualTo((byte)'A'));
        Assert.That(buffer[2], Is.EqualTo(0)); // 'A' in UTF16LE
        Assert.That(buffer[3], Is.EqualTo((byte)'B'));
        Assert.That(buffer[4], Is.EqualTo(0)); // 'B' in UTF16LE
        Assert.That(writer.BytesWritten, Is.EqualTo(5));
    }

    [Test]
    public void WriteCheckersMove_WritesCorrectSize()
    {
        var buffer = new byte[CheckersMove.ByteSize];
        var writer = new ByteWriter(buffer);

        var move = new CheckersMove(1, 2, true, 4);
        writer.WriteCheckersMove(ref move);

        Assert.That(writer.BytesWritten, Is.EqualTo(CheckersMove.ByteSize));
    }

    [Test]
    public void WriteCheckersMoves_WritesArrayCorrectly()
    {
        var buffer = new byte[CheckersMove.ByteSize * 2];
        var writer = new ByteWriter(buffer);

        var moves = new[]
        {
            new CheckersMove(1,2,true,4),
            new CheckersMove(5,6,false,8),
        };
        
        writer.WriteCheckersMoves(moves, 2);

        Assert.That(writer.BytesWritten, Is.EqualTo(CheckersMove.ByteSize * 2));
    }
    
    [Test]
    public void ForcedMovesWriter_CalculatePayLoadLength_ReturnsCorrectLength()
    {
        var jumps = new[]
        {
           new JumpPath(1,true, 0, 9),
           new JumpPath(2,true, 0, 4),
        };
        
        var writer = new ForcedMovesWriter(jumps);
        var byteWriter = new ByteWriter(new byte[1024]);
        
        writer.WriteBytes(ref byteWriter);
        
        Assert.That(writer.CalculatePayLoadLength(), Is.EqualTo(byteWriter.BytesWritten));
    }
    
    [Test]
    public void StringEncodingLength_SimpleAscii_ReturnsCorrectLength()
    {
        var input = "Hi"; // 2 characters
        var expectedBytes = ByteWriterCommon.StringLengthEncodingBytes + Encoding.Unicode.GetByteCount(input); // 1 + 4
        var result = ByteWriterCommon.StringEncodingLength(input);

        Assert.That(result, Is.EqualTo(expectedBytes));
    }

    [Test]
    public void StringEncodingLength_EmptyString_Returns1()
    {
        var input = "";
        var expected = ByteWriterCommon.StringLengthEncodingBytes; // Just the prefix byte

        var result = ByteWriterCommon.StringEncodingLength(input);

        Assert.That(result, Is.EqualTo(expected));
    }
}
