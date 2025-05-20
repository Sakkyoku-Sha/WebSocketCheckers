using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct PlayerJoinedWriter(PlayerInfo playerInfo) : IMessageWriter
{
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteGuid(playerInfo.PlayerId);
        byteWriter.WriteLengthPrefixedStringUTF16LE(playerInfo.PlayerName);
    }

    public int CalculatePayLoadLength()
    {
        return ByteWriterCommon.GuidByteLength + 
               ByteWriterCommon.StringEncodingLength(playerInfo.PlayerName);
    }
    
    public static ToClientMessageType ResponseType => ToClientMessageType.PlayerJoined;
    public static ushort Version => 1;
}