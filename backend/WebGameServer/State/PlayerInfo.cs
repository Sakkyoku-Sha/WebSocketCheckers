namespace WebGameServer.State;

public struct PlayerInfo(Guid playerId, string playerName)
{
    public PlayerInfo() : this(Guid.Empty, string.Empty)
    {
    }
    
    public PlayerInfo(Guid playerId) : this(playerId, DefaultName(playerId))
    {
    }
    
    public Guid PlayerId = playerId; 
    public string PlayerName = playerName;
    
    public bool IsDefined => PlayerId != Guid.Empty;
    public static PlayerInfo Empty = new PlayerInfo();

    private static string DefaultName(Guid playerId)
    {
        return "Player" + playerId.ToString()[..5];
    }
}