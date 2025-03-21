using WebGameServer.GameLogic;

namespace WebGameServer.API;

public class Query
{
    public ActiveGameInfo GetActiveGameInfo() =>
        new ActiveGameInfo()
        {
            GameId = Guid.NewGuid(),
            GameName = "Checkers Game 1",
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            GameStateSnapShot = new CheckersGameState().ToByteArray()
        };
}