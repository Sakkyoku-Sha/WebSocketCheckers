using NUnit.Framework;
using WebApplication1;

namespace WebApplication1Tests
{
    [TestFixture]
    public class GameManager_Tests
    {
        private IGameManager _gameManager;
        // For baseline comparisons we create a fresh game state instance.
        private CheckersGameState _initialState;

        [SetUp]
        public void Setup()
        {
            _gameManager = new GameManager();
            _initialState = new CheckersGameState();
        }

        [Test]
        public void TryMove_FromEmptySquare_ReturnsFalseAndStateUnchanged()
        {
            bool result = _gameManager.TryMove((3, 3), (4, 4), out CheckersGameState state);
            Assert.IsFalse(result);
            Assert.AreEqual(_initialState.Player1Turn, state.Player1Turn);
            Assert.AreEqual(_initialState.Board[3][3], state.Board[3][3]);
            Assert.AreEqual(_initialState.Board[4][4], state.Board[4][4]);
        }

        [Test]
        public void TryMove_InvalidNonDiagonalMove_ReturnsFalseAndStateUnchanged()
        {
            bool result = _gameManager.TryMove((5, 0), (5, 1), out CheckersGameState state);
            Assert.IsFalse(result);
            Assert.AreEqual(_initialState.Player1Turn, state.Player1Turn);
            Assert.AreEqual(_initialState.Board[5][0], state.Board[5][0]);
        }

        [Test]
        public void TryMove_ValidMove_UpdatesBoardAndSwitchesTurn()
        {
            bool result = _gameManager.TryMove((5, 0), (4, 1), out CheckersGameState state);
            Assert.IsTrue(result);
            Assert.AreEqual(GameBoardSquare.Empty, state.Board[5][0]);
            Assert.AreEqual(GameBoardSquare.Player1Pawn, state.Board[4][1]);
            // For a valid, non-capturing move the turn should switch.
            Assert.AreEqual(!_initialState.Player1Turn, state.Player1Turn);
        }

        [Test]
        public void TryMove_MoveOutOfBounds_ReturnsFalseAndStateUnchanged()
        {
            bool result = _gameManager.TryMove((5, 0), (8, 1), out CheckersGameState state);
            Assert.IsFalse(result);
            Assert.AreEqual(_initialState.Player1Turn, state.Player1Turn);
            Assert.AreEqual(_initialState.Board[5][0], state.Board[5][0]);
        }

        [Test]
        public void TryMove_MovingOpponentsPiece_ReturnsFalseAndStateUnchanged()
        {
            bool result = _gameManager.TryMove((0, 1), (1, 0), out CheckersGameState state);
            Assert.IsFalse(result);
            Assert.AreEqual(_initialState.Player1Turn, state.Player1Turn);
            Assert.AreEqual(_initialState.Board[0][1], state.Board[0][1]);
        }

        [Test]
        public void TryMove_PawnMovingBackward_ReturnsFalse()
        {
            bool result = _gameManager.TryMove((5, 0), (6, 1), out CheckersGameState state);
            Assert.IsFalse(result);
            Assert.AreEqual(_initialState.Player1Turn, state.Player1Turn);
            Assert.AreEqual(_initialState.Board[5][0], state.Board[5][0]);
        }
    }
}
