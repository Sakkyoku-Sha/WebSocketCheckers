using WebGameServer.State;

namespace WebGameServer.GameStateManagement.GameStateStore;

internal readonly struct GameSlot(SemaphoreSlim gameLock, GameInfo gameInfo)
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
        var lastCreatedGameSlot = _lastCreatedGameSlot; //create a copy as to not cause a race on the original
        var i = _lastCreatedGameSlot;
        var didCreateGame = false;
        var creatingPlayer = new PlayerInfo(playerId, true);
        
        GameMetaData createdGameMetaData = default;
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
                if (gameSlot.GameInfo.IsActive == false)
                {
                    didCreateGame = true;
                    gameSlot.GameInfo.Reset();
                    gameSlot.GameInfo.Player1 = creatingPlayer;
                    gameSlot.GameInfo.Player2 = PlayerInfo.Empty;
                    gameSlot.GameInfo.Status = GameStatus.WaitingForPlayers;
                    _lastCreatedGameSlot = i;
                    
                    createdGameMetaData = gameSlot.GameInfo.ToMetaData();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            finally
            {
                gameSlot.GameLock.Release();
            }

            i++;
            if (i >= _gameSpace.Length)
            {
                i = 0;
            }

        } while (i != lastCreatedGameSlot && didCreateGame == false);

        return new TryCreateGameResult(didCreateGame, createdGameMetaData);
    }
    
    public static async Task TryJoinGame(int gameId, Guid playerId, Func<GameInfo, PlayerInfo, Task> onSuccess, Action onFail)
    {
        await LockExecuteState(gameId, async (gameInfo) =>
        {
            PlayerInfo opponentInfo;

            //trying to join an already occupied position
            if (gameInfo.Player1.IsDefined && gameInfo.Player2.IsDefined)
            {
                onFail();
                return;
            }
            if (gameInfo.Player1.IsDefined)
            {
                gameInfo.Player2 = new PlayerInfo(playerId, false);
                opponentInfo = gameInfo.Player1;
            }
            else
            {
                gameInfo.Player1 = new PlayerInfo(playerId, true);
                opponentInfo = gameInfo.Player2;
            }
            
            await onSuccess(gameInfo, opponentInfo);
        });
    }

    public static async Task TryMakeMove(int gameId, byte fromXy, byte toXy, Func<GameInfo, CheckersMove, Task> onSuccessfulMove)
    {
        await LockExecuteState(gameId, async (gameInfo) =>
        {
            //todo re add validation here so you can't make moves for your opponent 
            var result = GameLogic.GameLogic.TryApplyMove(ref gameInfo.GameState, fromXy, toXy);
            if (result.Success)
            {
                var checkersMove = new CheckersMove(fromXy, toXy, result.Promoted, result.CapturedPieces);
                gameInfo.AddHistory(checkersMove);
                gameInfo.RefreshStatus();
                await onSuccessfulMove(gameInfo, checkersMove);
            }
        });
    }

    public static GameMetaData[] GetActiveGames()
    {
        //optimize this later. Probably want to maintain a list or hashset of active games instead. 
        var activeGamesCount = 0;
        for (var i = 0; i < _gameSpace.Length; i++)
        {
            ref readonly var gameSlot = ref _gameSpace[i];
            if (gameSlot.GameInfo.IsActive)
            {
                activeGamesCount++;
            }
        }
        var activeGames = new GameMetaData[activeGamesCount];
        for (var i = 0; i < activeGamesCount; i++)
        {
            ref readonly var gameSlot = ref _gameSpace[i];
            if (gameSlot.GameInfo.IsActive)
            {
                activeGames[i] = gameSlot.GameInfo.ToMetaData();
            }
        }
        
        return activeGames;
    }
    
    private static GameSlot GetGameSlotIfActive(int gameId)
    {
        if (gameId < 0 || gameId >= _gameSpace.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(gameId));
        }
        
        ref var gameSlot = ref _gameSpace[gameId];
        if (gameSlot.GameInfo.IsActive == false)
        {
            throw new InvalidOperationException("Attempted to mutate non active game");
        }

        return gameSlot;
    }
    
    private static GameSlot GetGameSlot(int gameId)
    {
        if (gameId < 0 || gameId >= _gameSpace.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(gameId));
        }
        
        ref var gameSlot = ref _gameSpace[gameId];
        if (gameSlot.GameInfo.IsActive == false)
        {
            throw new InvalidOperationException("Attempted to mutate non active game");
        }

        return gameSlot;
    }
    
    public static async Task LockExecuteState(int gameId, Action<GameInfo> mutation)
    {
        var gameSlot = GetGameSlotIfActive(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            mutation(gameSlot.GameInfo);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }
    }
    public static async Task<TResult?> LockExecuteState<TResult>(int gameId, Func<GameInfo, TResult> mutation)
    {
        TResult? result = default;
        
        var gameSlot = GetGameSlotIfActive(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            result = mutation(gameSlot.GameInfo);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }

        return result;
    }
    
    public static async Task LockExecuteState(int gameId, Func<GameInfo, Task> mutation)
    {
        var gameSlot = GetGameSlotIfActive(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            await mutation(gameSlot.GameInfo);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }
    }
    
    public static async Task<TResult?> LockExecuteState<TResult>(int gameId, Func<GameInfo, Task<TResult>> mutation)
    {
        TResult? result = default;
        
        var gameSlot = GetGameSlotIfActive(gameId);
        
        await gameSlot.GameLock.WaitAsync();  // Asynchronously acquire the lock
        try
        {
            result = await mutation(gameSlot.GameInfo);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
        finally
        {
            gameSlot.GameLock.Release();  // Release the lock
        }

        return result;
    }

    public static void ResetGame(int gameInfoGameId)
    {
        //If we are calling a reset we do not care about any other requests on this state after. 
        //So race conditions are ignored. 
        var gameSlot = _gameSpace[gameInfoGameId];
        gameSlot.GameInfo.Reset();
    }

    public static PlayerInfo GetOpponentInfo(int sourceSessionGameId, Guid sourceSessionPlayerId)
    {
        var users = _gameSpace[sourceSessionGameId].GameInfo.GetNonNullUsers();
        foreach (var user in users)
        {
            if (user.PlayerId == sourceSessionPlayerId) continue;
            return user;
        }
        
        return PlayerInfo.Empty;
    }

    public static void UpdateGameStatus(int sourceSessionGameId, GameStatus status)
    {
        //Assumes we don't care about any moves made in and around this time since GameStatus changes are made to end 
        //the game.
        var gameSlot = GetGameSlot(sourceSessionGameId); 
        gameSlot.GameInfo.Status = status;
        
        //No need to reset the data since if the game is not active it will be recycled later when creating games. 
    }
} 

public readonly struct TryCreateGameResult(bool didCreateGame, GameMetaData snapShot)
{
    public readonly bool DidCreateGame = didCreateGame;
    public readonly GameMetaData CreatedGame = snapShot;
}

public readonly struct GameMetaData(int gameId, PlayerInfo player1, PlayerInfo player2)
{
    public readonly PlayerInfo Player1 = player1;
    public readonly PlayerInfo Player2 = player2;
    public readonly int GameId = gameId; 
}