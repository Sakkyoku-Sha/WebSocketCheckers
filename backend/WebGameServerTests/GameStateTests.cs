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

   
    
    
}