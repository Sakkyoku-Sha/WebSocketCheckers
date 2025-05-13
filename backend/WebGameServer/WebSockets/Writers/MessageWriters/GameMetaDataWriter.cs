using System.Diagnostics;
using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.WebSockets.Writers.ByteWriters;

namespace WebGameServer.WebSockets.Writers.MessageWriters;

public readonly struct GameMetaDataWriter(GameMetaData[] gameMetaData) : IMessageWriter
{
    private const int ActiveGamesLengthBytes = sizeof(ushort); 
    public void WriteBytes(ref ByteWriter byteWriter)
    {   
        Debug.Assert(gameMetaData.Length <= ushort.MaxValue);
        
        byteWriter.WriteUShort((ushort)gameMetaData.Length);
        for (var i = 0; i < gameMetaData.Length; i++)
        {
            var game = gameMetaData[i]; 
            byteWriter.WriteInt(game.GameId);
            byteWriter.WriteLengthPrefixedStringUTF16LE(ref game.Player1.PlayerName);
            byteWriter.WriteLengthPrefixedStringUTF16LE(ref game.Player2.PlayerName);
        }
    }

    public int CalculatePayLoadLength()
    {
        var totalBytes = ActiveGamesLengthBytes;
        for (var i = 0; i < gameMetaData.Length; i++)
        {
            var game = gameMetaData[i];
            totalBytes += GameInfoWriter.GameIdByteLength; 
            totalBytes += ByteWriterCommon.StringEncodingLength(game.Player1.PlayerName);
            totalBytes += ByteWriterCommon.StringEncodingLength(game.Player2.PlayerName);
        }
        return totalBytes;
    }

    public static ToClientMessageType MessageType => ToClientMessageType.ActiveGamesMessage;
    public static ushort Version => 1;
}