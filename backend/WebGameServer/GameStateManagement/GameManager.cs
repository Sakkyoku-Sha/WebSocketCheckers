using WebGameServer.GameStateManagement.KeyValueStore;
using WebGameServer.State;

namespace WebGameServer.GameStateManagement;

public class GameManager
{
    private readonly IKeyGameInfoStore _store; 
    public GameManager(IKeyGameInfoStore store)
    {
        _store = store; 
    }

    public GameInfo AddGame()
    {
        var newGameId = Guid.NewGuid();
        var newGameState = new GameState(true); 
        var dummyPlayer1 = new PlayerInfo(Guid.NewGuid(), "Player1"); 
        var dummyPlayer2 = new PlayerInfo(Guid.NewGuid(), "Player2");
        var newGameInfo = new GameInfo(newGameId, dummyPlayer1, dummyPlayer2, newGameState);
        
        _store.SetGameInfo(newGameId, newGameInfo);
        return newGameInfo; 
    }
    public void RemoveGame(Guid gameId)
    {
        _store.RemoveGameInfo(gameId);
    }
    public bool TryGetGame(Guid gameId, out GameInfo? gameInfo)
    {
        return _store.TryGetState(gameId, out gameInfo);
    }
    
}