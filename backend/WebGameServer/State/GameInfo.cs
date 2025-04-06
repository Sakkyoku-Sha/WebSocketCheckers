using System.Runtime.InteropServices;

namespace WebGameServer.State;


public enum GameStatus : byte
{
    WaitingForPlayers,
    InProgress,
    Finished
}

//Required for Efficient Byte Serialization
[StructLayout(LayoutKind.Sequential)]
public record struct CheckersMove(byte FromIndex, byte ToIndex, bool Promoted, ulong CapturedPieces);


public class GameInfo(Guid gameId, PlayerInfo? player1, PlayerInfo? player2, GameState gameState, string gameName, GameStatus status)
{
    private const int DefaultHistoryCapacity = 64; 
    
    public Guid GameId { get; set; } = gameId;
    public GameStatus Status { get; set; } = status;
    public string GameName { get; set; } = gameName;
    public PlayerInfo? Player1 { get; set; } = player1;
    public PlayerInfo? Player2 { get; set; } = player2;
    public GameState GameState { get; set; } = gameState;
    public CheckersMove[] MoveHistory  { get; set; } = new CheckersMove[DefaultHistoryCapacity];
    public int MoveHistoryCount { get; set; }  = 0;  
    private int _moveHistoryCapacity = DefaultHistoryCapacity;
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
}