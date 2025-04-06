using System.Numerics;
using WebGameServer.State;

namespace WebGameServer.GameLogic;

public static class GameLogic
{
    private static bool IsOnBoard(int x, int y) => x >= 0 && x < GameState.BoardSize && y >= 0 && y < GameState.BoardSize;
    private static bool IsDarkSquare(int x, int y) => (x + y) % 2 == 1;
    
    public static TryMoveResult TryApplyMove(ref GameState state, int fromBitIndex, int toBitIndex)
    {
        var validationResult = ValidateMove(state, fromBitIndex, toBitIndex);
        if (validationResult.Valid == false)
        {
            return TryMoveResult.Fail;
        }
        
        var playerKings = state.IsPlayer1Turn ? state.Player1Kings : state.Player2Kings;
        var wasKing = GameState.IsBitSet(playerKings, fromBitIndex);
        MovePlayerPiece(ref state, fromBitIndex, toBitIndex, wasKing);
        
        //Remove opponent pieces if jumped
        if (validationResult.JumpInfo.HasValue)
        {
            RemoveOpponentPieces(ref state, validationResult.JumpInfo.Value.capturedPieces);
        }
        
        //Promotion Logic. 
        var shouldPromote = ShouldPromote(toBitIndex, state.IsPlayer1Turn) | validationResult.JumpInfo?.jumpedIntoPromotionSquare ?? false; 
        if (shouldPromote)
        {
            PromotePiece(ref state, toBitIndex);
        }
        
        //Flip Turns, jumps must go to the last possible position and turns change. 
        state.IsPlayer1Turn = !state.IsPlayer1Turn;
        return new TryMoveResult(true, shouldPromote, validationResult.JumpInfo?.capturedPieces ?? 0ul);
    }

    private static void PromotePiece(ref GameState state, int toBitIndex)
    {
        if (state.IsPlayer1Turn)
        {
            GameState.SetBit(ref state.Player1Kings, toBitIndex);
            GameState.ClearBit(ref state.Player1Pawns, toBitIndex);
        }
        else
        {
            GameState.SetBit(ref state.Player2Kings, toBitIndex);
            GameState.ClearBit(ref state.Player2Pawns, toBitIndex);
        }
    }

    private static void RemoveOpponentPieces(ref GameState state, ulong capturedPieces)
    {
        if (state.IsPlayer1Turn)
        {
            state.Player2Kings &= ~capturedPieces; //works since we should never have a captured piece that isn't the opponents piece. 
            state.Player2Pawns &= ~capturedPieces;
        }
        else
        {
            state.Player1Kings &= ~capturedPieces;
            state.Player1Pawns &= ~capturedPieces;
        }
    }

    private static MoveValidationResult ValidateMove(GameState state, int fromBitIndex, int toBitIndex)
    {
        var (fromX, fromY) = GameState.GetXy(fromBitIndex);
        var (toX, toY) = GameState.GetXy(toBitIndex);
        
        //Move must be on the board and on a dark square 
        if (
            !IsOnBoard(fromX, fromY) || 
            !IsOnBoard(toX, toY) || 
            !IsDarkSquare(fromX, fromY) || 
            !IsDarkSquare(toX, toY))
        {
            return MoveValidationResult.Invalid;
        }
        
        //Can't move to a space with a piece, (unless it's the same square (as cycles jumps are supported)) 
        var allPieces = state.GetAllPieces();
        if (GameState.IsBitSet(allPieces, toBitIndex) && fromBitIndex != toBitIndex)
        {
            return MoveValidationResult.Invalid;
        }
        
        //Only Can move the current turn pieces. 
        var playerPieces = state.IsPlayer1Turn ? state.GetPlayer1Pieces() : state.GetPlayer2Pieces();
        if (!GameState.IsBitSet(playerPieces, fromBitIndex))
        {
            return MoveValidationResult.Invalid;
        }
        
        //Determine if we are working with a king or a pawn 
        var playerKings = state.IsPlayer1Turn ? state.Player1Kings : state.Player2Kings;
        var isKing = GameState.IsBitSet(playerKings, fromBitIndex);
        
        //Determine offsets. 
        var dx = toX- fromX;
        var dy = toY - fromY;
        
        //Pawn movement must be forward 
        if (!isKing && (state.IsPlayer1Turn && dy > 0) || !isKing && (!state.IsPlayer1Turn && dy < 0))
        {
            return MoveValidationResult.Invalid;
        }
        
        Span<StackFrame> results = stackalloc StackFrame[6];
        Span<StackFrame> work    = stackalloc StackFrame[32];
        int count = DetermineAllPossibleJumps(state.IsPlayer1Turn, playerPieces, playerKings, allPieces, results, work);
        if (count == 0 && Math.Abs(dx) == 1 && Math.Abs(dy) == 1)
        {
            return new MoveValidationResult(true, null);
        }
        
        //Only valid if there is a path with an end with the request to location.
        StackFrame jumpPath = default;
        for (int i = 0; i < count; i++)
        {
            if (results[i].CurrentEndOfPath == toBitIndex)
            {
                jumpPath = results[i];
                break;
            }
        }
        if (jumpPath.CapturedPieces == 0ul) //should Only occur in default case as this would be a jump without any captures 
        {
            return MoveValidationResult.Invalid;
        }
        
        return new MoveValidationResult(true, (jumpPath.CapturedPieces, jumpPath.IsKing));
    }
    private static void MovePlayerPiece(ref GameState state, int fromBitIndex, int toBitIndex, bool wasKing)
    {
        ref var playerPieceBoard = ref state.Player1Pawns; 
        
        if (state.IsPlayer1Turn)
            playerPieceBoard = ref wasKing ? ref state.Player1Kings : ref state.Player1Pawns;
        else
            playerPieceBoard = ref wasKing ? ref state.Player2Kings : ref state.Player2Pawns;
        
        // Clear the bit and set the new bit on the selected board.
        GameState.ClearBit(ref playerPieceBoard, fromBitIndex);
        GameState.SetBit(ref playerPieceBoard, toBitIndex);
    }

    private struct StackFrame(int currentEndOfPath, bool isKing, ulong capturedPieces, int initialPosition)
    {
        public int CurrentEndOfPath = currentEndOfPath;
        public bool IsKing = isKing;
        public ulong CapturedPieces = capturedPieces;
        public int InitialPosition = initialPosition; 
    }
    
    private static int DetermineAllPossibleJumps(
        bool isP1,
        ulong pPieces,
        ulong pKings,
        ulong allPieces,
        Span<StackFrame> results,  // e.g. stackalloc StackFrame[6]
        Span<StackFrame> work      // e.g. stackalloc StackFrame[32]
    )
    {
        int resCount = 0, workCount = 0;
        ulong opp = pPieces ^ allPieces;

        // Seed the work‑stack
        while (pPieces != 0)
        {
            int i = BitOperations.TrailingZeroCount(pPieces);
            bool king = ((pKings >> i) & 1) != 0;
            work[workCount].CurrentEndOfPath = i;
            work[workCount].IsKing = king;
            work[workCount].CapturedPieces = 0ul;
            work[workCount].InitialPosition = i;
            workCount++;
            pPieces &= pPieces - 1;
        }

        // Process
        while (workCount > 0)
        {
            var f = work[--workCount];
            int i = f.CurrentEndOfPath;
            int file = i & 7, rank = i >> 3;
            bool up    = rank > 1  && (f.IsKing || isP1);
            bool down  = rank < 6  && (f.IsKing || !isP1);
            bool left  = file > 1;
            bool right = file < 6;

            bool didJump = false;

            // ——— Direction 1: Up‑Left ———
            if (up && left)
            {
                int over = i - 9, to = i - 18;
                if (((f.CapturedPieces >> over) & 1) == 0
                 && (((opp >> over) & 1) != 0)
                 && (to == f.InitialPosition || ((allPieces >> to) & 1) == 0))
                {
                    ulong nextCap = f.CapturedPieces | (1UL << over);
                    bool nextKing = f.IsKing || ShouldPromote(to, isP1);
                    
                    work[workCount].CurrentEndOfPath = to;
                    work[workCount].IsKing = nextKing;
                    work[workCount].CapturedPieces = nextCap;
                    work[workCount].InitialPosition = f.InitialPosition;
                    workCount++;
                    
                    didJump = true;
                }
            }

            // ——— Direction 2: Up‑Right ———
            if (up && right)
            {
                int over = i - 7, to = i - 14;
                if (((f.CapturedPieces >> over) & 1) == 0
                 && (((opp >> over) & 1) != 0)
                 && (to == f.InitialPosition || ((allPieces >> to) & 1) == 0))
                {
                    ulong nextCap = f.CapturedPieces | (1UL << over);
                    bool nextKing = f.IsKing || ShouldPromote(to, isP1);
                    
                    work[workCount].CurrentEndOfPath = to;
                    work[workCount].IsKing = nextKing;
                    work[workCount].CapturedPieces = nextCap;
                    work[workCount].InitialPosition = f.InitialPosition;
                    workCount++;
                    
                    didJump = true;
                }
            }

            // ——— Direction 3: Down‑Left ———
            if (down && left)
            {
                int over = i + 7, to = i + 14;
                if (((f.CapturedPieces >> over) & 1) == 0
                 && (((opp >> over) & 1) != 0)
                 && (to == f.InitialPosition || ((allPieces >> to) & 1) == 0))
                {
                    ulong nextCap = f.CapturedPieces | (1UL << over);
                    bool nextKing = f.IsKing || ShouldPromote(to, isP1);
                    
                    work[workCount].CurrentEndOfPath = to;
                    work[workCount].IsKing = nextKing;
                    work[workCount].CapturedPieces = nextCap;
                    work[workCount].InitialPosition = f.InitialPosition;
                    workCount++;
                    
                    didJump = true;
                }
            }

            // ——— Direction 4: Down‑Right ———
            if (down && right)
            {
                int over = i + 9, to = i + 18;
                if (((f.CapturedPieces >> over) & 1) == 0
                 && (((opp >> over) & 1) != 0)
                 && (to == f.InitialPosition || ((allPieces >> to) & 1) == 0))
                {
                    ulong nextCap = f.CapturedPieces | (1UL << over);
                    bool nextKing = f.IsKing || ShouldPromote(to, isP1);
                   
                    work[workCount].CurrentEndOfPath = to;
                    work[workCount].IsKing = nextKing;
                    work[workCount].CapturedPieces = nextCap;
                    work[workCount].InitialPosition = f.InitialPosition;
                    workCount++;
                    
                    didJump = true;
                }
            }

            // If we did no jumps but have captured at least one, record the result
            if (!didJump && f.CapturedPieces != 0UL)
            {
                results[resCount++] = f;
            }
        }

        return resCount;
    }
    
    private static bool ShouldPromote(int jumpIndex, bool player1Turn)
    {
        return player1Turn ? jumpIndex < 8 : jumpIndex > 55;
    }
}

public readonly struct TryMoveResult(bool success, bool promoted, ulong capturedPieces)
{
    public readonly bool Success = success; 
    public readonly bool Promoted = promoted;
    public readonly ulong CapturedPieces = capturedPieces;

    public static readonly TryMoveResult Fail = new(false, false, 0);
}
public readonly struct MoveValidationResult(bool valid, (ulong capturedPieces, bool jumpedIntoPromotionSquare)? jumpInfo)
{
    public readonly bool Valid  = valid;
    public readonly (ulong capturedPieces, bool jumpedIntoPromotionSquare)? JumpInfo = jumpInfo;
    
    public static readonly MoveValidationResult Invalid = new(false, null); 
}