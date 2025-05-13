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
    private static readonly List<int> ActiveGames = new(GameSpaceCapacity); 
    
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
        var newGameId = -1;
        var didCreateGame = false;
        var creatingPlayer = new PlayerInfo(playerId);
        
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
                    gameSlot.GameInfo.Player1 = creatingPlayer;
                    gameSlot.GameInfo.Player2 = PlayerInfo.Empty;
                    gameSlot.GameInfo.Status = GameStatus.WaitingForPlayers;
                    _lastCreatedGameSlot = i;
                    newGameId = i;
                    
                    ActiveGames.Add(newGameId);
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

        return new TryCreateGameResult(didCreateGame, newGameId, creatingPlayer);
    }
    
    public static async Task TrySetPlayerInfo(int gameId, Guid playerId, Func<GameInfo, PlayerInfo, Task> onSuccess, Action onFail)
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
                gameInfo.Player2 = new PlayerInfo(playerId);
                opponentInfo = gameInfo.Player1;
            }
            else
            {
                gameInfo.Player1 = new PlayerInfo(playerId);
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
                await onSuccessfulMove(gameInfo, checkersMove);
            }
        });
    }

    public static GameMetaData[] GetActiveGames()
    {
        var result = new GameMetaData[ActiveGames.Count];
        for (var i = 0; i < ActiveGames.Count; i++)
        {
            var gameId = ActiveGames[i];
            
            var gameInfo = _gameSpace[gameId].GameInfo; 
            result[i] = new GameMetaData(gameId, ref gameInfo.Player1, ref gameInfo.Player2);
        }
        return result;
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
            gameSlot.GameInfo.Status == GameStatus.NoPlayers)
        {
            throw new InvalidOperationException("Attempted to mutate non active game");
        }

        return gameSlot;
    }
    
    public static async Task LockExecuteState(int gameId, Action<GameInfo> mutation)
    {
        var gameSlot = GetGameSlotIfValid(gameId);
        
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
        
        var gameSlot = GetGameSlotIfValid(gameId);
        
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
        var gameSlot = GetGameSlotIfValid(gameId);
        
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
        
        var gameSlot = GetGameSlotIfValid(gameId);
        
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
} 

public readonly struct TryCreateGameResult(bool didCreateGame, int gameId, PlayerInfo creatingPlayer)
{
    public readonly bool DidCreateGame = didCreateGame; 
    public readonly GameMetaData CreatedGame = new GameMetaData(gameId, ref creatingPlayer, ref PlayerInfo.Empty);
}

public struct GameMetaData(int gameId, ref PlayerInfo player1, ref PlayerInfo player2)
{
    public PlayerInfo Player1 = player1;
    public PlayerInfo Player2 = player2;
    public readonly int GameId = gameId; 
}