﻿using System.Diagnostics;
using WebGameServer.State;

namespace WebGameServer.WebSockets.Writers.ByteWriters;

public readonly struct GameInfoWriter(GameInfo? gameInfo) : IByteWriter
{
    public const int GameIdByteLength = sizeof(int);

    private const int GameStatusEncodingLength = 1; 
    private const int GameHistoryLengthEncodingLength = 2;
    
    private readonly ForcedMovesWriter? _forcedMovesWriter = 
        gameInfo == null ? null : new ForcedMovesWriter(gameInfo.GameState.CurrentForcedJumps);
    
    public void WriteBytes(ref ByteWriter byteWriter)
    {
        if (gameInfo == null) return; 
        
        //GameInfo
        byteWriter.WriteInt(gameInfo.GameId);
        byteWriter.WriteByte((byte)gameInfo.Status);
        byteWriter.WriteLengthPrefixedStringUTF16LE(gameInfo.GameName);
        
        //Player 1 Info 
        ref var player1 = ref gameInfo.Player1;
        byteWriter.WriteGuid(player1.PlayerId);
        byteWriter.WriteLengthPrefixedStringUTF16LE(player1.PlayerName);
        
        //Player 2 Info 
        ref var player2 = ref gameInfo.Player2;
        byteWriter.WriteGuid(player2.PlayerId);
        byteWriter.WriteLengthPrefixedStringUTF16LE(player2.PlayerName);

        //Game History Info
        byteWriter.WriteUShort((ushort)gameInfo.MoveHistoryCount);
        byteWriter.WriteCheckersMoves(gameInfo.MoveHistory, gameInfo.MoveHistoryCount);
        
        //Forced Moves in Current Position. 
        _forcedMovesWriter?.WriteBytes(ref byteWriter);
    }
    
    public int CalculatePayLoadLength()
    {   
        if(gameInfo == null) return 0;
        
        var gameNameEncodedLength = ByteWriterCommon.StringEncodingLength(gameInfo.GameName);
        var player1NameEncodedLength =  ByteWriterCommon.StringEncodingLength(gameInfo.Player1.PlayerName);
        var player2NameEncodedLength =  ByteWriterCommon.StringEncodingLength(gameInfo.Player2.PlayerName);
        var forcedMovesLength = _forcedMovesWriter?.CalculatePayLoadLength() ?? 0;
        
        Debug.Assert(gameNameEncodedLength <= byte.MaxValue);
        Debug.Assert(player1NameEncodedLength <= byte.MaxValue);
        Debug.Assert(player2NameEncodedLength <= byte.MaxValue);
        
        var totalByteCount =
            GameIdByteLength +
            GameStatusEncodingLength + 
            gameNameEncodedLength +
            ByteWriterCommon.GuidByteLength + player1NameEncodedLength +
            ByteWriterCommon.GuidByteLength + player2NameEncodedLength +
            GameHistoryLengthEncodingLength + 
            (CheckersMove.ByteSize * gameInfo.MoveHistoryCount) + 
            forcedMovesLength;

        return totalByteCount;
    }
}