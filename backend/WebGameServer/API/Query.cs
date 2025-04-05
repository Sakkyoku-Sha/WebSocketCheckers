using WebGameServer.GameStateManagement;
using WebGameServer.State;

namespace WebGameServer.API;

public class Query
{   
    private readonly GameStateManager _gameStateStateManagement;
    public Query(GameStateManager gameStateStateManagement)
    {
        _gameStateStateManagement = gameStateStateManagement;
    }
    
    public GameInfo[] OpenGames() => _gameStateStateManagement.ResolveOpenGames().ToArray();
}