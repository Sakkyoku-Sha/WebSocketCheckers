using System.Runtime.InteropServices;
using WebGameServer.GameStateManagement.GameStateStore;
using WebGameServer.GameStateManagement.Timers;

namespace WebGameServer.State;

public enum GameStatus : byte
{
    NoPlayers = 0,
    WaitingForPlayers = 1,
    InProgress = 2,
    Abandoned = 3,
    Player1Win = 4,
    Player2Win = 5,
    Draw = 6,
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct TimedCheckersMove(long timeMs, CheckersMove move) 
{
    [FieldOffset(0)] public readonly long TimeMs = timeMs;
    [FieldOffset(8)] public readonly CheckersMove Move = move;
    
    public const int ByteSize = sizeof(long) + CheckersMove.ByteSize; // 8 + 19 = 27 bytes
}
public static class CheckersMoveExtension{
    public static TimedCheckersMove ToTimedMove(this CheckersMove move, long timeMs)
    {
        return new TimedCheckersMove(timeMs, move);
    }
}

public class GameInfo
{
    public const uint EmptyGameId = uint.MaxValue;
    private const int DefaultHistoryCapacity = 64;
    
    public readonly uint GameId;
    public readonly uint TimerId;
    public GameTimer GetTimer() => GameTimers.GetTimer(TimerId);
    private long GetDeltaTimeMs() => GameTimers.GetTimer(TimerId).GetDeltaTime();
    
    //Time Related Fields 
    private const int DefaultPlayerTimeMs = 300000; //5 minutes for each player;
    public uint Player1RemainingTimeMs = DefaultPlayerTimeMs; 
    public uint Player2RemainingTimeMs = DefaultPlayerTimeMs; 
    
    public GameStatus Status = GameStatus.NoPlayers;
    public string GameName = "Checkers Game";
    public PlayerInfo Player1;
    public PlayerInfo Player2;
    public GameState GameState;
    public TimedCheckersMove[] MoveHistory = new TimedCheckersMove[DefaultHistoryCapacity];
    public int MoveHistoryCount;
    
    private int _moveHistoryCapacity = DefaultHistoryCapacity;
    public long GameStartTimeMs = -1;
    
    public GameInfo(uint gameId, uint timerId)
    {
        GameState = new GameState(true); 
        GameId = gameId >= LocalGameSpace.GameSpaceCapacity ? throw new ArgumentOutOfRangeException(nameof(gameId)) : gameId;
        TimerId = timerId >= GameTimers.TimerAmount ? throw new ArgumentOutOfRangeException(nameof(timerId)) : timerId;
    }

    public bool IsActive => Status is GameStatus.InProgress or GameStatus.WaitingForPlayers;

    public GameMetaData ToMetaData()
    {
        return new GameMetaData(GameId, Player1, Player2);
    }

    public void AddHistory(CheckersMove move)
    {
        if (MoveHistoryCount == 0)
        {
            GameStartTimeMs = GetTimer().elapsedTimeMs; 
        }
        
        var timedMove = move.ToTimedMove(GetTimer().elapsedTimeMs);
        
        if (MoveHistoryCount >= _moveHistoryCapacity)
        {
            var newHistoryCapacity = _moveHistoryCapacity << 1;
            var largerArray = new TimedCheckersMove[newHistoryCapacity];
            Array.Copy(MoveHistory, largerArray, _moveHistoryCapacity);
            MoveHistory = largerArray;
            _moveHistoryCapacity = newHistoryCapacity; 
        }
        MoveHistory[MoveHistoryCount] = timedMove;
        MoveHistoryCount++;
    }
    
    public void Reset()
    {
        Player1 = PlayerInfo.Empty;
        Player2 = PlayerInfo.Empty;
        Status = GameStatus.NoPlayers;
        GameState.SetUpDefaultBoard();
        MoveHistoryCount = 0;
        GameStartTimeMs = 0;
    }

    public PlayerInfo[] GetNonNullPlayers()
    {
        if (Player1.IsDefined && Player2.IsDefined)
        {
            return [Player1, Player2]; 
        }
        if (Player1.IsDefined)
        {
            return [Player1];
        }
        if (Player2.IsDefined)
        {
            return [Player2];
        }

        return [];
    }
    public Guid[] GetNonNullPlayerIds()
    {
        if (Player1.IsDefined && Player2.IsDefined)
        {
            return [Player1.PlayerId, Player2.PlayerId]; 
        }
        if (Player1.IsDefined)
        {
            return [Player1.PlayerId];
        }
        if (Player2.IsDefined)
        {
            return [Player2.PlayerId];
        }

        return [];
    }
    

    public void RefreshStatus()
    {
        RefreshStatusOnTime();
        if (!IsActive)
        {
            return; 
        }
        if (Player1.IsDefined && Player2.IsDefined)
        {
            Status = GameState.Result switch
            {
                GameResult.Player1Win => GameStatus.Player1Win,
                GameResult.Player2Win => GameStatus.Player2Win,
                GameResult.Draw => GameStatus.Draw,
                GameResult.InProgress => Status, // Keep the current status if the game is still in progress
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        else if (Player1.IsDefined || Player2.IsDefined)
        {
            Status = GameStatus.WaitingForPlayers;
        }
        else
        {
            Status = GameStatus.NoPlayers;
        }
    }
    
    public void SetAbandon()
    {
        Status = GameStatus.Abandoned;
    }

    public bool IsGameFinished()
    {
        return Status is GameStatus.Abandoned or GameStatus.Player1Win or GameStatus.Player2Win or GameStatus.Draw;
    }

    /// <summary>
    /// Method is called in async context, so this should only update the dynamic time fields.
    /// This is done to not have to lock to the game states while updating the time, as these are updated by a
    /// background timer. DO NOT UPDATE OTHER GAME FIELDS IN THIS METHOD 
    /// <returns></returns>
    public void UpdateTime()
    {
        var deltaMs = GetDeltaTimeMs();
        if (GameState.IsPlayer1Turn)
        {
            Player1RemainingTimeMs = Math.Max(0, Player1RemainingTimeMs - (uint)deltaMs);
        }
        else
        {
            Player2RemainingTimeMs = Math.Max(0, Player2RemainingTimeMs - (uint)deltaMs);
        }
    }
    public bool IsTimedOut()
    {
        return Player1RemainingTimeMs <= 0 || Player2RemainingTimeMs <= 0;
    }
    
    private void RefreshStatusOnTime()
    {
        if (GameState.IsPlayer1Turn)
        {
            if (Player1RemainingTimeMs <= 0)
            {
                GameState.Result = GameResult.Player2Win;
                Status = GameStatus.Player2Win;
            }
        }
        else
        {
            if (Player2RemainingTimeMs <= 0)
            {
                GameState.Result = GameResult.Player1Win;
                Status = GameStatus.Player1Win;
            }
        }
    }
}