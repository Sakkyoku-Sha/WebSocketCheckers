namespace WebGameServer.State;

public struct PlayerInfo(Guid playerId, string playerName)
{
    public Guid PlayerId = playerId; 
    public string PlayerName = playerName;
}