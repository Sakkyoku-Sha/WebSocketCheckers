using WebGameServer.State;

namespace WebGameServer.API;

public class Query
{
    public GameInfo[] GetActiveGameInfos() =>
    [
        new(Guid.NewGuid(), new PlayerInfo(Guid.NewGuid(), "player1"), new PlayerInfo(Guid.NewGuid(), "player 2"), new GameState()),
        new(Guid.NewGuid(), new PlayerInfo(Guid.NewGuid(), "player3"), new PlayerInfo(Guid.NewGuid(), "player 4"), new GameState()),
    ]; 
}