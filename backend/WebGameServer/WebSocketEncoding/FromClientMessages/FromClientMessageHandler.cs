using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSocketEncoding.Writers;

namespace WebGameServer.WebSocketEncoding.FromClientMessages;

public static class FromClientMessageHandler
{
    public static async Task OnIdentifyUser(UserSession session, IdentifyUserMessage message)
    {
        if (session.PlayerId == null)
        {
            SessionSocketHandler.SetPlayerSession(session, message.PlayerId);
            if (session.IsInGame) //Was in a game and reconnected 
            {
                await LocalGameSpace.LockExecuteState(session.GameId, async gameInfo =>
                {
                    await WriteToClient.WriteGameInfoAsync([session], gameInfo); 
                });
            }
        }
    }
    public static async Task OnTryMakeMoveRequest(TryMakeMoveRequest request)
    {
        await LocalGameSpace.TryMakeMove(request.GameId, request.FromXy, request.ToXy, OnSuccessfulMove);
    }
    private static async Task OnSuccessfulMove(GameInfo gameInfo, CheckersMove move)
    {
        var playerIds = gameInfo.GetNonNullUsers().Select(x => x.PlayerId).ToArray();
        var sessionIds = SessionSocketHandler.GetSessionsForPlayers(playerIds);
        
        await WriteToClient.WriteNewMoveAsync(sessionIds, move);
    }

    public static async Task OnTryJoinGameRequest(UserSession session, TryJoinGameRequest request)
    {
        if (session.IsInGame) { return; }
        
        await LocalGameSpace.TrySetPlayerInfo(request.GameId, request.PlayerId, "",
            OnSuccessfullyJoinedGame(session), 
            OnFailedToJoinGame(session));
    }
    
    private static Func<GameInfo, PlayerInfo?, Task> OnSuccessfullyJoinedGame(UserSession session)
    {
        return async (gameInfo, opponentInfo) =>
        {
            session.GameId = gameInfo.GameId;
            await WriteToClient.WriteTryJoinGameResult(session, true, gameInfo);

            if (opponentInfo.HasValue)
            {
                var opponentSocket = SessionSocketHandler.GetSessionForUserId(opponentInfo.Value);
                _ = WriteToClient.WriteOtherPlayerJoinedAsync([opponentSocket], opponentInfo.Value);
            }
        };
    }
    private static Action OnFailedToJoinGame(UserSession session)
    {
        return () =>
        {
            _ = WriteToClient.WriteTryJoinGameResult(session, false, null);
        };
    }

    public static async Task OnTryCreateGameRequest(UserSession session, TryCreateGameRequest request)
    {
        if (session.IsInGame)
        {
            return; 
        }
        
        var result = await LocalGameSpace.TryCreateNewGame(request.PlayerId);
        session.GameId = result.GameId; 
        
        await WriteToClient.WriteTryGameCreateResult(session, result.GameId);
    }


    private static DateTime _lastCacheUpdate;
    private const int UpdateTimeFrameSeconds = 5;
    private static byte[] _lastWrittenBytes = []; 
    public static async Task OnGetActiveGamesRequest(UserSession sourceSession)
    {
        if (DateTime.UtcNow - _lastCacheUpdate > TimeSpan.FromSeconds(UpdateTimeFrameSeconds))
        {
            var activeGameIds = LocalGameSpace.GetActiveGame(); 
            _lastWrittenBytes = await WriteToClient.WriteActiveGames(sourceSession, activeGameIds);
            _lastCacheUpdate = DateTime.UtcNow;
        }
        else
        {
            await sourceSession.SocketWriter.SendAsync(_lastWrittenBytes);
        }
    }
}