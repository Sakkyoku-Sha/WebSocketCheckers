using System.Runtime.InteropServices;

namespace WebGameServer.GameLogic;

public enum GameResult : byte
{
    Player1Win = 0,
    Player2Win = 1,
    Draw = 2,
    InProgress = 3
}

//Required for Byte Serialization
[StructLayout(LayoutKind.Sequential)]
public readonly record struct CheckersMove(byte FromX, byte FromY, byte ToX, byte ToY);
    
public struct GameState()
{
    public const int BoardSize = 8; 
    private const ulong EmptyBoard = 0ul;
    private const int DefaultHistoryCapacity = 64; 
    
    private const ulong DefaultPlayer1Pawns =
        (1UL << 1)  | (1UL << 3)  | (1UL << 5)  | (1UL << 7)  |
        (1UL << 8)  | (1UL << 10) | (1UL << 12) | (1UL << 14) |
        (1UL << 17) | (1UL << 19) | (1UL << 21) | (1UL << 23);
    
    private const ulong DefaultPlayer2Pawns = 
        (1UL << 40) | (1UL << 42) | (1UL << 44) | (1UL << 46) |
        (1UL << 49) | (1UL << 51) | (1UL << 53) | (1UL << 55) |
        (1UL << 56) | (1UL << 58) | (1UL << 60) | (1UL << 62);
    
    public ulong Player1Pawns = EmptyBoard; //bitboard representations 
    public ulong Player1Kings = EmptyBoard;
    public ulong Player2Pawns = EmptyBoard;
    public ulong Player2Kings = EmptyBoard;
    public CheckersMove[] MoveHistory = new CheckersMove[DefaultHistoryCapacity];
    public int MoveHistoryCount = 0;  
    private int _MoveHistoryCapacity = DefaultHistoryCapacity;
    
    public bool IsPlayer1Turn = true;
    public GameResult Result = GameResult.InProgress;

    public GameState(GameState other) : this()
    {
        Player1Pawns = other.Player1Pawns;
        Player2Pawns = other.Player2Pawns;
        Player1Kings = other.Player1Kings;
        Player2Kings = other.Player2Kings;
        MoveHistory = other.MoveHistory;
        IsPlayer1Turn = other.IsPlayer1Turn;
        Result = other.Result;
    }
    
    public void SetUpDefaultBoard()
    {
        Player1Pawns = DefaultPlayer1Pawns;
        Player1Kings = EmptyBoard;
        Player2Pawns = DefaultPlayer2Pawns;
        Player2Kings = EmptyBoard;
        IsPlayer1Turn = true;
        MoveHistory = new CheckersMove[0];
        Result = GameResult.InProgress;
    }

    // Bitboard helper functions.
    public static int GetBitIndex(int x, int y) => y * BoardSize + x;
    public static ulong SetBit(ulong board, int index) => board | (1UL << index);
    public static ulong ClearBit(ulong board, int index) => board & ~(1UL << index);
    public static int GetX(int index) => index % BoardSize;
    public static int GetY(int index) => index / BoardSize;
    public static bool IsBitSet(ulong board, int index) => (board & (1UL << index)) != 0;
    
    public bool IsSquareOccupied(int x, int y) =>
        IsBitSet(Player1Pawns, GetBitIndex(x, y)) ||
        IsBitSet(Player1Kings, GetBitIndex(x, y)) ||
        IsBitSet(Player2Pawns, GetBitIndex(x, y)) ||
        IsBitSet(Player2Kings, GetBitIndex(x, y));

    public bool IsSquareOccupiedByPlayer1(int x, int y) =>
        IsBitSet(Player1Pawns, GetBitIndex(x, y)) ||
        IsBitSet(Player1Kings, GetBitIndex(x, y));

    public bool IsSquareOccupiedByPlayer2(int x, int y) =>
        IsBitSet(Player2Pawns, GetBitIndex(x, y)) ||
        IsBitSet(Player2Kings, GetBitIndex(x, y));

    public void AddHistory(CheckersMove move)
    {
        if (MoveHistoryCount >= _MoveHistoryCapacity)
        {
            var newHistoryCapacity = _MoveHistoryCapacity << 1;
            var largerArray = new CheckersMove[newHistoryCapacity];
            Array.Copy(MoveHistory, largerArray, _MoveHistoryCapacity);
            _MoveHistoryCapacity = newHistoryCapacity; 
        }
        MoveHistory[MoveHistoryCount] = move;
        MoveHistoryCount++;
    }
    public void ClearHistory()
    {
        MoveHistoryCount = 0;
        _MoveHistoryCapacity = DefaultHistoryCapacity;
        MoveHistory = new CheckersMove[_MoveHistoryCapacity]; 
    }
    
    public bool IsSquareEmpty(int x, int y) => !IsSquareOccupied(x, y);

    public ulong GetPlayer1Pieces() => Player1Pawns | Player1Kings;
    public ulong GetPlayer2Pieces() => Player2Pawns | Player2Kings;
    public ulong GetAllPieces() => Player1Pawns | Player2Pawns | Player2Kings | Player1Kings;
}  