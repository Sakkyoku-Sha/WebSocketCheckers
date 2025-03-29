using WebGameServer.GameStateManagement;
using WebGameServer.State;

namespace WebGameServer.API;

public class Query
{   
    private readonly GameManager GameStateManagement;
    public Query(GameManager gameStateManagement)
    {
        GameStateManagement = gameStateManagement;
    }
    
    public GameInfo[] GetOpenGames() => GameStateManagement.ResolveOpenGames()
        .Select(gameInfo => new GameInfo(gameInfo.GameId, gameInfo.Player1, gameInfo.Player2, new GameState())) //Intentionally cull the game state 
        .ToArray();
}