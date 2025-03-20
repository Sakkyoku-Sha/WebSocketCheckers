using System.Text.Json;

namespace WebApplication1
{
    public enum GameBoardSquare
    {
        Empty = 0,
        Player1Pawn = 1,
        Player1King = 2,
        Player2Pawn = 3,
        Player2King = 4
    }

    public class CheckersGameState
    {
        public GameBoardSquare[][] Board { get; set; }
        public bool Player1Turn { get; set; }
        
        public CheckersGameState()
        {
            Board = InitBoard();
            Player1Turn = true;
        }
        
        private GameBoardSquare[][] InitBoard()
        {
            // Create an 8x8 jagged array.
            var board = new GameBoardSquare[8][];
            for (int row = 0; row < 8; row++)
            {
                board[row] = new GameBoardSquare[8];
            }
            
            // Initialize Player2 pieces in the top rows.
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 != 0)
                    {
                        board[row][col] = GameBoardSquare.Player2Pawn;
                    }
                }
            }

            // Initialize Player1 pieces in the bottom rows.
            for (int row = 5; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 != 0)
                    {
                        board[row][col] = GameBoardSquare.Player1Pawn;
                    }
                }
            }

            return board;
        }
        
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // For pretty-printing.
            };
            // With no custom converter, enums are serialized as ints by default.
            return JsonSerializer.Serialize(this, options);
        }
    }
}