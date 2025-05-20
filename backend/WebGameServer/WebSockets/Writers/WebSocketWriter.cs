using System.Buffers;
using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers.ByteWriters;
using WebGameServer.WebSockets.Writers.MessageWriters;

namespace WebGameServer.WebSockets.Writers;

public enum ToClientMessageType : ushort
{
    //Initial Connection Message 
    SessionStartMessage = 0,
    
    //Initial State for Connected Client 
    InitialStateMessage = 1,
    
    //Updating All User Messages
    GameCreatedMessage = 2,
    
    //Querying Responses 
    ActiveGamesResponse = 3,
    TryCreateGameResponse = 4,
    TryJoinGameResponse = 5,
    
    //Game State Updates 
    NewMove = 7,
    GameStatusChanged = 8,
    DrawRequest = 9,
    DrawRequestRejected = 10,
    PlayerJoined = 11,
}

public interface IByteWriter
{
    void WriteBytes(ref ByteWriter byteWriter);
    int CalculatePayLoadLength();
}
public interface IMessageWriter : IByteWriter
{
    public static abstract ToClientMessageType ResponseType { get; }
    public static abstract ushort Version { get; }
}

public static class WebSocketWriter
{   
    private const int MessagePrefixByteSize = 4;
    
    private static async Task RentWriteSendAsync<T>(UserSession session, T writer) where T : struct, IMessageWriter
    {
        var totalBytes = MessagePrefixByteSize + writer.CalculatePayLoadLength();
        var buffer = ArrayPool<byte>.Shared.Rent(totalBytes);

        try
        {
            var byteWriter = new ByteWriter(buffer); 
            
            byteWriter.WriteUShort(T.Version);
            byteWriter.WriteUShort((ushort)T.ResponseType);
            
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
    private static async Task RentWriteSendAsync<T>(UserSession[] sessions, T writer) where T : struct, IMessageWriter
    {
        var totalBytes = MessagePrefixByteSize + writer.CalculatePayLoadLength();
        var buffer = ArrayPool<byte>.Shared.Rent(totalBytes);
        var writeTasks = ArrayPool<Task>.Shared.Rent(sessions.Length);
        
        try
        {
            var byteWriter = new ByteWriter(buffer); 
            
            byteWriter.WriteUShort(T.Version);
            byteWriter.WriteUShort((ushort)T.ResponseType);
            
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
        var writer = new NewMoveWriter(checkersMove, forcedMovesInPosition);
        await RentWriteSendAsync(userSessions, writer);
    }
    public static async Task WriteSessionStartAsync(UserSession session, Guid sessionId)
    {
        var writer = new SessionStartWriter(sessionId);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WritePlayerJoinedAsync(UserSession[] userSessions, PlayerInfo playerInfo)
    {
        var writer = new PlayerJoinedWriter(playerInfo);
        await RentWriteSendAsync(userSessions, writer);
    }
    public static async Task WriteTryJoinGameResult(UserSession session, bool tryJoinGameResult, GameInfo? gameInfo)
    {
        var writer = new JoinGameResultWriter(tryJoinGameResult, gameInfo);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WriteGameCreatedOrUpdated(UserSession[] session, GameMetaData createdGame)
    {
        var writer = new GameCreatedWriter(createdGame);
        await RentWriteSendAsync(session, writer);
    }
    public static async Task WriteActiveGames(UserSession sourceSession, GameMetaData[] activeGames)
    {
        var writer = new GameMetaDataWriter(activeGames);
        await RentWriteSendAsync(sourceSession, writer);
    }
    public static async Task WriteTryCreateGameResult(UserSession session, bool resultDidCreateGame, int createdGameGameId)
    {
        var writer = new TryCreateGameResultWriter(resultDidCreateGame ? createdGameGameId : -1);
        await RentWriteSendAsync(session, writer);
    }
    
    public static async Task WriteDrawRequest(UserSession opponentSession)
    {
        var writer = new DrawRequestWriter();
        await RentWriteSendAsync(opponentSession, writer);
    }

    public static async Task WriteGameStatusUpdate(List<UserSession> userSessions, GameStatus draw)
    {
        var writer = new GameCreatedWriter(); 
        await RentWriteSendAsync(userSessions.ToArray(), writer);
    }

    public static async Task WriteDrawRejected(UserSession opponentSession)
    {
        var writer = new DrawRequestRejectedWriter();
        await RentWriteSendAsync(opponentSession, writer);
    }
}

