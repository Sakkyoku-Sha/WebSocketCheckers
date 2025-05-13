using System.Buffers;
using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;
using WebGameServer.WebSockets.Writers.MessageWriters;

namespace WebGameServer.WebSockets.Writers;

public enum ToClientMessageType : ushort
{
    SessionStartMessage = 0,
    PlayerJoined = 1,
    NewMoveMessage = 2, 
    InitialServerMessage = 3,
    TryJoinGameResultMessage = 4,
    CreateGameResultMessage = 5,
    ActiveGamesMessage = 6,
}

public interface IByteWriter
{
    void WriteBytes(ref ByteWriter byteWriter); 
    int CalculatePayLoadLength();
}
public interface IMessageWriter : IByteWriter
{
    public static abstract ToClientMessageType MessageType { get; }
    public static abstract ushort Version { get; }
}

public static class WebSocketWriter
{   
    private const int MessagePrefixByteSize = 4;
    
    private static async Task RentWriteSendAsync<T>(UserSession session, T writer) where T : IMessageWriter
    {
        var totalBytes = MessagePrefixByteSize + writer.CalculatePayLoadLength();
        var buffer = ArrayPool<byte>.Shared.Rent(totalBytes);

        try
        {
            var byteWriter = new ByteWriter(buffer); 
            
            byteWriter.WriteUShort(T.Version);
            byteWriter.WriteUShort((ushort)T.MessageType);
            
            writer.WriteBytes(ref byteWriter);
            await session.SocketChannel.SendAsync(buffer);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    private static async Task RentWriteSendAsync<T>(UserSession[] sessions, T writer) where T : IMessageWriter
    {
        var totalBytes = MessagePrefixByteSize + writer.CalculatePayLoadLength();
        var buffer = ArrayPool<byte>.Shared.Rent(totalBytes);
        var writeTasks = ArrayPool<Task>.Shared.Rent(sessions.Length);
        
        try
        {
            var byteWriter = new ByteWriter(buffer); 
            
            byteWriter.WriteUShort(T.Version);
            byteWriter.WriteUShort((ushort)T.MessageType);
            
            writer.WriteBytes(ref byteWriter);

            for (var i = 0; i < sessions.Length; i++)
            {
                writeTasks[i] = sessions[i].SocketChannel.SendAsync(buffer);
            }
            await Task.WhenAll(writeTasks.AsSpan(0, sessions.Length));
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            ArrayPool<Task>.Shared.Return(writeTasks);
        }
    }
    
    public static async Task WriteInitialServerMessage(UserSession session, GameMetaData[] activeGames, GameInfo? gameInfo)
    {
        var writer = new InitialMessageWriter(activeGames, gameInfo);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WriteNewMoveAsync(UserSession[] userSessions, CheckersMove checkersMove, JumpPath[] forcedMovesInPosition)
    {
        var writer = new NewMoveWriter(ref checkersMove, forcedMovesInPosition);
        await RentWriteSendAsync(userSessions, writer);
    }
    public static async Task WriteSessionStartAsync(UserSession session, Guid sessionId)
    {
        var writer = new SessionStartWriter(ref sessionId);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WriteOtherPlayerJoinedAsync(UserSession[] userSessions, PlayerInfo playerInfo)
    {
        var writer = new OtherPlayerJoinedWriter(ref playerInfo);
        await RentWriteSendAsync(userSessions, writer);
    }
    public static async Task WriteTryJoinGameResult(UserSession session, bool tryJoinGameResult, GameInfo? gameInfo)
    {
        var writer = new JoinGameResultWriter(tryJoinGameResult, gameInfo);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WriteTryGameCreateResult(UserSession session, int newGameId)
    {
        var writer = new CreateGameResultWriter(newGameId);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WriteActiveGames(UserSession sourceSession, GameMetaData[] activeGames)
    {
        var writer = new GameMetaDataWriter(activeGames);
        await RentWriteSendAsync(sourceSession, writer);
    }
}