namespace WebGameServer.State;

public enum GameResult : byte
{
    Player1Win = 0,
    Player2Win = 1,
    Draw = 2,
    InProgress = 3,
}

public struct GameState()
{
    public const int BoardSize = 8; 
    public const ulong EmptyBoard = 0ul;
    
    private const ulong DefaultPlayer2Pawns =
        (1UL << 1)  | (1UL << 3)  | (1UL << 5)  | (1UL << 7)  |
        (1UL << 8)  | (1UL << 10) | (1UL << 12) | (1UL << 14) |
        (1UL << 17) | (1UL << 19) | (1UL << 21) | (1UL << 23);
    
    private const ulong DefaultPlayer1Pawns = 
        (1UL << 40) | (1UL << 42) | (1UL << 44) | (1UL << 46) |
        (1UL << 49) | (1UL << 51) | (1UL << 53) | (1UL << 55) |
        (1UL << 56) | (1UL << 58) | (1UL << 60) | (1UL << 62);
    
    public ulong Player1Pawns  = EmptyBoard;
    public ulong Player1Kings = EmptyBoard;
    public ulong Player2Pawns = EmptyBoard;
    public ulong Player2Kings = EmptyBoard;
    public bool IsPlayer1Turn = true;
    
    public GameResult Result = GameResult.InProgress;
    
    //Mainly exists to help the client know if it should show a forced jump.
    public JumpPath[] CurrentForcedJumps = [];
    
    public GameState(GameState other) : this()
    {
        Player1Pawns = other.Player1Pawns;
        Player2Pawns = other.Player2Pawns;
        Player1Kings = other.Player1Kings;
        Player2Kings = other.Player2Kings;
        IsPlayer1Turn = other.IsPlayer1Turn;
        Result = other.Result;
        CurrentForcedJumps = other.CurrentForcedJumps;
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
        Result = GameResult.InProgress;
        CurrentForcedJumps = [];
    }
    
    // Bitboard helper functions.
    public static int GetBitIndex(int x, int y) => y * BoardSize + x;
    public static void SetBit(ref ulong board, int index) => board |= (1UL << index);
    public static void ClearBit(ref ulong board, int index) => board &= ~(1UL << index);
    public static (int x, int y) GetXy(int index) => (index % BoardSize, index / BoardSize);
    public static bool IsBitSet(ulong board, int index) => (board & (1UL << index)) != 0;
    public ulong GetPlayer1Pieces() => Player1Pawns | Player1Kings;
    public ulong GetPlayer2Pieces() => Player2Pawns | Player2Kings;
    public ulong GetAllPieces() => Player1Pawns | Player2Pawns | Player2Kings | Player1Kings;
} 

public struct JumpPath(int currentEndOfPath, bool isKing, ulong capturedPieces, int initialPosition)
{
    public int CurrentEndOfPath = currentEndOfPath;
    public bool IsKing = isKing;
    public ulong CapturedPieces = capturedPieces;
    public int InitialPosition = initialPosition; 
}