namespace WebGameServer.State;

public enum GameResult : byte
{
    Player1Win = 0,
    Player2Win = 1,
    Draw = 2,
    InProgress = 3
}

//Required for Byte Serialization
public record struct CheckersMove(byte FromIndex, byte ToIndex, bool Promoted, ulong CapturedPieces);

public class GameState()
{
    public const int BoardSize = 8; 
    private const ulong EmptyBoard = 0ul;
    private const int DefaultHistoryCapacity = 64; 
    
    private const ulong DefaultPlayer2Pawns =
        (1UL << 1)  | (1UL << 3)  | (1UL << 5)  | (1UL << 7)  |
        (1UL << 8)  | (1UL << 10) | (1UL << 12) | (1UL << 14) |
        (1UL << 17) | (1UL << 19) | (1UL << 21) | (1UL << 23);
    
    private const ulong DefaultPlayer1Pawns = 
        (1UL << 40) | (1UL << 42) | (1UL << 44) | (1UL << 46) |
        (1UL << 49) | (1UL << 51) | (1UL << 53) | (1UL << 55) |
        (1UL << 56) | (1UL << 58) | (1UL << 60) | (1UL << 62);
    
    /// Getters and setters exists to support JSON serialization for now....
    public ulong Player1Pawns { get; set; }  = EmptyBoard;
    public ulong Player1Kings { get; set; }  = EmptyBoard;
    public ulong Player2Pawns { get; set; }  = EmptyBoard;
    public ulong Player2Kings { get; set; } = EmptyBoard;
    
    //todo think about serialization of this field (ideally serialize this whole thing to bytes) 
    public CheckersMove[] MoveHistory  { get; set; } = new CheckersMove[DefaultHistoryCapacity];
    public int MoveHistoryCount { get; set; }  = 0;  
    private int _moveHistoryCapacity = DefaultHistoryCapacity;
    
    public bool IsPlayer1Turn { get; set; }  = true;
    public GameResult Result { get; set; }  = GameResult.InProgress;

    public GameState(GameState other) : this()
    {
        Player1Pawns = other.Player1Pawns;
        Player2Pawns = other.Player2Pawns;
        Player1Kings = other.Player1Kings;
        Player2Kings = other.Player2Kings;
        
        MoveHistory = other.MoveHistory;
        MoveHistoryCount = other.MoveHistoryCount;
        _moveHistoryCapacity = other._moveHistoryCapacity;
        
        IsPlayer1Turn = other.IsPlayer1Turn;
        Result = other.Result;
    }
    
    public GameState(bool useDefaultBoard) : this()
    {
        if (useDefaultBoard)
        {
            SetUpDefaultBoard();
        }
    }
    
    public void SetUpDefaultBoard()
    {
        Player1Pawns = DefaultPlayer1Pawns;
        Player1Kings = EmptyBoard;
        Player2Pawns = DefaultPlayer2Pawns;
        Player2Kings = EmptyBoard;
        IsPlayer1Turn = true;
        
        MoveHistory = new CheckersMove[DefaultHistoryCapacity];
        MoveHistoryCount = 0;
        _moveHistoryCapacity = DefaultHistoryCapacity;
        
        Result = GameResult.InProgress;
    }

    // Bitboard helper functions.
    public static int GetBitIndex(int x, int y) => y * BoardSize + x;
    public static ulong SetBit(ulong board, int index) => board | (1UL << index);
    public static ulong ClearBit(ulong board, int index) => board & ~(1UL << index);
    public static int GetX(int index) => index % BoardSize;
    public static int GetY(int index) => index / BoardSize;
    public static (int x, int y) GetXY(int index) => (GetX(index), GetY(index));
    public static bool IsBitSet(ulong board, int index) => (board & (1UL << index)) != 0;

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

    public ulong GetPlayer1Pieces() => Player1Pawns | Player1Kings;
    public ulong GetPlayer2Pieces() => Player2Pawns | Player2Kings;
    public ulong GetAllPieces() => Player1Pawns | Player2Pawns | Player2Kings | Player1Kings;
}  