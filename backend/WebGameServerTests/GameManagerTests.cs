using WebGameServer.GameLogic;

namespace WebGameServerTests
{
    [TestFixture]
    public class GameManagerTests
    {
        private IGameManager _gameManager;
        private CheckersGameState _initialState;

        [SetUp]
        public void Setup()
        {
            _gameManager = new GameManager();
            _initialState = new CheckersGameState();
        }

        [Test]
        public void FirstMoveValidTest()
        {
            var gameState = new CheckersGameState();
            var validMoves = new[] { (1, 4), (3,4) };
            var from = (2, 5); 
            foreach (var move in validMoves)
            {
                var result = _gameManager.TryMove(gameState, from, move, out var nextState);
            
                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.True);
                    Assert.That(nextState.Player1Turn, Is.EqualTo(false));
                    Assert.That(nextState.Board[from.Item2, from.Item1], Is.EqualTo(GameBoardSquare.Empty));
                    Assert.That(nextState.Board[move.Item2, move.Item1], Is.EqualTo(GameBoardSquare.Player1Pawn));
                });
            }
        }
        
        [Test]
        public void SimpleInvalidMoveValidTest()
        {
            var gameState = new CheckersGameState(); 
            
            //Move Right 
            var invalidMovements = new[] { (1, 5), (0, 4), (0, 6), (9, 9), (-1, 4), (2, 3) };
            foreach (var move in invalidMovements)
            {
                var result = _gameManager.TryMove(gameState, (0, 5), move, out var nextState);
                
                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.False);
                    Assert.That(nextState.Player1Turn, Is.EqualTo(true));
                    CollectionAssert.AreEqual(gameState.Board, nextState.Board);
                });
            }
        }
        
        [Test]
        public void JumpMoveValidTest()
        {
            var gameState = new CheckersGameState();
    
            // Clear the board for a controlled test setup
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    gameState.Board[y, x] = GameBoardSquare.Empty;
                }
            }
    
            // Set up a Player 1 piece and a Player 2 piece to jump over
            gameState.Board[2, 3] = GameBoardSquare.Player2Pawn; // Player 1 piece
            gameState.Board[3, 4] = GameBoardSquare.Player1Pawn; // Player 2 piece to jump over
    
            var from = (4, 3); // (x, y) of Player 1's piece
            var to = (2, 1);   // Target jump location
    
            var result = _gameManager.TryMove(gameState, from, to, out var nextState);
    
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True); // Move should be valid
                Assert.That(nextState.Player1Turn, Is.EqualTo(true)); // Turn should NOT switch
                Assert.That(nextState.Board[from.Item2, from.Item1], Is.EqualTo(GameBoardSquare.Empty)); // Original spot should be empty
                Assert.That(nextState.Board[to.Item2, to.Item1], Is.EqualTo(GameBoardSquare.Player1Pawn)); // Piece should be at new location
                Assert.That(nextState.Board[2, 3], Is.EqualTo(GameBoardSquare.Empty)); // Opponent's piece should be captured
            });
        }
        
        [Test]
        public void KingPromotionTest()
        {
            var gameState = new CheckersGameState();
    
            // Clear the board for a controlled test setup
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    gameState.Board[y, x] = GameBoardSquare.Empty;
                }
            }
            
            gameState.Board[1, 1] = GameBoardSquare.Player1Pawn;
    
            var from = (1, 1); 
            var to = (0, 0);   
    
            var result = _gameManager.TryMove(gameState, from, to, out var nextState);
    
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(nextState.Player1Turn, Is.EqualTo(false)); 
                Assert.That(nextState.Board[from.Item2, from.Item1], Is.EqualTo(GameBoardSquare.Empty)); // Original spot should be empty
                Assert.That(nextState.Board[to.Item2, to.Item1], Is.EqualTo(GameBoardSquare.Player1King)); // Piece should be at new location
            });
            
            //King can move backwards
            nextState.Player1Turn = true;
            result = _gameManager.TryMove(nextState, to, from, out var newState2);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(newState2.Player1Turn, Is.EqualTo(false)); 
                Assert.That(newState2.Board[to.Item2, to.Item1], Is.EqualTo(GameBoardSquare.Empty)); // Original spot should be empty
                Assert.That(newState2.Board[from.Item2, from.Item1], Is.EqualTo(GameBoardSquare.Player1King)); // Piece should be at new location
            });
        }
    }
}