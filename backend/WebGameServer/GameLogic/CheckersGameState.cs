namespace WebGameServer.GameLogic
{
    public enum GameBoardSquare : byte
    {
        Empty = 0,
        Player1Pawn = 1,
        Player1King = 2,
        Player2Pawn = 3,
        Player2King = 4
    }

    public class CheckersGameState : IByteSerializable<CheckersGameState>
    {
        // Using a 2D array instead of a jagged array.
        public GameBoardSquare[,] Board { get; } 
        public bool Player1Turn;
        
        public CheckersGameState()
        {
            var newBoard = new GameBoardSquare[8, 8];
            Buffer.BlockCopy(InitialGameBoard, 0, newBoard, 0,BoardSizeBytes );
            Board = newBoard;
            Player1Turn = true;
        }
        
        public CheckersGameState(CheckersGameState gameState)
        {
            Board = (GameBoardSquare[,])gameState.Board.Clone(); 
            Player1Turn = gameState.Player1Turn;
        }
        
        private CheckersGameState(GameBoardSquare[,] board, bool player1Turn)
        {
            Board = board;
            Player1Turn = player1Turn;
        } 
        
        // Updated to use a 2D array for InitialGameBoard.
        private static readonly GameBoardSquare[,] InitialGameBoard =
        {
            {
                GameBoardSquare.Empty, GameBoardSquare.Player2Pawn, GameBoardSquare.Empty, GameBoardSquare.Player2Pawn,
                GameBoardSquare.Empty, GameBoardSquare.Player2Pawn, GameBoardSquare.Empty, GameBoardSquare.Player2Pawn
            },
            {
                GameBoardSquare.Player2Pawn, GameBoardSquare.Empty, GameBoardSquare.Player2Pawn, GameBoardSquare.Empty,
                GameBoardSquare.Player2Pawn, GameBoardSquare.Empty, GameBoardSquare.Player2Pawn, GameBoardSquare.Empty
            },
            {
                GameBoardSquare.Empty, GameBoardSquare.Player2Pawn, GameBoardSquare.Empty, GameBoardSquare.Player2Pawn,
                GameBoardSquare.Empty, GameBoardSquare.Player2Pawn, GameBoardSquare.Empty, GameBoardSquare.Player2Pawn
            },
            {
                GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty,
                GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty
            },
            {
                GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty,
                GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty, GameBoardSquare.Empty
            },
            {
                GameBoardSquare.Player1Pawn, GameBoardSquare.Empty, GameBoardSquare.Player1Pawn, GameBoardSquare.Empty,
                GameBoardSquare.Player1Pawn, GameBoardSquare.Empty, GameBoardSquare.Player1Pawn, GameBoardSquare.Empty
            },
            {
                GameBoardSquare.Empty, GameBoardSquare.Player1Pawn, GameBoardSquare.Empty, GameBoardSquare.Player1Pawn,
                GameBoardSquare.Empty, GameBoardSquare.Player1Pawn, GameBoardSquare.Empty, GameBoardSquare.Player1Pawn
            },
            {
                GameBoardSquare.Player1Pawn, GameBoardSquare.Empty, GameBoardSquare.Player1Pawn, GameBoardSquare.Empty,
                GameBoardSquare.Player1Pawn, GameBoardSquare.Empty, GameBoardSquare.Player1Pawn, GameBoardSquare.Empty
            }
        };
        
        private const int BoardSizeBytes = 64;
        private const int PlayerTurnFlag = 1;
        private const int TotalBytes = BoardSizeBytes + PlayerTurnFlag;
        
        public byte[] ToByteArray()
        {
            var data = new byte[TotalBytes];
            Buffer.BlockCopy(Board, 0, data, 0, BoardSizeBytes);  // Copy all except last byte
            data[BoardSizeBytes] = Player1Turn ? (byte)1 : (byte)0;

            return data; 
        }
        public static CheckersGameState FromBytes(byte[] bytes)
        {
            var board = new GameBoardSquare[8, 8];
            Buffer.BlockCopy(bytes, 0, board, 0, BoardSizeBytes); // Copy all except last byte
            return new CheckersGameState(board, bytes[BoardSizeBytes] == 1);
        }
    }
}
