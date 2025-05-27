namespace WebGameServer.State;

public readonly struct PlayerInfo(Guid playerId, string playerName, bool isPlayer1)
{
    public PlayerInfo() : this(Guid.Empty, string.Empty, false)
    {
    }
    
    public PlayerInfo(Guid playerId, bool isPlayer1) : this(playerId, DefaultName(playerId), isPlayer1)
    {
    }
    
    public readonly Guid PlayerId = playerId; 
    public readonly string PlayerName = playerName;
    public readonly bool IsPlayer1 = isPlayer1; 
    
    public bool IsDefined => PlayerId != Guid.Empty;
    
    private static readonly PlayerInfo EmptyPlayer = new();
    public static PlayerInfo Empty = EmptyPlayer;

    private static string DefaultName(Guid playerId)
    {
        return "Player" + playerId.ToString()[..5];
    }
}