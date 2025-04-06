﻿using System.Collections.Concurrent;
using WebGameServer.GameLogic;
using WebGameServer.GameStateManagement.KeyValueStore;
using WebGameServer.State;

namespace WebGameServer.GameStateManagement;

public class GameStateManager(IKeyGameInfoStore gameInfoStore)
{
    private readonly ConcurrentDictionary<Guid, GameInfo> _playerGames = new();

    public GameInfo CreateNewGame(Guid playerId)
    {
        var newGameId = Guid.NewGuid();
        var newGameState = new GameState(true); 
        var dummyPlayer1 = new PlayerInfo(playerId, "Player1"); 
        var title = $"Checkers {new string(newGameId.ToString().Take(10).ToArray())}";
        var status = GameStatus.WaitingForPlayers; 
        
        var newGameInfo = new GameInfo(newGameId, dummyPlayer1, null, newGameState, title, status);
        
        gameInfoStore.SetGameInfo(newGameId, newGameInfo);
        _playerGames[playerId] = newGameInfo; 
        
        return newGameInfo; 
    }
    public void RemoveGame(Guid gameId)
    {
        lock (ExecuteExitJoinGameLock)
        {
            gameInfoStore.RemoveGameInfo(gameId);
        }
    }
    public bool TryGetGameByGameId(Guid gameId, out GameInfo? gameInfo)
    {
        return gameInfoStore.TryGetState(gameId, out gameInfo);
    }
    
    //To make joining games Thread Safe
    private static readonly Lock ExecuteExitJoinGameLock = new Lock();
    public bool TryJoinGame(Guid requestGameId, Guid requestPlayerId, out GameInfo? gameInfo)
    {
        gameInfo = null;
        //Race Condition Exists on Joining a Match, only make it so 1 player can join a game. 
        lock (ExecuteExitJoinGameLock)
        {
            if (_playerGames.TryGetValue(requestPlayerId, out var _))
            {
                // Player is already in a game
                return false;
            }
            if (!gameInfoStore.TryGetState(requestGameId, out gameInfo) || gameInfo == null)
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
            gameInfoStore.SetGameInfo(requestGameId, gameInfo);
        }
        return true;
    }
    
    public void RemoveUsersFromGames(Guid userId)
    {
        lock (ExecuteExitJoinGameLock)
        {
            if (_playerGames.TryRemove(userId, out var gameInfo))
            {
                if (gameInfo.Player1?.UserId == userId)
                {
                    gameInfo.Player1 = null;
                }
                else if (gameInfo.Player2?.UserId == userId)
                {
                    gameInfo.Player2 = null;
                }
                if (gameInfo.Player1 == null && gameInfo.Player2 == null) //No Users are in the game so remove it. 
                {
                    gameInfoStore.RemoveGameInfo(gameInfo.GameId);
                }
            }
        }
    }

    public IEnumerable<GameInfo> ResolveOpenGames()
    {
        return gameInfoStore.GamesWhere(x => x.Status == GameStatus.WaitingForPlayers); 
    }

    public TryMoveResult TryApplyMove(Guid moveGameId, byte moveFromIndex, byte moveToIndex, out GameInfo? gameInfo)
    {
        gameInfo = null;
        if (!gameInfoStore.TryGetState(moveGameId, out gameInfo) || gameInfo == null)
        {
            return TryMoveResult.Fail;
        }
        
        var gameState = gameInfo.GameState;
        return GameLogic.GameLogic.TryApplyMove(ref gameState, moveFromIndex, moveToIndex);
    }

    public bool PlayerInGame(Guid requestUserId)
    {
        return _playerGames.ContainsKey(requestUserId);
    }

    public bool TryGetGameByUserId(Guid userId, out GameInfo? gameInfo)
    {
        return _playerGames.TryGetValue(userId, out gameInfo);
    }
}