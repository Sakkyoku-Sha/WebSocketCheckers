using System.Runtime.InteropServices;

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
        public readonly GameBoardSquare[,] Board;
        public GameResult Result;
        public bool Player1Turn;
        
        private CheckersMove[] _moveHistory;
        private int _moveHistoryIndex = 0;
        private int _moveHistoryCapacity = 32;
        
        public CheckersGameState()
        {
            var newBoard = new GameBoardSquare[8, 8];
            Buffer.BlockCopy(InitialGameBoard, 0, newBoard, 0,BoardSizeInBytes );
            
            Board = newBoard;
            Player1Turn = true;
            Result = GameResult.InProgress;
            
            _moveHistory = new CheckersMove[_moveHistoryCapacity];
        }
        
        public CheckersGameState(CheckersGameState gameState)
        {
            Board = (GameBoardSquare[,])gameState.Board.Clone(); 
            _moveHistory = (CheckersMove[])gameState._moveHistory.Clone();
            Player1Turn = gameState.Player1Turn;
            Result = gameState.Result;
        }
        private CheckersGameState(GameBoardSquare[,] board,bool player1Turn, GameResult result, CheckersMove[] moveHistory)
        {
            Board = board;
            Player1Turn = player1Turn;
            _moveHistory = moveHistory;
            Result = result;
        }

        public void AddToMoveHistory(CheckersMove move)
        {
            if(_moveHistoryIndex == _moveHistoryCapacity)
            {
                _moveHistoryCapacity *= 2;
                var newMoveHistory = new CheckersMove[_moveHistoryCapacity];
                _moveHistory.CopyTo(newMoveHistory, 0);
                _moveHistory = newMoveHistory;
            }
            _moveHistory[_moveHistoryIndex] = move;
            _moveHistoryIndex++;
        }
        public CheckersMove[] GetMoveHistory()
        {
            return (CheckersMove[])_moveHistory.Clone(); 
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
        
        private const int BoardSizeInBytes  = sizeof(GameBoardSquare) * 8 * 8;
        private const int PlayerTurnFlagSize = sizeof(GameBoardSquare);
        private const int ResultFlag = sizeof(GameResult); 
        
        public byte[] ToByteArray()
        {
            var historySize = _moveHistoryIndex * Marshal.SizeOf<CheckersMove>();
            var totalBytes = BoardSizeInBytes + PlayerTurnFlagSize + ResultFlag + historySize;
    
            var data = new byte[totalBytes];
    
            // Copy Board Data First 
            Buffer.BlockCopy(Board, 0, data, 0, BoardSizeInBytes);
    
            // Copy Player Turn Flag
            data[BoardSizeInBytes] = Player1Turn ? (byte)1 : (byte)0;

            // Copy Result Flag
            data[BoardSizeInBytes + PlayerTurnFlagSize] = (byte)Result;
    
            // Copy Move History using MemoryMarshal.AsBytes
            var moveHistoryBytes = MemoryMarshal.AsBytes(_moveHistory.AsSpan(0, _moveHistoryIndex));
            moveHistoryBytes.CopyTo(data.AsSpan(BoardSizeInBytes + PlayerTurnFlagSize + ResultFlag));
    
            return data; 
        }
        
        public static CheckersGameState FromBytes(byte[] bytes)
        {
            //Extract Board Data
            var board = new GameBoardSquare[8, 8];
            Buffer.BlockCopy(bytes, 0, board, 0, BoardSizeInBytes); 
            
            //Extract Player Turn Flag
            var flag = bytes[BoardSizeInBytes] == 1;
            
            //Extract Result Flag
            var result = (GameResult)bytes[BoardSizeInBytes + PlayerTurnFlagSize];
            
            //Extract Move History
            var remainingBytes = bytes.Length - (BoardSizeInBytes + PlayerTurnFlagSize + ResultFlag);
            
            //View data as span to copy to moveHistory
            var span = new Span<byte>(bytes, BoardSizeInBytes + PlayerTurnFlagSize + ResultFlag, remainingBytes);
            
            // Use MemoryMarshal to cast the Span<byte> to a Span<CheckersMove> actually dangerous code 
            Span<CheckersMove> movesSpan = MemoryMarshal.Cast<byte, CheckersMove>(span);
            var moveHistory = movesSpan.ToArray(); 
            
            return new CheckersGameState(board, flag, result, moveHistory);
        }
    }
}
