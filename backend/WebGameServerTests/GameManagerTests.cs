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
        public void TryMove_FromEmptySquare_ReturnsFalseAndStateUnchanged()
        {
            var result = _gameManager.TryMove((3, 3), (4, 4), out var state);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(state.Player1Turn, Is.EqualTo(_initialState.Player1Turn));
                Assert.That(state.Board[3, 3], Is.EqualTo(_initialState.Board[3, 3]));
                Assert.That(state.Board[4, 4], Is.EqualTo(_initialState.Board[4, 4]));
            });
        }

        [Test]
        public void TryMove_InvalidNonDiagonalMove_ReturnsFalseAndStateUnchanged()
        {
            var result = _gameManager.TryMove((5, 0), (5, 1), out var state);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(state.Player1Turn, Is.EqualTo(_initialState.Player1Turn));
                Assert.That(state.Board[5, 0], Is.EqualTo(_initialState.Board[5, 0]));
            });
        }

        [Test]
        public void TryMove_ValidMove_UpdatesBoardAndSwitchesTurn()
        {
            var result = _gameManager.TryMove((5, 0), (4, 1), out var state);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(state.Board[5, 0], Is.EqualTo(GameBoardSquare.Empty));
                Assert.That(state.Board[4, 1], Is.EqualTo(GameBoardSquare.Player1Pawn));
                Assert.That(state.Player1Turn, Is.EqualTo(!_initialState.Player1Turn));
            });
        }

        [Test]
        public void TryMove_MoveOutOfBounds_ReturnsFalseAndStateUnchanged()
        {
            var result = _gameManager.TryMove((5, 0), (8, 1), out var state);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(state.Player1Turn, Is.EqualTo(_initialState.Player1Turn));
                Assert.That(state.Board[5, 0], Is.EqualTo(_initialState.Board[5, 0]));
            });
        }

        [Test]
        public void TryMove_MovingOpponentsPiece_ReturnsFalseAndStateUnchanged()
        {
            var result = _gameManager.TryMove((0, 1), (1, 0), out var state);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(state.Player1Turn, Is.EqualTo(_initialState.Player1Turn));
                Assert.That(state.Board[0, 1], Is.EqualTo(_initialState.Board[0, 1]));
            });
        }

        [Test]
        public void TryMove_PawnMovingBackward_ReturnsFalse()
        {
            var result = _gameManager.TryMove((5, 0), (6, 1), out var state);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(state.Player1Turn, Is.EqualTo(_initialState.Player1Turn));
                Assert.That(state.Board[5, 0], Is.EqualTo(_initialState.Board[5, 0]));
            });
        }
    }
}