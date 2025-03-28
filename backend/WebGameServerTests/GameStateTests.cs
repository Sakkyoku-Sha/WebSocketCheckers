using WebGameServer.GameLogic;
using WebGameServer.State;

namespace WebGameServerTests;

public class GameStateTests
{
    [Test]
    public void DefaultBoard()
    {
        var state = new GameState();
        state.SetUpDefaultBoard();
        
        Assert.IsTrue(GameState.IsBitSet(state.Player1Pawns, GameState.GetBitIndex(0, 5)));
    }

    [Test]
    public void AddHistory()
    {
        var state = new GameState();
        state.SetUpDefaultBoard();
        
        var move = new CheckersMove(0, 9, false, 0);
        state.AddHistory(move);
        
        Assert.IsTrue(state.MoveHistoryCount == 1);
        Assert.IsTrue(state.MoveHistory[0].Equals(move));
    }
    
    
}