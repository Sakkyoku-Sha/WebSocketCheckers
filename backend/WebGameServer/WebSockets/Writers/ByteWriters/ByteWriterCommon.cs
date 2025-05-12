using System.Diagnostics;
using System.Text;

namespace WebGameServer.WebSockets.Writers.ByteWriters;

public static class ByteWriterCommon
{
    public const int GuidByteLength = 16;
    public const int StringLengthEncodingBytes = 1;
    public static int StringEncodingLength(string str)
    {
        var encodingLength = Encoding.Unicode.GetByteCount(str);
        Debug.Assert(encodingLength <= byte.MaxValue);
        
        return StringLengthEncodingBytes + encodingLength;
    }
}