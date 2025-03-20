using WebGameServer.GameLogic;

namespace WebGameServerTests;

public class CheckersGameStateTests
{
    private CheckersGameState _checkersGameState;
    
    [SetUp]
    public void Setup()
    {
        _checkersGameState = new CheckersGameState();
    }

    [Test]
    public void InitialBoardState()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_checkersGameState.Board[4, 0], Is.EqualTo(GameBoardSquare.Empty));
            Assert.That(_checkersGameState.Board[0, 1], Is.EqualTo(GameBoardSquare.Player2Pawn));
            Assert.That(_checkersGameState.Board[7, 0], Is.EqualTo(GameBoardSquare.Player1Pawn));
        });
    }
    
    [Test]
    public void InitialPlayerTurn()
    {
        Assert.That(_checkersGameState.Player1Turn, Is.True);
    }

    [Test]
    public void ByteSerializationTest()
    {
        _checkersGameState.Player1Turn = false;
        
        _checkersGameState.Board[2, 4] = GameBoardSquare.Player1Pawn;
        _checkersGameState.Board[7, 7] = GameBoardSquare.Player1King;
        _checkersGameState.Board[0, 0] = GameBoardSquare.Player2King;
        _checkersGameState.Board[3,4] = GameBoardSquare.Player2Pawn;
        _checkersGameState.Board[5, 5] = GameBoardSquare.Empty;

        var bytes = _checkersGameState.ToByteArray();
        var newBoard = CheckersGameState.FromByteArray(bytes);
        
        Assert.That(_checkersGameState.Player1Turn, Is.EqualTo(newBoard.Player1Turn));
        CollectionAssert.AreEqual(_checkersGameState.Board, newBoard.Board);
        
        //Re-serialization
        var newBoardBytes = newBoard.ToByteArray();
        CollectionAssert.AreEqual(bytes, newBoardBytes);
    }
}