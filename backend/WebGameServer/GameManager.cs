namespace WebApplication1;

public interface IGameManager
{
    bool TryMove((int x, int y) from, (int x, int y) to, out CheckersGameState state);
}

public class GameManager : IGameManager
{
    public CheckersGameState _gameState;

    public GameManager()
    {
        _gameState = new CheckersGameState();
    }
    
    public bool TryMove((int x, int y) from, (int x, int y) to, out CheckersGameState state)
    {
        state = _gameState;
        if (!IsInside(from.x, from.y) || !IsInside(to.x, to.y))
            return false;

        var piece = _gameState.Board[from.x][from.y];
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

        if (_gameState.Board[to.x][to.y] != GameBoardSquare.Empty)
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
                    (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2Pawn ||
                     _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2King))
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
                    (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1Pawn ||
                     _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1King))
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
                       (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2Pawn ||
                        _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2King))
                    {
                        validMove = true;
                        isCapture = true;
                    }
                    else if (!isPlayer1Turn &&
                       (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1Pawn ||
                        _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1King))
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
            _gameState.Board[jumpedX][jumpedY] = GameBoardSquare.Empty;
        _gameState.Board[to.x][to.y] = piece;
        _gameState.Board[from.x][from.y] = GameBoardSquare.Empty;

        bool promoted = false;
        if (piece == GameBoardSquare.Player1Pawn && to.x == 0)
        {
            _gameState.Board[to.x][to.y] = GameBoardSquare.Player1King;
            promoted = true;
        }
        else if (piece == GameBoardSquare.Player2Pawn && to.x == 7)
        {
            _gameState.Board[to.x][to.y] = GameBoardSquare.Player2King;
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
        var piece = _gameState.Board[x][y];
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
                if (IsInside(nx, ny) && _gameState.Board[nx][ny] == GameBoardSquare.Empty &&
                    IsInside(jumpedX, jumpedY) &&
                    (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2Pawn ||
                     _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2King))
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
                if (IsInside(nx, ny) && _gameState.Board[nx][ny] == GameBoardSquare.Empty &&
                    IsInside(jumpedX, jumpedY) &&
                    (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1Pawn ||
                     _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1King))
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
                    if (IsInside(nx, ny) && _gameState.Board[nx][ny] == GameBoardSquare.Empty &&
                        IsInside(jumpedX, jumpedY))
                    {
                        bool opponent = false;
                        if (piece == GameBoardSquare.Player1King)
                        {
                            opponent = (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2Pawn ||
                                        _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player2King);
                        }
                        else
                        {
                            opponent = (_gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1Pawn ||
                                        _gameState.Board[jumpedX][jumpedY] == GameBoardSquare.Player1King);
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
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }
}
