namespace WebGameServer.State;

public struct PlayerInfo(Guid playerId, string playerName)
{
    public PlayerInfo() : this(Guid.Empty, string.Empty)
    {
    }
    
    public Guid PlayerId = playerId; 
    public string PlayerName = playerName;
    
    public bool IsDefined => PlayerId != Guid.Empty;
}