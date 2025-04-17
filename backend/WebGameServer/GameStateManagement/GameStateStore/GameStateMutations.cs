using WebGameServer.GameLogic;

namespace WebGameServer.GameStateManagement.GameStateStore;

public static class GameStateMutations
{
    public static async Task<TryMoveResult> TryMakeMove(int gameId, Guid playerId, byte fromBitIndex, byte toBitIndex)
    {
        return await LocalGameSpace.LockExecuteState(gameId, (gameInfo) =>
        {
            //Must be a player registered to the game 
            var player1Id = gameInfo.Player1?.PlayerId;
            var player2Id = gameInfo.Player2?.PlayerId;
            
            if (player1Id?.Equals(playerId) == false && player2Id?.Equals(playerId) == false)
            {
                return TryMoveResult.Fail;
            }
            
            return GameLogic.GameLogic.TryApplyMove(ref gameInfo.GameState, fromBitIndex, toBitIndex);
        });
    }
}