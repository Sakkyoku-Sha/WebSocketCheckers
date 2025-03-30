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
    
    public GameInfo[] OpenGames() => GameStateManagement.ResolveOpenGames() //Intentionally cull the game state 
        .ToArray();
}