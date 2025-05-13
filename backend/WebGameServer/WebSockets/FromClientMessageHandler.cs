using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers;

namespace WebGameServer.WebSockets;

public static class FromClientMessageHandler
{
    public static async Task OnIdentifyUser(UserSession session, IdentifyUserMessage message)
    {
        if (session.PlayerId == null)
        {
            SessionSocketHandler.SetPlayerSession(session, message.PlayerId);
            var activeGames = LocalGameSpace.GetActiveGames();
            if (session.IsInGame) //Was in a game and reconnected 
            {
                await LocalGameSpace.LockExecuteState(session.GameId, async gameInfo =>
                {
                    await WebSocketWriter.WriteInitialServerMessage(session, activeGames, gameInfo); 
                });
            }
            else
            {
                await WebSocketWriter.WriteInitialServerMessage(session, activeGames, null);
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
        await WebSocketWriter.WriteNewMoveAsync(sessionIds, move, gameInfo.GameState.CurrentForcedJumps);
    }

    public static async Task OnTryJoinGameRequest(UserSession session, TryJoinGameRequest request)
    {
        if (session.IsInGame) { return; }
        
        await LocalGameSpace.TrySetPlayerInfo(request.GameId, request.PlayerId, "",
            OnSuccessfullyJoinedGame(session), 
            OnFailedToJoinGame(session));
    }
    
    private static Func<GameInfo, PlayerInfo, Task> OnSuccessfullyJoinedGame(UserSession session)
    {
        return async (gameInfo, opponentInfo) =>
        {
            session.GameId = gameInfo.GameId;
            await WebSocketWriter.WriteTryJoinGameResult(session, true, gameInfo);

            if (opponentInfo.IsDefined)
            {
                var opponentSocket = SessionSocketHandler.GetSessionForUserId(opponentInfo.PlayerId);
                _ = WebSocketWriter.WriteOtherPlayerJoinedAsync([opponentSocket], opponentInfo);
            }
        };
    }
    private static Action OnFailedToJoinGame(UserSession session)
    {
        return () =>
        {
            _ = WebSocketWriter.WriteTryJoinGameResult(session, false, null);
        };
    }

    public static async Task OnTryCreateGameRequest(UserSession session, TryCreateGameRequest request)
    {
        if (session.IsInGame)
        {
            return; 
        }
        
        var result = await LocalGameSpace.TryCreateNewGame(request.PlayerId);
        if (result.DidCreateGame)
        {
            session.GameId = result.CreatedGame.GameId;
            await WebSocketWriter.WriteTryGameCreateResult(SessionSocketHandler.AllUserSessions(), result.CreatedGame);
        }
    }

    
    public static async Task OnGetActiveGamesRequest(UserSession sourceSession)
    {
        var activeGameIds = LocalGameSpace.GetActiveGames(); 
        await WebSocketWriter.WriteActiveGames(sourceSession, activeGameIds);
    }
}