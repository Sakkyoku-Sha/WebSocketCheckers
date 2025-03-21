namespace WebGameServer.GameLogic;

public interface IGameManager
{
    bool TryMove((int x, int y) from, (int x, int y) to, out CheckersGameState state);
    bool TryMove(CheckersGameState state, (int x, int y) from, (int x, int y) to, out CheckersGameState newState); 
}

public class GameManager : IGameManager
{
    public CheckersGameState _gameState;

    public GameManager()
    {
        _gameState = new CheckersGameState();
    }
    
    public bool TryMove(CheckersGameState state, (int x, int y) from, (int x, int y) to, out CheckersGameState newState)
    {
        newState = _gameState;
        
        //Valid From/To 
        if (!IsInside(from.x, from.y) || !IsInside(to.x, to.y))
        {
            return false; 
        }
        
        var distY = to.y - from.y;
        var distX = to.x - from.x;
        var absDistX = Math.Abs(distX);
        var absDistY = Math.Abs(distY);
        
        var fromValue = state.Board[from.y, from.x];
        var toValue = state.Board[to.y, to.x];
        
        if (
            absDistX < 0 || 
            absDistX > 2 || 
            absDistY < 0 || 
            absDistY > 2 || 
            absDistX != absDistY || 
            toValue != GameBoardSquare.Empty) //Can Only Move to Empty Squares 
        {
            return false; 
        }

        GameBoardSquare midValue = GameBoardSquare.Empty; 
        if (absDistX == 2) 
        {
            midValue = state.Board[from.y + (distY / 2), from.x + (distX / 2)]; //works since the only jump value is must be 2
        }
        
        //Player1 Turn Logic 
        if (state.Player1Turn)
        {
            if (
                (fromValue != GameBoardSquare.Player1King && fromValue != GameBoardSquare.Player1Pawn) || //Must be moving a player1 Piece 
                (fromValue == GameBoardSquare.Player1Pawn && distY > 0) ||  //Can only Move pawns forwards (moving up) 
                (absDistX == 2 && (midValue != GameBoardSquare.Player2King && midValue != GameBoardSquare.Player2Pawn))
                )
            {
                return false; 
            }
        }
        else
        {
            if (
                (fromValue != GameBoardSquare.Player2King &&
                 fromValue != GameBoardSquare.Player2Pawn) || //Must be moving a player1 Piece 
                (fromValue == GameBoardSquare.Player2Pawn && distY < 0) || //Can only Move pawns forwards (moving down)
                (absDistX == 2 && (midValue != GameBoardSquare.Player1King && midValue != GameBoardSquare.Player1Pawn))
            )
            {
                return false;
            }
        }
        
        //The move is assumed to be valid at this point so we just need to update the board. 
        newState = new CheckersGameState(state);
        
        //If a piece has moved to the edge of the board and the move is valid it should be promoted to a King 

        //Move the Piece 
        newState.Board[to.y, to.x] = fromValue;
        newState.Board[from.y, from.x] = GameBoardSquare.Empty;
        
        //Promote if Necessary 
        if (to.y == 0 || to.y == 7)
        {
            if ((byte)newState.Board[to.y, to.x] % 2 == 1) //Only Promote Pawns 1 / 3 
            {
                newState.Board[to.y, to.x]++;  //1->2 // 3->4
            } 
        }
        
        //Only Change the Turn if it's not a jump
        if (absDistX == 1)
        {
            newState.Player1Turn = !newState.Player1Turn;
        }
        else if (absDistX == 2) //Remove the Piece we hopped 
        {
            newState.Board[from.y + (distY / 2), from.x + (distX / 2)] = GameBoardSquare.Empty;
        }
        
        return true; 
    } 
    
    public bool TryMove((int x, int y) from, (int x, int y) to, out CheckersGameState state)
    {
        state = _gameState;
        if (!IsInside(from.x, from.y) || !IsInside(to.x, to.y))
            return false;

        var piece = _gameState.Board[from.x, from.y];
        if (piece == GameBoardSquare.Empty)
            return false;

        bool isPlayer1Turn = _gameState.Player1Turn;
        if (isPlayer1Turn)
        {
            if (piece != GameBoardSquare.Player1Pawn && piece != GameBoardSquare.Player1King)
                return false;
        }
        else
        {
            if (piece != GameBoardSquare.Player2Pawn && piece != GameBoardSquare.Player2King)
                return false;
        }

        if (_gameState.Board[to.x, to.y] != GameBoardSquare.Empty)
            return false;

        int rowDiff = to.x - from.x;
        int colDiff = to.y - from.y;
        bool validMove = false;
        bool isCapture = false;
        int jumpedX = 0, jumpedY = 0;

        if (piece == GameBoardSquare.Player1Pawn)
        {
            if (rowDiff == -1 && Math.Abs(colDiff) == 1)
                validMove = true;
            else if (rowDiff == -2 && Math.Abs(colDiff) == 2)
            {
                jumpedX = from.x - 1;
                jumpedY = from.y + colDiff / 2;
                if (IsInside(jumpedX, jumpedY) &&
                    (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2Pawn ||
                     _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2King))
                {
                    validMove = true;
                    isCapture = true;
                }
            }
        }
        else if (piece == GameBoardSquare.Player2Pawn)
        {
            if (rowDiff == 1 && Math.Abs(colDiff) == 1)
                validMove = true;
            else if (rowDiff == 2 && Math.Abs(colDiff) == 2)
            {
                jumpedX = from.x + 1;
                jumpedY = from.y + colDiff / 2;
                if (IsInside(jumpedX, jumpedY) &&
                    (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1Pawn ||
                     _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1King))
                {
                    validMove = true;
                    isCapture = true;
                }
            }
        }
        else if (piece == GameBoardSquare.Player1King || piece == GameBoardSquare.Player2King)
        {
            if (Math.Abs(rowDiff) == 1 && Math.Abs(colDiff) == 1)
                validMove = true;
            else if (Math.Abs(rowDiff) == 2 && Math.Abs(colDiff) == 2)
            {
                jumpedX = from.x + rowDiff / 2;
                jumpedY = from.y + colDiff / 2;
                if (IsInside(jumpedX, jumpedY))
                {
                    if (isPlayer1Turn &&
                       (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2Pawn ||
                        _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2King))
                    {
                        validMove = true;
                        isCapture = true;
                    }
                    else if (!isPlayer1Turn &&
                       (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1Pawn ||
                        _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1King))
                    {
                        validMove = true;
                        isCapture = true;
                    }
                }
            }
        }

        if (!validMove)
            return false;

        if (isCapture)
            _gameState.Board[jumpedX, jumpedY] = GameBoardSquare.Empty;
        _gameState.Board[to.x, to.y] = piece;
        _gameState.Board[from.x, from.y] = GameBoardSquare.Empty;

        bool promoted = false;
        if (piece == GameBoardSquare.Player1Pawn && to.x == 0)
        {
            _gameState.Board[to.x, to.y] = GameBoardSquare.Player1King;
            promoted = true;
        }
        else if (piece == GameBoardSquare.Player2Pawn && to.x == 7)
        {
            _gameState.Board[to.x, to.y] = GameBoardSquare.Player2King;
            promoted = true;
        }

        if (isCapture && !promoted && HasCaptureMove(to.x, to.y))
        {
            // Same player's turn for additional captures.
        }
        else
        {
            _gameState.Player1Turn = !_gameState.Player1Turn;
        }

        return true;
    }

    private bool HasCaptureMove(int x, int y)
    {
        var piece = _gameState.Board[x, y];
        if (piece == GameBoardSquare.Empty)
            return false;

        if (piece == GameBoardSquare.Player1Pawn)
        {
            foreach (var dcol in new int[] { -2, 2 })
            {
                int nx = x - 2;
                int ny = y + dcol;
                int jumpedX = x - 1;
                int jumpedY = y + dcol / 2;
                if (IsInside(nx, ny) && _gameState.Board[nx, ny] == GameBoardSquare.Empty &&
                    IsInside(jumpedX, jumpedY) &&
                    (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2Pawn ||
                     _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2King))
                {
                    return true;
                }
            }
        }
        else if (piece == GameBoardSquare.Player2Pawn)
        {
            foreach (var dcol in new int[] { -2, 2 })
            {
                int nx = x + 2;
                int ny = y + dcol;
                int jumpedX = x + 1;
                int jumpedY = y + dcol / 2;
                if (IsInside(nx, ny) && _gameState.Board[nx, ny] == GameBoardSquare.Empty &&
                    IsInside(jumpedX, jumpedY) &&
                    (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1Pawn ||
                     _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1King))
                {
                    return true;
                }
            }
        }
        else if (piece == GameBoardSquare.Player1King || piece == GameBoardSquare.Player2King)
        {
            foreach (var dr in new int[] { -2, 2 })
            {
                foreach (var dc in new int[] { -2, 2 })
                {
                    int nx = x + dr;
                    int ny = y + dc;
                    int jumpedX = x + dr / 2;
                    int jumpedY = y + dc / 2;
                    if (IsInside(nx, ny) && _gameState.Board[nx, ny] == GameBoardSquare.Empty &&
                        IsInside(jumpedX, jumpedY))
                    {
                        bool opponent = false;
                        if (piece == GameBoardSquare.Player1King)
                        {
                            opponent = (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2Pawn ||
                                        _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player2King);
                        }
                        else
                        {
                            opponent = (_gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1Pawn ||
                                        _gameState.Board[jumpedX, jumpedY] == GameBoardSquare.Player1King);
                        }
                        if (opponent)
                            return true;
                    }
                }
            }
        }
        return false;
    }

    private bool IsInside(int x, int y)
    {
        return x is >= 0 and < 8 && y is >= 0 and < 8;
    }
}