using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.State;
using WebGameServer.WebSockets.Writers;

namespace WebGameServer.WebSockets;

public static class FromClientMessageHandler
{
    public static async Task OnIdentifyUser(UserSession session, IdentifyUserMessage message)
    {
        if (session.Identified == false)
        {
            SessionSocketHandler.IdentifyPlayerSessions(session, message.PlayerId);
            var activeGames = LocalGameSpace.GetActiveGames();
            if (session.IsInGame) //Was in a game and reconnected 
            {
                await LocalGameSpace.LockExecuteState(session.GameId, async gameInfo =>
                {
                    //Consider using SnapShots with a Delta Counter to avoid locking state for writing. 
                    await WebSocketWriter.WriteInitialServerMessage(session, activeGames, gameInfo); 
                });
            }
            else
            {
                await WebSocketWriter.WriteInitialServerMessage(session, activeGames, null);
            }
        }
    }
    public static void OnTryMakeMoveRequest(UserSession session, TryMakeMoveRequest request)
    {
        if (!session.IsInGame) { return; }
        _ = LocalGameSpace.TryMakeMove(session.GameId, request.FromXy, request.ToXy, OnSuccessfulMove);
    }
    private static async Task OnSuccessfulMove(GameInfo gameInfo, TimedCheckersMove move)
    {
        var playerIds = gameInfo.GetNonNullPlayers().Select(x => x.PlayerId).ToArray();
        var sessionIds = SessionSocketHandler.GetSessionsForPlayers(playerIds);
        var gameDidFinish = gameInfo.IsGameFinished();
        
        await WebSocketWriter.WriteNewMoveAsync(sessionIds, move, gameInfo.GameState.CurrentForcedJumps);

        if (gameDidFinish == false) { return; }
        
        var player1Session =  SessionSocketHandler.GetSessionForUserId(gameInfo.Player1.PlayerId);
        var player2Session = SessionSocketHandler.GetSessionForUserId(gameInfo.Player2.PlayerId);

        player1Session.ResetGameId();
        player2Session.ResetGameId();
    }

    public static void OnTryJoinGameRequest(UserSession session, TryJoinGameRequest request)
    {
        if (session.IsInGame || session.Identified == false) { return; }
        
        _ = LocalGameSpace.TryJoinGame(request.GameId, session.PlayerId,
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
                _ = WebSocketWriter.WritePlayerJoinedAsync([opponentSocket], opponentInfo);
            }
            
            //For Now Notify all users that a game has been joined (updated) in this case for their 
            //game browser. This is heavy; but we won't have users to matter for a while. 
            _ = WebSocketWriter.WriteGameCreatedOrUpdated(SessionSocketHandler.AllUserSessions(), gameInfo.ToMetaData());  
        };
    }
    private static Action OnFailedToJoinGame(UserSession session)
    {
        return () =>
        {
            _ = WebSocketWriter.WriteTryJoinGameResult(session, false, null);
        };
    }

    public static async Task OnTryCreateGameRequest(UserSession session)
    {
        if (session.IsInGame || session.Identified == false)
        {
            return; 
        }
        
        var result = await LocalGameSpace.TryCreateNewGame(session.PlayerId);
        await WebSocketWriter.WriteTryCreateGameResult(session, result.DidCreateGame, result.CreatedGame.GameId);
        
        if (result.DidCreateGame)
        {
            session.GameId = result.CreatedGame.GameId;
            _ = WebSocketWriter.WriteGameCreatedOrUpdated(SessionSocketHandler.AllUserSessions(), result.CreatedGame);
        }
    }
    
    public static async Task OnGetActiveGamesRequest(UserSession sourceSession)
    {
        var activeGameIds = LocalGameSpace.GetActiveGames(); 
        await WebSocketWriter.WriteActiveGames(sourceSession, activeGameIds);
    }

    public static void OnSurrenderGame(UserSession sourceSession)
    {
        if (!sourceSession.IsInGame) { return; }
        
        //Game has ended, notify the opponent; 
        var opponentInfo = LocalGameSpace.GetOpponentInfo(sourceSession.GameId, sourceSession.PlayerId);
        if (opponentInfo.IsDefined == false) { return; }
            
        var opponentSession = SessionSocketHandler.GetSessionForUserId(opponentInfo.PlayerId);

        var gameResult = opponentInfo.IsPlayer1 ? GameStatus.Player1Win : GameStatus.Player2Win;
        LocalGameSpace.UpdateGameStatus(sourceSession.GameId, gameResult);
            
        _ = WebSocketWriter.WriteGameStatusUpdate([sourceSession, opponentSession], gameResult);
    }
    
    public static void OnDrawRequest(UserSession sourceSession)
    {
        if (!sourceSession.IsInGame) { return; }
        
        var opponentInfo = LocalGameSpace.GetOpponentInfo(sourceSession.GameId, sourceSession.PlayerId);
        if (!opponentInfo.IsDefined) { return; }
        
        var opponentSession = SessionSocketHandler.GetSessionForUserId(opponentInfo.PlayerId);
        _ = WebSocketWriter.WriteDrawRequest(opponentSession);
    }

    public static void OnDrawResponse(UserSession sourceSession, DrawRequestResponse drawResponse)
    {
        if (!sourceSession.IsInGame) { return; }
        
        var opponentInfo = LocalGameSpace.GetOpponentInfo(sourceSession.GameId, sourceSession.PlayerId);
        if (!opponentInfo.IsDefined) { return; }
        
        var opponentSession = SessionSocketHandler.GetSessionForUserId(opponentInfo.PlayerId);
        if (drawResponse.Accepted)
        {
            _ = WebSocketWriter.WriteGameStatusUpdate([sourceSession, opponentSession], GameStatus.Draw);
            LocalGameSpace.UpdateGameStatus(sourceSession.GameId, GameStatus.Draw); 
        }
        else
        {
            _ = WebSocketWriter.WriteDrawRejected(opponentSession);
        }
    }
}