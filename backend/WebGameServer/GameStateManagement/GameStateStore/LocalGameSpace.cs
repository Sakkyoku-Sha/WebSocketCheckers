using WebGameServer.State;

namespace WebGameServer.GameStateManagement.GameStateStore;

internal struct GameSlot(SemaphoreSlim gameLock, GameInfo gameInfo)
{
    public readonly SemaphoreSlim GameLock = gameLock; 
    public readonly GameInfo GameInfo = gameInfo;
}

public static class LocalGameSpace
{
    private const int GameSpaceCapacity = 1000;
    private static GameSlot[] _gameSpace = new GameSlot[GameSpaceCapacity];
    private static int _lastCreatedGameSlot = 0;

    public static void Initialize()
    {
        _gameSpace = new GameSlot[GameSpaceCapacity];
        for (var i = 0; i < GameSpaceCapacity; i++)
        {
            _gameSpace[i] = new GameSlot(new SemaphoreSlim(1,1), new GameInfo(i));
        }
    }
    
    public static async Task<TryCreateGameResult> TryCreateNewGame(Guid playerId)
    {
        var i = _lastCreatedGameSlot;
        var newGameId = -1;
        var didCreateGame = false;
        
        do
        {
            var gameSlot = _gameSpace[i];
            //If we are currently blocked don't wait for it, as we know this game is in use by thread, and we 
            //can't create a new game here. 
            if (!await gameSlot.GameLock.WaitAsync(TimeSpan.Zero))
            {
                continue;
            }
            
            try
            {
                if (gameSlot.GameInfo.Status is GameStatus.Finished or GameStatus.Abandoned or GameStatus.NoPlayers)
                {
                    didCreateGame = true;
                    gameSlot.GameInfo.Reset();
                    gameSlot.GameInfo.Player1 = new PlayerInfo(playerId, "Player 1");
                    _lastCreatedGameSlot = i;
                    newGameId = i;
                }
            }
            finally
            {
                gameSlot.GameLock.Release();  // Release the lock
            }

            i++;
            if (i >= _gameSpace.Length)
            {
                i %= _gameSpace.Length;
            }

        } while (i != _lastCreatedGameSlot && didCreateGame == false);

        return new TryCreateGameResult(newGameId);
    }
    
    public static async Task LockExecuteState(int gameId, Action<GameInfo> mutation)
    {
        var gameSlot = GetGameSlotIfValid(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            mutation(gameSlot.GameInfo);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }
    }
    public static async Task<TResult?> LockExecuteState<TResult>(int gameId, Func<GameInfo, TResult> mutation)
    {
        TResult? result;
        
        var gameSlot = GetGameSlotIfValid(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            result = mutation(gameSlot.GameInfo);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }

        return result;
    }
    
    public static async Task LockExecuteState(int gameId, Func<GameInfo, Task> mutation)
    {
        var gameSlot = GetGameSlotIfValid(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            await mutation(gameSlot.GameInfo);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }
    }
    
    public static async Task<TResult?> LockExecuteState<TResult>(int gameId, Func<GameInfo, Task<TResult>> mutation)
    {
        TResult? result;
        
        var gameSlot = GetGameSlotIfValid(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            result = await mutation(gameSlot.GameInfo);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }

        return result;
    }
    
    public static async Task<TrySetPlayerResult> TrySetPlayerInfo(int gameId, Guid playerId, string playerName)
    {
        var gameSlot = GetGameSlotIfValid(gameId);
        await gameSlot.GameLock.WaitAsync();
        
        try
        {
            PlayerInfo? opponentInfo; 
            var gameInfo = gameSlot.GameInfo;
            var joinedAsPlayer1 = false; 
            
            //trying to join an already occupied position
            if (gameInfo.Player1 != null && gameInfo.Player2 != null)
            {
                return TrySetPlayerResult.Failed;
            }
        
            if (gameInfo.Player1 == null)
            {
                gameInfo.Player1 = new PlayerInfo(playerId, playerName);
                opponentInfo = gameInfo.Player2;
                joinedAsPlayer1 = true;
            }
            else
            {
                gameInfo.Player2 = new PlayerInfo(playerId, playerName);
                opponentInfo = gameInfo.Player1;
            }
            
            return new TrySetPlayerResult(true, opponentInfo, joinedAsPlayer1);
        }
        finally
        {
            gameSlot.GameLock.Release();
        }
    }
    
    private static GameSlot GetGameSlotIfValid(int gameId)
    {
        if (gameId < 0 || gameId >= _gameSpace.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(gameId));
        }
        
        ref var gameSlot = ref _gameSpace[gameId];
        if (gameSlot.GameInfo == null || 
            gameSlot.GameInfo.Status == GameStatus.Finished || 
            gameSlot.GameInfo.Status == GameStatus.Abandoned || 
            gameSlot.GameInfo.Status == GameStatus.NoPlayers || 
            gameSlot.GameInfo.Status == GameStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException("Attempted to mutate non active game");
        }

        return gameSlot;
    }
}

public readonly struct TrySetPlayerResult(bool success, PlayerInfo? opponentInfo, bool player1)
{
    public readonly bool Success = success;
    public readonly PlayerInfo? OpponentInfo = opponentInfo;
    public readonly bool Player1 = player1;
    
    public static TrySetPlayerResult Failed = new(false, null, false);
}
public readonly struct TryCreateGameResult(int gameId)
{
    public readonly int GameId = gameId;
}
public readonly struct GameInfoMetaData(int gameId, PlayerInfo? player1, PlayerInfo? player2)
{
    public readonly int GameId = gameId;
    public readonly PlayerInfo? Player1 = player1;
    public readonly PlayerInfo? Player2 = player2;
}