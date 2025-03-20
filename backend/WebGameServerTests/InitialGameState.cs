using WebApplication1;

namespace WebApplication1Tests;

public class Tests
{
    private CheckersGameState _checkersGameState;
    
    [SetUp]
    public void Setup()
    {
        _checkersGameState = new CheckersGameState();
    }

    [Test]
    public void Test1()
    {
        Assert.That(_checkersGameState.Board[4][0], Is.EqualTo(GameBoardSquare.Empty));
        Assert.That(_checkersGameState.Board[0][1], Is.EqualTo(GameBoardSquare.Player2Pawn));
        Assert.That(_checkersGameState.Board[7][0], Is.EqualTo(GameBoardSquare.Player1Pawn));
    }
}