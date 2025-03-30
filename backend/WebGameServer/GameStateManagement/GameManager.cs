using System.Collections.Concurrent;
using WebGameServer.GameStateManagement.KeyValueStore;
using WebGameServer.State;

namespace WebGameServer.GameStateManagement;

public class GameManager
{
    private readonly IKeyGameInfoStore _gameInfoStore; 
    private readonly ConcurrentDictionary<Guid, GameInfo> _playerGames;
    
    public GameManager(IKeyGameInfoStore gameInfoStore)
    {
        _gameInfoStore = gameInfoStore; 
        _playerGames = new ConcurrentDictionary<Guid, GameInfo>();
    }

    public GameInfo CreateNewGame(Guid playerId)
    {
        var newGameId = Guid.NewGuid();
        var newGameState = new GameState(true); 
        var dummyPlayer1 = new PlayerInfo(playerId, "Player1"); 
        var title = $"Checkers {new string(newGameId.ToString().Take(10).ToArray())}";
        var status = GameStatus.WaitingForPlayers; 
        
        var newGameInfo = new GameInfo(newGameId, dummyPlayer1, null, newGameState, title, status);
        
        _gameInfoStore.SetGameInfo(newGameId, newGameInfo);
        _playerGames[playerId] = newGameInfo; 
        
        return newGameInfo; 
    }
    public void RemoveGame(Guid gameId)
    {
        lock (ExecuteJoinGameLock)
        {
            _gameInfoStore.RemoveGameInfo(gameId);
        }
    }
    public bool TryGetGame(Guid gameId, out GameInfo? gameInfo)
    {
        return _gameInfoStore.TryGetState(gameId, out gameInfo);
    }
    
    //To make joining games Thread Safe
    private static readonly Lock ExecuteJoinGameLock = new Lock();
    public bool TryJoinGame(Guid requestGameId, Guid requestPlayerId, out GameInfo? gameInfo)
    {
        gameInfo = null;
        //Race Condition Exists on Joining a Match, only make it so 1 player can join a game. 
        lock (ExecuteJoinGameLock)
        {
            if (_playerGames.TryGetValue(requestPlayerId, out var _))
            {
                // Player is already in a game
                return false;
            }
            if (!_gameInfoStore.TryGetState(requestGameId, out gameInfo) || gameInfo == null)
            {
                // Game does not exist
                return false;
            }
            if(gameInfo.Player2 != null)
            {
                // Game is already full
                return false;
            }
            
            // Game exists and player can join
            _playerGames[requestPlayerId] = gameInfo;
            _gameInfoStore.SetGameInfo(requestGameId, gameInfo);
        }

        return true;
    }

    public IEnumerable<GameInfo> ResolveOpenGames()
    {
        return _gameInfoStore.GamesWhere(x => x.Status == GameStatus.WaitingForPlayers); 
    }

    public bool TryApplyMove(Guid moveGameId, byte moveFromIndex, byte moveToIndex, out GameInfo? gameInfo)
    {
        gameInfo = null;
        if (!_gameInfoStore.TryGetState(moveGameId, out gameInfo) || gameInfo == null)
        {
            return false;
        }
        
        var gameState = gameInfo.GameState;
        return GameLogic.GameLogic.TryApplyMove(gameState, moveFromIndex, moveToIndex);
    }

    public bool PlayerInGame(Guid requestUserId)
    {
        return _playerGames.ContainsKey(requestUserId);
    }
}