using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public struct OtherPlayerJoinedWriter(ref PlayerInfo playerInfo) : IMessageWriter
{
    private PlayerInfo _playerInfo = playerInfo;
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        byteWriter.WriteGuid(ref _playerInfo.PlayerId);
        byteWriter.WriteLengthPrefixedStringUTF16LE(ref _playerInfo.PlayerName);
    }

    public int CalculatePayLoadLength()
    {
        return ByteWriterCommon.GuidByteLength + 
               ByteWriterCommon.StringEncodingLength(_playerInfo.PlayerName);
    }

    public static ToClientMessageType MessageType => ToClientMessageType.PlayerJoined;
    public static ushort Version => 1;
}