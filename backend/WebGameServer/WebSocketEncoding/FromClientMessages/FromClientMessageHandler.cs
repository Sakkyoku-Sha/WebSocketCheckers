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
            SessionSocketHandler.IdentifyPlayer(session, message.PlayerId);
            if (session.GameId > 0) //Was in a game and reconnected 
            {
                await LocalGameSpace.LockExecuteState(session.GameId, async gameInfo =>
                {
                    await ToClientEncode.WriteGameInfoAsync([session], gameInfo); 
                });
            }
        }
    }
    public static async Task OnTryMakeMoveRequest(TryMakeMoveRequest request)
    {
        await LocalGameSpace.LockExecuteState<Task>(request.GameId, async gameInfo =>
        {
            //Must be a player registered to the game 
            var player1 = gameInfo.Player1;
            var player2 = gameInfo.Player2;

            //Only make moves on board with players 
            if ( player1.HasValue == false ||
                 player2.HasValue == false ||
                 (player1.Value.PlayerId.Equals(request.PlayerId) == false &&
                  player2.Value.PlayerId.Equals(request.PlayerId) == false))
            {
                return;
            }
            
            var result = GameLogic.GameLogic.TryApplyMove(ref gameInfo.GameState, request.FromXy, request.ToXy);
            var playerSockets = SessionSocketHandler.GetSessionsForPlayers([player1.Value.PlayerId, player2.Value.PlayerId]);
            
            await ToClientEncode.WriteNewMoveAsync(playerSockets, new CheckersMove()
            {
                CapturedPieces = result.CapturedPieces,
                Promoted = result.Promoted,
                FromIndex = request.FromXy,
                ToIndex = request.ToXy
            });
        });
    }

    public static async Task OnTryJoinGameRequest(UserSession session, TryJoinGameRequest request)
    {
        var result = await LocalGameSpace.TrySetPlayerInfo(request.GameId, request.PlayerId, "");
        if (result.Success)
        {
            _ = ToClientEncode.WriteTryJoinGameResult(session, true);

            if (result.OpponentInfo != null)
            {
                var opponentSocket = SessionSocketHandler.GetSessionForUserId(result.OpponentInfo.Value);
                _ = ToClientEncode.WriteOtherPlayerJoinedAsync([opponentSocket], new PlayerInfo(request.PlayerId, "Opponent"));
            }
        }
        else
        {
            _ = ToClientEncode.WriteTryJoinGameResult(session, false);
        }
    }

    public static async Task OnTryCreateGameRequest(UserSession session, TryCreateGameRequest request)
    {
        //Already in a game 
        if (session.GameId > 0)
        {
            return; 
        }
        
        var result = await LocalGameSpace.TryCreateNewGame(request.PlayerId);
        await ToClientEncode.WriteTryGameCreateResult(session, result.GameId);
    }
}