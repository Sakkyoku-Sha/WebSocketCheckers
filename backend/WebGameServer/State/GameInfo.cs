using System.Runtime.InteropServices;
using WebGameServer.GameStateManagement.GameStateStore;

namespace WebGameServer.State;


public enum GameStatus : byte
{
    NoPlayers = 0,
    WaitingForPlayers = 1,
    InProgress = 2,
    Finished = 3,
    Abandoned = 4
}

//Required for Efficient Byte Serialization DO NOT DELETE 
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CheckersMove(byte FromIndex, byte ToIndex, bool Promoted, ulong CapturedPieces)
{
    public readonly byte FromIndex = FromIndex;
    public readonly byte ToIndex = ToIndex;
    public readonly bool Promoted = Promoted; 
    public readonly ulong CapturedPieces = CapturedPieces;
    
    public const int ByteSize = 11; 
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
    
    public GameMetaData SnapShot()
    {
        return new GameMetaData(GameId, ref Player1, ref Player2);
    }

    public void AddHistory(CheckersMove move)
    {
        if (MoveHistoryCount >= _moveHistoryCapacity)
        {
            var newHistoryCapacity = _moveHistoryCapacity << 1;
            var largerArray = new CheckersMove[newHistoryCapacity];
            Array.Copy(MoveHistory, largerArray, _moveHistoryCapacity);
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
}