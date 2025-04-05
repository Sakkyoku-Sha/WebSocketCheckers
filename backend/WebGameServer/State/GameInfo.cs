namespace WebGameServer.State;


public enum GameStatus : byte
{
    WaitingForPlayers,
    InProgress,
    Finished
}

public class GameInfo(Guid gameId, PlayerInfo? player1, PlayerInfo? player2, GameState gameState, string gameName, GameStatus status)
{
    public Guid GameId { get; set; } = gameId;
    public GameStatus Status { get; set; } = status;
    public string GameName { get; set; } = gameName;
    
    public PlayerInfo? Player1 { get; set; } = player1;
    public PlayerInfo? Player2 { get; set; } = player2;
    public GameState GameState { get; set; } = gameState;
}