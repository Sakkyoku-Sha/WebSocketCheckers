namespace WebGameServer.State;

public class PlayerInfo
{
    public Guid PlayerId { get; set; } //used to map to WebSocket 
    public string PlayerName { get; set; }

    public PlayerInfo(Guid playerId, string playerName)
    {   
        PlayerId = playerId;
        PlayerName = playerName;
    }
}