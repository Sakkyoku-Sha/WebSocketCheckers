namespace WebGameServer.GameLogic;

public static class CheckersGameLogic
{
    public static bool TryMove(CheckersGameState state, (int x, int y) from, (int x, int y) to, out CheckersGameState? newState)
    {
        newState = null;
        if (!IsInside(from.x, from.y) || !IsInside(to.x, to.y))
        {
            return false; 
        }
        
        // Precompute differences and board values.
        int distX = to.x - from.x, distY = to.y - from.y;
        int absDistX = Math.Abs(distX), absDistY = Math.Abs(distY);
        var fromValue = state.Board[from.y, from.x];
        var toValue = state.Board[to.y, to.x];

        var possibleJumpMoves = GetFirstJumpMoves(state);
        if (possibleJumpMoves.Count > 0 && !possibleJumpMoves.Contains(to))
        {
            return false; 
        }
        
        // Combine the validity checks into a single if-statement.
        // Note: when absDistX==2 we inline the midpoint lookup.
        if (
            
             absDistX > 2 || absDistY > 2 || absDistX != absDistY ||
             toValue != GameBoardSquare.Empty ||
             // Now, check piece-specific conditions based on whose turn it is.
             (state.Player1Turn
                ? (
                     // Must be a Player1 piece, and if a pawn it can only move upward.
                     (fromValue != GameBoardSquare.Player1King && fromValue != GameBoardSquare.Player1Pawn) ||
                     (fromValue == GameBoardSquare.Player1Pawn && distY > 0) ||
                     // If attempting a jump, the midpoint must be an opponent’s piece.
                     (absDistX == 2 && 
                         (state.Board[from.y + (distY / 2), from.x + (distX / 2)] != GameBoardSquare.Player2King &&
                          state.Board[from.y + (distY / 2), from.x + (distX / 2)] != GameBoardSquare.Player2Pawn))
                  )
                : (
                     // Must be a Player2 piece, and if a pawn it can only move downward.
                     (fromValue != GameBoardSquare.Player2King && fromValue != GameBoardSquare.Player2Pawn) ||
                     (fromValue == GameBoardSquare.Player2Pawn && distY < 0) ||
                     // For jumps, the midpoint must be an opponent’s piece.
                     (absDistX == 2 && 
                         (state.Board[from.y + (distY / 2), from.x + (distX / 2)] != GameBoardSquare.Player1King &&
                          state.Board[from.y + (distY / 2), from.x + (distX / 2)] != GameBoardSquare.Player1Pawn))
                  )
             )
        )
        {
            return false;
        }
        
        // Passed all checks: perform the move.
        newState = new CheckersGameState(state);
        newState.Board[to.y, to.x] = fromValue;
        newState.Board[from.y, from.x] = GameBoardSquare.Empty;
        
        // Promote if necessary.
        if ((to.y == 0 || to.y == 7) && ((byte)newState.Board[to.y, to.x] % 2 == 1))
        {
            newState.Board[to.y, to.x]++;
        }
        
        // Change the turn if not a jump; if a jump, remove the jumped piece.
        if (absDistX == 1)
        {
            newState.Player1Turn = !newState.Player1Turn;
        }
        else if (absDistX == 2)
        {   
            //Determine if we have another jump from this position. 
            var canJumpAgain = false; 
            
            state.Player1Turn = canJumpAgain ? state.Player1Turn : !state.Player1Turn;
            newState.Board[from.y + (distY / 2), from.x + (distX / 2)] = GameBoardSquare.Empty;
        }
        
        return true;
    }

    public static List<(int x , int y)> GetFirstJumpMoves(CheckersGameState state)
    {
        var possibleJumpMoves = new List<(int x, int y)>();

        foreach (var currentTurnPieces in CurrentTurnPieces(state))
        {
            var jumpDirections = DetermineJumpMovements(currentTurnPieces.square);
            
            foreach (var direction in jumpDirections)
            {
                var jumpLocationX = currentTurnPieces.x + direction.x;
                var jumpLocationY = currentTurnPieces.y + direction.y; 
                var midLocationX = currentTurnPieces.x + (direction.x / 2);
                var midLocationY = currentTurnPieces.y + (direction.y / 2);
                
                if (IsInside(jumpLocationX, jumpLocationY) && 
                    state.Board[jumpLocationY, jumpLocationX] == GameBoardSquare.Empty && 
                    (
                        state.Player1Turn ? 
                            state.Board[midLocationY, midLocationX] is GameBoardSquare.Player2King or GameBoardSquare.Player2Pawn :
                            state.Board[midLocationY, midLocationX] is GameBoardSquare.Player1King or GameBoardSquare.Player1Pawn
                    ))
                {
                    possibleJumpMoves.Add((jumpLocationX, jumpLocationY));
                }
            }
        }
        
        return possibleJumpMoves;
    }
    
    private static IEnumerable<(int x, int y, GameBoardSquare square)> CurrentTurnPieces(CheckersGameState state)
    {
        for(var i = 0; i < 8; i++)
        {
            for(var j = 0; j < 8; j++)
            {
                if (state.Player1Turn)
                {
                    if (state.Board[i, j] == GameBoardSquare.Player1King || state.Board[i, j] == GameBoardSquare.Player1Pawn)
                    {
                        yield return (j, i, state.Board[i, j]);
                    }
                }
                else
                {
                    if (state.Board[i, j] == GameBoardSquare.Player2King || state.Board[i, j] == GameBoardSquare.Player2Pawn)
                    {
                        yield return (j, i, state.Board[i, j]);
                    }
                }
            }
        }
    }

    private static (int x, int y)[] DetermineMovements(GameBoardSquare square)
    {
        return square switch
        {
            GameBoardSquare.Player1King => KingMovementChecks,
            GameBoardSquare.Player2King => KingMovementChecks,
            GameBoardSquare.Player1Pawn => Player1PawnMovementChecks,
            GameBoardSquare.Player2Pawn => Player2PawnMovementChecks,
            GameBoardSquare.Empty => [],
            _ => throw new ArgumentOutOfRangeException(nameof(square), square, null)
        };
    }
    private static readonly (int x, int y)[] KingMovementChecks = [(1, 1), (1, -1), (-1, 1), (-1, -1)];
    private static readonly (int x, int y)[] Player1PawnMovementChecks = [(1, -1), (-1, -1)];
    private static readonly (int x, int y)[] Player2PawnMovementChecks = [(-1, 1), (1, 1)];
    
    private static (int x, int y)[] DetermineJumpMovements(GameBoardSquare square)
    {
        return square switch
        {
            GameBoardSquare.Player1King => KingJumpDirectionChecks,
            GameBoardSquare.Player2King => KingJumpDirectionChecks,
            GameBoardSquare.Player1Pawn => Player1PawnJumpDirectionChecks,
            GameBoardSquare.Player2Pawn => Player2PawnJumpDirectionChecks,
            GameBoardSquare.Empty => [],
            _ => throw new ArgumentOutOfRangeException(nameof(square), square, null)
        };
    }
    private static readonly (int x, int y)[] KingJumpDirectionChecks = [(2, 2), (2, -2), (-2, 2), (-2, -2)];
    private static readonly (int x, int y)[] Player1PawnJumpDirectionChecks = [(2, -2), (-2, -2)];
    private static readonly (int x, int y)[] Player2PawnJumpDirectionChecks = [(-2, 2), (2, 2)];
    
    private static bool IsInside(int x, int y)
    {
        return x is >= 0 and < 8 && y is >= 0 and < 8;
    }
}