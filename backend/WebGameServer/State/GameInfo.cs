using System.Runtime.InteropServices;

namespace WebGameServer.State;


public enum GameStatus : byte
{
    NoPlayers,
    WaitingForPlayers,
    InProgress,
    Finished,
    Abandoned
}

//Required for Efficient Byte Serialization DO NOT DELETE 
[StructLayout(LayoutKind.Sequential)]
public readonly record struct CheckersMove(byte FromIndex, byte ToIndex, bool Promoted, ulong CapturedPieces)
{
    public const int ByteSize = 11; 
}

public class GameInfo
{
    private const int DefaultHistoryCapacity = 64;

    public int GameId = -1; 
    public GameStatus Status = GameStatus.WaitingForPlayers;
    public string GameName = "Checkers Game";
    public PlayerInfo? Player1;
    public PlayerInfo? Player2;
    public GameState GameState;
    public CheckersMove[] MoveHistory = new CheckersMove[DefaultHistoryCapacity];
    public int MoveHistoryCount;  
    private int _moveHistoryCapacity = DefaultHistoryCapacity;

    public GameInfo(int gameId)
    {
        gameId = gameId < 0 ? throw new ArgumentOutOfRangeException(nameof(gameId)) : gameId;
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
        Player1 = null;
        Player2 = null;
        Status = GameStatus.WaitingForPlayers;
        GameState.SetUpDefaultBoard();
        MoveHistoryCount = 0;
    }
}