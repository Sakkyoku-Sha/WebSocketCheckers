using WebGameServer.State;

namespace WebGameServerTests;

public class GameInfoTests
{
    [Test]
    public void AddHistory()
    {
        var state = new GameInfo(Guid.NewGuid(), new PlayerInfo(Guid.NewGuid(), ""), new PlayerInfo(Guid.NewGuid(), ""), new GameState(true), "game1", GameStatus.InProgress);
        
        var move = new CheckersMove(0, 9, false, 0);
        state.AddHistory(move);
        
        Assert.IsTrue(state.MoveHistoryCount == 1);
        Assert.IsTrue(state.GetHistory()[0].Equals(move));
    }
}