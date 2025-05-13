using System.Diagnostics;
using WebGameServer.State;

namespace WebGameServer.WebSockets.Writers.ByteWriters;

public readonly struct ForcedMovesWriter(JumpPath[] jumpPaths) : IByteWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {   
        Debug.Assert(jumpPaths.Length <= byte.MaxValue);
        byteWriter.WriteByte((byte)jumpPaths.Length);
        for(var i = 0; i < jumpPaths.Length; i++)
        {
            byteWriter.WriteInt(jumpPaths[i].InitialPosition);
            byteWriter.WriteInt(jumpPaths[i].EndOfPath);
        }
    }

    public int CalculatePayLoadLength()
    {
        return sizeof(byte) + (sizeof(int) * jumpPaths.Length * 2);
    }
}