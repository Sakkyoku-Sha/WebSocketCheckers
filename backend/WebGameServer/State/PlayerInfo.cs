namespace WebGameServer.State;

public class PlayerInfo
{
    public Guid UserId { get; set; } //used to map to WebSocket 
    public string PlayerName { get; set; }

    public PlayerInfo(Guid userId, string playerName)
    {   
        UserId = userId;
        PlayerName = playerName;
    }
}