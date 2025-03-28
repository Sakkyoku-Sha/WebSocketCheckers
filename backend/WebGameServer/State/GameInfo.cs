namespace WebGameServer.State;

public class GameInfo(Guid gameId, PlayerInfo player1, PlayerInfo player2, GameState gameState)
{
    public Guid GameId { get; set; } = gameId;
    public PlayerInfo Player1 { get; set; } = player1;
    public PlayerInfo Player2 { get; set; } = player2;
    public GameState GameState { get; set; } = gameState;
}