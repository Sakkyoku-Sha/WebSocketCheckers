using System.Runtime.InteropServices;
using WebGameServer.GameStateManagement.GameStateStore;

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

//Required for Efficient Byte Serialization DO NOT DELETE 
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct CheckersMove(byte fromIndex, byte toIndex, bool promoted, ulong capturedPawns, ulong capturedKings)
{
    public readonly byte FromIndex = fromIndex;
    public readonly byte ToIndex = toIndex;
    public readonly bool Promoted = promoted; 
    public readonly ulong CapturedPawns = capturedPawns;
    public readonly ulong CapturedKings = capturedKings;
    
    public const int ByteSize = 19; // 1 + 1 + 1 + 8 + 8
}

public class GameInfo
{
    private const int DefaultHistoryCapacity = 64;

    public int GameId = -1; 
    public GameStatus Status = GameStatus.NoPlayers;
    public string GameName = "Checkers Game";
    public PlayerInfo Player1;
    public PlayerInfo Player2;
    public GameState GameState;
    public CheckersMove[] MoveHistory = new CheckersMove[DefaultHistoryCapacity];
    public int MoveHistoryCount;  
    private int _moveHistoryCapacity = DefaultHistoryCapacity;

    public GameInfo(int gameId)
    {
        GameState = new GameState(true); 
        GameId = gameId < 0 ? throw new ArgumentOutOfRangeException(nameof(gameId)) : gameId;
    }

    public bool IsActive => Status is GameStatus.InProgress or GameStatus.WaitingForPlayers;

    public GameMetaData ToMetaData()
    {
        return new GameMetaData(GameId, Player1, Player2);
    }

    public void AddHistory(CheckersMove move)
    {
        if (MoveHistoryCount >= _moveHistoryCapacity)
        {
            var newHistoryCapacity = _moveHistoryCapacity << 1;
            var largerArray = new CheckersMove[newHistoryCapacity];
            Array.Copy(MoveHistory, largerArray, _moveHistoryCapacity);
            MoveHistory = largerArray;
            _moveHistoryCapacity = newHistoryCapacity; 
        }
        MoveHistory[MoveHistoryCount] = move;
        MoveHistoryCount++;
    }
    public void ClearHistory()
    {
        MoveHistoryCount = 0;
        _moveHistoryCapacity = DefaultHistoryCapacity;
        MoveHistory = new CheckersMove[_moveHistoryCapacity]; 
    }
    public Span<CheckersMove> GetHistory()
    {
        if (MoveHistoryCount == 0)
        {
            return Span<CheckersMove>.Empty;
        }
        return new Span<CheckersMove>(MoveHistory, 0, MoveHistoryCount);
    }

    public void Reset()
    {
        Player1 = new PlayerInfo();
        Player2 = new PlayerInfo();
        Status = GameStatus.NoPlayers;
        GameState.SetUpDefaultBoard();
        MoveHistoryCount = 0;
    }

    public PlayerInfo[] GetNonNullUsers()
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

    public void RefreshStatus()
    {
        if (Player1.IsDefined && Player2.IsDefined)
        {
            Status = GameState.Result switch
            {
                GameResult.Player1Win => GameStatus.Player1Win,
                GameResult.Player2Win => GameStatus.Player2Win,
                GameResult.Draw => GameStatus.Draw,
                GameResult.InProgress => GameStatus.InProgress,
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
}