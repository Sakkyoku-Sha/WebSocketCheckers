using System.Buffers;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using WebGameServer.State;

namespace WebGameServer.WebSocketEncoding.Writers;

public enum ToClientMessageType : ushort
{
    SessionStartMessage = 0,
    PlayerJoined = 1,
    NewMoveMessage = 2, 
    GameInfoMessage = 3,
}

/*
 *  The goal of this class, and the reason it is written the way it is written is due to an attempt 
 *  to have minimal memory copying of data from the GameInfo data type to writing to the socket.
 *
 *  Copying of Structs when serializing has non-trivial costs at scale, and the attempt here is to
 *  figure out how to utilize a ref struct byte writer, and calls to the ref values of fields of game info
 *  and other types in order to achieve this.
 *
 *  The problem comes with this approach is code duplication, due to ref structs existing on stack, and not the heap,
 *  you cannot simply write a helper ref struct to handle the writing of the data and pass it into a refactored async
 *  method like "WriteAsync" as the ref struct cannot be passed into an async method in C# at the time of writing. 
 *
 *  This means you either need to A) eat the cost of copying the data into a class (or other heap allocated member)
 *  or B) have code duplication.
 * 
 *  If we had a class load up the logic, only then could you finally make an interface that could abstract away
 *  the duplication of code in the class. However, if doing so you incur the cost of copying the underlying data to
 *  the constructor method of such a class (as classes cannot have ref field members), as well as incur the cost of
 *  allocating heap memory for such a class, just so we can abstract a way logic.
 *  This is a not a "zero cost abstraction" in this case. 
 *
 *  To avoid that I attempted to try and use generics with static methods to avoid such costs; however if done so we have
 *  a problem of how to calculate the total amount of bytes.
 *
 *  Since we must know the amount of bytes a message contains in order to request the bytes from the array pool; we must
 *  calculate the byte totals BEFORE we rent the bytes, we then must actually call the calls to write to that rented
 *  bytes After we write the bytes; meaning that either A) that calls to rent the bytes would be need to be in our static
 *  methods, in which case we still have code duplication just across a bunch of separate files. Or B) we have 2 methods
 *  one abstraction to calculate the bytes of a message, and a second to write those bytes to the buffer.
 *  While this is fine for the most part, it incurs the cost of having the calculate the length of dynamic fields
 *  twice since the bytes while written will need to be known when writing the bytes, and when calculating the total bytes. 
 *
 *  This is likely why something like protobuf encodes their dynamic length values without precomputing lengths at
 *  the cost of encoding time.
 *
 *  I prefer this micro-optimization for my personal project over the refactoring.
 */
public static class ToClientEncode
{   
    private const int GuidByteLength = 16;
    
    /// <summary>
    /// - 2 byte: Message Version
    /// - 2 byte: Message Type
    /// 
    /// - 4 bytes: GameId
    /// - 1 byte:  Status
    /// - 1 byte:  GameNameLength (Lg)
    /// - Lg bytes: GameName (UTF16LE)
    /// 
    /// - 16 bytes: Player1Id                //Guid.Empty means no player 
    /// - 1 byte:  Player1NameLength (Lp1)   //Can be 0 
    /// - Lp1 bytes: Player1Name (UTF16LE)   //0 in the case of no player 
    /// 
    /// - 16 bytes: Player2Id 
    /// - 1 byte:  Player2NameLength (Lp2)  
    /// - Lp2 bytes: Player2Name (UTF16LE)
    /// 
    /// - 2 byte:  GameHistoryCount (C)
    /// - C * CheckersMove.ByteSize bytes: CheckersMove[]
    /// </summary>
    private const int GameInfoMessageVersion = 1;
    private const int StringLengthEncodingBytes = 1;
    private const int GameStatusEncodingBytes = 1; 
    private const int GameHistoryLengthEncodingBytes = 2;
    public static async Task WriteGameInfoAsync(UserSession[] sessions, GameInfo gameInfo)
    {
        var gameNameEncodedLength = Encoding.Unicode.GetByteCount(gameInfo.GameName);
        var player1NameEncodedLength = Encoding.Unicode.GetByteCount(gameInfo.Player1?.PlayerName ?? string.Empty);
        var player2NameEncodedLength = Encoding.Unicode.GetByteCount(gameInfo.Player2?.PlayerName ?? string.Empty);
        
        Debug.Assert(gameNameEncodedLength <= byte.MaxValue);
        Debug.Assert(player1NameEncodedLength <= byte.MaxValue);
        Debug.Assert(player2NameEncodedLength <= byte.MaxValue);
        
        var totalByteCount =
            MessagePrefixByteSize +
            sizeof(int) + GameStatusEncodingBytes + StringLengthEncodingBytes + gameNameEncodedLength +
            GuidByteLength + StringLengthEncodingBytes + player1NameEncodedLength +
            GuidByteLength + StringLengthEncodingBytes + player2NameEncodedLength +
            GameHistoryLengthEncodingBytes + (CheckersMove.ByteSize * gameInfo.MoveHistoryCount);
        
        var messageBuffer = ArrayPool<byte>.Shared.Rent(totalByteCount);
        var writingTasks = ArrayPool<Task>.Shared.Rent(sessions.Length);
        
        try
        {
            var byteWriter = new ByteWriter(messageBuffer);
            
            WriteMessagePrefix(byteWriter, GameInfoMessageVersion, ToClientMessageType.GameInfoMessage);
            
            byteWriter.WriteInt(gameInfo.GameId);
            byteWriter.WriteByte((byte)gameInfo.Status);
            byteWriter.WriteByte((byte)gameNameEncodedLength);
            byteWriter.WriteStringUTF16LE(ref gameInfo.GameName);

            var player1Guid = gameInfo.Player1?.PlayerId ?? Guid.Empty;
            var player1Name = gameInfo.Player1?.PlayerName ?? string.Empty;
            byteWriter.WriteGuid(ref player1Guid);
            byteWriter.WriteByte((byte)player1NameEncodedLength);
            byteWriter.WriteStringUTF16LE(ref player1Name);

            var player2Guid = gameInfo.Player2?.PlayerId ?? Guid.Empty;
            var player2Name = gameInfo.Player2?.PlayerName ?? string.Empty;
            byteWriter.WriteGuid(ref player2Guid);
            byteWriter.WriteByte((byte)player2NameEncodedLength);
            byteWriter.WriteStringUTF16LE(ref player2Name);

            byteWriter.WriteUShort((ushort)gameInfo.MoveHistoryCount);
            byteWriter.WriteCheckersMoves(gameInfo.MoveHistory, gameInfo.MoveHistoryCount);
            
            var arrayBuffer = new ArraySegment<byte>(messageBuffer, 0, byteWriter.BytesWritten);
            for (var i = 0; i < sessions.Length; i++)
            {
                writingTasks[i] = sessions[i].SocketWriter.SendAsync(arrayBuffer);
            }

            await Task.WhenAll(writingTasks);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageBuffer);
            ArrayPool<Task>.Shared.Return(writingTasks);
        }
    }
    
    /// <summary>
    /// - 2 byte: Message Version
    /// - 2 byte: Message Type
    /// 
    /// - CheckersMoves.ByteSize Bytes: Checks Move 
    /// </summary>
    const int WriteNewMoveMessageVersion = 1;
    public static async Task WriteNewMoveAsync(UserSession[] userSessions, CheckersMove checkersMove)
    {
        const int byteCount = MessagePrefixByteSize + CheckersMove.ByteSize;
        
        var messageBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        var writingTasks = ArrayPool<Task>.Shared.Rent(userSessions.Length);
        
        try
        {
            var byteWriter = new ByteWriter(messageBuffer);
            
            WriteMessagePrefix(byteWriter, WriteNewMoveMessageVersion, ToClientMessageType.NewMoveMessage);
            byteWriter.WriteCheckersMove(ref checkersMove);
            
            var arrayBuffer = new ArraySegment<byte>(messageBuffer, 0, byteWriter.BytesWritten);
            for (var i = 0; i < userSessions.Length; i++)
            {
                writingTasks[i] = userSessions[i].SocketWriter.SendAsync(arrayBuffer);
            }

            await Task.WhenAll(writingTasks);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageBuffer);
            ArrayPool<Task>.Shared.Return(writingTasks);
        }
    }
    
    private const int MessagePrefixByteSize = 4;
    private static void WriteMessagePrefix(ByteWriter byteWriter, ushort version, ToClientMessageType messageType)
    {
        byteWriter.WriteUShort(version);
        byteWriter.WriteUShort((ushort)messageType);
    }
    
    /// <summary>
    /// - 2 byte: Message Version
    /// - 2 byte: Message Type
    /// 
    /// - 16 bytes: SessionId
    /// </summary>
    private const ushort SessionStartMessageVersion = 1;
    public static async Task WriteSessionStartAsync(UserSession userSessions, Guid sessionId)
    {
        const int byteCount = MessagePrefixByteSize + GuidByteLength;
        var messageBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        
        try
        {
            var byteWriter = new ByteWriter(messageBuffer);
           
            WriteMessagePrefix(byteWriter, SessionStartMessageVersion, ToClientMessageType.SessionStartMessage);
            byteWriter.WriteGuid(ref sessionId);
            
            var arrayBuffer = new ArraySegment<byte>(messageBuffer, 0, byteWriter.BytesWritten);
            await userSessions.SocketWriter.SendAsync(arrayBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageBuffer);
        }
    }
    
    /// <summary>
    /// - 2 byte: Message Version
    /// - 2 byte: Message Type
    /// 
    ///  - 16 byte: UserId
    ///  - 1  byte: UserNameLength
    ///  - N  byte: UserName
    /// </summary>
    const ushort OtherPlayerJoinedMessageVersion = 1;
    public static async Task WriteOtherPlayerJoinedAsync(UserSession[] userSessions, PlayerInfo playerInfo)
    {
        Debug.Assert(playerInfo.PlayerName != null);
        var userNameLength = Encoding.Unicode.GetByteCount(playerInfo.PlayerName);
        Debug.Assert(userNameLength <= byte.MaxValue);
        
        var totalByteCount = 
            MessagePrefixByteSize + 
            GuidByteLength + StringLengthEncodingBytes + userNameLength;    
        
        var messageBuffer = ArrayPool<byte>.Shared.Rent(totalByteCount);
        var writingTasks = ArrayPool<Task>.Shared.Rent(userSessions.Length);
        
        try
        {
            var byteWriter = new ByteWriter(messageBuffer);
           
            WriteMessagePrefix(byteWriter, OtherPlayerJoinedMessageVersion, ToClientMessageType.PlayerJoined);
            byteWriter.WriteGuid(ref playerInfo.PlayerId);
            byteWriter.WriteByte((byte)userNameLength);
            byteWriter.WriteStringUTF16LE(ref playerInfo.PlayerName);
            
            var arrayBuffer = new ArraySegment<byte>(messageBuffer, 0, byteWriter.BytesWritten);
            for (var i = 0; i < userSessions.Length; i++)
            {
                writingTasks[i] = userSessions[i].SocketWriter.SendAsync(arrayBuffer);
            }

            await Task.WhenAll(writingTasks);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageBuffer);
            ArrayPool<Task>.Shared.Return(writingTasks);
        }
    }
    
    /// <summary>
    /// - 2 byte: Message Version
    /// - 2 byte: Message Type
    /// - 1 byte: Join Success bool 
    /// </summary>
    const ushort TryJoinGameResultMessageVersion = 1;
    public static async Task WriteTryJoinGameResult(UserSession requestingSession, bool tryJoinGameResult)
    {
        const int totalByteCount = MessagePrefixByteSize + sizeof(byte);
        var messageBuffer = ArrayPool<byte>.Shared.Rent(totalByteCount);
        
        try
        {
            var byteWriter = new ByteWriter(messageBuffer);
           
            WriteMessagePrefix(byteWriter, TryJoinGameResultMessageVersion, ToClientMessageType.PlayerJoined);
            byteWriter.WriteByte(tryJoinGameResult ? (byte)1 : (byte)0);
            
            var arrayBuffer = new ArraySegment<byte>(messageBuffer, 0, byteWriter.BytesWritten);
            await requestingSession.SocketWriter.SendAsync(arrayBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageBuffer);
        }
    }

    /// <summary>
    /// - 2 byte: Message Version
    /// - 2 byte: Message Type
    /// - 4 byte: GameId, -1 when no game was created.  
    /// </summary>
    public static async Task WriteTryGameCreateResult(UserSession session, int newGameId)
    {
        const int totalByteCount = MessagePrefixByteSize + sizeof(byte);
        var messageBuffer = ArrayPool<byte>.Shared.Rent(totalByteCount);
        
        try
        {
            var byteWriter = new ByteWriter(messageBuffer);
           
            WriteMessagePrefix(byteWriter, TryJoinGameResultMessageVersion, ToClientMessageType.PlayerJoined);
            byteWriter.WriteInt(newGameId);
            
            var arrayBuffer = new ArraySegment<byte>(messageBuffer, 0, byteWriter.BytesWritten);
            await session.SocketWriter.SendAsync(arrayBuffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageBuffer);
        }
    }
}