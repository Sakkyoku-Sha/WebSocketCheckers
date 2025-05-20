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
        var shouldPromote = ShouldPromote(toBitIndex, state.IsPlayer1Turn) 
                            || (validationResult.JumpInfo?.jumpedIntoPromotionSquare ?? false);
 
        if (shouldPromote)
        {
            PromotePiece(ref state, toBitIndex);
        }
        
        //Flip Turns, jumps must go to the last possible position and turns change. 
        state.IsPlayer1Turn = !state.IsPlayer1Turn;

        var forcedJumps = DetermineForcedJumpsInPosition(ref state);
        state.CurrentForcedJumps = forcedJumps;
        state.ForcedJumpsCalculated = true; 
        
        //Update the game result if the game is over.
        UpdateGameResult(ref state);
        
        return new TryMoveResult(true, shouldPromote, validationResult.JumpInfo?.capturedPieces ?? 0ul);
    }

    private static JumpPath[] DetermineForcedJumpsInPosition(ref GameState state)
    {
        var playerPieces = state.IsPlayer1Turn 
            ? state.Player1Pawns | state.Player1Kings 
            : state.Player2Pawns | state.Player2Kings;
        
        var playerKings = state.IsPlayer1Turn 
            ? state.Player1Kings 
            : state.Player2Kings;
        
        var allPieces = state.GetAllPieces();
        
        Span<JumpPath> results = stackalloc JumpPath[6];
        Span<JumpPath> work    = stackalloc JumpPath[32];
        var count = DetermineAllPossibleJumps(state.IsPlayer1Turn, playerPieces, playerKings, allPieces, results, work);
        
        return results[..count].ToArray();
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
        //Move must be on a game in progress
        if(state.Result != GameResult.InProgress){ return MoveValidationResult.Invalid; }
        
        if (state.ForcedJumpsCalculated == false)
        {
            var forcedJumps = DetermineForcedJumpsInPosition(ref state);
            state.CurrentForcedJumps = forcedJumps;
            state.ForcedJumpsCalculated = true;
        }
        
        var matchIndex = Array.FindIndex(state.CurrentForcedJumps, x => x.InitialPosition == fromBitIndex && x.EndOfPath == toBitIndex);
        if (matchIndex >= 0)
        {
            var matchingJump = state.CurrentForcedJumps[matchIndex];
            return new MoveValidationResult(true, (matchingJump.CapturedPieces, matchingJump.IsKing));
        }
        if (state.CurrentForcedJumps.Length > 0 && matchIndex == -1)
        {
           return MoveValidationResult.Invalid;     
        }
        
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
        if (Math.Abs(dx) != 1 || Math.Abs(dy) != 1) //If it's not a jump it must be a move by a single square 
        {
            return MoveValidationResult.Invalid;
        }
        
        //Pawn movement that isn't a jump, must be forward 
        var movingForward = state.IsPlayer1Turn && dy < 0 || !state.IsPlayer1Turn && dy > 0;
        if (isKing == false && movingForward == false)
        {
            return MoveValidationResult.Invalid;
        }
        
        return new MoveValidationResult(true, null);
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
    
    private static int DetermineAllPossibleJumps(
        bool isP1,
        ulong pPieces,
        ulong pKings,
        ulong allPieces,
        Span<JumpPath> results,  // e.g. stackalloc StackFrame[6]
        Span<JumpPath> work      // e.g. stackalloc StackFrame[32]
    )
    {
        int resCount = 0, workCount = 0;
        ulong opp = pPieces ^ allPieces;

        // Seed the work‑stack
        while (pPieces != 0)
        {
            int i = BitOperations.TrailingZeroCount(pPieces);
            bool king = ((pKings >> i) & 1) != 0;
            work[workCount].EndOfPath = i;
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
            int i = f.EndOfPath;
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
                    
                    work[workCount].EndOfPath = to;
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
                    
                    work[workCount].EndOfPath = to;
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
                    
                    work[workCount].EndOfPath = to;
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
                   
                    work[workCount].EndOfPath = to;
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

    private static void UpdateGameResult(ref GameState state)
    {
        var player1Pieces = state.GetPlayer1Pieces();
        var player2Pieces = state.GetPlayer2Pieces();

        if (player1Pieces == GameState.EmptyBoard)
        {
            state.Result = GameResult.Player2Win;
        }
        else if (player2Pieces == GameState.EmptyBoard)
        {
            state.Result = GameResult.Player1Win;
        }
        else if (MovesArePossible(ref state))
        {
            //We have already flipped the turns at this point, so the current turn loses. 
            state.Result = state.IsPlayer1Turn ? GameResult.Player2Win : GameResult.Player1Win;
        }
        else
        {
            state.Result = GameResult.InProgress;
        }
    }

    private static bool MovesArePossible(ref GameState state)
    {
        // Check if there are no possible moves left for the current player
        var playerPieces = state.IsPlayer1Turn ? state.GetPlayer1Pieces() : state.GetPlayer2Pieces();
        var playerKings = state.IsPlayer1Turn ? state.Player1Kings : state.Player2Kings;
        var allPieces = state.GetAllPieces(); 
        var isPlayer1Turn = state.IsPlayer1Turn;
        
        // Check all pieces to see if they have a valid move. If no moves are possible, and now jumps are possible
        // then the game is a draw.
        for(var i = 0; i < GameState.BoardSize * GameState.BoardSize; i++)
        {
            if (!GameState.IsBitSet(playerPieces, i)) continue;
            
            var piecesIsKing = GameState.IsBitSet(playerKings, i);
            var pieceMovements = piecesIsKing ? 
                KingDirections :
                (isPlayer1Turn ? Player1PawnDirections : Player2PawnDirections);

            foreach (var movement in pieceMovements)
            {
                var (x, y) = GameState.GetXy(i);
                var toCheck = (x + movement.Item1, y + movement.Item2);
                
                if(!IsOnBoard(toCheck.Item1, toCheck.Item2)) continue;
                
                var toCheckIndex = GameState.GetBitIndex(toCheck.Item1, toCheck.Item2);
                if (!GameState.IsBitSet(allPieces, toCheckIndex))
                {
                    //A movement is possible, so we are no in a draw state
                    return false;
                }
            }
        }
        
        //Assumes the GameState has an up-to-date list of forced jumps. (which are all jumps);
        return state.CurrentForcedJumps.Length == 0; 
    }

    private static readonly (int, int)[] Player1PawnDirections = [(-1, -1), (1, -1)]; 
    private static readonly (int, int)[] Player2PawnDirections = [(-1, 1), (1, 1)];
    private static readonly (int, int)[] KingDirections = [(-1, -1), (-1, 1), (1, -1), (1, 1)];
}

public struct TryMoveResult(bool success, bool promoted, ulong capturedPieces)
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